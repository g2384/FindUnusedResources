namespace FindUnusedResources
{
    public class Settings
    {
        public string SourceFilePath { get; set; }

        public string[] ExcludeFolders { get; set; }

        public string[] FileExtensions { get; set; }

        public void Init()
        {
            FileExtensions = new[] { ".*" };
            ExcludeFolders = new[] { @"\obj\", @"\bin\" };
        }
    }
}
