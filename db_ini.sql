CREATE DATABASE movie_db;  -- Create Database schema

USE movie_db; -- Use Database Schema

-- Create Tables in schema movie_db:

-- Table of all movies. General, not user related, metadata retrieved from tmdb.
CREATE TABLE movies (
    movie_id INTEGER AUTO_INCREMENT,  -- ID to identify a movie. Primary Key (unique, not null).
    title VARCHAR(100),
    original_title VARCHAR(100),
    alternative_titles TEXT,
    release_date DATE,
    country VARCHAR(40),
    genre VARCHAR(255),
    rating VARCHAR(10),
    tmdb_id VARCHAR(10),
    imdb_id VARCHAR(10),
    poster_path VARCHAR(40),
    added TIMESTAMP,					-- Date when the movie was added to database. Useful to sort by newest entry.
    tmdb_synchro BOOL NOT NULL DEFAULT FALSE,
    PRIMARY KEY (movie_id)
);

-- Table of all versions of movies. Physical properties.
CREATE TABLE versions (
    version_id INTEGER AUTO_INCREMENT,   -- Primary Key
    movie_id INTEGER,					 -- Foreign Key references movies table to explicitly link a version to a movie		
    resolution VARCHAR(20),
    type VARCHAR(40),
    codec VARCHAR(40),
    audio VARCHAR(100),
    length VARCHAR(20),
    size VARCHAR(20),
    ending VARCHAR(5),
    added TIMESTAMP,
    PRIMARY KEY (version_id),
    FOREIGN KEY (movie_id)
        REFERENCES movies (movie_id)
);


-- Table of persons. Here used for actors and directors. Table could be extended later for more information.
CREATE TABLE crew (
	crew_id INTEGER AUTO_INCREMENT,			-- Primary Key
    name VARCHAR(100),						
    PRIMARY KEY (crew_id)
);

-- Table to link actors to movies. Each movie has many actors, each actor many movies (n-n relationship). 
-- The first column contains movie_ids, the second column crew_ids. So for each movie you get several crew ids and vice versa.
-- The Primary Key consists of a combination of movie_id and crew_id to ensure uniqueness. The columns reference the movie and crew tables, respectively.
CREATE TABLE actors (
	movie_id INTEGER NOT NULL,
    crew_id INTEGER NOT NULL,
    PRIMARY KEY (movie_id, crew_id),
    FOREIGN KEY (movie_id)
		REFERENCES movies (movie_id),
	FOREIGN KEY (crew_id)
		REFERENCES crew (crew_id)
);

-- Table to link directors to movies. See actors.
CREATE TABLE directors (
	movie_id INTEGER NOT NULL,
    crew_id INTEGER NOT NULL,
    PRIMARY KEY (movie_id, crew_id),
    FOREIGN KEY (movie_id)
		REFERENCES movies (movie_id),
	FOREIGN KEY (crew_id)
		REFERENCES crew (crew_id)
);

-- Table of users, when they were added and their role (user, admin) 
CREATE TABLE users (
    user_name VARCHAR(40),
    added TIMESTAMP,
    role VARCHAR(20),
    PRIMARY KEY (user_name)
);

-- Table that connects users to versions. Works like actors table.
CREATE TABLE collection (
	user_name VARCHAR(40),
    version_id INTEGER,
    PRIMARY KEY (user_name, version_id),
    FOREIGN KEY (user_name)
		REFERENCES users (user_name),
	FOREIGN KEY (version_id)
		REFERENCES versions (version_id)
);

-- View on collection table that has only entries of the current user. This allows row level security.
-- The user has the right to see the collection table but can only delete from the view. This way he can only delete his own versions.
CREATE VIEW user_collection(
  user_name,
  version_id
)
AS
SELECT 
  user_name AS user_name,
  version_id AS version_id
FROM collection
WHERE
  (user_name = SUBSTRING_INDEX(USER(), '@', 1));  -- This removes the connection string from user().
  
  
-- Table for the not yet implemented shoutbox. 
CREATE TABLE shoutbox (
	user_name VARCHAR(40),						-- Who shouted?
    added TIMESTAMP,							-- When did he shout?
    pn_user_name VARCHAR(40),					-- To whom did he shout (pn)?
    message TEXT,								-- What did he shout?
    PRIMARY KEY (user_name, added),
    FOREIGN KEY (user_name)
		REFERENCES users (user_name),
	FOREIGN KEY (pn_user_name)
		REFERENCES users (user_name)
);

-- Table to store current version of client. Client could check on program start if a newer version exists
-- and, if so, display the description message. With a link to the data files, an auto update feature could be implemented later.
CREATE TABLE version (
	version_id VARCHAR(10),
	added TIMESTAMP,
	description TEXT,
	link VARCHAR(40),
	PRIMARY KEY (version_id)
);


-- Creation of roles to set privileges

-- Normal user. Can see everything, insert into most but delete or change very little
CREATE OR REPLACE ROLE movie_db_user_role;
GRANT SELECT ON movie_db.* TO movie_db_user_role;
GRANT INSERT ON movie_db.movies TO movie_db_user_role;
GRANT INSERT ON movie_db.versions TO movie_db_user_role;
GRANT INSERT ON movie_db.shoutbox TO movie_db_user_role;
GRANT INSERT ON movie_db.collection TO movie_db_user_role;
GRANT INSERT ON movie_db.directors TO movie_db_user_role;
GRANT INSERT ON movie_db.actors TO movie_db_user_role;
GRANT INSERT ON movie_db.crew TO movie_db_user_role;
GRANT DELETE ON movie_db.user_collection TO movie_db_user_role;

-- A new user would be created by following statements:
-- CREATE USER [username] IDENTIFIED BY [password]
-- GRANT movie_db_user_role TO [username]@'%';
-- SET DEFAULT ROLE movie_db_user_role FOR [username]@'%';
-- INSERT INTO movie_db.users (user_name, role) VALUES ('[username]','user');


-- Admin role. This user can perform administrative tasks on the movie_db schema without having root privileges.
-- So he can only destroy the movie_db and not the system.
CREATE OR REPLACE ROLE movie_db_admin_role;
GRANT movie_db_user_role TO movie_db_admin_role WITH ADMIN OPTION;  -- With admin option means that the user can grant the privileges to other users.
GRANT CREATE USER ON *.* TO movie_db_admin_role;					-- Admin can create other users accounts (and delete them)
GRANT UPDATE, DELETE ON movie_db.movies TO movie_db_admin_role;
GRANT UPDATE, DELETE ON movie_db.versions TO movie_db_admin_role;
GRANT INSERT, UPDATE, DELETE ON movie_db.users TO movie_db_admin_role;

-- A new user would be created by following statements:
-- CREATE USER [username] IDENTIFIED BY [password]
-- GRANT movie_db_admin_role TO [username]@'%';           -- You could specify the WITH ADMIN OPTION to allow the admin to create other admins
-- SET DEFAULT ROLE movie_db_admin_role FOR [username]@'%';
-- INSERT INTO movie_db.users (user_name, role) VALUES ('[username]','admin');

