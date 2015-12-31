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
      
        public ScanViewModel(dynamic w)
        {        
            winHelper = w;
            winID = winHelper.Open(this, 600, 400);
            InitializeBackgroundWorker();
        }


        // The result of the file system scan comes as a DataTable. 
        // This DataTable is transmitted to the MainWindowViewModel via the Mediator after a successfull scanning operation.
        private DataTable scanResults = new DataTable();
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

        private DataTable tempScanResults = new DataTable();

        // Path and filename of failed scan attempts are stored in a List<string> so that the user can try again with another regular expression
        private List<string> failures = new List<string>();
        public List<string> Failures
        {
            get { return failures; }
            set
            {
                if (value != failures)
                {
                    failures = value;
                    OnPropertyChanged("Failures");
                }
            }
        }


        // List of filenames of successful scans
        private List<string> successes = new List<string>();

        // FreshScan is true when a fresh scan is done, and false when a refined scan is done
        bool freshScan;

        // The FileScan class proviedes methods to scan the file system for video files and retrieve metadata
        private Filescan newscan = new Filescan();

        // Control of the TabItems
        private bool tabItemFailIsEnabled = false;
        public bool TabItemFailIsEnabled
        {
            get { return tabItemFailIsEnabled; }
            set
            {
                if (value != tabItemFailIsEnabled)
                {
                    tabItemFailIsEnabled = value;
                    OnPropertyChanged("TabItemFailIsEnabled");
                }
            }
        }

        private bool tabItemSuccessIsEnabled = false;
        public bool TabItemSuccessIsEnabled
        {
            get { return tabItemSuccessIsEnabled; }
            set
            {
                if (value != tabItemSuccessIsEnabled)
                {
                    tabItemSuccessIsEnabled = value;
                    OnPropertyChanged("TabItemSuccessIsEnabled");
                }
            }
        }

        private int tabSelectedIndex = 0;
        public int TabSelectedIndex
        {
            get { return tabSelectedIndex; }
            set
            {
                if (value != tabSelectedIndex)
                {                
                    tabSelectedIndex = value;
                    OnPropertyChanged("TabSelectedIndex");
                }
            }
        }

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
            + "?...?:\tIn Fragezeichen eingschlossene Bereiche werden verwendet falls sie vorhanden sind.\n\n"
            + "Hinweis: Die Verwendung der ? kann leicht zu Fehlidentifikationen führen. Es ist auch möglich mehrere Scans mit unterschiedlichen regulären Audrücken hintereinander auszuführen.";  
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
            //win.Close();
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
                ButtonScanIsEnabled = false;
                freshScan = true;
                StartScan();          
        }

        public ICommand ButtonNewScan
        {
            get
            {
                return new RelayCommand(param => buttonNewScan());
            }
        }

        private void buttonNewScan()
        {
            freshScan = false;
            ButtonScanIsEnabled = false;
            StartScan(Failures);       
        }

        public ICommand ButtonAccept
        {
            get
            {
                return new RelayCommand(param => buttonAccept());
            }
        }

        private void buttonAccept()
        {         
                Mediator.NotifyColleagues("Metadata", ScanResults);
                closeWindow();
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
                // Do the metadata scan              
                tempScanResults = newscan.Deepscan(worker, e.Argument as List<string>);
            }
            catch (Exception ex)
            {
                MessageBoxViewModel mbox = new MessageBoxViewModel(winHelper, "Deepscan: " + ex.ToString());
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

                // Add results to ScanResults if not already in the Table
                if (ScanResults.Rows.Count > 0)
                {
                    var merge = tempScanResults.AsEnumerable().Union(ScanResults.Copy().AsEnumerable(), DataRowComparer.Default);                  
                    ScanResults.Rows.Clear();
                    foreach (DataRow dr in merge)
                    {
                        ScanResults.Rows.Add(dr.ItemArray);
                    }                 
                }
                else               
                    ScanResults = tempScanResults.Copy();
               
                // Set Failures for Failure Tab
                if (freshScan)
                {
                    Failures = new List<string>(newscan.Failures);
                    successes = new List<string>(newscan.Successes);
                }
                else
                {               
                    foreach (string file in newscan.Successes)
                    {
                        if (!successes.Contains(file))
                            successes.Add(file);
                    }
                    Failures = new List<string>(newscan.Failures);
                    foreach (string file in Failures)
                    {
                        if (successes.Contains(file))
                        {
                            Failures.Remove(file);
                        }
                    }

                }
               
                // Enable Buttons and Tabs and change to output tab       
                ButtonScanIsEnabled = true;
                if (newscan.NumberSuccess > 0)
                    TabItemSuccessIsEnabled = true;
                if (newscan.NumberSuccess < newscan.NumberVideos)
                    TabItemFailIsEnabled = true;
                TabSelectedIndex = 0;

                // Give feedback about Scan results in giant TextBox
                TextBlockOutput = "Scan abgeschlossen.\n"
                + newscan.NumberVideos + " Videodateien gefunden.\n"
                + newscan.NumberSuccess + " erfolgreiche Anwendungen des regulären Ausdrucks.\n\n"
                + "Zum erneut scannen, erneut Scan drücken.\n"
                + "Im zweiten Tab können Fehlschläge begutachtet und erneut gescannt werden.\n"
                + "Im dritten Tab können Erfolge begutachtet und übernommen werden.";               
            }
        }

        // Starts the Backgroundworker to do the Deep Scan
        public void Startscan(List<string> files)
        {
            bkw.RunWorkerAsync(files);
        }

      
        // Starts the scan progress. The FileScan class provides methods for the file system scan.
        private void StartScan(List<string> files = null)
        {
            // RegEval analyses the user given regular expression. Returns an error message if the expression is not valid.
            string errorReg = newscan.RegEval(TextboxReg);
            int number_files = newscan.Fastscan(TextboxPath);
            if (number_files > -1)
            {
                if (errorReg == null)
                {
                    if (files != null)
                        ProgressMax = files.Count();
                    else
                    {
                        ProgressMax = newscan.Fastscan(TextboxPath) - 1;
                    }
                    if (ProgressMax > 5000)
                        TextBlockOutput = "Scanne " + ProgressMax + " Dateien. Geh dir besser erstmal einen Kaffee holen.";
                    else if (ProgressMax > 1000)
                        TextBlockOutput = "Scanne " + ProgressMax + " Dateien. Dies könnte etwas dauern.";
                    else
                        TextBlockOutput = "Scanne " + ProgressMax + " Dateien...";
                    Startscan(files);
                }
                else
                {
                    MessageBoxViewModel mbox = new MessageBoxViewModel(winHelper, errorReg);
                    ButtonScanIsEnabled = true;
                }
            }
            else
            {
                MessageBoxViewModel mbox = new MessageBoxViewModel(winHelper, TextboxPath + ":\nPfad nicht gefunden");
                ButtonScanIsEnabled = true;
            }
        }
    }
 
}
