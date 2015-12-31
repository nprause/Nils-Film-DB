using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Nils_Film_DB.Helper;
using Nils_Film_DB.DataAccess;

namespace Nils_Film_DB.ViewModel
{
    class OnlineDataViewModel : ViewModel
    {
        TMDbConnection TMDb = new TMDbConnection();    

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

        // Constructor
        public OnlineDataViewModel(WindowHelper w, DataTable dt)
        {
            Data = dt;
            winHelper = w;
            winID = winHelper.Open(this, 600, 400);
            Mediator.Register("Choice", returnChoice);
        }

        //Button Commands
        public ICommand ButtonCancel
        {
            get
            {
                return new RelayCommand(param => CloseWindow());
            }
        }

        public void CloseWindow()
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

        private void returnChoice(object args)
        {
            string id = (args as List<object>)[0].ToString();
            DataRow dr = (args as List<object>)[1] as DataRow;
            populate(dr, id);          
        }

        async void start()
        {
                
           foreach (DataRow dr in Data.Rows)
           {
               string title = dr[1].ToString();
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
               string res = await TMDb.Search(ApiKey, title, year);

               List<string[]> ids = parseID(res);
               if (ids.Count == 1)
               {
                   populate(dr, ids[0][0]);
                   
               }
               if (ids.Count > 1)
               {
                   OnlineDataChoiceViewModel ovm = new OnlineDataChoiceViewModel(winHelper, ids, dr);
               }
           }         
        }
        
        async void populate (DataRow dr, string id)
        {
            string details = await TMDb.MovieDetail(id, ApiKey);
            List<string> dts = parseDetails(details);
            if (dts[6] != null)
                dr["Titel"] = dts[6];
            dr["Originaltitel"] = dts[2];
            dr["_Titel_alt"] = dts[8];
            dr["Jahr"] = dts[5].Remove(4);
            dr["Land"] = dts[4];
            dr["Regisseur"] = dts[10];
            dr["Genre"] = dts[0];
            dr["_Cast"] = dts[9];
            dr["Rating"] = Convert.ToDecimal(dts[7]) / 10;
            dr["_TMDb_Id"] = id;
            dr["_IMDB_Id"] = dts[1];
            dr["_Poster"] = dts[3];
            dr["_Synchro"] = true;
        }


        private List<string[]> parseID(string st)
        {
            List<string[]> id = new List<string[]>();
            int ind = 0;
            string tmdb_id, title, release;
            while ((ind = st.IndexOf("\"release_date\":", ind + 1)) != -1)
            {
                tmdb_id = st.Substring(st.IndexOf("\"id\":", ind) + 5, st.IndexOf("\"", st.IndexOf("\"id\":", ind) + 5) - st.IndexOf("\"id\":", ind) - 6);
                title = st.Substring(st.IndexOf("\"original_title\":", ind) + 18, st.IndexOf("\"", st.IndexOf("\"original_title\":", ind) + 18) - st.IndexOf("\"original_title\":", ind) - 18);
                release = st.Substring(st.IndexOf("\"release_date\":", ind) + 16, st.IndexOf("\"", st.IndexOf("\"release_date\":", ind) + 16) - st.IndexOf("\"release_date\":", ind) - 16);    
                id.Add(new string[]{tmdb_id, title, release});
            }
            return id;
        }

        private List<string> parseDetails(string st)
        {
            string result;
            List<string> details = new List<string>();
            string genres = "", imdb_id, original_title, poster_path, countries="", release, title, rating, alt_titles="", cast="", director="";
            int ind1, ind2;
            ind1 = st.IndexOf("\"genres\":"); ind2 = st.IndexOf("]", ind1);
            while ( (ind1 = st.IndexOf("\"name\":", ind1 + 1)) < ind2 && ind1 != -1)
            {
                genres += st.Substring(ind1 + 8, st.IndexOf("\"", ind1 + 8) - ind1 - 8) + ", ";
            }
            if (genres.LastIndexOf(", ") != -1)
                genres = genres.Remove(genres.LastIndexOf(", "));
            result = "Genres: " + genres + "\n";
            details.Add(genres);

            imdb_id = st.Substring(st.IndexOf("\"imdb_id\":") + 11, st.IndexOf("\"", st.IndexOf("\"imdb_id\":") + 11) - st.IndexOf("\"imdb_id\":")-11);
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
       
            //MessageBoxViewModel mbv = new MessageBoxViewModel(winHelper, result);
            return details;
        }
      
    }
}
