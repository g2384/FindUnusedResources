namespace FindUnusedResources.Desktop
{
    public class Settings
    {
        public string SourceFilePath { get; set; }

        public string[] ExcludeFolders { get; set; }

        public string[] FileExtensions { get; set; }

        public string[] ExcludeFiles { get; set; }

        public void Init()
        {
            FileExtensions = new[] { ".*" };
            ExcludeFolders = new[] { @"\obj\", @"\bin\" };
        }
    }
}
