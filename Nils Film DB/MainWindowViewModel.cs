using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nils_Film_DB
{
    class MainWindowViewModel
    {
        ObservableCollection<string> Header = new ObservableCollection<string>();
        public void MainViewModel()
        {
            Header.Add("filme");
            Header.Add("nils");
        }
    }
}
