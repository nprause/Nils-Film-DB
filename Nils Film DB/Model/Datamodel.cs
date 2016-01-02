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

                // Check if Tables "Filme", "Versionen" and "[username]" are in the DataBase. If not, they are created. 
                // This is done by just trying to create the Table and ignoring errors that would arise when the tables are already present.
                // Naming Convention: Column names that begin with an '_' are not shown in the apllication main window. They can however be used in searches and may be added later. 
                try
                {
                    MyConn.Execute("CREATE TABLE Filme (Nr INTEGER NOT NULL AUTO_INCREMENT, Titel VARCHAR (100), Originaltitel VARCHAR (100), _Titel_alt TEXT, Jahr VARCHAR (4), Land VARCHAR (40), Regisseur VARCHAR (255), _Cast TEXT, Genre VARCHAR (255), Rating VARCHAR (10), _TMDb_Id VARCHAR (10), _IMDB_Id VARCHAR (10), _Poster VARCHAR (40), Hinzugefügt DATE, _Synchro BOOL DEFAULT FALSE, PRIMARY KEY (Nr));");
                }
                catch
                { }
                try
                {
                    MyConn.Execute("CREATE TABLE Versionen (V_Nr INTEGER NOT NULL AUTO_INCREMENT, Nr INTEGER, Auflösung VARCHAR (20), Typ VARCHAR (40), Codec VARCHAR (40), Audio VARCHAR (100), Länge VARCHAR (20), Dateigröße VARCHAR (20), Dateiendung VARCHAR (20), Hinzugefügt DATE, PRIMARY KEY (V_Nr), FOREIGN KEY (Nr) REFERENCES Filme (Nr));");
                }
                catch
                { }
                try
                {
                    MyConn.Execute("CREATE TABLE " + username + " (U_Nr INTEGER NOT NULL AUTO_INCREMENT, V_Nr INTEGER, PRIMARY KEY (U_Nr), FOREIGN KEY (V_Nr) REFERENCES Versionen (V_Nr));");
                }
                catch
                { }
                MyConn.Close();
                return username + ": Verbunden zu " + login[0] as string;
            }
            else return "Verbindungsfehler";
        }


        // Returns a list of DataTables containing the data on the SQL Server MyConn. Without an argument all data is returned.
        // Alternatively a search string with list of columns to be searched and a list of tables which content is not to be shown can be given
        public List<DataTable> GetTables(string search = null, List<string> search_columns = null, List<string> tab_exclude = null, bool versions = false)
        {
            List<DataTable> tables = new List<DataTable>();
            if (tab_exclude == null)
                tab_exclude = new List<string>();
            MyConn.Open();

            DataTable db = MyConn.GetSchema("Tables");

            // The colums of the "Filme" and the "Versionen" tables are retrieved from the database.
            // The tablenames of the user tables are stored in table_names.
            List<string> table_names = new List<string>();
            table_names.Add("Filme");
            string columns_movies = "", columns_versions = "";
            db = MyConn.GetSchema("Columns");
            foreach (DataRow dr in db.Rows)
            {
                if (dr[2].ToString().ToLower() == "filme" )
                {
                    if (!dr[3].ToString().Contains("Nr") && dr[3].ToString()[0] != '_')
                        columns_movies += "Filme." + dr[3] + ", ";
                }
                else if (dr[2].ToString().ToLower() == "versionen")
                {
                    if (!dr[3].ToString().Contains("Nr") && dr[3].ToString()[0] != '_')
                        columns_versions += "Versionen." + dr[3] + ", ";
                }
                else if (!table_names.Contains(dr[2].ToString()))
                {
                    table_names.Add(dr[2].ToString());
                    if (dr[2].ToString() == username.ToLower())
                        userID = table_names.Count() - 1;
                }
            }
            columns_movies = columns_movies.Remove(columns_movies.LastIndexOf(","));
            columns_versions = columns_versions.Remove(columns_versions.LastIndexOf(","));
            string cmd;

            // The data is retrieved from the server
            foreach (string tab in table_names)
            {
                // This SQL Command returns all data on the database for the current table.
                if (tab == "Filme")
                    cmd = "SELECT DISTINCT " + columns_movies + " FROM (";
                else
                    cmd = "SELECT DISTINCT Titel, " + columns_versions + " FROM ((";

                foreach (string table in tab_exclude)
                {
                    if (table != tab)
                        cmd += "(";
                }
                cmd += tab;

                if (tab == "Filme")
                {
                    cmd += " JOIN Versionen ON Versionen.Nr=Filme.Nr)";
                }
                if (tab != "Filme")
                {
                    cmd += " JOIN Versionen ON Versionen.V_Nr=" + tab + ".V_Nr) JOIN Filme ON Filme.Nr=Versionen.Nr)";
                }

                // If movies or versions from certain collections are to be excluded
                if (tab_exclude.Count() > 0)
                {
                    if (versions)
                    {
                        foreach (string table in tab_exclude)
                        {
                            if (table != tab)
                                cmd += " LEFT JOIN " + table + " ON Versionen.V_Nr=" + table + ".V_Nr)";
                        }
               
                            cmd += " WHERE (";
                            foreach (string table in tab_exclude)
                            {
                                cmd += table + ".V_Nr IS NULL AND ";
                            }
                            cmd = cmd.Remove(cmd.LastIndexOf("AND"));
                            cmd += ")";
                        
                    }             
                    else
                    {
                        if (tab == "Filme")
                        {
                            cmd = "SELECT " +  columns_movies + " FROM Filme";
                            foreach (string table in tab_exclude)
                                cmd += " LEFT JOIN (" + table + " JOIN Versionen AS " + table + "_vers ON " + table + ".V_Nr=" + table + "_vers.V_Nr) ON Filme.Nr=" + table + "_vers.Nr";
                            cmd += " WHERE (";
                            foreach (string table in tab_exclude)
                                cmd += table + "_vers.Nr IS NULL AND ";
                            cmd = cmd.Remove(cmd.LastIndexOf("AND"));
                            cmd += ")";
                        }

                        else
                        {
                            cmd = "SELECT DISTINCT Titel, " + columns_versions + " FROM (Filme JOIN Versionen ON (Filme.Nr=Versionen.Nr) LEFT JOIN " + tab + " ON (" + tab + ".V_Nr=Versionen.V_Nr)) WHERE (" + tab + ".U_Nr IS NOT NULL";
                            foreach (string table in tab_exclude)
                                cmd += " AND Versionen.Nr NOT IN (SELECT Filme.Nr FROM Filme JOIN Versionen ON Filme.Nr=Versionen.Nr JOIN " + table + " ON " + table + ".V_Nr=Versionen.V_Nr)";
                            cmd += ")";

                        }
                    }
                }
                
                // If search options are specified the command is extended accordingly.
                if (search != null && search != "" && search_columns.Count() > 0)
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
                List<string> parameter = new List<string>();
                List<string> parameterValue = new List<string>();
                parameter.Add("@suche");
                parameterValue.Add("%" + search + "%");
                dt = MyConn.GetData(cmd, parameter, parameterValue);
                dt.TableName = tab;
                tables.Add(dt);

            }
            MyConn.Close();
            return tables;
        }

        // Returns the DataTable with name 'table'. The result can be filtered by gving a list of columns and values.
        public DataTable GetTable(string table, List<string> columns = null, List<string> values = null, List<string> returnColumns = null)
        {
            MyConn.Open();

            // Creat string from list returnColumns
            string rc = "";
            if (returnColumns != null)
            {
                foreach (string col in returnColumns)
                    rc += col + ",";
                if (rc.LastIndexOf(',') != -1)
                    rc = rc.Remove(rc.LastIndexOf(','));
                if (rc.Length == 0)
                    rc = "*";
            }
            else rc = "*";

            string cmd = "SELECT " + rc + " FROM " + table;
            if (columns != null)
            {
                if (columns.Count() > 0)
                {
                    cmd += " WHERE ";
                    for (int i=0; i<columns.Count(); ++i)
                    {
                        cmd += columns[i] + " LIKE '%" + values[i] + "%'"; 
                    }
                }
            }
            DataTable result = MyConn.GetData(cmd); 
            MyConn.Close();
            return result;
        }

        public void AddData(DataTable newdata)
        {
            try
            {            
                MyConn.Open();
                // Get data from database               
                DataTable movies = MyConn.GetData("SELECT * FROM Filme");
                DataTable versions = MyConn.GetData("SELECT * FROM Versionen");

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
                    DataView dw = new DataView(movies);
                    dw.Sort = "Titel";
                    duplikate = movies.Select("Titel = '" + titel.Replace("'", "''") + "' AND Jahr = '" + jahr.Replace("'", "''") + "'");

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
                    DataRow dr_movie = movies.NewRow();
                    dr_movie["Titel"] = newdata.Rows[i]["Titel"];
                    dr_movie["Originaltitel"] = newdata.Rows[i]["Originaltitel"];
                    dr_movie["Jahr"] = newdata.Rows[i]["Jahr"];
                    dr_movie["Land"] = newdata.Rows[i]["Land"];

                    DataRow dr_version = versions.NewRow();
                    dr_version["Auflösung"] = newdata.Rows[i]["Auflösung"];
                    dr_version["Typ"] = newdata.Rows[i]["Typ"];
                    dr_version["Codec"] = newdata.Rows[i]["Codec"];
                    dr_version["Audio"] = newdata.Rows[i]["Audio"];
                    dr_version["Länge"] = newdata.Rows[i]["Länge"];
                    dr_version["Dateigröße"] = newdata.Rows[i]["Dateigröße"];
                    dr_version["Dateiendung"] = newdata.Rows[i]["Dateiendung"];
                    
                    // If movie is not yet in Database it is included. Else it is checked if the user already has this version of the movie in the database.
                    if (duplikate.Length == 0)
                    {
                        command = "INSERT INTO Filme VALUES (null , @title, @original, null, @year, @country, null, null, null, null, null, null, null, CURDATE(), FALSE);";
                        MyConn.Execute(command, parameter, parameterValue);
                        id = MyConn.GetLastId();
                        dr_movie["Nr"] = id;

                        command = "INSERT INTO Versionen VALUES (null," + id +
                                ", @res, @type, @codec, @audio, @length, @size, @ending, CURDATE());";
                        MyConn.Execute(command, parameter, parameterValue);
                        id = MyConn.GetLastId();
                        dr_version["V_Nr"] = id;

                        command = "INSERT INTO " + username.ToLower() + " VALUES (null," + id + ");";
                        MyConn.Execute(command, parameter, parameterValue);

                        // Also add to olddata to prevent another insertion of the same movie
                        movies.Rows.Add(dr_movie);
                        versions.Rows.Add(dr_version);
                    }
                    // Movie is already in the database. Check if version is in database and if user already has it.
                    else
                    {
                        // Get the primary key of the movie
                        int fk = Convert.ToInt32(duplikate[0][0]);
                        string res = newdata.Rows[i][4].ToString();
                        string codec = newdata.Rows[i][6].ToString();
                        string length = newdata.Rows[i][8].ToString();
                        string size = newdata.Rows[i][9].ToString();

                        // Check for this version of movie. It is consiered unique by resolution, codec, length and size.
                        DataView dw2 = new DataView(versions);
                        dw2.Sort = "Dateigröße";
                        duplikate = versions.Select("Dateigröße = '" + size + "' AND Auflösung = '" + res + "' AND Codec = '" + codec + "' AND Länge = '" + length + "'");

                        if (duplikate.Length == 0)
                        {
                            command = "INSERT INTO Versionen VALUES (null, " + fk +
                                ", @res, @type, @codec, @audio, @length, @size, @ending, CURDATE());";
                            MyConn.Execute(command, parameter, parameterValue);
                            id = MyConn.GetLastId();
                            dr_version["V_Nr"] = id;

                            command = "INSERT INTO " + username.ToLower() + " VALUES (null," + id + ");";
                            MyConn.Execute(command, parameter, parameterValue);

                            // also add to olddata to prevent another insertion of this version
                            versions.Rows.Add(dr_version);
                        }
                        else
                        {
                            fk = Convert.ToInt32(duplikate[0][0]);
                            command = "INSERT INTO " + username.ToLower() + " (V_Nr) (SELECT * FROM (SELECT (" + fk + ")) AS tmp WHERE NOT EXISTS (SELECT V_Nr FROM " + username.ToLower() + " WHERE V_Nr=" + fk + "));";
                            MyConn.Execute(command, parameter, parameterValue);
                        }
                    }
                }
                // Commit SQL commands and unlock Tables
                MyConn.Execute("COMMIT");
                MyConn.Execute("UNLOCK TABLES");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            MyConn.Close();
        }

        public string GetValue(string table, string returncolumn, string column, string value)
        {
            MyConn.Open();
            string cmd = "SELECT " + returncolumn + " FROM " + table + " WHERE " + column + "=" + value;
            DataTable dt = MyConn.GetData(cmd);
            MyConn.Close();
            if (dt.Rows.Count > 0)
            {
                string val = dt.Rows[0][0].ToString();
                return val;
            }
            else return null;    
        }

        /* Check if a value exists in table
        public bool Exists(string table, string column, string value)
        {
            MyConn.Open();
            string cmd = "SELECT " + column + " FROM " + table + " WHERE " + column + "=" + value;
            DataTable dt = MyConn.GetData(cmd);
            MyConn.Close();
            if (dt.Rows.Count > 0)
                return true;
            else return false;
        } */

        // Deletes a row from table where the column has the value
        public void DeleteRow(string table, string column, string value)
        {
            MyConn.Open();
            string cmd = "DELETE FROM " + table + " WHERE " + column + " = " + value;
            // If a row cannot be deleted beacuse of a foreign key restraint, nothing happens.
            try
            {
                MyConn.Execute(cmd);
            }
            catch { };
            MyConn.Close();
        }

        // Update DataRow of given table in database
        public void UpdateRow(string table, DataRow dr, int pk)
        {
            MyConn.Open();
            List<string> parameter = new List<string>();
            List<string> parameterValue = new List<string>();
            string cmd = "UPDATE " + table + " SET ";
            for (int i = 0; i < dr.ItemArray.Count(); ++i)
            {
                if (dr.Table.Columns[i].ColumnName != "Hinzugefügt")
                {
                    if (dr.Table.Columns[i].ColumnName == "_Synchro")
                    {
                        cmd += dr.Table.Columns[i].ColumnName + "=" + dr[i] + ",";
                    }
                    else if (dr[i].ToString() != "")               
                    {
                        cmd += dr.Table.Columns[i].ColumnName + "=@param" + i + ",";
                        parameter.Add("@param" + i);
                        parameterValue.Add(dr[i].ToString());
                    }
                }
            }
            cmd = cmd.Remove(cmd.LastIndexOf(","));
            cmd += " WHERE Nr=" + pk;
            MyConn.Execute(cmd, parameter, parameterValue);

            MyConn.Close();
        }

        // Look for values of a DataRow in a given Table and return the primary key if found.
        public int GetPrimaryKey(DataRow dr,  string table, string column)
        {
            MyConn.Open();
            List<string> parameter = new List<string>();
            List<string> parameterValue = new List<string>();
            string cmd = "SELECT " + column + " FROM " + table + " WHERE (";
            for (int i = 0; i < dr.ItemArray.Count(); ++i)
            {
                if (dr.Table.Columns[i].ColumnName != "Hinzugefügt" )
                {
                    if (table != "Versionen" || (table == "Versionen" && dr.Table.Columns[i].ColumnName != "Titel"))
                    {
                        if (dr[i].ToString() != "")
                        {
                            cmd += dr.Table.Columns[i].ColumnName + "=@param" + i + " AND ";
                            parameter.Add("@param" + i);
                            parameterValue.Add(dr[i].ToString());
                        }
                    }
                }
            }
            cmd = cmd.Remove(cmd.LastIndexOf("AND"));
            cmd += ")";
            DataTable dt = MyConn.GetData(cmd, parameter, parameterValue);
            MyConn.Close();
            return Convert.ToInt16(dt.Rows[0][0]);         
        }
    }

}
