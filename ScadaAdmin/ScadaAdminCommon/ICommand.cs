using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScadaAdmin.Common
{
    /// <summary>
    /// Интрефейс команды
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICommand<T>
    {
        /// <summary>
        /// Выполняет команду
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        T Do(T input);

        /// <summary>
        /// Отменяет выполнение команды
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        T Undo(T input);
    }
}
