using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Net;
using System.Windows.Input;
using Newtonsoft.Json;
using Nils_Film_DB.Helper;
using Nils_Film_DB.DataAccess;
using System.IO;

/// <summary>
/// 
/// Interaction logic for Scan window
/// Scans a file system for media files, using methods provided by the FileScan class, and transmits results to the MainWindowViewModel
/// 
/// Needs serious refactoring!
/// 
/// </summary>

namespace Nils_Film_DB.ViewModel
{


    class ScanViewModel : ViewModel
    {
        private const string startText = "Bitte Pfad zur Filmsammlung und regulären Ausdruck eingeben.\n\n"
            + "%titel:\tTitel des Films\n"
            + "%orig:\tOriginaltitel\n"
            + "%jahr:\tErscheinungsjahr\n"
            + "%land:\tProdunktionsland\n"
            + "%igno:\tWird ignoriert\n"
            + "?...?:\tIn Fragezeichen eingschlossene Bereiche werden verwendet falls sie vorhanden sind.\n\n"
            + "Hinweis: Die Verwendung der ? kann leicht zu Fehlidentifikationen führen. Es ist auch möglich mehrere Scans mit unterschiedlichen regulären Audrücken hintereinander auszuführen.";

        int counter = 1;
        TMDbConnection TMDb = new TMDbConnection();

        DataTable db_movies;
        DataTable db_versions;

        DataTable scanResults = new DataTable();

        IPAddress[] ips;
        int ip_curr = 0;


        // Constructor: Open window and initialize DataTables
        public ScanViewModel(dynamic w, DataTable movies, DataTable versions)
        {
            winHelper = w;
            winID = winHelper.Open(this, 600, 400);
            InitializeBackgroundWorker();

            Mediator.Register("Choice", add_scan_result);
            Mediator.Register("Choice_scan_drop", remove_choice);

            db_movies = movies;
            db_versions = versions;

            dataDisplay_ini();
            scanResults_ini();
        }

        // Get the next IP Address from a list
        private IPAddress get_ip()
        {
            if (ip_curr == ips.Length - 1)
                ip_curr = 0;
            else ++ip_curr;
            return ips[ip_curr];
        }

        // Initialize DataTable Object for display of scan results
        private void dataDisplay_ini()
        {
            dataDisplay.Columns.Add("movie_id", typeof(int));
            dataDisplay.Columns.Add("Titel", typeof(string));
            dataDisplay.Columns.Add("Originaltitel", typeof(string));
            dataDisplay.Columns.Add("Release", typeof(string));
            dataDisplay.Columns.Add("Land", typeof(string));
            dataDisplay.Columns.Add("Regisseur", typeof(string));
            dataDisplay.Columns.Add("Genre", typeof(string));
            dataDisplay.Columns.Add("Rating", typeof(string));
            dataDisplay.Columns.Add("Auflösung", typeof(string));
            dataDisplay.Columns.Add("Typ", typeof(string));
            dataDisplay.Columns.Add("Codec", typeof(string));
            dataDisplay.Columns.Add("Audio", typeof(string));
            dataDisplay.Columns.Add("Länge", typeof(string));
            dataDisplay.Columns.Add("Dateigröße", typeof(string));
            dataDisplay.Columns.Add("Dateiendung", typeof(string));
            dataDisplay.Columns.Add("Pfad", typeof(string));
        }

        // Initioalize DataTable of scan results
        private void scanResults_ini()
        {
            scanResults.Columns.Add("movie_id", typeof(int));
            scanResults.Columns.Add("title", typeof(string));
            scanResults.Columns.Add("original_title", typeof(string));
            scanResults.Columns.Add("release_date", typeof(DateTime));
            scanResults.Columns.Add("country", typeof(string));
            scanResults.Columns.Add("directors", typeof(string));
            scanResults.Columns.Add("genre", typeof(string));
            scanResults.Columns.Add("rating", typeof(string));
            scanResults.Columns.Add("resolution", typeof(string));
            scanResults.Columns.Add("type", typeof(string));
            scanResults.Columns.Add("codec", typeof(string));
            scanResults.Columns.Add("audio", typeof(string));
            scanResults.Columns.Add("length", typeof(string));
            scanResults.Columns.Add("size", typeof(string));
            scanResults.Columns.Add("ending", typeof(string));
            scanResults.Columns.Add("tmdb_id", typeof(string));
            scanResults.Columns.Add("imdb_id", typeof(string));
            scanResults.Columns.Add("poster_path", typeof(string));
            scanResults.Columns.Add("alternative_titles", typeof(string));
            scanResults.Columns.Add("actors", typeof(string));
            scanResults.Columns.Add("tmdb_synchro", typeof(int));
            scanResults.Columns.Add("path", typeof(string));
        }

        // Add result of user choice from OnlineDataChoiceView
        private void add_scan_result(object args)
        {
            string id = (args as List<object>)[0].ToString();
            DataRow dr = (args as List<object>)[1] as DataRow;
            populate(dr, id);
        }

        // Remove object from OnlineDataChoice
        private void remove_choice(object args)
        {
            TmdbFailures.Remove(args as OnlineDataChoiceViewModel);
        }

        // Check if scanned version is already in results and if scanned movie is already in database.
        // Update dataDisplay and scanResults accordingly. 
        private void process_movie(DataRow meta)
        {
            ++ProgressValue;
            ProgressText = ProgressValue + " Dateien gescannt";
            DataRow[] duplicate_scanResult = new DataRow[0];

            // Check if scanned version is already in results to identify duplicates 
            if (scanResults.Rows.Count > 0)
                duplicate_scanResult = scanResults.Select("size = '" + meta[9] + "' AND length = '" + meta[8] + "' AND resolution = '" + meta[4] + "' AND codec = '" + meta[6] + "'");

            if (scanResults.Rows.Count == 0 || duplicate_scanResult.Length == 0)
            {

                // Add scanned information to dataDisplay
                DataRow dataDisplay_newRow = dataDisplay.NewRow();
                for (int i = 0; i < meta.ItemArray.Length; ++i)
                {
                    if (i < 4)
                        dataDisplay_newRow[i + 1] = meta[i];
                    else
                        dataDisplay_newRow[i + 4] = meta[i];
                }
                DataDisplay.Rows.Add(dataDisplay_newRow);

                // Add scanned information to scanResults
                DataRow scanResults_newRow = scanResults.NewRow();
                for (int i = 0; i < meta.ItemArray.Length; ++i)
                {
                    if (i < 4 && i != 2)
                        scanResults_newRow[i + 1] = meta[i];
                    else
                        scanResults_newRow[i + 4] = meta[i];
                }

                if (meta[2].ToString() != "")
                    scanResults_newRow[3] = new DateTime(Convert.ToInt16(meta[2]), 1, 1);
                else
                    scanResults_newRow[3] = new DateTime();
                scanResults_newRow[20] = 0;
                scanResults.Rows.Add(scanResults_newRow);



                // Check if scanned movie is already in database                     
                DataView dw = new DataView(db_movies);
                dw.Sort = "title";
                DataRow[] duplicate_movie = db_movies.Select("title = '" + meta[0].ToString().Replace("'", "''") + "' AND release_date LIKE '%" + meta[2].ToString().Replace("'", "''") + "%'");
                // If not get additional infos from tmdb
                if (duplicate_movie.Length == 0)
                {
                    DataRow new_movie = db_movies.NewRow();
                    new_movie["movie_id"] = -counter;
                    scanResults.Rows[scanResults.Rows.Count - 1]["movie_id"] = -counter;
                    dataDisplay.Rows[dataDisplay.Rows.Count - 1]["movie_id"] = -counter;
                    ++counter;
                    new_movie["title"] = meta[0];
                    new_movie["original_title"] = meta[1];
                    new_movie["release_date"] = meta[2];
                    get_tmdb(new_movie);
                }
                // Else add movie information to scanResults and dataDisplay
                else
                {
                    scanResults.Rows[scanResults.Rows.Count - 1]["movie_id"] = duplicate_movie[0][0].ToString();
                    dataDisplay.Rows[dataDisplay.Rows.Count - 1]["movie_id"] = duplicate_movie[0][0].ToString();
                    update_results(duplicate_movie[0]);
                }
            }
        }

        // Fetch additional movie metedata from TMDB
        async void get_tmdb(DataRow movie)
        {
            string title = movie[1].ToString();
            string original_title = movie[2].ToString();
            string year = movie[3].ToString();

            // If year is not a valid value, a search without the year is conducted.
            try
            {
                int year_int = Convert.ToInt16(year);
                if (year_int < 1900 || year_int > 2050)
                    year = null;
            }
            catch
            {
                year = null;
            }

            // Fetch reults
            string res = "";

            try
            {
                // Try to fetch movie information until successful.
                bool success = false;
                while (success == false)
                {
                    try
                    {
                        while ((res = await TMDb.Search(get_ip(), TextboxApi, title, year)).Contains("Your request count")) ;
                        success = true;
                    }
                    catch
                    {

                    }
                }


                // If no results found try again with original title instead of german title
                if (res.Contains("\"total_results\":0"))
                {
                    success = false;
                    while (success == false)
                    {
                        try
                        {
                            while ((res = await TMDb.Search(get_ip(), TextboxApi, original_title, year)).Contains("Your request count")) ;
                            success = true;
                        }
                        catch
                        {

                        }
                    }
                }

                // If exactly one result was found populate dataDisplay and scanResults
                List<string[]> ids = parseID(res);
                if (ids.Count == 1)
                {
                    populate(movie, ids[0][0]);

                }

                // If more results were found check for exact original title matches
                if (ids.Count > 1)
                {
                    int match = 0, match_c = 0;
                    for (int i = 0; i < ids.Count(); ++i)
                    {
                        if (ids[i][2] == title)
                        {
                            ++match_c;
                            match = i;
                        }
                    }
                    if (match_c == 1)
                        populate(movie, ids[match][0]);
                    // If no match is found check for an exact match with german title
                    else if (match_c == 0)
                    {
                        for (int i = 0; i < ids.Count(); ++i)
                        {
                            if (ids[i][1] == title)
                            {
                                ++match_c;
                                match = i;
                            }
                        }
                        if (match_c == 1)
                            populate(movie, ids[match][0]);
                        // Else add to OnlineDataChoiceViewModel for user input
                        else
                        {
                            OnlineDataChoiceViewModel ovm = new OnlineDataChoiceViewModel(ids, movie);
                            TmdbFailures.Add(ovm);
                            Failures.Add(movie[11].ToString());
                        }
                    }
                    else
                    {
                        OnlineDataChoiceViewModel ovm = new OnlineDataChoiceViewModel(ids, movie);
                        TmdbFailures.Add(ovm);
                        Failures.Add(movie[11].ToString());
                    }
                }
                // If no results were found add to OnlineDataChoiceViewModel for user input.
                if (ids.Count == 0)
                {
                    OnlineDataChoiceViewModel ovm = new OnlineDataChoiceViewModel(null, movie);
                    TmdbFailures.Add(ovm);
                    Failures.Add(movie[11].ToString());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

        // Get tmdb_id, original title, title and release date of all matches in TMDB fetch.
        private List<string[]> parseID(string st)
        {
            List<string[]> id = new List<string[]>();
            int ind = 0;
            string tmdb_id, title, original_title, release;
            while ((ind = st.IndexOf("\"release_date\":", ind + 1)) != -1)
            {
                tmdb_id = st.Substring(st.IndexOf("\"id\":", ind) + 5, st.IndexOf("\"", st.IndexOf("\"id\":", ind) + 5) - st.IndexOf("\"id\":", ind) - 6);
                original_title = st.Substring(st.IndexOf("\"original_title\":", ind) + 18, st.IndexOf("\"", st.IndexOf("\"original_title\":", ind) + 18) - st.IndexOf("\"original_title\":", ind) - 18);
                title = st.Substring(st.IndexOf("\"title\":", ind) + 9, st.IndexOf("\"", st.IndexOf("\"title\":", ind) + 9) - st.IndexOf("\"title\":", ind) - 9);
                release = st.Substring(st.IndexOf("\"release_date\":", ind) + 16, st.IndexOf("\"", st.IndexOf("\"release_date\":", ind) + 16) - st.IndexOf("\"release_date\":", ind) - 16);
                id.Add(new string[] { tmdb_id, original_title, title, release });
            }
            return id;
        }

        // Parse movie metadata from detailled movie scan. 
        // TODO: Change to some nicer format. 
        private List<string> parseDetails(string st)
        {
            string result;
            List<string> details = new List<string>();
            string genres = "", imdb_id, original_title, poster_path, countries = "", release, title, rating, alt_titles = "", cast = "", director = "";
            int ind1, ind2;
            // var md = JsonConvert.DeserializeObject(st);

            try
            {
                ind1 = st.IndexOf("\"genres\":"); ind2 = st.IndexOf("]", ind1);
                while ((ind1 = st.IndexOf("\"name\":", ind1 + 1)) < ind2 && ind1 != -1)
                {
                    genres += st.Substring(ind1 + 8, st.IndexOf("\"", ind1 + 8) - ind1 - 8) + ", ";
                }
                if (genres.LastIndexOf(", ") != -1)
                    genres = genres.Remove(genres.LastIndexOf(", "));
                result = "Genres: " + genres + "\n";
                details.Add(genres);

                imdb_id = st.Substring(st.IndexOf("\"imdb_id\":") + 11, st.IndexOf("\"", st.IndexOf("\"imdb_id\":") + 11) - st.IndexOf("\"imdb_id\":") - 11);
                result += "Imdb ID: " + imdb_id + "\n";
                details.Add(imdb_id);

                original_title = st.Substring(st.IndexOf("\"original_title\":") + 18, st.IndexOf("\"", st.IndexOf("\"original_title\":") + 18) - st.IndexOf("\"original_title\":") - 18);
                result += "Originaltitel: " + original_title + "\n";
                details.Add(original_title);

                poster_path = st.Substring(st.IndexOf("\"poster_path\":") + 16, st.IndexOf("\"", st.IndexOf("\"poster_path\":") + 16) - st.IndexOf("\"poster_path\":") - 16);
                result += "Poster: " + poster_path + "\n";
                details.Add(poster_path);

                ind1 = st.IndexOf("\"production_countries\":"); ind2 = st.IndexOf("]", ind1);
                while ((ind1 = st.IndexOf("\"iso_3166_1\":", ind1 + 1)) < ind2 && ind1 != -1)
                {
                    countries += st.Substring(ind1 + 14, st.IndexOf("\"", ind1 + 14) - ind1 - 14) + ", ";
                }
                if (countries.LastIndexOf(", ") != -1)
                    countries = countries.Remove(countries.LastIndexOf(", "));
                result += "Countries: " + countries + "\n";
                details.Add(countries);

                release = st.Substring(st.IndexOf("\"release_date\":") + 16, st.IndexOf("\"", st.IndexOf("\"release_date\":") + 16) - st.IndexOf("\"release_date\":") - 16);
                result += "Datum: " + release + "\n";
                details.Add(release);

                ind1 = st.IndexOf("\"translations\":"); ind2 = st.IndexOf("]", ind1);
                if (st.IndexOf("Deutsch") == -1 || (st.IndexOf("\"name\":Deutsch") > ind2))
                {
                    details.Add(null);
                }
                else
                {
                    title = st.Substring(st.IndexOf("\"title\":") + 9, st.IndexOf("\"", st.IndexOf("\"title\":") + 9) - st.IndexOf("\"title\":") - 9);
                    result += "Titel: " + title + "\n";
                    details.Add(title);
                }

                rating = st.Substring(st.IndexOf("\"vote_average\":") + 15, st.IndexOf(",", st.IndexOf("\"vote_average\":") + 15) - st.IndexOf("\"vote_average\":") - 15);
                result += "Rating: " + rating + "\n";
                details.Add(rating);

                ind1 = st.IndexOf("titles"); ind2 = st.IndexOf("]", ind1);
                while ((ind1 = st.IndexOf("\"title\":", ind1 + 1)) < ind2 && ind1 != -1)
                {
                    alt_titles += st.Substring(ind1 + 9, st.IndexOf("\"", ind1 + 9) - ind1 - 9) + ", ";
                }
                if (alt_titles.LastIndexOf(", ") != -1)
                    alt_titles = alt_titles.Remove(alt_titles.LastIndexOf(", "));
                result += "Alternative Titel: " + alt_titles + "\n";
                details.Add(alt_titles);

                ind1 = st.IndexOf("\"cast\":"); ind2 = st.IndexOf("]", ind1);
                while ((ind1 = st.IndexOf("\"name\":", ind1 + 1)) < ind2 && ind1 != -1)
                {
                    cast += st.Substring(ind1 + 8, st.IndexOf("\"", ind1 + 8) - ind1 - 8) + ", ";
                }
                if (cast.LastIndexOf(", ") != -1)
                    cast = cast.Remove(cast.LastIndexOf(", "));
                result += "Schauspieler: " + cast + "\n";
                details.Add(cast);

                ind1 = st.IndexOf("\"crew\":");
                while ((ind1 = st.IndexOf("\"job\":\"Director\"", ind1 + 1)) != -1)
                {
                    ind1 = st.IndexOf("\"name\":", ind1);
                    director += st.Substring(ind1 + 8, st.IndexOf("\"", ind1 + 8) - ind1 - 8) + ", ";
                }
                if (director.LastIndexOf(", ") != -1)
                    director = director.Remove(director.LastIndexOf(", "));
                result += "Regisseur(e): " + director + "\n";
                details.Add(director);
            }
            catch { }

            return details;
        }

        // When tmdb ID of movie was found check for detailed movie metadata
        async void populate(DataRow dr, string id)
        {
            string details = "";
            bool success = false;
            while (success == false)
            {
                try
                {
                    while ((details = await TMDb.MovieDetail(get_ip(), id, TextboxApi)).Contains("Your request count")) ;
                    success = true;
                }
                catch
                { }
            }

            if (!details.Contains("The resource you requested could not be found"))
            {
                List<string> dts = new List<string>();

                dts = parseDetails(details);

                if (dts[6] != null)
                    dr["title"] = dts[6];
                dr["original_title"] = dts[2];
                dr["alternative_titles"] = dts[8];
                dr["release_date"] = dts[5];
                dr["country"] = dts[4];
                dr["directors"] = dts[10];
                dr["genre"] = dts[0];
                dr["actors"] = dts[9];
                dr["rating"] = Convert.ToDecimal(dts[7]) / 10;
                dr["tmdb_id"] = id;
                dr["imdb_id"] = dts[1];
                dr["poster_path"] = dts[3];
            }
            else
            {
                MessageBox.Show("Fehler beim holen der Details von tmdb ID " + id);
            }
            update_results(dr);
        }

        // Add results of TMDB fetch to dataDisplay and scanResults
        private void update_results(DataRow movie_data)
        {

            string id = movie_data[0].ToString();
            // Update movie information in scanResults
            foreach (DataRow dr in scanResults.Rows)
            {
                if (dr["movie_id"].ToString() == id)
                {
                    for (int i = 0; i < dr.ItemArray.Length - 1; ++i)
                    {
                        if (i <= 7 && i != 3)
                        {
                            dr[i] = movie_data.ItemArray[i];
                        }
                        dr[3] = DateTime.Parse(movie_data.ItemArray[3].ToString());
                        if (i >= 15)
                        {
                            dr[i] = movie_data.ItemArray[i - 7];
                        }
                    }
                    dr[20] = 1;
                }
            }
            // Update movie information in dataDisplay
            foreach (DataRow dr in dataDisplay.Rows)
            {
                if (dr["movie_id"].ToString() == id)
                {
                    for (int i = 0; i < dr.ItemArray.Length - 1; ++i)
                    {
                        if (i <= 7)
                        {
                            dr[i] = movie_data.ItemArray[i];
                        }
                    }
                }
            }
            if (Failures.Contains(movie_data[11].ToString()))
            {
                Failures.Remove(movie_data[11].ToString());
            }

        }

        // DataTable that is shown on the UI. Not all metatdata are shown.
        private DataTable dataDisplay = new DataTable();
        public DataTable DataDisplay
        {
            get { return dataDisplay; }
            set
            {
                if (value != dataDisplay)
                {
                    dataDisplay = value;
                    OnPropertyChanged("DataDisplay");
                }
            }
        }

        // Ambigious or failed TMDB scans added to OnlineDataChoice
        private ObservableCollection<OnlineDataChoiceViewModel> tmdbFailures = new ObservableCollection<OnlineDataChoiceViewModel>();
        public ObservableCollection<OnlineDataChoiceViewModel> TmdbFailures
        {
            get { return tmdbFailures; }
            set
            {
                if (value != tmdbFailures)
                {
                    tmdbFailures = value;
                    OnPropertyChanged("TmdbFailures");
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

        // FreshScan is true when a fresh scan is done, and false when a refined scan is done.
        bool freshScan;

        // The FileScan class provides methods to scan the file system for video files and retrieve metadata.
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

        private bool tabItemTmdbIsEnabled = false;
        public bool TabItemTmdbIsEnabled
        {
            get { return tabItemTmdbIsEnabled; }
            set
            {
                if (value != tabItemTmdbIsEnabled)
                {
                    tabItemTmdbIsEnabled = value;
                    OnPropertyChanged("TabItemTmdbIsEnabled");
                }
            }
        }

        private bool tabItemSortIsEnabled = false;
        public bool TabItemSortIsEnabled
        {
            get { return tabItemSortIsEnabled; }
            set
            {
                if (value != tabItemSortIsEnabled)
                {
                    tabItemSortIsEnabled = value;
                    OnPropertyChanged("TabItemSortIsEnabled");
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

        // Textboxes for api key, path and regular expression
        private string textboxApi;
        public string TextboxApi
        {
            get { return textboxApi; }
            set { textboxApi = value; }
        }

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
        private string textBlockOutput = startText;
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

        // Textboxes for Sort option
        private string regSort = @"D:\Filme\%titel (%orig) [%jahr %land - %regi]\%titel (%orig) [%jahr %land - %regi]";
        public string RegSort
        {
            get { return regSort; }
            set
            {
                if (regSort != value)
                {
                    regSort = value;
                    OnPropertyChanged("RegSort");
                }
            }
        }

        private const string startTextSort = "Bitte (absoluten oder relativen) Pfad und regulären Ausdruck eingeben.\n\n"
            + "%titel:\tTitel des Films\n"
            + "%orig:\tOriginaltitel\n"
            + "%jahr:\tErscheinungsjahr\n"
            + "%land:\tProdunktionsland\n"
            + "%regi:\tRegisseur\n";


        private string textBlockSort = startTextSort;
        public string TextBlockSort
        {
            get { return textBlockSort; }
            set
            {
                if (textBlockSort != value)
                {
                    textBlockSort = value;
                    OnPropertyChanged("TextBlockSort");
                }
            }
        }

        // Button Sort
        public ICommand ButtonSort
        {
            get
            {
                return new RelayCommand(param => buttonSort());
            }
        }

        private void buttonSort()
        {
            TextBlockSort = "Verschiebe ... ";
            foreach (DataRow dr in scanResults.Rows)
            {
                try
                {
                    TextBlockSort += "\n" + dr["path"].ToString() ;

                    // Check if path is absolute
                    bool isabsolute = false;
                    foreach (DriveInfo di in DriveInfo.GetDrives())
                    {
                        if (RegSort.ToUpper().StartsWith(di.Name))
                            isabsolute = true;
                    }

                    String sortpath = "";

                    // Remove invalid filesystem characters
                    String[] pathArray;
                    if (isabsolute)
                        pathArray = RegSort.Substring(3).Split('\\');
                    else
                        pathArray = RegSort.Split('\\');
                    for (int i = 0; i < pathArray.Length; ++i)
                    {
                        pathArray[i] = String.Join("", pathArray[i].Split(Path.GetInvalidFileNameChars()));
                    }

                    // Combine clean path
                    if (isabsolute)
                        sortpath = RegSort.Substring(0, 2) + String.Join("\\", pathArray) + "." + dr["ending"].ToString();
                    else
                        sortpath = dr["path"].ToString().Substring(0, dr["path"].ToString().LastIndexOf('\\') + 1) + String.Join("\\", pathArray) + "." + dr["ending"].ToString();


                    // Replace regular expressions with scanned values 
                    sortpath = sortpath.Replace("%titel", String.Join("", dr["title"].ToString().Split(Path.GetInvalidFileNameChars())));
                    sortpath = sortpath.Replace("%orig", String.Join("", dr["original_title"].ToString().Split(Path.GetInvalidFileNameChars())));
                    sortpath = sortpath.Replace("%jahr", ((DateTime)dr["release_date"]).Year.ToString());
                    sortpath = sortpath.Replace("%land", dr["country"].ToString());
                    sortpath = sortpath.Replace("%regi", dr["directors"].ToString());

                    // Create Directory
                    if (!Directory.Exists(sortpath.Substring(0, sortpath.LastIndexOf('\\'))))
                    {
                        Directory.CreateDirectory(sortpath.Substring(0, sortpath.LastIndexOf('\\')));
                    }

                    // Move file
                    File.Move(dr["path"].ToString(), sortpath);
                    TextBlockSort += " -> " + sortpath;
                }
                catch (Exception e)
                {
                    TextBlockSort += ": " + e.Message;
                }

                
            }
            TextBlockSort += "\n\nFertig!";

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
            TabItemSuccessIsEnabled = false;
            TabItemFailIsEnabled = false;
            TabItemTmdbIsEnabled = false;
            ButtonScanIsEnabled = false;
            freshScan = true;
            TextBlockOutput = "Fahre Festplatte hoch... ";
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
            TabItemSuccessIsEnabled = false;
            TabItemFailIsEnabled = false;
            TabItemTmdbIsEnabled = false;
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
            Mediator.NotifyColleagues("Metadata", scanResults);
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
            //tempScanResults = newscan.Deepscan(worker, e.Argument as List<string>);
            e.Result = newscan.Deepscan(e.Argument as string);
        }

        // The ProgressChanged Event is called from the FileScan.Deepscan method and updates the progress bar.
        private void bkw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //ProgressValue = e.ProgressPercentage;
            //ProgressText = (e.ProgressPercentage + 1).ToString() + " Dateien gescannt";
        }

        private void bkw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                //resultLabel.Text = "Canceled!";
            }
            else if (e.Error != null)
            {
                MessageBoxViewModel mbox = new MessageBoxViewModel(winHelper, e.Error.ToString());
                //resultLabel.Text = "Error: " + e.Error.Message;
            }
            else
            {
                if (count == 0)
                {
                    if (freshScan)
                    {
                        Failures = new List<string>();
                        successes = new List<string>();
                    }
                }
                // This is executed when the Backgroundworker completes its task without error. 
                // If the last file of the list global_files was scanned the UI is updated.   
                // TODO: Live update of the UI while file scan. The Multithreading would have to be coded a bit cleaner for that.         

                if (e.Result != null)
                {
                    // Check if scanned file is already in database
                    DataView dw_f = new DataView(db_versions);
                    dw_f.Sort = "size";
                    DataRow meta = e.Result as DataRow;
                    DataRow[] duplicate_version = db_versions.Select("size = '" + meta[9] + "' AND length = '" + meta[8] + "' AND resolution = '" + meta[4] + "' AND codec = '" + meta[6] + "'");
                    if (duplicate_version.Length == 0)
                    {
                        if (meta[0].ToString() != "")
                            process_movie(meta);
                        else
                        {
                            newscan.Successes.Remove(meta[11].ToString());
                            newscan.Failures.Add(meta[11].ToString());
                        }
                    }
                    else
                    {
                        ++newscan.NumberSort;
                        DataRow dataDisplay_newRow = dataDisplay.NewRow();
                        DataRow scanResults_newRow = scanResults.NewRow();

                        dataDisplay_newRow["movie_id"] = duplicate_version[0][1].ToString();
                        scanResults_newRow["movie_id"] = duplicate_version[0][1].ToString();
                        dataDisplay_newRow["Pfad"] = meta[11];
                        scanResults_newRow["path"] = meta[11];

                        for (int i = 4; i < meta.ItemArray.Length; ++i)
                        {
                            dataDisplay_newRow[i + 4] = meta[i];
                            scanResults_newRow[i + 4] = meta[i];
                        }
                        DataDisplay.Rows.Add(dataDisplay_newRow);
                        scanResults.Rows.Add(scanResults_newRow);

                        DataRow[] duplicate_movie = db_movies.Select("movie_id = " + duplicate_version[0][1].ToString());
                        update_results(duplicate_movie[0]);

                        ++ProgressValue;
                        ProgressText = ProgressValue + " Dateien gescannt";
                    }
                }
                else
                {
                    ++ProgressValue;
                    ProgressText = ProgressValue + " Dateien gescannt";
                }
                ++count;
                if (count < files_global.Count)
                {
                    Startscan(files_global[count]);
                }
                else
                {
                    // Set Failures for Failure Tab


                    // Enable Buttons and Tabs and change to output tab       
                    ButtonScanIsEnabled = true;
                    if (newscan.NumberSuccess > 0 || newscan.NumberSort > 0)
                    {
                        TabItemSuccessIsEnabled = true;
                        TabItemSortIsEnabled = true;
                    }
                    if (newscan.NumberSuccess < newscan.NumberVideos)
                        TabItemFailIsEnabled = true;
                    TabItemTmdbIsEnabled = true;
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
        }

        private List<string> files_global;

        private int count = 0;

        // Starts the Backgroundworker to do the Deep Scan
        public void Startscan(List<string> files)
        {
            bkw.RunWorkerAsync(files);
        }

        public void Startscan(string file)
        {
            bkw.RunWorkerAsync(file);
        }


        // Starts the scan progress. The FileScan class provides methods for the file system scan.
        private void StartScan(List<string> files = null)
        {
            try
            {
                ips = Dns.GetHostAddresses("api.themoviedb.org");
            }
            catch (Exception e)
            {
                MessageBoxViewModel mbox = new MessageBoxViewModel(winHelper, e.ToString());
            }
            // Ping of ip Adresses does not work. 
            /* foreach (IPAddress ip in ips)
            {
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(ip,1000);

                if (pingReply.Status == IPStatus.Success)
                {
                    //Server is alive
                    Logger.Write(ip.ToString() + ":  " + pingReply.RoundtripTime);
                }
                else
                    Logger.Write(ip.ToString() + ":  " + "Keine Verbindung");

            } */

            // RegEval analyses the user given regular expression. Returns an error message if the expression is not valid.
            string errorReg = newscan.RegEval(TextboxReg);
            if (errorReg == null)
            {
                if (files == null)
                    files = newscan.Fastscan(TextboxPath);
                if (files != null)
                {
                    int number_files = files.Count;
                    files_global = files;

                    ProgressMax = files.Count();
                    if (ProgressMax > 5000)
                        TextBlockOutput = "Scanne " + ProgressMax + " Dateien. Geh dir besser erstmal einen Kaffee holen.";
                    else if (ProgressMax > 1000)
                        TextBlockOutput = "Scanne " + ProgressMax + " Dateien. Dies könnte etwas dauern.";
                    else
                        TextBlockOutput = "Scanne " + ProgressMax + " Dateien...";
                    //bkw.RunWorkerAsync(files);
                    if (freshScan)
                        newscan.Reset();
                    Startscan(files[0]);
                }
                else
                {
                    MessageBoxViewModel mbox = new MessageBoxViewModel(winHelper, TextboxPath + ":\nPfad nicht gefunden");
                    ButtonScanIsEnabled = true;
                    TextBlockOutput = startText;
                }
            }
            else
            {
                MessageBoxViewModel mbox = new MessageBoxViewModel(winHelper, errorReg);
                ButtonScanIsEnabled = true;
                TextBlockOutput = startText;
            }
        }
    }
}
