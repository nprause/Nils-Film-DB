using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nils_Film_DB.Helper
{
    static class Logger
    {
        public static void Write(string text)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NP_FilmDB\film_db.log";
            System.IO.StreamWriter log_file = new System.IO.StreamWriter(path, true);
            log_file.WriteLine(DateTime.Now + "  "  + text);
            log_file.Close();
        }
    }
}
