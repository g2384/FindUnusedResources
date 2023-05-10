using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using OperationCanceledException = System.OperationCanceledException;

namespace FindUnusedResources.Desktop
{
    public class MainWindowViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
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
                Settings = JsonSerializer.Deserialize<Settings>(setting);
            }
            else
            {
                Settings = new Settings();
                Settings.Init();
            }

            _sourceFilePath = Settings.SourceFilePath;
            _fileExtensions = string.Join("; ", Settings.FileExtensions);
            _excludeFolders = ConvertArrayToString(Settings.ExcludeFolders);
            _excludeFiles = ConvertArrayToString(Settings.ExcludeFiles);
        }

        private string ConvertArrayToString(string[] array)
        {
            if (array?.Any() != true)
            {
                return "";
            }
            return "\"" + string.Join("\"; \"", array) + "\"";
        }

        private RelayCommand _analyzeCommand;

        public RelayCommand AnalyzeCommand =>
            _analyzeCommand ?? (_analyzeCommand = new RelayCommand(() => RunAsyncTask(AnalyzeAsync), () => _isAnalyzeButtonEnabled && !string.IsNullOrWhiteSpace(SourceFilePath)));

        private RelayCommand _cancelCommand;

        public RelayCommand CancelCommand =>
            _cancelCommand ?? (_cancelCommand = new RelayCommand(Cancel));

        private void Cancel()
        {
            _cancellationTokenSource.Cancel();
            Status = "Canceling";
        }

        private bool _showUnusedOnly;

        public bool ShowUnusedOnly
        {
            get => _showUnusedOnly;
            set
            {
                if (SetProperty(ref _showUnusedOnly, value))
                {
                    if (value)
                    {
                        Results = new ObservableCollection<Resource>(_allResults.Where(e => e.Count == 0));
                    }
                    else
                    {
                        Results = new ObservableCollection<Resource>(_allResults);
                    }
                }
            }
        }

        private string _sourceFilePath;

        public string SourceFilePath
        {
            get => _sourceFilePath;
            set
            {
                if (SetProperty(ref _sourceFilePath, value))
                {
                    AnalyzeCommand.NotifyCanExecuteChanged();
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

        private string _excludeFiles;

        public string ExcludeFiles
        {
            get => _excludeFiles;
            set => SetProperty(ref _excludeFiles, value);
        }

        private bool _isAnalyzeButtonEnabled = true;

        private bool _isProgressVisible;

        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set => SetProperty(ref _isProgressVisible, value);
        }

        private IList<Resource> _allResults;

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
                AnalyzeCommand.NotifyCanExecuteChanged();
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
                if (ae.IsTypeOf<OperationCanceledException>())
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
            var resourceFiles = GetAllResourceFiles(path);
            var namePattern = new Regex(@"<data name=""(\w+)""");
            var resourceDesignerFiles = resourceFiles.Select(e => e.Replace(".resx", ".Designer.cs")).ToArray();
            Parallel.ForEach(resourceFiles, file =>
            {
                GetResourceKeys(file, namePattern);
            });

            var filteredAllFiles = allFiles.Except(resourceDesignerFiles).Except(resourceDesignerFiles).ToArray();
            Results = new ObservableCollection<Resource>(_allResources.Values);
            _allResults = _allResources.Values.ToList();
            ShowUnusedOnly = false;
            CheckFiles(filteredAllFiles);
        }

        private string[] GetAllResourceFiles(string path)
        {
            var allFiles = GetAllFiles(path, new[] { ".resx" });
            var excludeFiles = Settings.ExcludeFiles.Select(e => "\\" + e).ToArray();
            return allFiles.Where(e => !excludeFiles.Any(e.EndsWith)).ToArray();
        }

        private void GetResourceKeys(string file, Regex namePattern)
        {
            var fileName = file.Split("\\").Last();
            fileName = fileName.Split(".").First();
            var text = File.ReadAllText(file);
            text = new Regex("<!--(.*?)-->", RegexOptions.Singleline).Replace(text, ""); // remove comments
            var lines = text.Split("</data>");
            var token = _cancellationTokenSource.Token;
            Parallel.ForEach(lines, line =>
            {
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }

                var matches = namePattern.Match(line);
                var name = matches.Success ? matches.Groups[1].Captures[0].Value : null;
                if (name != null)
                {
                    var key = fileName + "." + name;
                    _allResources[key] = new Resource(key);
                }
            });
        }

        private void CheckFiles(IReadOnlyCollection<string> allFiles)
        {
            _totalFilesCount = allFiles.Count;
            Parallel.ForEach(allFiles, file =>
            {
                CheckLines(file);
                _processedFiles++;
                Status = _processedFiles + "/" + _totalFilesCount;
                Progress = (double)_processedFiles * 100 / _totalFilesCount;
            });
        }

        private void CheckLines(string file)
        {
            var lines = File.ReadAllLines(file).Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
            var token = _cancellationTokenSource.Token;
            Parallel.ForEach(Results, r =>
            {
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }

                try
                {
                    var name = r.Name;
                    foreach (var line in lines)
                    {
                        if (line.Contains(name))
                        {
                            r.Count++;
                        }
                    }
                }
                catch (Exception e)
                {
                    ShowErrorMessageBox(e.Message + Environment.NewLine + e.StackTrace);
                }
            });
        }

        private string[] GetAllFiles(string path, string[] fileExtensions)
        {
            return FileHelper.GetAllFiles(path, fileExtensions, Settings.ExcludeFolders);
        }

        private void SaveSettings()
        {
            Settings.SourceFilePath = SourceFilePath;
            Settings.ExcludeFolders = ConvertToArray(ExcludeFolders);
            Settings.ExcludeFiles = ConvertToArray(ExcludeFiles);
            if (!string.IsNullOrWhiteSpace(FileExtensions))
            {
                Settings.FileExtensions = new Regex(@"[^\*\.\w]+").Split(FileExtensions).Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
            }

            File.WriteAllText(SettingFile, JsonSerializer.Serialize(Settings, new JsonSerializerOptions()
            {
                WriteIndented = true
            }));
        }

        private string[] ConvertToArray(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new string[0];
            }
            var extensions = text.Substring(1, text.Length - 2);
            return new Regex(@""";\s+""").Split(extensions).Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
        }
    }
}
