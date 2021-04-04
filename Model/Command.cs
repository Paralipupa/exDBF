
using System;
using System.Windows.Input;

namespace exDBF
{
    public class Command : ICommand
    {
        private Action<Object> execute;
        private Func<Object, bool> can;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public Command(Action<Object> execute, Func<Object, bool> can = null)
        {
            this.execute = execute;
            this.can = can;
        }

        public bool CanExecute(Object parameter)
        {
            return can == null || can(parameter);
        }

        public void Execute(Object parameter)
        {
            execute(parameter);
        }
    }
}
