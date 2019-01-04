using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scada.UI.CommandManager
{
    /// <summary>
    /// Интерфейс команды
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Вызов команды
        /// </summary>
        void Invoke();

        /// <summary>
        /// Отмена команды
        /// </summary>
        void Undo();

        /// <summary>
        /// Возврат команды
        /// </summary>
        void Redo();

        CommandManager.CommandType GetType();
    }
}
