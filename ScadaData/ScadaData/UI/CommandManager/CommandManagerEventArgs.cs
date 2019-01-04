using System;

namespace Scada.UI.CommandManager
{
    public class CommandManagerEventArgs : EventArgs
    {
        public bool UndoReady { get; set; }
        public bool RedoReady { get; set; }
        public CommandManager.CommandType Type { get; set; }

        public CommandManagerEventArgs()
            : base()
        {
            UndoReady = false;
            RedoReady = false;
            Type = CommandManager.CommandType.Undef;
        }
    }
}
