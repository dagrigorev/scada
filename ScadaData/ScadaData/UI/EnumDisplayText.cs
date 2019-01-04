using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scada.UI
{
    /// <summary>
    /// Атрибут описания элементов перечисления
    /// </summary>
    public class EnumDisplayText : Attribute
    {
        private string _desc;
        public string Description
        {
            get { return _desc; }
            set { _desc = value; }
        }

        public EnumDisplayText(string description)
        {
            this._desc = description;
        }
    }
}
