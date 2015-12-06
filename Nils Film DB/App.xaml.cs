using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Nils_Film_DB.ViewModel;
using Nils_Film_DB.Helper;

namespace Nils_Film_DB
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //
        // Starts the applictaion. For this an instance of the MainWindowViewModel is created. The WindowHelper class provides methods to open and close UI windows.
        // For testing it can be replaced by a dummy object.
        // 

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            WindowHelper winhelp = new WindowHelper();
            MainWindowViewModel mvwm = new MainWindowViewModel(winhelp);
        }

    }
}
