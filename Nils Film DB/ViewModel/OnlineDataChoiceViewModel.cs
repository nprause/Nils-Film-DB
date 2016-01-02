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

        public ICommand ButtonCancel
        {
            get
            {
                return new RelayCommand(param => CloseWindow());
            }
        }

        private void CloseWindow()
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
            foreach (RadioButton b in RButton)
            {
                List<object> retValues = new List<object>();
                if (b.IsChecked == true)
                {             
                    retValues.Add(b.Text.Remove(b.Text.IndexOf(":")));
                    retValues.Add(dr);
                    Mediator.NotifyColleagues("Choice", retValues);
                    CloseWindow();
                }
                if (IsCheckedEnterID == true)
                {
                    if (EnterID != "")
                    {
                        retValues.Add(EnterID);
                        retValues.Add(dr);
                        Mediator.NotifyColleagues("Choice", retValues);
                        CloseWindow();
                    }                                    
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
