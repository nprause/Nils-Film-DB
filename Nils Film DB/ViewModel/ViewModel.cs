using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nils_Film_DB.ViewModel
{

    // 
    // Base Class for ViewModel classes to implement the PropertyChanged évent 
    //

    public abstract class ViewModel : INotifyPropertyChanged
    {
        protected int winID;
        protected dynamic winHelper;
      
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {           
            if (PropertyChanged != null)
                PropertyChanged(this, args);
        }
    }
}
