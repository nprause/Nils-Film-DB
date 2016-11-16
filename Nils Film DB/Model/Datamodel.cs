using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        private string db_name = "movie_db";

        // Name of current user and an numerical id that represents which table in DataBase is connected to the user
        private string username;

        // Adds the name of tha database to the login information and transmits it to the MySQLConnection instance. 
        // Additionally the DataBase structure is created if not yet present.
        public string SetLogin(List<object> login)
        {
            username = login[1] as string;
            login.Add(db_name);
            string connection = MyConn.SetLoginInfo(login);

            if (connection == null)
            {            
                return username + ": Verbunden zu " + login[0] as string;
            }
            else return connection;
        }


        // Returns the table Movies and all user Tables on the SQL Server MyConn. Without an argument all data is returned.
        // Alternatively a search string with list of columns to be searched and a list of tables which content is not to be shown can be given
        public List<DataTable> GetTables(string search = null, List<string> search_columns = null, List<string> tab_exclude = null, bool versions = false)
        {
            List<DataTable> tables = new List<DataTable>();
            if (tab_exclude == null)
                tab_exclude = new List<string>();

            MyConn.Open();
            string cmd = "SELECT DISTINCT m.title 'Titel', "
                         + "m.original_title 'Originaltitel', "
                         + "d.name 'Regisseur', "
                         + "DATE_FORMAT(m.release_date, '%m/%Y') 'Release', "
                         + "m.country 'Land', "
                         + "m.genre 'Genre', "
                         + "m.rating 'Rating', "
                         + "DATE_FORMAT(m.added, '%e.%m.%Y') 'Hinzugefügt' "
                         + "FROM movies m "
                         + "LEFT JOIN (SELECT movie_id, GROUP_CONCAT(name SEPARATOR ', ') 'name' FROM directors JOIN crew USING (crew_id) GROUP BY movie_id) d "
                         + "ON d.movie_id = m.movie_id "
                         + "LEFT JOIN(SELECT movie_id, name FROM actors JOIN crew USING(crew_id)) a "
                         + "ON a.movie_id = m.movie_id "
                         + "JOIN versions v "
                         + "ON m.movie_id = v.movie_id "
                         + "JOIN collection c "
                         + "ON c.version_id = v.version_id "
                         + "JOIN users u "
                         + "ON c.user_name = u.user_name ";
            string cmd_search = "";
            if (search != null && search != "" && search_columns.Count() > 0)
            {
                cmd_search += "WHERE ";
                for (int i = 0; i <= search_columns.Count() - 1; ++i)
                {
                    cmd_search += search_columns[i] + " LIKE @suche OR ";
                }
                cmd_search = cmd_search.Substring(0, cmd_search.LastIndexOf("OR"));
            }
            cmd += cmd_search;
            string tabs = "";
            if (tab_exclude.Count() > 0)
            {
                foreach (string tab in tab_exclude)
                {
                    tabs += "'" + tab + "', ";
                }
                tabs = tabs.Remove(tabs.LastIndexOf(","));
                if (cmd_search != "")
                    cmd += "AND ";
                else
                    cmd += "WHERE ";
                cmd += "m.movie_id NOT IN ( "
                    + "SELECT m.movie_id FROM "
                    + "movies m JOIN versions v "
                    + "USING (movie_id) "
                    + "JOIN collection c "
                    + "USING (version_id) "
                    + "JOIN users u "
                    + "USING (user_name) "
                    + "WHERE user_name IN ( ";
                cmd += tabs + ")) ";
            }
            cmd += "ORDER BY Titel";

            DataTable dt = new DataTable();
            List<string> parameter = new List<string>();
            List<string> parameterValue = new List<string>();
            parameter.Add("@suche");
            parameterValue.Add("%" + search + "%");
            dt = MyConn.GetData(cmd, parameter, parameterValue);
            dt.TableName = "Filme";
            tables.Add(dt);

            cmd = "SELECT DISTINCT user_name FROM users JOIN collection USING (user_name)";
            dt = MyConn.GetData(cmd);
            List<string> users = new List<string>();

            foreach (DataRow dr in dt.Rows)
                users.Add(dr[0].ToString());

            foreach (string user in users)
            {
                cmd = "SELECT DISTINCT CONCAT(m.title, ' (', DATE_FORMAT(m.release_date, '%Y'), ')') AS 'Titel', "
                        + "v.resolution 'Auflösung', "
                        + "v.type 'Typ', "
                        + "v.codec 'Codec', "
                        + "v.audio 'Audio', "
                        + "v.length 'Länge', "
                        + "v.size 'Größe', "
                        + "v.ending 'Endung', "
                        + "DATE_FORMAT(v.added, '%e.%m.%Y') 'Hinzugefügt' "
                        + "FROM movies m "
                        + "LEFT JOIN (SELECT movie_id, name FROM directors JOIN crew USING (crew_id)) d "
                        + "ON d.movie_id = m.movie_id "
                        + "LEFT JOIN(SELECT movie_id, name  FROM actors JOIN crew USING(crew_id)) a "
                        + "ON a.movie_id = m.movie_id "
                        + "JOIN versions v "
                        + "ON m.movie_id = v.movie_id "
                        + "JOIN collection c "
                        + "ON v.version_id = c.version_id "
                        + "WHERE c.user_name = '";
                cmd += user + "' ";
                cmd_search = "";
                if (search != null && search != "" && search_columns.Count() > 0)
                {                 
                    for (int i = 0; i <= search_columns.Count() - 1; ++i)
                    {
                        cmd_search += search_columns[i] + " LIKE @suche OR ";
                    }
                    cmd_search = cmd_search.Substring(0, cmd_search.LastIndexOf("OR"));
                    cmd += "AND " + cmd_search;
                }

                if (tab_exclude.Count() > 0)
                {
                    cmd += " AND m.movie_id NOT IN ("
                        + "SELECT m.movie_id FROM "
                        + "movies m JOIN versions v "
                        + "USING (movie_id) "
                        + "JOIN collection c "
                        + "USING (version_id) "
                        + "JOIN users u "
                        + "USING (user_name) "
                        + "WHERE user_name IN ( ";
                    cmd += tabs + ")) ";
                }
                cmd += "ORDER BY Titel";

                parameter = new List<string>();
                parameterValue = new List<string>();
                parameter.Add("@suche");
                parameterValue.Add("%" + search + "%");
                dt = MyConn.GetData(cmd, parameter, parameterValue);
                dt.TableName = user;
                tables.Add(dt);
            }


                /*
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
                */
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

        public DataTable Get_movies_full()
        {
            MyConn.Open();
            string cmd = "SELECT DISTINCT m.movie_id, "
                         + "m.title, "
                         + "m.original_title, "
                         + "DATE_FORMAT(m.release_date, '%m/%Y') 'release_date', "
                         + "m.country, "
                         + "d.name 'directors', "                                        
                         + "m.genre, "
                         + "m.rating, "
                         + "m.tmdb_id, "
                         + "m.imdb_id, "
                         + "m.poster_path, "                  
                         + "m.alternative_titles, "
                         + "a.name 'actors', "
                         + "m.tmdb_synchro "
                         + "FROM movies m "
                         + "LEFT JOIN (SELECT movie_id, GROUP_CONCAT(name SEPARATOR ', ') 'name' FROM directors JOIN crew USING (crew_id) GROUP BY movie_id) d "
                         + "ON d.movie_id = m.movie_id "
                         + "LEFT JOIN (SELECT movie_id, GROUP_CONCAT(name SEPARATOR ', ') 'name' FROM actors JOIN crew USING (crew_id)) a "
                         + "ON a.movie_id = m.movie_id "
                         + "JOIN versions v "
                         + "ON m.movie_id = v.movie_id "
                         + "JOIN collection c "
                         + "ON c.version_id = v.version_id "
                         + "JOIN users u "
                         + "ON c.user_name = u.user_name ";
            DataTable result = MyConn.GetData(cmd);
            MyConn.Close();
            return result;
        }

        public DataTable Get_versions_user()
        {
            MyConn.Open();
            string cmd = "SELECT v.version_id, "
                    + "v.movie_id, "
                    + "v.resolution, "
                    + "v.type, "
                    + "v.codec, "
                    + "v.audio, "
                    + "v.length, "
                    + "v.size, "
                    + "v.ending, "
                    + "DATE_FORMAT(v.added, '%e.%m.%Y') "
                    + "FROM movies m JOIN versions v "
                    + "ON m.movie_id = v.movie_id "
                    + "JOIN collection c "
                    + "ON v.version_id = c.version_id "
                    + "WHERE c.user_name = '";
            cmd += username + "'";
            DataTable result = MyConn.GetData(cmd);
            MyConn.Close();
            return result;
        }

        public DataTable Get_versions()
        {
            MyConn.Open();
            string cmd = "SELECT v.version_id, "
                    + "v.movie_id, "
                    + "v.resolution, "
                    + "v.type, "
                    + "v.codec, "
                    + "v.audio, "
                    + "v.length, "
                    + "v.size, "
                    + "v.ending, "
                    + "DATE_FORMAT(v.added, '%e.%m.%Y')"
                    + "FROM movies m JOIN versions v "
                    + "ON m.movie_id = v.movie_id "
                    + "JOIN collection c "
                    + "ON v.version_id = c.version_id";
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
                DataTable movies = MyConn.GetData("SELECT * FROM movies");
                DataTable versions = MyConn.GetData("SELECT * FROM versions");             

                // Lock Database    
                //MyConn.Execute("LOCK TABLES movies WRITE, versions WRITE, collection WRITE");

                string command;
                int id;

                // SQL commands are transmitted to the Server as strings. When user generated data is part of these strings they can botch up the command.
                // This can even be done on purpose by the user to change the the actual SQL command (SQL injection).
                // To prevent this, the MySQL connector provides the Parameters property, which masks everything that could be misinterpreted as an SQL statement.
                List<string> parameter = new List<string>();
                parameter.Add("@title");
                parameter.Add("@original_title");
                parameter.Add("@release_date");
                parameter.Add("@country");
                parameter.Add("@genre");
                parameter.Add("@rating");
                parameter.Add("@res");
                parameter.Add("@type");
                parameter.Add("@codec");
                parameter.Add("@audio");
                parameter.Add("@length");
                parameter.Add("@size");
                parameter.Add("@ending");
                parameter.Add("@tmdb_id");
                parameter.Add("@imdb_id");
                parameter.Add("@poster_path");
                parameter.Add("@alternative_titles");
                parameter.Add("@tmdb_synchro");

                // All SQL commands are transmitted in a TRANSACTION block. This increases the speed drastically. 
                MyConn.Execute("START TRANSACTION");            
                // Loop through all movies in newdata and add them to database if not already there.
                for (int i = 0; i < newdata.Rows.Count; ++i)
                {
                    if ((int)(newdata.Rows[i][20]) == 1)
                    {
                        if (Convert.ToInt32(newdata.Rows[i][0]) < 0)
                        {
                            string title = @newdata.Rows[i][1].ToString();
                            DateTime year = DateTime.Parse(newdata.Rows[i][3].ToString());

                            //Checking if movies are already in Database. This is done locally. A movie is consiered unique by title and year. 
                            DataRow[] duplikate;
                            DataView dw = new DataView(movies);
                            dw.Sort = "title";
                            duplikate = movies.Select("title = '" + title.Replace("'", "''") + "' AND release_date = '" + year + "'");

                            // Assigning parameters
                            List<string> parameterValue = new List<string>();
                            for (int j = 0; j < newdata.Columns.Count; ++j)
                            {
                                if (j != 0 && j != 3 && j != 5 && j != 19)
                                    parameterValue.Add(newdata.Rows[i][j].ToString());
                                else if (j == 3)
                                    parameterValue.Add(Convert.ToDateTime(newdata.Rows[i][j]).ToString("dd.MM.yyyy"));
                            }

                            // Splitting the scan data into one row for the general movie information and one for the user data
                            DataRow dr_movie = movies.NewRow();
                            dr_movie["title"] = newdata.Rows[i]["title"];
                            dr_movie["original_title"] = newdata.Rows[i]["original_title"];
                            dr_movie["release_date"] = DateTime.Parse(newdata.Rows[i]["release_date"].ToString());
                            dr_movie["country"] = newdata.Rows[i]["country"];
                            dr_movie["genre"] = newdata.Rows[i]["genre"];
                            dr_movie["rating"] = newdata.Rows[i]["rating"];
                            dr_movie["tmdb_id"] = newdata.Rows[i]["tmdb_id"];
                            dr_movie["imdb_id"] = newdata.Rows[i]["imdb_id"];
                            dr_movie["poster_path"] = newdata.Rows[i]["poster_path"];
                            dr_movie["alternative_titles"] = newdata.Rows[i]["alternative_titles"];
                            dr_movie["tmdb_synchro"] = newdata.Rows[i]["tmdb_synchro"];

                            DataRow dr_version = versions.NewRow();
                            dr_version["resolution"] = newdata.Rows[i]["resolution"];
                            dr_version["type"] = newdata.Rows[i]["type"];
                            dr_version["codec"] = newdata.Rows[i]["codec"];
                            dr_version["audio"] = newdata.Rows[i]["audio"];
                            dr_version["length"] = newdata.Rows[i]["length"];
                            dr_version["size"] = newdata.Rows[i]["size"];
                            dr_version["ending"] = newdata.Rows[i]["ending"];
                            // If movie is not yet in Database it is included. Else it is checked if the user already has this version of the movie in the database.
                            if (duplikate.Length == 0)
                            {
                                command = "INSERT INTO movies VALUES (null , @title, @original_title, alternative_titles, STR_TO_DATE(@release_date, '%d.%m.%Y'), @country, @genre, @rating, @tmdb_id, @imdb_id, @poster_path, null, 1);";
                                MyConn.Execute(command, parameter, parameterValue);
                                id = MyConn.GetLastId();
                                dr_movie["movie_id"] = id;
                                // Check if actors/directors of movie are in database. Yes -> Get crew Id. No -> Add to crew and get crew id. 
                                string cmd = "SELECT crew_id, name FROM crew WHERE name IN (";
                                if (newdata.Rows[i]["actors"].ToString() != "")
                                {
                                    List<string> actors = new List<string>(newdata.Rows[i]["actors"].ToString().Replace(", ", ",").Replace("'", "''").Split(','));
                                    List<string> par_actors = new List<string>();
                                    for (int j = 0; j < actors.Count; ++j)
                                    {
                                        par_actors.Add("@act_" + j.ToString());
                                        cmd += "@act_" + j.ToString() + ", ";
                                    }
                                    cmd = cmd.Remove(cmd.LastIndexOf(','));
                                    cmd += ")";
                                    DataTable actor_id = MyConn.GetData(cmd, par_actors, actors);

                                    foreach (string actor in actors)
                                    {
                                        int act_id = -1;
                                        foreach (DataRow actor_id_row in actor_id.Rows)
                                        {
                                            if (actor_id_row[1].ToString() == actor)
                                                act_id = Convert.ToInt16(actor_id_row.ItemArray[0]);
                                        }
                                        if (act_id > -1)
                                        {
                                            cmd = "INSERT INTO actors (movie_id, crew_id) VALUES (" + id + ", '" + act_id + "')";
                                            MyConn.Execute(cmd);
                                        }
                                        else
                                        {
                                            cmd = "INSERT INTO crew (name) VALUES ('" + @actor + "')";
                                            MyConn.Execute(cmd, "@actor", actor);
                                            act_id = MyConn.GetLastId();
                                            cmd = "INSERT INTO actors (movie_id, crew_id) VALUES (" + id + ", " + act_id + ")";
                                            MyConn.Execute(cmd);
                                        }
                                    }
                                }
                                if (newdata.Rows[i]["directors"].ToString() != "")
                                {
                                    List<string> directors = new List<string>(newdata.Rows[i]["directors"].ToString().Replace(", ", ",").Replace("'", "''").Split(','));
                                    List<string> par_directors = new List<string>();
                                    cmd = "SELECT crew_id, name FROM crew WHERE name IN (";
                                    for (int j = 0; j < directors.Count; ++j)
                                    {
                                        par_directors.Add("@dir_" + j.ToString());
                                        cmd += "@dir_" + j.ToString() + ", ";
                                    }
                                    cmd = cmd.Remove(cmd.LastIndexOf(','));
                                    cmd += ")";
                                    DataTable director_id = MyConn.GetData(cmd, par_directors, directors);

                                    foreach (string director in directors)
                                    {
                                        int dir_id = -1;
                                        foreach (DataRow director_id_row in director_id.Rows)
                                        {
                                            if (director_id_row[1].ToString() == director)
                                                dir_id = Convert.ToInt16(director_id_row.ItemArray[0]);
                                        }
                                        if (dir_id > -1)
                                        {
                                            cmd = "INSERT INTO directors (movie_id, crew_id) VALUES (" + id + ", '" + dir_id + "')";
                                            MyConn.Execute(cmd);
                                        }
                                        else
                                        {
                                            cmd = "INSERT INTO crew (name) VALUES ('" + @director + "')";
                                            MyConn.Execute(cmd, "@director", director);
                                            dir_id = MyConn.GetLastId();
                                            cmd = "INSERT INTO directors (movie_id, crew_id) VALUES (" + id + ", " + dir_id + ")";
                                            MyConn.Execute(cmd);
                                        }
                                    }
                                }
                                command = "INSERT INTO versions VALUES (null," + id +
                                        ", @res, @type, @codec, @audio, @length, @size, @ending, null);";
                                MyConn.Execute(command, parameter, parameterValue);
                                id = MyConn.GetLastId();
                                dr_version["version_id"] = id;

                                command = "INSERT INTO collection VALUES ('" + username.ToLower() + "', " + id + "); ";
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
                                string res = newdata.Rows[i][8].ToString();
                                string codec = newdata.Rows[i][10].ToString();
                                string length = newdata.Rows[i][12].ToString();
                                string size = newdata.Rows[i][13].ToString();
                                // Check for this version of movie. It is consiered unique by resolution, codec, length and size.
                                DataView dw2 = new DataView(versions);
                                dw2.Sort = "size";
                                duplikate = versions.Select("size = '" + size + "' AND resolution = '" + res + "' AND codec = '" + codec + "' AND length = '" + length + "'");

                                if (duplikate.Length == 0)
                                {
                                    command = "INSERT INTO versions VALUES (null," + fk +
                                        ", @res, @type, @codec, @audio, @length, @size, @ending, null);";
                                    MyConn.Execute(command, parameter, parameterValue);
                                    id = MyConn.GetLastId();
                                    dr_version["version_id"] = id;

                                    command = "INSERT INTO collection VALUES ('" + username.ToLower() + "', " + id + "); ";
                                    MyConn.Execute(command, parameter, parameterValue);

                                    // also add to olddata to prevent another insertion of this version
                                    versions.Rows.Add(dr_version);
                                }
                                else
                                {
                                    fk = Convert.ToInt32(duplikate[0][0]);
                                    //command = "INSERT INTO collection VALUES '" + username.ToLower() + "', (SELECT * FROM (SELECT (" + fk + ")) AS tmp WHERE NOT EXISTS (SELECT version_id FROM collection WHERE version_id=" + fk + "));";
                                    command = "INSERT INTO collection SELECT '" + username.ToLower() + "', " + fk + " FROM DUAL WHERE NOT EXISTS (SELECT version_id FROM collection WHERE version_id = " + fk + ")";
                                    MyConn.Execute(command, parameter, parameterValue);
                                }
                            }
                        }
                    }
                    
                }
                // Commit SQL commands and unlock Tables                 
                //MyConn.Execute("UNLOCK TABLES");
                MyConn.Execute("COMMIT");
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
