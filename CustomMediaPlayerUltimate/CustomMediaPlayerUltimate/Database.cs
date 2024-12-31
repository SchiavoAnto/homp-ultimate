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
            connection = new SqliteConnection($"Data Source='{DB_PATH}'");
            connection.Open();

            if (dbExists) return true;
            int r = ExecuteGenericQuery(@"
            CREATE TABLE songs (
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
            if (r == -2) return false;
            r = ExecuteGenericQuery(@"
            CREATE TABLE folders (
                path TEXT NOT NULL PRIMARY KEY,
                last_modified INTEGER DEFAULT 0
            );");
            if (r == -2) return false;
            r = ExecuteGenericQuery(@"
            CREATE TABLE homp (
                last_db_update INTEGER NOT NULL DEFAULT 0
            );
            INSERT INTO homp (last_db_update) VALUES (0);");
            if (r == -2) return false;

            return true;
        }
        catch
        {
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
            return cmd.ExecuteNonQuery();
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
        cmd.CommandType = System.Data.CommandType.Text;
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
                ("@_rating", 0) // TODO
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

    public static void SetLastUpdate(long dateTicks)
    {
        ExecuteGenericQuery("UPDATE homp SET last_db_update = @_ticks", [("@_ticks", dateTicks)]);
    }
}
