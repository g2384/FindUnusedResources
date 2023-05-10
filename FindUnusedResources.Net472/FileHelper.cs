using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FindUnusedResources
{
    public static class FileHelper
    {
        public static IEnumerable<string> GetAllFiles(string path, string mask, Func<FileInfo, bool> checkFile = null)
        {
            if (string.IsNullOrEmpty(mask))
                mask = "*.*";
            var files = Directory.GetFiles(path, mask, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (checkFile == null || checkFile(new FileInfo(file)))
                    yield return file;
            }
        }

        public static string[] GetAllFiles(string path, string[] extensions, IEnumerable<string> excludedFolders)
        {
            var allFiles = new List<string>();
            foreach (var ext in extensions)
            {
                var extension = ext.StartsWith("*") ? ext : "*" + ext;
                allFiles.AddRange(GetAllFiles(path, extension, info =>
                    extensions.Any(i => i == ".*" || Path.GetExtension(info.Name) == i)
                    && !info.IsReadOnly
                    && excludedFolders.All(
                        i => (info.Directory?.FullName.Replace(path, @"\") + "\\").Contains(i) == false
                    )));
            }

            return allFiles.ToArray();
        }
    }
}
