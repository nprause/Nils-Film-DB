using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Nils_Film_DB.Helper;
using Nils_Film_DB.Model;

namespace Nils_Film_DB.ViewModel
{
    class MainWindowViewModel : ViewModel
    {
        //
        // The MainWindowViewModel handels the interaction logic of the MainWindow. 
        // It is responsible for cumminication with the other viewmodels and the DataModel.
        // It is the main hub of the application.
        //

        // An instance of the Datamodel class is created. This class actually contains no data but handles communicates with the SQL Server.
        // At this stage in this project only a single instance of this class is created, which represents a Movie Collection. 
        // In the future this may be extended to Series, Music, Books, etc. 
        private Datamodel data_model = new Datamodel();
        public Datamodel Data_model
        {
            get { return data_model; }
            set { data_model = value; }
        }

        private string username;

        //
        // Constructor: Upon program start the data is loaded from the database if the user has saved his login credentials at an earlier session.
        //

        public MainWindowViewModel(dynamic w)
        {
            winHelper = w;
            winID = winHelper.Open(this,800,600);

            // The Mediator is used to exchange data between classes. 
            // Get Login Info from ConnectViewModel
            Mediator.Register("SetLogin", setLogin);

            // Get Metadata from ScanViewModel and transfer it to DataModel
            Mediator.Register("Metadata", addData);

            // Edit, change and delete row from TabControlViewModel
            Mediator.Register("DeleteRow", deleteRow);
            Mediator.Register("EditRow", editRow);
            Mediator.Register("ChangeRow", changeRow);

            // Check if Login information are stored locally       
            readLogin();
          
        }
   

        //
        // The TabControl displays the data
        //

        // Each instance of tabs contains the data of one of the tabs.        
        private ObservableCollection<TabControlViewModel> tabs = new ObservableCollection<TabControlViewModel>();
        public ObservableCollection<TabControlViewModel> Tabs
        {
            get { return tabs; }
        }
          
        //
        // The menu and the Serverstatus Box
        //

        // Connect to SQL-Server. Opens the Connection Dialogue
        public ICommand Menu_Connect
        {
            get
            {
                return new RelayCommand(param => openConnect());
            }
        }

        private void openConnect()
        {
            ConnectViewModel cvm = new ConnectViewModel(winHelper);
        }

        // Scan Movie Collection. Open Scan Window
        public ICommand Menu_Scan
        {
            get
            {
                return new RelayCommand(param => openScan());
            }
        }

        private void openScan()
        {
            ScanViewModel svm = new ScanViewModel(winHelper);
        }

        // Get Online Data from https://www.themoviedb.org/
        public ICommand Menu_TMDb
        {
            get
            {
                return new RelayCommand(param => openTMDb());
            }
        }

        private void openTMDb()
        {
            List<string> columns = new List<string>();
            List<string> values = new List<string>();
            List<string> rcol = new List<string>();

            columns.Add("_Synchro");
            values.Add("0");
            rcol.AddRange(new string[7]{"Nr","Titel","Originaltitel","Jahr","Land","Regisseur","Rating"});

            DataTable dt = Data_model.GetTable("Filme", columns, values);
            OnlineDataViewModel ovm = new OnlineDataViewModel(winHelper, dt);
        }

        // Exit
        public ICommand Menu_Exit
        {
            get
            {
                return new RelayCommand(param => Exit());
            }
        }

        private void Exit()
        {
            System.Environment.Exit(0);
        }

        // TextblockServerStatus shows the connection status
        private string serverstatustext;
        public string Serverstatustext
        {
            get { return serverstatustext; }
            set
            {
                if (serverstatustext != value)
                {
                    serverstatustext = value;
                    OnPropertyChanged("Serverstatustext");
                }
            }
        }


        //
        // The search box and the search options
        //

        // SearchBox Text
        private string searchboxtext = "Suche";
        public string Searchboxtext
        {
            get { return searchboxtext; }
            set
            {
                if (value != searchboxtext)
                {
                    searchboxtext = value;
                    OnPropertyChanged("Searchboxtext");
                }
            }
        }


        // Visibility of the Search Options
        private bool searchboxVisibility;
        public bool SearchboxVisibility
        {
            get { return searchboxVisibility; }
            set
            {
                if (searchboxVisibility != value)
                {
                    searchboxVisibility = value;
                    OnPropertyChanged("SearchboxVisibility");
                }
            }
        }

        // A Search is triggered with the Enter key. Aditionaly the search options are hidden. 
        public ICommand SearchboxEnter
        {
            get
            {
                return new RelayCommand(param => startSearch());
            }
        }

        // When the Searchbox gets focus, the search Options are shown
        public ICommand SearchboxOnfocus
        {
            get
            {
                return new RelayCommand(param => searchFocus(true));
            }
        }

        // The Search is executed
        private void startSearch()
        {
            List<string> search_options = new List<string>();
            List<string> search_users = new List<string>();
            foreach (CheckBoxViewModel chk in SearchColumnBoxes)
            {
                if (chk.IsChecked)            
                    search_options.Add("Filme." + chk.Title);              
            }
            foreach (CheckBoxViewModel chk in SearchUserBoxes)
            {
                if (chk.IsChecked)
                    search_users.Add(chk.Title);
            }
            populate(Searchboxtext, search_options, search_users, ComboboxVersionsIsSelcted);

            // Remove Focus from Textbox to hide search options (maybe better to do in code behind)
            TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Down);
            (Keyboard.FocusedElement as UIElement).MoveFocus(request);
            SearchboxVisibility = false;
        }


        // If Searchbox gets focus
        private void searchFocus(bool val)
        {
            if (Searchboxtext == "Suche")
                Searchboxtext = "";
            SearchboxVisibility = val;
        }

        // Combobox to switch between movie and version for 'not in collection of ...' search
        private bool comboboxMoviesIsSelcted = true;
        public bool ComboboxMoviesIsSelcted
        {
            get { return comboboxMoviesIsSelcted; }
            set
            {
                if (value != comboboxMoviesIsSelcted)
                {
                    comboboxMoviesIsSelcted = value;
                    OnPropertyChanged("ComboboxMoviesIsSelcted");
                }
            }
        }

        private bool comboboxVersionsIsSelcted;
        public bool ComboboxVersionsIsSelcted
        {
            get { return comboboxVersionsIsSelcted; }
            set
            {
                if (value != comboboxVersionsIsSelcted)
                {
                    comboboxVersionsIsSelcted = value;
                    OnPropertyChanged("ComboboxVersionsIsSelcted");
                }
            }
        }

        // Checkboxes for search options 
        private ObservableCollection<CheckBoxViewModel> searchColumnBoxes = new ObservableCollection<CheckBoxViewModel>();
        public ObservableCollection<CheckBoxViewModel> SearchColumnBoxes
        {
            get { return searchColumnBoxes; }
            set { searchColumnBoxes = value; }
        }

        private ObservableCollection<CheckBoxViewModel> searchUserBoxes = new ObservableCollection<CheckBoxViewModel>();
        public ObservableCollection<CheckBoxViewModel> SearchUserBoxes
        {
            get { return searchUserBoxes; }
            set { searchUserBoxes = value; }
        }

        // If user deletes a version of a movie it is tried to remove it from the Versions and Movies tables as well. 
        // If another user has the same version or another version of the movie exists, the foreign key constraints will prevent the deletion.
        private void deleteRow(object args)
        {
            DataRow drow = args as DataRow;
            int key = Data_model.GetPrimaryKey(drow, "Versionen", "V_Nr");
            Data_model.DeleteRow(username, "V_Nr", key.ToString());
            string key2 = Data_model.GetValue("Versionen", "Nr", "V_Nr", key.ToString());
            Data_model.DeleteRow("Versionen", "V_Nr", key.ToString());
            Data_model.DeleteRow("Filme", "Nr", key2.ToString());
            startSearch();
        }

        // Opens a dialouge to edit a row and transfer changes to database
        private void editRow(object args)
        {
            DataRow dr = (args as DataRowView).Row;
            int pk = Data_model.GetPrimaryKey(dr, "Filme", "Nr");
            EditViewModel evm = new EditViewModel(winHelper, dr, pk);
        }

        private void changeRow(object args)
        {
            DataRow dr = (args as List<object>)[0] as DataRow;
            int pk = Convert.ToInt16((args as List<object>)[1]);
            Data_model.UpdateRow("filme", dr, pk);
            startSearch();
        }

        // If save file exists, read login information from save file and starts setLogin to transmit login information to Datamodel
        private void readLogin()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NP_FilmDB\daten";
            if (System.IO.File.Exists(path))
            {
                BinaryReader read = new BinaryReader(new FileStream(path, FileMode.Open));
                string server = read.ReadString();
                username = read.ReadString();
                string pass = read.ReadString();
                read.Close();
                List<object> loginInfo = new List<object>();
                loginInfo.Add(server);
                loginInfo.Add(username);
                loginInfo.Add(SecureStringConverter.StringToSecureString(pass));
                setLogin(loginInfo);
            }
        }

        // This method transmits the login information to the Datamodel and starts populate() to fill the Datagrid with data
        // The result of the connection test is returned as a string and displayed on the MainWindow View.
        private void setLogin(object args)
        {
            username = (args as List<object>)[1].ToString();
            Serverstatustext = Data_model.SetLogin(args as List<object>);
            if (Serverstatustext != "Verbindungsfehler")
            {
                populate();              
            }
        }

        // populate() gets data from the SQL-Server via the DataModel and fills the DataGrid by adding the DataTables to tabs
        private void populate(string search = null, List<string> search_columns = null, List<string> search_user = null, bool versions = false)
        {
            tabs.Clear();
            SearchUserBoxes.Clear();
            SearchColumnBoxes.Clear();
            List<DataTable> tables = new List<DataTable>();
            tables = Data_model.GetTables(search, search_columns, search_user, versions);
            foreach (DataTable dt in tables)
            {
                TabControlViewModel tvm = new TabControlViewModel(username, dt);
                Tabs.Add(tvm);

                // Add Checkboxes for User Search
                if (dt.TableName.ToLower() != "filme")
                {
                    CheckBoxViewModel chkbx = new CheckBoxViewModel(false);
                    chkbx.Title = dt.TableName;
                    SearchUserBoxes.Add(chkbx);
                }
            }

            // Add checkboxes for Column Search
            foreach (DataColumn dc in Tabs[0].Data.Columns)
            {
                if (dc.ColumnName != "Nr")
                {
                    CheckBoxViewModel chkbx = new CheckBoxViewModel();
                    chkbx.Title = dc.ColumnName;
                    SearchColumnBoxes.Add(chkbx);
                }
            }
        }

        // Add newly scanned data to Database and refresh the view with populate()
        private void addData(object args)
        {
            Data_model.AddData(args as DataTable);
            populate();
        }
       
    }

}

