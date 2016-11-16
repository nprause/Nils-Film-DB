using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MySql.Data.MySqlClient;
using Nils_Film_DB.Helper;

namespace Nils_Film_DB.DataAccess
{
    //
    // This class handels communtication with the MySQL Server. The Password is stored as a SecureString object.
    //

    class MySQLConnection
    {
        private string server;
        private string username;
        private SecureString password;
        private string database;

        private MySqlConnection connection = new MySqlConnection();
        private MySqlCommand command = new MySqlCommand();

        // The Login Information can be given as arguemnts on creation of an instance or set later with the SetLoginInfo(...) method.
        public MySQLConnection()
        {
        }
        public MySQLConnection(string serv, string un, SecureString pw, string db)
        {
            server = serv;
            username = un;
            password = pw;
            database = db;
        }

        private string connstring()
        {
            return "server=" + server + ";userid=" + username + ";password=" + SecureStringConverter.SecureStringToString(password) + ";database=" + database + ";Convert Zero Datetime=True";
        }

        // Login info can be given directly or as List<objects>. 
        // This method also test whether a connection can be established and retruns the result as bool. 
        public string SetLoginInfo(string serv, string un, SecureString pw, string db)
        {
            server = serv;
            username = un;
            password = pw;
            database = db;
            try
            {
                connection.ConnectionString = connstring();
                connection.Open();
                connection.Close();
                connection.ConnectionString = "";
                return null;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public string SetLoginInfo(List<object> login)
        {
            server = login[0] as string;
            username = login[1] as string;
            password = login[2] as SecureString;
            database = login[3] as string;
            try
            {
                connection.ConnectionString = connstring();
                connection.Open();
                connection.Close();
                connection.ConnectionString = null;
                return null;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public bool Open()
        {
            connection.ConnectionString = connstring();
            try
            {
                connection.Open();              
                return true;
            }
            catch
            {
                connection.ConnectionString = null;
                return false;
            }
        }

        public void Close()
        {        
            connection.Close();
            connection.ConnectionString = null;
        }


        // Return the Schema sc_name for the Database
        public DataTable GetSchema(string sc_name)
        {
           
            DataTable dt =  new DataTable();            
            try
            {
                dt = connection.GetSchema(sc_name);
            }
            catch { }
            return dt;
        }

        // Execute NonQuery SQL Command
        public bool Execute(string comm, List<string> parameter = null, List<string> parameterValue = null)
        {
            command.CommandText = comm;
            if (parameter != null)
            {
                command.Parameters.Clear();
                for (int i = 0; i < parameter.Count(); ++i)
                {
                    command.Parameters.AddWithValue(parameter[i], parameterValue[i]);
                }
            }           
            command.Connection = connection;
            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }

        // Execute NonQuery SQL Command single parameter version
        public bool Execute(string comm, string parameter, string parameterValue)
        {
            command.CommandText = comm;
            command.Parameters.Clear();
            command.Parameters.AddWithValue(parameter, parameterValue);
            command.Connection = connection;
            try
            {
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }

        // Return DataTable with SQL search string comm. Parameters can be given to prevent SQL injections. 
        public DataTable GetData(string comm, List<string> parameter = null, List<string> parameterValue = null)
        {
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand cmd = new MySqlCommand();
            cmd.CommandText = comm;            
            cmd.Connection = connection;  
            if (parameter != null)
            {
                cmd.Parameters.Clear();
                for (int i = 0; i < parameter.Count(); ++i)
                {
                    cmd.Parameters.AddWithValue(parameter[i], parameterValue[i]);
                }
            }
            adapter.SelectCommand = cmd;
            DataTable dt = new DataTable();
            adapter.Fill(dt);           
            return dt;
        }

        // Get Id of last inserted item
        public int GetLastId()
        {
            command.CommandText = "SELECT LAST_INSERT_ID()";
            return Convert.ToInt32(command.ExecuteScalar());
        }


    }
}

//SELECT DISTINCT CONCAT(m.title, ' (', DATE_FORMAT(m.release_date, '%Y'), ')') AS 'Titel', v.resolution 'Auflösung', v.type 'Typ', v.codec 'Codec', v.audio 'Audio', v.length 'Länge', v.size 'Größe', v.ending 'Endung', DATE_FORMAT(v.added, '%e.%m.%Y') 'Hinzugefügt' FROM movies m LEFT JOIN (SELECT movie_id, GROUP_CONCAT(name SEPARATOR ', ') 'name' FROM directors JOIN crew USING (crew_id)) d ON d.movie_id = m.movie_id LEFT JOIN(SELECT movie_id, name  FROM actors JOIN crew USING(crew_id)) a ON a.movie_id = m.movie_id JOIN versions v ON m.movie_id = v.movie_id JOIN collection c ON v.version_id = c.version_id WHERE c.user_name = 'nils' m.movie_id NOT IN (SELECT m.movie_id FROM movies m JOIN versions v USING (movie_id) JOIN collection c USING (version_id) JOIN users u USING (user_name) WHERE user_name IN ( 'nils')) ORDER BY Titel"