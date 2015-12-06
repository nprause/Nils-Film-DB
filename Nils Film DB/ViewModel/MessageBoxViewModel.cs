using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Nils_Film_DB.ViewModel
{
    class MessageBoxViewModel : ViewModel
    {

        private string messageBoxText;
        public string MessageBoxText
        {
            get { return messageBoxText; }
            set
            {
                if (value != messageBoxText)
                {
                    messageBoxText = value;
                    OnPropertyChanged("MessageBoxText");
                }
            }
        }

        public MessageBoxViewModel(dynamic w, string txt)
        {
            winHelper = w;
            winID = winHelper.Open(this);
            MessageBoxText = txt;
        }

        public ICommand ButtonOk
        {
            get
            {
                return new RelayCommand(param => Close());
            }
        }

        private void Close()
        {
            winHelper.Close(winID);
        }
    }
}
