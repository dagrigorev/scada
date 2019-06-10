using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScadaAdmin.Common
{
    public class AddRowsCommand : ICommand<DataTable>
    {
        /// <summary>
        /// Хранилище команд
        /// </summary>
        private DataTable _dataContainer;

        /// <summary>
        /// Возвращает источник данных
        /// </summary>
        public DataTable DataContainer => _dataContainer;

        public AddRowsCommand()
        {
            _dataContainer = new DataTable();
        }

        public DataTable Do(DataTable input)
        {
            _dataContainer.Merge(input);
            if (_dataContainer.HasErrors)
                return null;
            return _dataContainer;
        }

        public DataTable Undo(DataTable input)
        {
            for (var dataRowIndex = 0; dataRowIndex < input.Rows.Count; dataRowIndex++)
            {
                _dataContainer.Rows.Remove(_dataContainer.Rows.Find(input.Rows[dataRowIndex]));
            }
            if (_dataContainer.HasErrors)
                return null;
            return _dataContainer;
        }
    }
}
