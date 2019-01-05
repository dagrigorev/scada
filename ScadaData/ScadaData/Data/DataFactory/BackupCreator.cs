using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scada.Data.DataFactory
{
    public abstract class BackupCreator
    {
        public abstract BackupProduct Make(string sourceFilePath);
    }
}
