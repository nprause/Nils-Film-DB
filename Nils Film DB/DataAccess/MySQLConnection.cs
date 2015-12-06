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
            return "server=" + server + ";userid=" + username + ";password=" + SecureStringConverter.SecureStringToString(password) + ";database=" + database;
        }

        // Login info can be given directly or as List<objects>. 
        // This method also test whether a connection can be established and retruns the result as bool. 
        public bool SetLoginInfo(string serv, string un, SecureString pw, string db)
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
                return true;
            }
            catch
            {
                return false;
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

        public bool SetLoginInfo(List<object> login)
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
                return true;
            }
            catch
            {
                return false;
            }
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
            catch
            {
                return false;
            }
        }

        // Return DataTable with SQL search string comm. Parameters can be given to prevent SQL injections. 
        public DataTable GetData(string comm, string parameter, string parameterValue)
        {
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand cmd = new MySqlCommand();
            cmd.CommandText = comm;            
            cmd.Connection = connection;
            adapter.SelectCommand = cmd;
            adapter.SelectCommand.Parameters.AddWithValue(parameter, parameterValue);
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
