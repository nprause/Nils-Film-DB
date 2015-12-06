using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Nils_Film_DB.Helper;

namespace Nils_Film_DB.ViewModel
{
    class ScanViewModel : ViewModel
    {
        //
        // Interaction logic for Scan View. 
        // Scans a file system for media files, using methods provided by the FileScan class, and transmitts the results to the MainWindowViewModel
        //

        // Shows a new Window
        private Window win;
        public ScanViewModel()
        {         
            win = new Window();
            win.Content = this;
            win.Height = 400;
            win.Width = 600;
            win.Show();
        }


        // The result of the file system scan comes as a DataTable. 
        // This DataTable is transmitted to the MainWindowViewModel via the Mediator after a successfull scanning operation.
        private DataTable scanResults;
        public DataTable ScanResults
        {
            get { return scanResults; }
            set
            {
                if (value != scanResults)
                {
                    scanResults = value;
                    OnPropertyChanged("ScanResults");
                }
            }
        }

        // The FileScan class proviedes methods to scan the file system for video files and retrieve metadata
        private Filescan newscan = new Filescan();

        // Textboxes für path and regular expression
        private string textboxPath = @"F:\";
        public string TextboxPath
        {
            get { return textboxPath; }
            set { textboxPath = value; }
        }

        private string textboxReg = @"%name ?(%orig) ?[%jahr %land? - %igno?]";
        public string TextboxReg
        {
            get { return textboxReg; }
            set { textboxReg = value; }
        }
    
        // Giant Output TextBlock
        private string textBlockOutput = 
              "Bitte Pfad zur Filmsammlung und regulären Ausdruck eingeben.\n\n"
            + "%titel:\tTitel des Films\n"
            + "%orig:\tOriginaltitel\n"
            + "%jahr:\tErscheinungsjahr\n"
            + "%land:\tProdunktionsland\n"
            + "%igno:\tWird ignoriert\n"
            + "?...?:\tIn Fragezeichen eingschlossene Bereiche werden verwendet falls sie vorhanden sind.";              
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
                return new RelayCommand(param => CloseWindow());
            }
        }
     
        public void CloseWindow()
        {         
            win.Close();
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
            if (ButtonScanContent == "Scan")
            {
                ButtonScanIsEnabled = false;
                StartScan();
            }
            else
            {
                Mediator.NotifyColleagues("Metadata", ScanResults);
                CloseWindow();
            }
        }


        // Buttons Failures and Successes
        private bool buttonFailIsEnabled = false;
        public bool ButtonFailIsEnabled
        {
            get { return buttonFailIsEnabled; }
            set
            {
                if (value != buttonFailIsEnabled)
                {
                    buttonFailIsEnabled = value;
                    OnPropertyChanged("ButtonFailIsEnabled");
                }
            }
        }

        private bool buttonSuccessIsEnabled = false;
        public bool ButtonSuccessIsEnabled
        {
            get { return buttonSuccessIsEnabled; }
            set
            {
                if (value != buttonSuccessIsEnabled)
                {
                    buttonSuccessIsEnabled = value;
                    OnPropertyChanged("ButtonSuccessIsEnabled");
                }
            }
        }

        public ICommand ButtonFail
        {
            get
            {
                return new RelayCommand(param => buttonFail());
            }
        }

        public ICommand ButtonSuccess
        {
            get
            {
                return new RelayCommand(param => buttonSuccess());
            }
        }

        private void buttonFail()
        {
            FailViewModel fvm = new FailViewModel(newscan.Failures);
        }

        private void buttonSuccess()
        {
            SuccessViewModel svm = new SuccessViewModel(ScanResults);          
        }

        private bool buttonsResultVisibility = false;
        public bool ButtonsResultVisibility
        {
            get { return buttonsResultVisibility; }
            set
            {
                if (value != buttonsResultVisibility)
                {
                    buttonsResultVisibility = value;
                    OnPropertyChanged("ButtonsResultVisibility");
                }
            }
        }


        // Backgroundworker is used to do stuff in a new thread.
        BackgroundWorker bkw = new BackgroundWorker();

        private void InitializeBackgroundWorker()
        {
            bkw.DoWork += new DoWorkEventHandler(bkw_DoWork);
            bkw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bkw_RunWorkerCompleted);
            bkw.ProgressChanged += new ProgressChangedEventHandler(bkw_ProgressChanged);
            bkw.WorkerReportsProgress = true;
        }

        private void bkw_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            try
            {
                scanResults = newscan.Deepscan(worker);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Deepscan: " + ex.ToString());
            }
        }

        // The ProgressChanged Event is called from the FileScan.Deepscan method and unpdates the progress bar
        private void bkw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {           
            ProgressValue = e.ProgressPercentage;
            ProgressText = (e.ProgressPercentage + 1).ToString() + " Dateien gescannt";          
        }

        private void bkw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                //resultLabel.Text = "Canceled!";
            }
            else if (e.Error != null)
            {
                //resultLabel.Text = "Error: " + e.Error.Message;
            }
            else
            {
                // This gets executed when the Backgroundworker completes its task without error

                // Change Scan Button to Accept Button and enable ist
                ButtonScanContent = "Übernehmen";
                ButtonScanIsEnabled = true;

                // Give feedback about Scan results in giant TextBox
                TextBlockOutput = "Scan abgeschlossen.\n"
                + newscan.NumberVideos + " Videodateien gefunden.\n"
                + newscan.NumberSuccess + " erfolgreiche Anwendungen des regulären Ausdrucks.\n\n"
                + "Misserfolge und Erfolge können über die entsprechenden Buttons angezeigt werden.";

                // Activate Buttons Fail and/or Success if viable
                ButtonsResultVisibility = true;
                if (newscan.NumberSuccess > 0)
                    ButtonSuccessIsEnabled = true;
                if (newscan.NumberSuccess < newscan.NumberVideos)
                    ButtonFailIsEnabled = true;
            }
        }

        // Starts the Backgroundworker to do the Deep Scan
        public void Startscan()
        {
            InitializeBackgroundWorker();
            bkw.RunWorkerAsync();
        }

      
        // Starts the scan progress. The FileScan class provides methods for the file system scan.
        private void StartScan()
        {
            // RegEval analyses the user given regular expression. Returns an error message if the expression is not valid.
            string errorReg = newscan.RegEval(TextboxReg);
            if (errorReg == null)
            {
                ProgressMax = newscan.Fastscan(TextboxPath) - 1;
                if (ProgressMax > 200)
                    TextBlockOutput = "Scanne " + ProgressMax + " Dateien. Dies könnte etwas dauern.";
                else
                    TextBlockOutput = "Scanne " + ProgressMax + " Dateien...";
                Startscan();
            }
            else MessageBox.Show(errorReg);
        }
    }
 
}
