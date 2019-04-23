using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scada.Data.DataFactory
{
    public class FileBackupCreator : BackupCreator
    {
        /// <summary>
        /// Максимальное число продуктов
        /// </summary>
        public const int MaxProductCount = 1;

        /// <summary>
        /// Создает бэкап файла
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        public override BackupProduct Make(string sourceFilePath)
        {
            if (FileBackup.GetFileCount(sourceFilePath, "*.bak") >= MaxProductCount)
            {
                var files = FileBackup.GetFiles(sourceFilePath, "*.bak");
                File.Delete(files.Last().FullName);
            }

            var fb = new FileBackup
            {
                SourceFilePath = sourceFilePath,
                FilePath = sourceFilePath + "." + DateTime.Now.Ticks.ToString() + ".bak"
            };
            fb.Copy();
            return fb;
        }
    }
}
