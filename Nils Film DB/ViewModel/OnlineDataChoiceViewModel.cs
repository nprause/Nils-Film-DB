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
    

    class OnlineDataChoiceViewModel : ViewModel
    {
        private DataRow dr;

        private ObservableCollection<RadioButton> rButton = new ObservableCollection<RadioButton>();
        public ObservableCollection<RadioButton> RButton
        {
            get { return rButton; }
            set
            {
                if (value != rButton)
                {
                    rButton = value;
                    OnPropertyChanged("RButton");
                }
            }
        }

        private string dataRowText;
        public string DataRowText
        {
            get { return dataRowText; }
            set
            {
                if (value != dataRowText)
                {
                    dataRowText = value;
                    OnPropertyChanged("DataRowText");
                }
            }
        }

        private bool isCheckedEnterID;
        public bool IsCheckedEnterID
        {
            get { return isCheckedEnterID; }
            set
            {
                if (value != isCheckedEnterID)
                {
                    isCheckedEnterID = value;
                    OnPropertyChanged("IsCheckedEnterID");
                }
            }
        }

        private string enterID;
        public string EnterID
        {
            get { return enterID; }
            set
            {
                if (value != enterID)
                {
                    enterID = value;
                    OnPropertyChanged("EnterID");
                }
            }
        }

        public OnlineDataChoiceViewModel (object w, List<string[]> movies, DataRow row)
        {
            winHelper = w;
            winID = winHelper.Open(this);
            dr = row;
            DataRowText = dr[1].ToString();
            DataRowText += " : " + dr[4].ToString() + " : " + dr[5].ToString();
            if (movies != null)
            {
                foreach (string[] str in movies)
                {
                    string text = str[0] + ": " + str[2] + " (" + str[1] + ") [" + str[3] + "]";
                    RButton.Add(new RadioButton(text));
                }
            }       
        }

        public OnlineDataChoiceViewModel(List<string[]> movies, DataRow row)
        {          
            dr = row;
            if (dr[1].ToString() != null)
            {
                DataRowText = dr[1].ToString();
                DataRowText += " : " + dr[3].ToString() + " : " + dr[5].ToString();
            }
            else
                DataRowText = dr[11].ToString();
            if (movies != null)
            {
                foreach (string[] str in movies)
                {
                    string text = str[0] + ": " + str[2] + " (" + str[1] + ") [" + str[3] + "]";
                    RButton.Add(new RadioButton(text));
                }
            }
        }

        public ICommand ButtonCancel
        {
            get
            {
                return new RelayCommand(param => CloseWindow());
            }
        }

        private void CloseWindow()
        {
            if (winID > 0)
                winHelper.Close(winID);
            else
                Mediator.NotifyColleagues("Choice_scan_drop", this);
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
            List<object> retValues = new List<object>();
            foreach (RadioButton b in RButton)
            {
               
                if (b.IsChecked == true)
                {
                    retValues.Add(b.Text.Remove(b.Text.IndexOf(":")));
                    retValues.Add(dr);
                    CloseWindow();
                    Mediator.NotifyColleagues("Choice", retValues);

                }
            }
                if (IsCheckedEnterID == true)
                {
                    if (EnterID != "")
                    {
                        retValues.Add(EnterID);
                        retValues.Add(dr);
                        CloseWindow();
                        Mediator.NotifyColleagues("Choice", retValues);                     
                        
                    }                                    
                }
            }      
    }

    class RadioButton : ViewModel
    {
        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                if (value != text)
                {
                    text = value;
                    OnPropertyChanged("Text");
                }
            }
        }

        private bool isChecked;
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (value != isChecked)
                {
                    isChecked = value;
                    OnPropertyChanged("IsChecked");
                }
            }
        }

        public RadioButton(string t)
        {
            Text = t;
        }
    }
}
