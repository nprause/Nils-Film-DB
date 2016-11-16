using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Net;
using Nils_Film_DB.Helper;
using Nils_Film_DB.DataAccess;

namespace Nils_Film_DB.ViewModel
{
    class OnlineDataViewModel : ViewModel
    {
        TMDbConnection TMDb = new TMDbConnection();

        // Constructor
        public OnlineDataViewModel(WindowHelper w, DataTable dt)
        {
            Data = dt;
            winHelper = w;
            winID = winHelper.Open(this, 600, 400);
            Mediator.Register("Choice", returnChoice);
        }

        private IPAddress get_ip()
        {
            return IPAddress.Parse("127.0.0.1");
        }

        // TextBox for the API key
        private string apiKey;
        public string ApiKey
        {
            get { return apiKey; }
            set
            {
                if (value != apiKey)
                {
                    apiKey = value;
                    OnPropertyChanged("ApiKey");
                }
            }
        }

        // Data display
        private DataTable data;
        public DataTable Data
        {
            get { return data; }
            set
            {
                if (value != data)
                {
                    data = value;
                    OnPropertyChanged("Data");
                }
            }
        }

        // Buttons IsEnabled
        private bool buttonAcceptIsEnabled = false;
        public bool ButtonAcceptIsEnabled
        {
            get { return buttonAcceptIsEnabled; }
            set
            {
                if (value != buttonAcceptIsEnabled)
                {
                    buttonAcceptIsEnabled = value;
                    OnPropertyChanged("ButtonAcceptIsEnabled");
                }
            }
        }

        private bool buttonStartIsEnabled = true;
        public bool ButtonStartIsEnabled
        {
            get { return buttonStartIsEnabled; }
            set
            {
                if (value != buttonStartIsEnabled)
                {
                    buttonStartIsEnabled = value;
                    OnPropertyChanged("ButtonStartIsEnabled");
                }
            }
        }

        //Button Commands
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

        public ICommand ButtonStart
        {
            get
            {
                return new RelayCommand(param => start());
            }
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
            foreach (DataRow dr in Data.Rows)
            {
                if ( Convert.ToBoolean(dr["tmdb_synchro"]) == true)
                {
                    List<object> args = new List<object>();
                    args.Add(dr);
                    args.Add(dr[0]);
                    Mediator.NotifyColleagues("ChangeRow", args);
                }
            }
            closeWindow();
        }

        private void returnChoice(object args)
        {
            string id = (args as List<object>)[0].ToString();
            DataRow dr = (args as List<object>)[1] as DataRow;
            populate(dr, id);          
        }

        async void start()
        {
           ButtonStartIsEnabled = false; 
           foreach (DataRow dr in Data.Rows)
           {
               string title = dr[1].ToString();
               string o_title = dr[2].ToString();
               string year = dr[4].ToString();
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
               string res;
               while ((res = await TMDb.Search(get_ip(), ApiKey, title, year)).Contains("Your request count"))
               {
                   System.Threading.Thread.Sleep(2000);
               }
               if (res.Contains("\"total_results\":0"))
               {
                   while ((res = await TMDb.Search(get_ip(),ApiKey, o_title, year)).Contains("Your request count"))
                   {
                       System.Threading.Thread.Sleep(2000);
                   }
               }
               List<string[]> ids = parseID(res);
               if (ids.Count == 1)
               {
                   populate(dr, ids[0][0]);
                   
               }
               if (ids.Count > 1)
               {
                   int match = 0, match_c = 0;
                   for (int i=0; i<ids.Count(); ++i) 
                   {
                       if (ids[i][2] == title)
                       {
                           ++match_c;
                           match = i;
                       }
                   }
                   if (match_c == 1)
                       populate(dr, ids[match][0]);
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
                           populate(dr, ids[match][0]);
                       else
                       {
                           OnlineDataChoiceViewModel ovm = new OnlineDataChoiceViewModel(winHelper, ids, dr);
                       }
                   }
                   else
                   {
                       OnlineDataChoiceViewModel ovm = new OnlineDataChoiceViewModel(winHelper, ids, dr);
                   }
               }
               if (ids.Count == 0)
               {
                   OnlineDataChoiceViewModel ovm = new OnlineDataChoiceViewModel(winHelper, null, dr);
               }
           }
           ButtonAcceptIsEnabled = true;
           ButtonStartIsEnabled = true;
        }

        async void populate(DataRow dr, string id)
        {
            string details;
            while ((details = await TMDb.MovieDetail(get_ip(), id, ApiKey)).Contains("Your request count"))
            {
                System.Threading.Thread.Sleep(2000);
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
                    //dr["director"] = dts[10];
                    dr["genre"] = dts[0];
                    //dr["actor"] = dts[9];
                    dr["rating"] = Convert.ToDecimal(dts[7]) / 10;
                    dr["tmdb_id"] = id;
                    dr["imdb_id"] = dts[1];
                    dr["poster_path"] = dts[3];
                    dr["tmdb_synchro"] = true;
            }
            else
            {
                //OnlineDataChoiceViewModel ovm = new OnlineDataChoiceViewModel(winHelper, null, dr);
            }
        }

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

        private List<string> parseDetails(string st)
        {
            
                string result;
                List<string> details = new List<string>();
                string genres = "", imdb_id, original_title, poster_path, countries = "", release, title, rating, alt_titles = "", cast = "", director = "";
                int ind1, ind2;

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

                       
              
            //MessageBoxViewModel mbv = new MessageBoxViewModel(winHelper, result);
            return details;
        }
      
    }
}
