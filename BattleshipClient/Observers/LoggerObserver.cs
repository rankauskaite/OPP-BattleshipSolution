using System;
using System.IO;

namespace BattleshipClient.Observers
{
    public class LoggerObserver : IGameObserver
    {
        private readonly string logFile = "game_log.txt";
        private bool isFirstWrite = true;

        public void OnGameEvent(string eventType, string playerName, object? data = null)
        {
            // Kai prasideda naujas žaidimas — išvalom logą
            if (isFirstWrite)
            {
                try
                {
                    if (File.Exists(logFile))
                        File.WriteAllText(logFile, string.Empty); // išvalom failą
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
