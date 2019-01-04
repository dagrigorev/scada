using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scada.UI.CommandManager
{
    public abstract class Memento<DataType, TargetType>
    {
        public enum ActionType { Do, Undo, Redo };
        public DataType MementoData { get; protected set; }
        protected TargetType Target { get; set; }
        public abstract void SetMemento(DataType _mementoData, CommandManager.CommandType type, ActionType actionType = ActionType.Do);
    }
}
