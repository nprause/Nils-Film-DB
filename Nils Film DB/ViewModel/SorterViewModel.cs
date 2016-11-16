using System.Windows.Input;

namespace Nils_Film_DB.ViewModel
{
    class SorterViewModel : ViewModel
    {
        // Constructor opens winmdow
        public SorterViewModel(dynamic w)
        {
            winHelper = w;
            winID = winHelper.Open(this, 600, 400);
        }

        // Textbox for path
        private string textboxPath = @"F:\";
        public string TextboxPath
        {
            get { return textboxPath; }
            set { textboxPath = value; }
        }

        // Giant Output TextBlock
        private string textBlockOutput = "Textbox output";

        public string TextBlockOutput
        {
            get { return textBlockOutput; }
            set
            {
                if (textBlockOutput != value)
                {
                    textBlockOutput = value;
                    OnPropertyChanged("TextBlockOutput");
                }
            }
        }


        // Progressbar. Maximum Value, Current Value and displayed Text
        private int progressMax;
        public int ProgressMax
        {
            get { return progressMax; }
            set
            {
                if (progressMax != value)
                {
                    progressMax = value;
                    OnPropertyChanged("ProgressMax");
                }
            }
        }

        private int progressValue;
        public int ProgressValue
        {
            get { return progressValue; }
            set
            {
                if (progressValue != value)
                {
                    progressValue = value;
                    OnPropertyChanged("ProgressValue");
                }
            }
        }

        private string progressText;
        public string ProgressText
        {
            get { return progressText; }
            set
            {
                if (progressText != value)
                {
                    progressText = value;
                    OnPropertyChanged("ProgressText");
                }
            }
        }

        // Buttons Cancel and Scan
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

        private string buttonScanContent = "Scan";
        public string ButtonScanContent
        {
            get
            {
                return buttonScanContent;
            }
            set
            {
                if (value != buttonScanContent)
                {
                    buttonScanContent = value;
                    OnPropertyChanged("ButtonScanContent");
                }
            }
        }

        private bool buttonScanIsEnabled = true;
        public bool ButtonScanIsEnabled
        {
            get { return buttonScanIsEnabled; }
            set
            {
                if (value != buttonScanIsEnabled)
                {
                    buttonScanIsEnabled = value;
                    OnPropertyChanged("ButtonScanIsEnabled");
                }
            }
        }

        public ICommand ButtonScan
        {
            get
            {
                return new RelayCommand(param => buttonScan());
            }
        }

        private void buttonScan()
        {
            
        }
    }
}
