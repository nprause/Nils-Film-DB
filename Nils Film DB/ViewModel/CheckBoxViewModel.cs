using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nils_Film_DB.ViewModel
{
    //
    // This is a very basic class to implement databound custom Checkboxes
    //
    class CheckBoxViewModel : ViewModel
    {       
        //Constructor: New Checkboxes are checked by default
        public CheckBoxViewModel(bool check = true)
        {
            IsChecked = check;
        }

        // Checkbox Title
        private string title;
        public string Title {
            get { return title; }
            set { title = value; }
        }

        // Ischecked?
        private bool isChecked;
        public bool IsChecked
        {
            get { return isChecked; }
            set { isChecked = value; }
        }
    }
}
