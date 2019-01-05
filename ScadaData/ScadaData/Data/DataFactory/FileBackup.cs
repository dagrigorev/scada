using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scada.Data.DataFactory
{
    /// <summary>
    /// Бэкап файла
    /// </summary>
    public class FileBackup: BackupProduct
    {
        public FileBackup()
        { }

        public void Copy()
        {
            if(!string.IsNullOrEmpty(SourceFilePath) && File.Exists(SourceFilePath) && IsCopyNeeded())
                File.Copy(SourceFilePath, FilePath);
        }

        private bool IsCopyNeeded()
        {
            var bakFiles = Directory.GetParent(SourceFilePath).GetFiles("*.bak");
            if (bakFiles.Length > 0)
            {
                var lastFilePath = bakFiles.Last().FullName;
                return Directory.GetLastWriteTime(lastFilePath).Ticks != Directory.GetLastWriteTime(SourceFilePath).Ticks;
            }
            return true;
        }

        public static int GetFileCount(string path, string fileMask)
        {
            return Directory.GetParent(path).GetFiles(fileMask).Count();
        }

        public static FileInfo[] GetFiles(string path, string fileMask)
        {
            return Directory.GetParent(path).GetFiles(fileMask);
        }
    }
}
