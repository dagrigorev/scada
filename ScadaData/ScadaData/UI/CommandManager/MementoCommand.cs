namespace Scada.UI.CommandManager
{
    public sealed class MementoCommand<T1, T2> : ICommand
    {
        public CommandManager.CommandType Type { get; set; }
        private Memento<T1, T2> _memento;
        private T1 _prev;
        private T1 _next;
        
        public MementoCommand(Memento<T1, T2> prev, Memento<T1, T2> next)
        {
            _memento = prev;
            _prev = prev.MementoData;
            _next = next.MementoData;
            Type = CommandManager.CommandType.Undef;
        }

        void ICommand.Invoke()
        {
            _prev = _memento.MementoData;
            _memento.SetMemento(_next, Type);
        }

        void ICommand.Redo()
        {
            _memento.SetMemento(_prev, Type, Memento<T1, T2>.ActionType.Redo);
            _next = _prev;
        }

        void ICommand.Undo()
        {
            _memento.SetMemento(_next, Type, Memento<T1, T2>.ActionType.Undo);
            _prev = _next;
        }

        CommandManager.CommandType ICommand.GetType()
        {
            return Type;
        }
    }
}
