using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Nils_Film_DB.DataAccess;
using Nils_Film_DB.Helper;

namespace Nils_Film_DB.ViewModel
{
    class ConnectViewModel : ViewModel
    {
        //
        // Interaction Logic for the Connect View. 
        // Reads login information from user input and transmits them to MainWindowViewModel and writes them to disk (optionally).
        //
       
        // Constructor: If the login information was saved locally on a previous session, it is retrieved and filled into the Textboxes     

        public ConnectViewModel(dynamic w)
        {
            winHelper = w;
            winID = winHelper.Open(this, -1, -1, false);

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NP_FilmDB\daten";
            if (System.IO.File.Exists(path))
            {
                BinaryReader br = new BinaryReader(new FileStream(path, FileMode.Open));
                ServerText = br.ReadString();
                UsernameText = br.ReadString();
                br.Close();      
            }
        }

        // Textboxes for Server, Username and Password
        private string serverText;
        public string ServerText
        {
            get { return serverText; }
            set
            {
                if (value != serverText)
                {
                    serverText = value;
                    OnPropertyChanged("ServerText");
                }
            }
        }

        private string usernameText;
        public string UsernameText
        {
            get { return usernameText; }
            set
            {
                if (value != usernameText)
                {
                    usernameText = value;
                    OnPropertyChanged("UsernameText");
                }
            }
        }

        private SecureString securepassword;
        public SecureString Securepassword
        {
            get { return securepassword; }
            set { securepassword = value; }
        }

        // Checkbox for saving login information
        private bool chkboxSave = true;
        public bool ChkboxSave
        {
            get { return chkboxSave; }
            set { chkboxSave = value; }
        }

        // Buttons Cancel and Connect
        public ICommand ButtonCancel
        {
            get
            {
                return new RelayCommand(param => CloseWindow());
            }
        }

        public ICommand ButtonConnect
        {
            get
            {
                return new RelayCommand(param => Connection());
            }
        }
    
        // The Command to close the window is transfered to the View via the Mediator. This way the ViewModel does not need to know the View.
        public void CloseWindow()
        {
            winHelper.Close(winID);
        }

        // The login information is passed to the MySQLConnection instance created in the MainWindowViewModel via the Mediator class
        // If the save Checkbox is saved, the information are saved under "\My Documents\NP_FilmDB\daten"
        private void Connection()
        {          
            // The connection information is transfered to the DataModel instance of the MainWindowView vie the Mediator class using a List of objects.
            List<object> coninfo = new List<object>();
            coninfo.Add(ServerText);
            coninfo.Add(UsernameText);
            coninfo.Add(Securepassword);

            try
            {
                // Transmit Login Information to MainWindowViewModel using the Mediator
                Mediator.NotifyColleagues("SetLogin", coninfo);
            }
            catch (Exception e)
            {
                MessageBoxViewModel mvm = new MessageBoxViewModel(winHelper, e.ToString());
            }

            // Save Login Information to file
            if (ChkboxSave == true)
            {             
                string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NP_FilmDB\daten";
                // Create Directory if it doesnt exist
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));               
                BinaryWriter bw = new BinaryWriter(new FileStream(path, FileMode.OpenOrCreate));
                bw.Write(ServerText);
                bw.Write(UsernameText);
                bw.Write(SecureStringConverter.SecureStringToString(Securepassword));
            }
            CloseWindow();
        }
    }
}
