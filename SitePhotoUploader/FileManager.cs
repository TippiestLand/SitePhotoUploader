using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SitePhotoUploader
{
    public class FileManager
    {
        public string FolderPath { get; private set; }
        public List<string> Files { get; private set; } = new List<string>();

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        public int LoadFolder(string folderPath)
        {
            FolderPath = folderPath;
            var files = Directory.EnumerateFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly).ToList();
            files.Sort((x, y) => StrCmpLogicalW(Path.GetFileName(x), Path.GetFileName(y)));
            Files = files;
            return Files.Count;
        }

        public string GetFile(int index)
        {
            if (index < 0 || index >= Files.Count) return null;
            return Files[index];
        }

        public int Count => Files.Count;
    }
}