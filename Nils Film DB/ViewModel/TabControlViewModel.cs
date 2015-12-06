using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Nils_Film_DB.Helper;

namespace Nils_Film_DB.ViewModel
{
    class TabControlViewModel : ViewModel
    {
        //
        // This class described the TabControl which is used to display and modify data on the MainWindowView
        //

        private string username;

        public TabControlViewModel(string user, DataTable dt)
        {         
            username = user;
            Data = dt;
            if (dt.TableName.ToLower() == user.ToLower())
                ContextMenuDeleteIsEnabled = true;
            if (dt.TableName.ToLower() == "filme")
                ContextMenuEditIsEnabled = true;
        }

        private DataTable data = new DataTable();
        public DataTable Data
        {
            get { return data; }
            set
            {
                if (value != data)
                {
                    data = value;
                    OnPropertyChanged("Data");
                }
            }
        }

        private bool contextMenuDeleteIsEnabled = false;
        public bool ContextMenuDeleteIsEnabled
        {
            get { return contextMenuDeleteIsEnabled; }
            set
            {
                if (value != contextMenuDeleteIsEnabled)
                {
                    contextMenuDeleteIsEnabled = value;
                    OnPropertyChanged("ContextMenuDeleteIsEnabled");
                }
            }
        }

        private bool contextMenuEditIsEnabled = false;
        public bool ContextMenuEditIsEnabled
        {
            get { return contextMenuEditIsEnabled; }
            set
            {
                if (value != contextMenuEditIsEnabled)
                {
                    contextMenuEditIsEnabled = value;
                    OnPropertyChanged("ContextMenuEditIsEnabled");
                }
            }
        }

        public ICommand ContextMenuDelete
        {
            get
            {
                return new RelayCommand(param => delete(param));
            }
        }

        private void delete(object param)
        {
            if (param != null && Data.TableName.ToLower() == username.ToLower())
            {
                DataRow dr = (param as DataRowView).Row;
                Mediator.NotifyColleagues("DeleteRow", new List<int>(new int[]{Convert.ToInt16(dr.ItemArray[0]), Convert.ToInt16(dr.ItemArray[1])}));
                Data.Rows.Remove(dr);         
            }           
        }

        public ICommand ContextMenuEdit
        {
            get
            {
                return new RelayCommand(param => edit(param));
            }
        }

        private void edit(object param)
        {
            if (param != null)
                Mediator.NotifyColleagues("EditRow", param);
        }
    }
}
