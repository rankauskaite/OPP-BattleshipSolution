using System;
using System.IO;

namespace BattleshipClient.Observers
{
    public class LoggerObserver : IGameObserver
    {
        private string logFile;
        private bool isFirstWrite = true;

        public LoggerObserver(string playerName)
        {
            // Sukuriamas failo pavadinimas pagal žaidėjo vardą
            string safeName = string.Join("_", playerName.Split(Path.GetInvalidFileNameChars()));
            logFile = $"game_log_{safeName}.txt";
        }

        public void OnGameEvent(string eventType, string playerName, object? data = null)
        {
            if (isFirstWrite)
            {
                try
                {
                    if (File.Exists(logFile))
                        File.WriteAllText(logFile, string.Empty);

                    File.AppendAllText(logFile, "==== Naujas žaidimas ====\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Nepavyko išvalyti log failo: " + ex.Message);
                }

                isFirstWrite = false;
            }

            string message = eventType switch
            {
                "HIT" => "pataikė",
                "MISS" => "nepataikė",
                "EXPLOSION" => "nuskandino laivą",
                "WIN" => "laimėjo žaidimą",
                "LOSE" => "pralaimėjo žaidimą",
                _ => eventType
            };

            string log = $"{DateTime.Now:HH:mm:ss} - {playerName} {message}";
            File.AppendAllText(logFile, log + Environment.NewLine);
        }
    }
}
