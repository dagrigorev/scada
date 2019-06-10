using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScadaAdmin.Common
{
    /// <summary>
    /// Очередь команд
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class UndoRedoStack<T>
    {
        /// <summary>
        /// Очередь отменяемых команд
        /// </summary>
        private Stack<ICommand<T>> _undoStack;

        /// <summary>
        /// Очередь выполняемых команд 
        /// </summary>
        private Stack<ICommand<T>> _redoStack;

        /// <summary>
        /// Количество отменяемвх команд
        /// </summary>
        public int UndoCount => _undoStack.Count;

        /// <summary>
        /// Количество выполняемых команд
        /// </summary>
        public int RedoStack => _redoStack.Count;

        public UndoRedoStack()
        {
            ResetStack();
        }

        /// <summary>
        /// Сбрасывает состояние очереди
        /// </summary>
        public void ResetStack()
        {
            _undoStack.Clear();
            _redoStack.Clear();

            _undoStack = new Stack<ICommand<T>>();
            _redoStack = new Stack<ICommand<T>>();
        }

        public T Do(ICommand<T> cmd, T input)
        {
            T output = cmd.Do(input);
            _undoStack.Push(cmd);
            _redoStack.Clear(); // Once we issue a new command, the redo stack clears
            return output;
        }
        public T Undo(T input)
        {
            if (_undoStack.Count > 0)
            {
                ICommand<T> cmd = _undoStack.Pop();
                T output = cmd.Undo(input);
                _redoStack.Push(cmd);
                return output;
            }
            else
            {
                return input;
            }
        }
        public T Redo(T input)
        {
            if (_redoStack.Count > 0)
            {
                ICommand<T> cmd = _redoStack.Pop();
                T output = cmd.Do(input);
                _undoStack.Push(cmd);
                return output;
            }
            else
            {
                return input;
            }
        }
    }
}
