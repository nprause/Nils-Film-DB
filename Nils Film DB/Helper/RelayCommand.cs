using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


// RealyCommand is used to bind UI Commands to Methods of the ViewModel. 

namespace Nils_Film_DB
{
    public class RelayCommand : ICommand
    {               
            readonly Action<object> execute;
            readonly Predicate<object> canExecute;

            public RelayCommand(Action<object> execute)
                : this(execute, null)
            {
            }
        
            public RelayCommand(Action<object> ex, Predicate<object> canex)
            {
                if (ex == null) 
                    throw new ArgumentNullException("execute");

                execute = ex;
                canExecute = canex;
            }

           
            public bool CanExecute(object parameter)
            {
                return canExecute == null ? true : canExecute(parameter);
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                execute(parameter);
            }
      
    }
}
