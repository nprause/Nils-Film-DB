using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Nils_Film_DB.DataAccess;

namespace Nils_Film_DB.Model
{
    class Datamodel
    {

        //
        // The DataModel is a bridge between the SQL Server and the MainWindowViewModel. It does not hold any data but provides methods for data access.
        //

        // Each Datamodel creates an instance of MySQLConnection. 
        private MySQLConnection myconn = new MySQLConnection();
        public MySQLConnection MyConn
        {
            get { return myconn; }
            set { myconn = value; }
        }

        // The name of the Database
        private string db_name = "film_db";

        // Name of current user and an numerical id that represents which table in DataBase is connected to the user
        private string username;
        private int userID;

        // Adds the name of tha database to the login information and transmits it to the MySQLConnection instance. 
        // Additionally the DataBase structure is created if not yet present.
        public string SetLogin(List<object> login)
        {
            username = login[1] as string;
            login.Add(db_name);
            bool connection = MyConn.SetLoginInfo(login);

            if (connection)
            {
                MyConn.Open();
                // Check if a DataBase with the name db_name is present on MyConn. If not, it is created.
                DataTable db = MyConn.GetSchema("DataBases");
                bool exists = false;
                foreach (DataRow row in db.Rows)
                {
                    if (row.ItemArray[1].ToString() == db_name)
                        exists = true;
                }
                if (!exists)
                {
                    try
                    {
                        MyConn.Execute("CREATE DATABASE " + db_name);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Datenbank " + db_name + " ist nicht vorhanden und konnte nicht erstellt werden.\n" + e.ToString());
                    }
                }

                // Check if Tables "filme" and "[username]" are in the DataBase. If not, they are created. 
                // This is done by just trying to create the Table and ignoring errors that would arise when the tables are already present.
                try
                {
                    MyConn.Execute("CREATE TABLE Filme (Nr INTEGER NOT NULL AUTO_INCREMENT, Titel VARCHAR (100), Originaltitel VARCHAR (100), Jahr VARCHAR(10), Land VARCHAR(40), PRIMARY KEY (Nr));");
                }
                catch
                { }
                try
                {
                    MyConn.Execute("CREATE TABLE " + login[1] as string + " (V_Nr INTEGER NOT NULL AUTO_INCREMENT, Nr INTEGER, Titel VARCHAR (100), Auflösung VARCHAR (20), Typ VARCHAR (40), Codec VARCHAR (40), Audio VARCHAR (100), Länge VARCHAR (20), Dateigröße VARCHAR (20), Dateiendung VARCHAR (20), PRIMARY KEY (V_Nr), FOREIGN KEY (Nr) REFERENCES Filme (Nr));");
                }
                catch
                { }
                MyConn.Close();
                return username + ": Verbunden zu " + login[0] as string;
            }
            else return "Verbindungsfehler";
        }


        // Returns a list of DataTables containing the data on the SQL Server MyConn. Without an argument all data is returned.
        // Alternatively a search string with list of columns to be searched and a list of tables which content is not to be shown (in any table)
        public List<DataTable> GetTables(string search = null, List<string> search_columns = null, List<string> tab_exclude = null)
        {
            List<DataTable> tables = new List<DataTable>();
            if (tab_exclude == null)
                tab_exclude = new List<string>();
            MyConn.Open();

            DataTable db = MyConn.GetSchema("Tables");
            int ind = 0;

            // The names of all tables of the database are stored in table_names. This is used for the SQL search command.
            List<string> table_names = new List<string>();
            for (int i = 0; i < db.Rows.Count; ++i)
            {
                table_names.Add(db.Rows[i][2].ToString());
                if (db.Rows[i][2].ToString() == username.ToLower())
                    userID = i;
            }


            // For each Table the data is retrieved from the server
            foreach (string tab in table_names)
            {
                // This SQL Command returns all data on the database for the current table.
                string cmd = "SELECT DISTINCT " + tab + ".* FROM (";


                foreach (string table in tab_exclude)
                {
                    if (table != tab)
                        cmd += "(";
                }

                cmd += tab;

                if (tab != "filme")
                {
                    cmd += " JOIN Filme ON Filme.Nr=" + tab + ".Nr)";
                }
                else
                    cmd += ")";

                foreach (string table in tab_exclude)
                {
                    if (table != tab)
                        cmd += " LEFT JOIN " + table + " ON Filme.Nr=" + table + ".Nr)";
                }

                if (tab_exclude.Count() > 0)
                {
                    cmd += " WHERE (";
                    foreach (string table in tab_exclude)
                    {
                        cmd += table + ".Nr IS NULL)";
                    }
                }




                // If search options are specified the command is extended accordingly.
                if (search != null && search_columns.Count() > 0)
                {
                    if (tab_exclude.Count() > 0)
                        cmd += " AND (";
                    else
                        cmd += " WHERE (";

                    for (int i = 0; i <= search_columns.Count() - 1; ++i)
                    {
                        cmd += search_columns[i] + " LIKE @suche OR ";
                    }
                    cmd = cmd.Substring(0, cmd.LastIndexOf("OR")) + ")";
                }


                // Data is retrieved from MySQL Server and added to List
                DataTable dt = new DataTable();
                string parameter = "@suche";
                string parameterValue = "%" + search + "%";
                dt = MyConn.GetData(cmd, parameter, parameterValue);
                dt.TableName = tab;
                tables.Add(dt);

                ++ind;
            }
            MyConn.Close();
            return tables;
        }

        public void AddData(DataTable newdata)
        {
            try
            {
                // Get data from database
                List<DataTable> olddata = this.GetTables();

                MyConn.Open();

                // Lock Database    
                MyConn.Execute("LOCK TABLES Filme WRITE, " + username.ToLower() + " WRITE");

                string command;
                int id;

                // SQL commands are transmitted to the Server as strings. When user generated data is part of these strings they can botch up the command.
                // This can even be done on purpose by the user to change the the actual SQL command (SQL injection).
                // To prevent this, the MySQL connector provides the Parameters property, which masks everything that could be misinterpreted as an SQL statement.
                List<string> parameter = new List<string>();
                parameter.Add("@title");
                parameter.Add("@original");
                parameter.Add("@year");
                parameter.Add("@country");
                parameter.Add("@res");
                parameter.Add("@type");
                parameter.Add("@codec");
                parameter.Add("@audio");
                parameter.Add("@length");
                parameter.Add("@size");
                parameter.Add("@ending");

                // All SQL commands are transmitted in a TRANSACTION block. That increases the speed drastically. 
                MyConn.Execute("START TRANSACTION");

                // Loop through all movies in newdata and add them to database if not already there.
                for (int i = 0; i < newdata.Rows.Count; ++i)
                {
                    string titel = @newdata.Rows[i][0].ToString();
                    string jahr = @newdata.Rows[i][2].ToString();

                    //Checking if movies are already in Database. This is done locally. A movie is consiered unique by title and year. 
                    DataRow[] duplikate;
                    DataView dw = new DataView(olddata[0]);
                    dw.Sort = "Titel";
                    duplikate = olddata[0].Select("Titel = '" + titel.Replace("'", "''") + "' AND Jahr = '" + jahr.Replace("'", "''") + "'");

                    // Assigning parameters
                    List<string> parameterValue = new List<string>();
                    parameterValue.Add(newdata.Rows[i][0].ToString());
                    parameterValue.Add(newdata.Rows[i][1].ToString());
                    parameterValue.Add(newdata.Rows[i][2].ToString());
                    parameterValue.Add(newdata.Rows[i][3].ToString());
                    parameterValue.Add(newdata.Rows[i][4].ToString());
                    parameterValue.Add(newdata.Rows[i][5].ToString());
                    parameterValue.Add(newdata.Rows[i][6].ToString());
                    parameterValue.Add(newdata.Rows[i][7].ToString());
                    parameterValue.Add(newdata.Rows[i][8].ToString());
                    parameterValue.Add(newdata.Rows[i][9].ToString());
                    parameterValue.Add(newdata.Rows[i][10].ToString());

                    // Splitting the scan data into one row for the general movie information and one for the user data
                    DataRow dr_movie = olddata[0].NewRow();
                    dr_movie["Titel"] = newdata.Rows[i]["Titel"];
                    dr_movie["Originaltitel"] = newdata.Rows[i]["Originaltitel"];
                    dr_movie["Jahr"] = newdata.Rows[i]["Jahr"];
                    dr_movie["Land"] = newdata.Rows[i]["Land"];

                    DataRow dr_user = olddata[userID].NewRow();
                    dr_user["Titel"] = newdata.Rows[i]["Titel"];
                    dr_user["Auflösung"] = newdata.Rows[i]["Auflösung"];
                    dr_user["Typ"] = newdata.Rows[i]["Typ"];
                    dr_user["Codec"] = newdata.Rows[i]["Codec"];
                    dr_user["Audio"] = newdata.Rows[i]["Audio"];
                    dr_user["Länge"] = newdata.Rows[i]["Länge"];
                    dr_user["Dateigröße"] = newdata.Rows[i]["Dateigröße"];
                    dr_user["Dateiendung"] = newdata.Rows[i]["Dateiendung"];

                    // If movie is not yet in Database it is included. Else it is checked if the user already has this version of the movie in the database.
                    if (duplikate.Length == 0)
                    {
                        command = "INSERT INTO Filme VALUES (null , @title, @original, @year, @country);";
                        MyConn.Execute(command, parameter, parameterValue);
                        id = MyConn.GetLastId();

                        command = "INSERT INTO " + username.ToLower() + " VALUES (null, (SELECT LAST_INSERT_ID()), "
                        + "@title, @res, @type, @codec, @audio, @length, @size, @ending);";
                        MyConn.Execute(command, parameter, parameterValue);

                        // Also add to olddata to prevent another insertion of the same movie
                        dr_movie["Nr"] = id;
                        olddata[0].Rows.Add(dr_movie);
                        olddata[userID].Rows.Add(dr_user);
                    }
                    // Movie is already in the database. Check if user has this version in database. If not, it is added.
                    else
                    {
                        // Get the primary key of the movie
                        int fk = Convert.ToInt32(duplikate[0][0]);
                        string res = newdata.Rows[i][4].ToString();
                        string codec = newdata.Rows[i][6].ToString();
                        string length = newdata.Rows[i][8].ToString();
                        string size = newdata.Rows[i][9].ToString();

                        // Check for this version of movie. It is consiered unique by resolution, codec, length and size.
                        DataView dw2 = new DataView(olddata[userID]);
                        dw2.Sort = "Titel";
                        duplikate = olddata[userID].Select("Titel = '" + titel.Replace("'", "''") + "' AND Auflösung = '" + res + "' AND Codec = '" + codec + "' AND Länge = '" + length + "' AND Dateigröße = '" + size + "'");

                        if (duplikate.Length == 0)
                        {
                            command = "INSERT INTO " + username.ToLower() + " VALUES (null," + fk +
                                ",@title, @res, @type, @codec, @audio, @length, @size, @ending);";
                            MyConn.Execute(command, parameter, parameterValue);

                            // also add to olddata to prevent another insertion of this version
                            olddata[userID].Rows.Add(dr_user);
                        }
                    }
                }
                // Commit SQL commands
                MyConn.Execute("COMMIT");
                // Unlock Datadase
                MyConn.Execute("UNLOCK TABLES");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            MyConn.Close();
        }


        // Deletes a row from table where the column has the value
        public void DeleteRow(string table, string column, string value)
        {
            MyConn.Open();
            string cmd = "DELETE FROM " + table + " WHERE " + column + " = " + value;
            MyConn.Execute(cmd);
            MyConn.Close();
        }

        // Update DataRow of given table in database
        public void UpdateRow(string table, DataRow dr)
        {
            MyConn.Open();
            List<string> parameter = new List<string>();
            List<string> parameterValue = new List<string>();
            string cmd = "UPDATE " + table + " SET ";
            for (int i = 1; i < dr.ItemArray.Count(); ++i)
            {
                cmd += dr.Table.Columns[i].ColumnName + "=@param" + i + ",";
                parameter.Add("@param" + i);
                parameterValue.Add(dr[i].ToString());
            }
            cmd = cmd.Remove(cmd.LastIndexOf(","));
            cmd += " WHERE " + dr.Table.Columns[0].ColumnName + "=" + dr[0].ToString();
            MyConn.Execute(cmd, parameter, parameterValue);

            MyConn.Close();
        }
    }

}
