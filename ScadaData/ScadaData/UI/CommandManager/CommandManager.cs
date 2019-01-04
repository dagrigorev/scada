using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Scada.UI.CommandManager
{
    public sealed class CommandManager
    {
        public enum CommandType
        {
            [EnumDisplayText("Добавить")]
            Add,
            [EnumDisplayText("Удалить")]
            Remove,
            [EnumDisplayText("Изменить")]
            Change,
            Undef
        };

        private int _maxStack = int.MaxValue;
        private Stack<ICommand> _undoStack;
        private Stack<ICommand> _redoStack;
        public delegate void CommandChanged(object sender, CommandManagerEventArgs args);
        public event CommandChanged OnCommandChanged;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public CommandManager()
        {
            this._undoStack = new Stack<ICommand>();
            this._redoStack = new Stack<ICommand>();
        }

        public CommandManager(int maxStack) : this()
        {
            _maxStack = maxStack;
        }

        public bool Invoke(ICommand command)
        {
            if (_undoStack.Count >= _maxStack) return false;
            command.Invoke();
            _redoStack.Clear();
            _undoStack.Push(command);

            if (OnCommandChanged != null)
            {
                OnCommandChanged(this, new CommandManagerEventArgs()
                {
                    UndoReady = _undoStack.Count != 0,
                    RedoReady = _redoStack.Count != 0,
                    Type = command.GetType()
                });
            }
            return true;
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            if (OnCommandChanged != null)
                OnCommandChanged(this, new CommandManagerEventArgs()
                {
                    UndoReady = _undoStack.Count != 0,
                    RedoReady = _redoStack.Count != 0,
                    Type = command.GetType()
                });
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            var command = _redoStack.Pop();
            command.Redo();
            _undoStack.Push(command);

            if (OnCommandChanged != null)
                OnCommandChanged(this, new CommandManagerEventArgs()
                {
                    UndoReady = _undoStack.Count != 0,
                    RedoReady = _redoStack.Count != 0,
                    Type = command.GetType()
                });
        }

        public void Refresh()
        {
            _undoStack.Clear();
            _redoStack.Clear();

            if (OnCommandChanged != null)
            {
                OnCommandChanged(this, new CommandManagerEventArgs()
                {
                    UndoReady = _undoStack.Count != 0,
                    RedoReady = _redoStack.Count != 0,
                    Type = CommandType.Undef
                });
            }
        }

        public List<string> GetUndoDescription()
        {
            var result = new List<string>();
            foreach (var undoCmd in _undoStack)
                result.Add(undoCmd.GetType().ToDescription());
            return result;
        }

        public List<string> GetRedoDescription()
        {
            var result = new List<string>();
            foreach (var redoCmd in _redoStack)
                result.Add(redoCmd.GetType().ToDescription());
            return result;
        }
    }
}
