using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Scada.UI.CommandManager
{

    public class DataTableMemento: Memento<Dictionary<int, DataRow>, DataTable>
    {
        public DataTableMemento(Dictionary<int, DataRow> mementoData, DataTable target)
        {
            base.MementoData = mementoData;
            base.Target = target;
        }

        public override void SetMemento(Dictionary<int, DataRow> _mementoData, CommandManager.CommandType CmdType, Memento<Dictionary<int, DataRow>, DataTable>.ActionType type)
        {
            base.MementoData = _mementoData;
            HandleAction(type, CmdType);
        }

        private void HandleAction(Memento<Dictionary<int, DataRow>, DataTable>.ActionType type, CommandManager.CommandType CmdType)
        {
            switch (type)
            {
                case Memento<Dictionary<int, DataRow>, DataTable>.ActionType.Undo:
                     if(CmdType == CommandManager.CommandType.Remove)
                        UndoRemove();
                    break;
                case Memento<Dictionary<int, DataRow>, DataTable>.ActionType.Redo:
                    if(CmdType == CommandManager.CommandType.Remove)
                        RedoRemove();
                    break;
                default:
                    break;
            }
        }

        private void UndoRemove()
        {
            // TODO: Изменить строки или добавить новые
            foreach (var dataUnit in MementoData)
            {
                if (Target.Rows.Contains(dataUnit.Key))
                {
                    dataUnit.Value.ItemArray.CopyTo(Target.Rows[dataUnit.Key].ItemArray, 0);
                }
                else
                {
                    Target.Rows.Add(dataUnit.Value);
                }
            }
        }

        private void RedoRemove()
        {
            foreach (var dataUnit in MementoData)
            {
                Target.Rows.Remove(dataUnit.Value);
            }
        }
    }
}
