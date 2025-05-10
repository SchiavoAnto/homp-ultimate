using System;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate;

public static class Database
{
    public static readonly string DB_PATH = $"{AppDomain.CurrentDomain.BaseDirectory}homp.db";

    private static SqliteConnection? connection = null;
    private static SqliteTransaction? currentTransaction = null;

    public static bool Init(bool dbExists)
    {
        try
        {
            Logger.Log("Connecting to database...");
            connection = new SqliteConnection($"Data Source='{DB_PATH}'");
            connection.Open();
            Logger.Log("Connected to database.");

            int r = ExecuteGenericQuery(@"
            CREATE TABLE IF NOT EXISTS songs (
                path TEXT NOT NULL PRIMARY KEY,
                title TEXT,
                artists TEXT,
                album_artist TEXT,
                album_title TEXT,
                year INTEGER,
                track_number INTEGER,
                genres TEXT,
                duration INTEGER,
                rating INTEGER
            );");
            if (r == -2)
            {
                Logger.Error("Failed to create 'songs' table in database!");
                return false;
            }
            r = ExecuteGenericQuery(@"
            CREATE TABLE IF NOT EXISTS folders (
                path TEXT NOT NULL PRIMARY KEY
            );");
            if (r == -2)
            {
                Logger.Error("Failed to create 'folders' table in database!");
                return false;
            }
            r = ExecuteGenericQuery(@"
            CREATE TABLE IF NOT EXISTS homp (
                last_db_update INTEGER NOT NULL DEFAULT 0,
                db_version INTEGER NOT NULL
            );");
            if (r == -2)
            {
                Logger.Error("Failed to create 'homp' table in database!");
                return false;
            }
            r = ExecuteGenericQuery(@"
            CREATE TABLE IF NOT EXISTS playlists (
                id INTEGER NOT NULL PRIMARY KEY,
                name TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS playlist_songs (
                playlist_id INTEGER NOT NULL,
                song_path TEXT
            );");
            if (r == -2)
            {
                Logger.Error("Failed to create 'playlists', 'playlist_songs' tables in database!");
                return false;
            }
            if (!dbExists)
            {
                r = ExecuteGenericQuery("INSERT INTO homp (last_db_update, db_version) VALUES (0, 1);");
                if (r == -2)
                {
                    Logger.Error("Failed to insert base 'last_db_update' and 'db_version' values in database!");
                    return false;
                }
            }

            Logger.Log("Database initialization finished.");
            return true;
        }
        catch
        {
            Logger.Error("Database connection failed!");
            return false;
        }
    }

    public static void Close()
    {
        connection?.Close();
        currentTransaction = null;
    }

    public static bool StartTransaction()
    {
        if (connection?.State != System.Data.ConnectionState.Open) return false;
        if (currentTransaction is not null) return false;
        currentTransaction = connection.BeginTransaction();
        return true;
    }

    public static bool EndTransaction()
    {
        if (connection?.State != System.Data.ConnectionState.Open) return false;
        if (currentTransaction is null) return false;
        currentTransaction.Commit();
        currentTransaction = null;
        return true;
    }

    public static bool CancelTransaction()
    {
        if (connection?.State != System.Data.ConnectionState.Open) return false;
        if (currentTransaction is null) return false;
        currentTransaction.Rollback();
        currentTransaction = null;
        return true;
    }

    /// <returns>-2 if fail</returns>
    public static int ExecuteGenericQuery(string query, List<(string paramName, object paramValue)>? parameters = null)
    {
        if (connection is null) return -2;
        using (SqliteCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = query;
            if (parameters != null)
            {
                foreach ((string pName, object pVal) in parameters)
                {
                    cmd.Parameters.AddWithValue(pName, pVal);
                }
            }
            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Exception("Exception in Database.ExecuteGenericQuery!", ex);
                return -2;
            }
        }
    }

    public static SqliteDataReader? ExecuteSelectQuery(string query, List<(string paramName, object paramValue)>? parameters = null)
    {
        if (connection is null) return null;
        SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = query;
        if (parameters != null)
        {
            foreach ((string pName, object pVal) in parameters)
            {
                cmd.Parameters.AddWithValue(pName, pVal ?? DBNull.Value);
            }
        }
        try
        {
            return cmd.ExecuteReader();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
            return null;
        }
    }

    public static bool AddFolder(string path)
    {
        int r = ExecuteGenericQuery(@"
            INSERT INTO folders (
                path
            ) VALUES (
                @_path
            );",
            [
                ("@_path", path)
            ]
        );
        return r != -2;
    }

    public static bool AddSong(Song song)
    {
        int r = ExecuteGenericQuery(@"
            INSERT INTO songs (
                path,
                title,
                artists,
                album_artist,
                album_title,
                year,
                track_number,
                genres,
                duration,
                rating
            ) VALUES (
                @_path,
                @_title,
                @_artists,
                @_album_artist,
                @_album_title,
                @_year,
                @_track_number,
                @_genres,
                @_duration,
                @_rating
            );",
            [
                ("@_path", song.FilePath),
                ("@_title", song.Title),
                ("@_artists", song.Artist),
                ("@_album_artist", song.AlbumArtist),
                ("@_album_title", song.Album ?? MainWindow.UNKNOWN_ALBUM),
                ("@_year", song.Year),
                ("@_track_number", song.TrackNumber),
                ("@_genres", string.Join(',', song.Genres)),
                ("@_duration", song.Duration.Ticks),
                ("@_rating", song.Rating)
            ]
        );
        return r != -2;
    }

    public static bool DeleteSong(Song song)
    {
        int r = ExecuteGenericQuery(@"DELETE FROM songs WHERE path = @_path;", [("@_path", song.FilePath)]);
        if (r == -2) return false;
        return RemoveSongFromAllPlaylists(song.FilePath);
    }

    public static bool AddSongToPlaylist(string songPath, int playlistId)
    {
        int r = ExecuteGenericQuery(@"
            INSERT INTO playlist_songs (
                playlist_id,
                song_path
            ) VALUES (
                @_id,
                @_path
            );",
            [
                ("@_id", playlistId),
                ("@_path", songPath)
            ]
        );
        return r != -2;
    }

    public static bool RemoveSongFromAllPlaylists(string songPath)
    {
        int r = ExecuteGenericQuery(@"DELETE FROM playlist_songs WHERE song_path = @_path;", [("@_path", songPath)]);
        return r != -2;
    }

    public static bool AddPlaylist(string name)
    {
        int r = ExecuteGenericQuery(@"
            INSERT INTO playlists (
                name
            ) VALUES (
                @_name
            );",
            [
                ("@_name", name)
            ]
        );
        return r != -2;
    }

    public static bool UpdateSong(Song song)
    {
        int r = ExecuteGenericQuery(@"
            UPDATE songs SET
                title = @_title,
                artists = @_artists,
                album_artist = @_album_artist,
                album_title = @_album_title,
                year = @_year,
                track_number = @_track_number,
                genres = @_genres,
                duration = @_duration,
                rating = @_rating
            WHERE path = @_path;",
            [
                ("@_title", song.Title),
                ("@_artists", song.Artist),
                ("@_album_artist", song.AlbumArtist),
                ("@_album_title", song.Album ?? MainWindow.UNKNOWN_ALBUM),
                ("@_year", song.Year),
                ("@_track_number", song.TrackNumber),
                ("@_genres", string.Join(',', song.Genres)),
                ("@_duration", song.Duration.Ticks),
                ("@_rating", song.Rating),
                ("@_path", song.FilePath)
            ]
        );
        return r != -2;
    }

    public static int GetRowCount(string tableName)
    {
        // TODO: Probably should use proper parameter insertion
        var reader = ExecuteSelectQuery($"SELECT count(*) FROM {tableName};");
        if (reader is null) return -1;
        if (reader.Read())
        {
            return reader.GetInt32(0);
        }
        return -1;
    }

    public static bool? DoesPlaylistExist(string playlistName)
    {
        var reader = ExecuteSelectQuery($"SELECT count(*) FROM playlists WHERE name = @_name;",
            [("@_name", playlistName)]);
        if (reader is null) return null;
        if (reader.Read())
        {
            return reader.GetInt32(0) > 0;
        }
        return null;
    }

    public static bool? DoesSongExist(string songPath)
    {
        var reader = ExecuteSelectQuery($"SELECT count(*) FROM songs WHERE path = @_path;",
            [("@_path", songPath)]);
        if (reader is null) return null;
        if (reader.Read())
        {
            return reader.GetInt32(0) > 0;
        }
        return null;
    }

    public static void SetLastUpdate(long dateTicks)
    {
        ExecuteGenericQuery("UPDATE homp SET last_db_update = @_ticks", [("@_ticks", dateTicks)]);
    }
}
