namespace FindUnusedResources.Console
{
    public class Settings
    {
        public string SourceCodeFolderPath { get; set; }

        public string[] ExcludeFolders { get; set; } = new[] { @"\obj\", @"\bin\" };

        public string[] FileExtensions { get; set; } = { ".cs" };

        public string[] ExcludeResxFiles { get; set; }
    }
}
