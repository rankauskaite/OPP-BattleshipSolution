using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace BattleshipServer.Data
{
    public class Database
    {
        private readonly string _dbPath;

        public Database(string path)
        {
            _dbPath = path;
            Initialize();
        }

        private void Initialize()
        {
            var folder = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Games (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Player1 TEXT,
                Player2 TEXT,
                Winner TEXT,
                StartedAt TEXT,
                EndedAt TEXT,
                MovesJson TEXT
            );

            CREATE TABLE IF NOT EXISTS Maps (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PlayerName TEXT,
                CreatedAt TEXT,
                MapJson TEXT
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public void SaveMap(string playerName, string mapJson)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Maps (PlayerName, CreatedAt, MapJson) VALUES ($p,$c,$m)";
            cmd.Parameters.AddWithValue("$p", playerName);
            cmd.Parameters.AddWithValue("$c", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("$m", mapJson);
            cmd.ExecuteNonQuery();
        }

        public void SaveGame(string p1, string p2, string winner)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Games (Player1,Player2,Winner,StartedAt) VALUES ($p1,$p2,$w,$s)";
            cmd.Parameters.AddWithValue("$p1", p1);
            cmd.Parameters.AddWithValue("$p2", p2);
            cmd.Parameters.AddWithValue("$w", winner);
            cmd.Parameters.AddWithValue("$s", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }
    }
}
