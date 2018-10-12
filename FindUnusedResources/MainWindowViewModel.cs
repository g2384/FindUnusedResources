using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using OperationCanceledException = System.OperationCanceledException;

namespace FindUnusedResources
{
    public class MainWindowViewModel : BindableBase
    {
        public const string SettingFile = "settings.json";

        public Settings Settings { get; set; }

        private CancellationTokenSource _cancellationTokenSource;

        public MainWindowViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingFile))
            {
                var setting = File.ReadAllText(SettingFile);
                Settings = JsonConvert.DeserializeObject<Settings>(setting);
            }
            else
            {
                Settings = new Settings();
                Settings.Init();
            }

            _sourceFilePath = Settings.SourceFilePath;
            _fileExtensions = string.Join("; ", Settings.FileExtensions);
            _excludeFolders = "\"" + string.Join("\"; \"", Settings.ExcludeFolders) + "\"";
        }

        private DelegateCommand _analyzeCommand;

        public DelegateCommand AnalyzeCommand =>
            _analyzeCommand ?? (_analyzeCommand = new DelegateCommand(() => RunAsyncTask(AnalyzeAsync), () => _isAnalyzeButtonEnabled && !string.IsNullOrWhiteSpace(SourceFilePath)));

        private DelegateCommand _cancelCommand;

        public DelegateCommand CancelCommand =>
            _cancelCommand ?? (_cancelCommand = new DelegateCommand(Cancel));

        private void Cancel()
        {
            _cancellationTokenSource.Cancel();
            Status = "Canceling";
        }

        private string _sourceFilePath;

        public string SourceFilePath
        {
            get => _sourceFilePath;
            set
            {
                if (SetProperty(ref _sourceFilePath, value))
                {
                    AnalyzeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _status;

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private double _progress;

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private string _fileExtensions;

        public string FileExtensions
        {
            get => _fileExtensions;
            set => SetProperty(ref _fileExtensions, value);
        }

        private string _excludeFolders;

        public string ExcludeFolders
        {
            get => _excludeFolders;
            set => SetProperty(ref _excludeFolders, value);
        }

        private bool _isAnalyzeButtonEnabled = true;

        private bool _isProgressVisible;

        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set => SetProperty(ref _isProgressVisible, value);
        }

        private ObservableCollection<Resource> _results;

        public ObservableCollection<Resource> Results
        {
            get => _results;
            set => SetProperty(ref _results, value);
        }

        private void ChangeAnalyzeCommandCanExecute(bool isEnabled)
        {
            var dispatcher = GetDispatcher();
            dispatcher?.Invoke(() =>
            {
                _isAnalyzeButtonEnabled = isEnabled;
                AnalyzeCommand.RaiseCanExecuteChanged();
            }, DispatcherPriority.Send);
        }

        private static Dispatcher GetDispatcher()
        {
            var app = Application.Current;
            return app?.Dispatcher;
        }

        private int _totalFilesCount;

        private async void RunAsyncTask(Action action)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            await Task.Run(action, _cancellationTokenSource.Token);
        }

        private void AnalyzeAsync()
        {
            var sp = new Stopwatch();
            var status = "";
            _processedFiles = 0;
            try
            {
                ChangeAnalyzeCommandCanExecute(false);
                IsProgressVisible = true;
                sp.Start();
                Analyze();
                sp.Stop();
                status = "Completed";
            }
            catch (OperationCanceledException)
            {
                Status = "Task is cancelled";
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions?.All(IsPureCalceledException) == true)
                {
                    status = "Task is cancelled";
                }
            }
            catch (Exception e)
            {
                ShowErrorMessageBox(e.Message);
                status = "Error occurred";
            }
            finally
            {
                var timeElapsed = $"Time elapsed: {sp.Elapsed.Hours}h {sp.Elapsed.Minutes}m {sp.Elapsed.Seconds}s {sp.Elapsed.Milliseconds}ms";
                Status = $"{status} ({timeElapsed}, Analysed {_processedFiles} files)";
                IsProgressVisible = false;
                ChangeAnalyzeCommandCanExecute(true);
            }
        }

        private static bool IsPureCalceledException(Exception i)
        {
            var isPure = true;
            if (i is AggregateException e)
            {
                isPure &= e.InnerExceptions.All(IsPureCalceledException);
                return isPure;
            }
            return i is OperationCanceledException;
        }

        private static void ShowErrorMessageBox(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK);
        }

        private bool PrepareEnvironment(string status, out string path)
        {
            Progress = 0;
            _totalFilesCount = 0;
            Status = status;
            path = SourceFilePath;
            if (!Directory.Exists(SourceFilePath))
            {
                ShowErrorMessageBox($"Wrong directory \"{SourceFilePath}\"");
                return false;
            }

            SaveSettings();

            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            return true;
        }

        private ConcurrentDictionary<string, Resource> _allResources;
        private int _processedFiles;

        private void Analyze()
        {
            if (!PrepareEnvironment("Analyzing...", out var path))
            {
                return;
            }
            _allResources = new ConcurrentDictionary<string, Resource>();
            var allFiles = GetAllFiles(path, Settings.FileExtensions);
            var resourceFiles = GetAllFiles(path, new[] { ".resx" });
            var namePattern = new Regex(@"<data name=""(\w+)""");
            Parallel.ForEach(resourceFiles, file =>
            {
                var fileName = file.Split("\\").Last();
                fileName = fileName.Split(".").First();
                var lines = File.ReadAllText(file).Split("</data>");
                Parallel.ForEach(lines, line =>
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var matches = namePattern.Match(line);
                    var name = matches.Success ? matches.Groups[1].Captures[0].Value : null;
                    if (name != null)
                    {
                        var key = fileName + "." + name;
                        _allResources[key] = new Resource(key);
                    }
                });
            });

            Results = new ObservableCollection<Resource>(_allResources.Values);
            _totalFilesCount = allFiles.Length;
            Parallel.ForEach(allFiles, file =>
            {
                var lines = File.ReadAllLines(file);
                Parallel.ForEach(lines, line =>
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    try
                    {
                        CheckLine(line);
                    }
                    catch (Exception e)
                    {
                        ShowErrorMessageBox(e.Message + Environment.NewLine + e.StackTrace);
                    }
                });

                _processedFiles++;
                Status = _processedFiles + "/" + _totalFilesCount;
                Progress = (double)_processedFiles * 100 / _totalFilesCount;
            });
        }

        private string[] GetAllFiles(string path, string[] fileExtensions)
        {
            return FileHelper.GetAllFiles(path, fileExtensions, Settings.ExcludeFolders);
        }

        private void CheckLine(string line)
        {
            Parallel.ForEach(Results, r =>
            {
                if (line.Contains(r.Name))
                {
                    r.Count++;
                }
            });
        }

        private void SaveSettings()
        {
            Settings.SourceFilePath = SourceFilePath;
            if (!string.IsNullOrWhiteSpace(ExcludeFolders))
            {
                var extensions = ExcludeFolders.Substring(1, ExcludeFolders.Length - 2);
                Settings.ExcludeFolders = new Regex(@""";\s+""").Split(extensions).Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
            }

            if (!string.IsNullOrWhiteSpace(FileExtensions))
            {
                Settings.FileExtensions = new Regex(@"[^\*\.\w]+").Split(FileExtensions).Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
            }

            File.WriteAllText(SettingFile, JsonConvert.SerializeObject(Settings, Formatting.Indented));
        }
    }
}
