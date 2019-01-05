using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scada.Data.DataFactory
{
    public abstract class BackupProduct
    {
        /// <summary>
        /// Путь к бэкапу
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Путь к исходному файлу
        /// </summary>
        public string SourceFilePath { get; set; }

        public BackupProduct()
        { }
    }
}
