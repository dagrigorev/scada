using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScadaAdmin.Common
{
    /// <summary>
    /// Аттрибут для включения снимков
    /// </summary>
    class UseSnapshotAttribute: Attribute
    {
        /// <summary>
        /// Флаг включения снимков
        /// </summary>
        public bool Enabled { get; set; }

        public UseSnapshotAttribute()
        {
            Enabled = false;
        }

        public UseSnapshotAttribute(bool val)
        {
            Enabled = val;
        }
    }
}
