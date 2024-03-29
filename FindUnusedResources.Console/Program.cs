﻿namespace FindUnusedResources.Console
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    internal class Program
    {
        private const string DefaultSettingsFile = "settings.yaml";

        private static Settings Settings;

        static void Main(string[] args)
        {
            Settings = GetSettings(args);

            if (string.IsNullOrEmpty(Settings.SourceCodeFolderPath))
            {
                Console.WriteLine(nameof(Settings.SourceCodeFolderPath) + " cannot be empty");
                return;
            }

            var resourceFilePath = GetAllResourceFiles(Settings.SourceCodeFolderPath);
            var resourceDesignerFiles = resourceFilePath.Select(e => e.Replace(".resx", ".Designer.cs")).ToArray();
            var allFiles = GetAllFiles(Settings.SourceCodeFolderPath, Settings.FileExtensions);
            var codeFiles = allFiles.Except(resourceFilePath).Except(resourceDesignerFiles).ToArray();

            var unusedVariables = new List<Resource>();

            var resxResources = new List<ResxResource>();
            foreach (var file in resourceDesignerFiles)
            {
                var strings = ResourceConverter.GetResourceStrings(file);
                resxResources.AddRange(strings);
            }

            foreach (var filePath in codeFiles)
            {
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
                var model = CSharpCompilation.Create("temp.dll", syntaxTrees: new[] { tree }).GetSemanticModel(tree);

                var usages = model.GetAllResxReferences();
                if (usages.Any())
                {
                    var friendlyFilePath = filePath.Replace(Settings.SourceCodeFolderPath, "");
                    foreach (var resource in resxResources)
                    {
                        var count = usages.Count(e => e.Equals(resource));
                        if (count > 0)
                        {
                            resource.References.Add(new Reference
                            {
                                ClassName = "",
                                Count = count,
                                FileName = friendlyFilePath
                            });
                        }
                    }
                }
            }

            PrintResults(resxResources);
        }

        private static void PrintResults(List<ResxResource> resxResources)
        {
            var unusedResources = resxResources.Where(e => !e.References.Any()).ToArray();
            Console.WriteLine("Unused resources:");
            Console.WriteLine();
            if (unusedResources.Any())
            {
                foreach (var resource in unusedResources)
                {
                    Console.WriteLine("  " + resource.ToShortString());
                }
            }
            else
            {
                Console.WriteLine("  <None>");
            }

            if (unusedResources.Length < resxResources.Count)
            {
                Console.WriteLine();
                Console.WriteLine("Stats of other resources:");
                foreach (var resource in resxResources)
                {
                    if (resource.References.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine("  " + resource.ToShortString() + $" ({resource.References.Sum(e => e.Count)})");
                        foreach (var reference in resource.References)
                        {
                            Console.WriteLine("    " + reference.ToString());
                        }
                    }
                }
            }
        }

        private static Settings GetSettings(string[] args)
        {
            Settings settings;

            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            string settingsFile = args.Any() ? args[0] : DefaultSettingsFile;
            if (!File.Exists(settingsFile))
            {
                Console.WriteLine($"Failed to find file: {settingsFile}.");
                settingsFile = DefaultSettingsFile;
            }

            if (File.Exists(settingsFile))
            {
                var text = File.ReadAllText(settingsFile);
                settings = deserializer.Deserialize<Settings>(text);
                Console.WriteLine($"Read settings from: {settingsFile}.");
            }
            else
            {
                settings = new Settings();
                File.WriteAllText(DefaultSettingsFile, serializer.Serialize(settings));
                Console.WriteLine($"Created setting file: {settingsFile}.");
            }

            Console.WriteLine();
            return settings;
        }

        private static string[] GetAllFiles(string path, string[] fileExtensions)
        {
            return FileHelper.GetAllFiles(path, fileExtensions, Settings.ExcludeFolders);
        }

        private static string[] GetAllResourceFiles(string path)
        {
            var allFiles = GetAllFiles(path, new[] { ".resx" });
            string[] excludedFiles;
            if (Settings.ExcludeResxFiles == null)
            {
                excludedFiles = Array.Empty<string>();
            }
            else
            {
                excludedFiles = Settings.ExcludeResxFiles.Select(e => "\\" + e).ToArray();
            }
            return allFiles.Where(e => !excludedFiles.Any(e.EndsWith)).ToArray();
        }
    }
}