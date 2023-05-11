# FindUnusedResources

Find unused resources in C# projects

## FindUnusedResources.Console

This console version uses `Microsoft.CodeAnalysis` to analyze the usage of strings in `.resx` files. To use it, simply run the command:

### How to use

```
FindUnusedResources.Console.exe settings.yaml
```

The `settings.yaml` can be automatically generated by running the `FindUnusedResources.Console.exe` for the first time.

```yaml
# an example of the settings.yaml file
sourceCodeFolderPath: C:\path\to\project
excludeFolders:
  - '\obj\'
  - '\bin\'
fileExtensions:
  - .cs
excludeResxFiles:
```

## FindUnusedResources.Desktop

This desktop version is designed to target .NET (not .NET Framework) and uses string matching to find references of strings in `.resx` files.

## FindUnusedResources.Net472

This legacy desktop version is no longer maintained.

## TODO

- [ ] use `Microsoft.CodeAnalysis` to analyse xaml (View) files;
- [ ] split code to a library project that can be shared by both console and desktop version;
- [ ] support both CodeAnalysis mode and string matching mode;

## Similar projects:

- [dotnet/ResXResourceManager](https://github.com/dotnet/ResXResourceManager)
- [RandomEngy/RESX-Unused-Finder](https://github.com/RandomEngy/RESX-Unused-Finder) (no longer maintained)
