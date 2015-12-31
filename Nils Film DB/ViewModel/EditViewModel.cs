using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Nils_Film_DB.Helper;

namespace Nils_Film_DB.ViewModel
{
    class EditViewModel : ViewModel
    {
        private DataRow dr;
        int pk;

        public EditViewModel(dynamic w, DataRow row, int key)
        {
            pk = key;
            winHelper = w;
            winID = winHelper.Open(this, -1, -1, false);
            dr = row;
            for (int i=0; i<dr.ItemArray.Count(); ++i)
            {
                if (dr.Table.Columns[i].ColumnName != "Hinzugefügt")
                {
                    Titles.Add(dr.Table.Columns[i].ColumnName);
                    Values.Add(new BindString(dr.ItemArray[i].ToString()));
                }
            }
        }

        private ObservableCollection<string> titles = new ObservableCollection<string>();
        public ObservableCollection<string> Titles
        {
            get { return titles; }
            set
            {
                if (value != titles)
                {
                    titles = value;
                    OnPropertyChanged("Titles");
                }
            }
        }

        private ObservableCollection<BindString> values = new ObservableCollection<BindString>();
        public ObservableCollection<BindString> Values
        {
            get { return values; }
            set
            {
                if (value != values)
                {
                    values = value;
                    OnPropertyChanged("Values");
                }
            }
        }

        public ICommand ButtonCancel
        {
            get
            {
                return new RelayCommand(param => closeWindow());
            }
        }

        private void closeWindow()
        {
            winHelper.Close(winID);
        }

        public ICommand ButtonAccept
        {
            get
            {
                return new RelayCommand(param => accept());
            }
        }

        private void accept()
        {
            bool changed = false;
            for (int i = 0; i < dr.ItemArray.Count() - 1; ++i)
            {
                if (dr.Table.Columns[i].ColumnName != "Hinzugefügt")
                {
                    if (dr.ItemArray[i].ToString() != Values[i].Bsval)
                    {
                        dr[i] = Values[i].Bsval;
                        changed = true;
                    }
                }
            }
            List<object> args = new List<object>();
            args.Add(dr);
            args.Add(pk);
            if (changed)
                Mediator.NotifyColleagues("ChangeRow", args);
            closeWindow();
        }
    }
    public class BindString : ViewModel
    {
        private string bsval;
        public string Bsval
        {
            get { return bsval; }
            set
            {
                if (value != bsval)
                {
                    bsval = value;
                    OnPropertyChanged("Bsval");
                }
            }
        }

        public BindString(string val)
        {
            Bsval = val;
        }
    }
}
