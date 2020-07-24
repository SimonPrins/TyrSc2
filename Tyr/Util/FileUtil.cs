using System;
using System.IO;

namespace Tyr.Util
{
    public class FileUtil
    {
        private static string TournamentFile;
        private static string SettingsFile = AppDomain.CurrentDomain.BaseDirectory + "settings.txt";
        private static string ResultsFile;
        private static string DataFolder = Directory.GetCurrentDirectory() + "/data/Tyr/";
        private static string LogFile;
        private static string DebugFile;
        private static string BuildFile = AppDomain.CurrentDomain.BaseDirectory + "build.txt";
        private static string ScoutLocationFile;
        public static bool AllowWritingFiles = true;

        public static void LogTournament(string line)
        {
            InitializeTournamentFile();
            File.AppendAllLines(TournamentFile, new string[] { line });
        }

        public static void Log(string line)
        {
            InitializeLogFile();
            if (AllowWritingFiles)
                File.AppendAllLines(LogFile, new string[] { line });
        }

        public static void Debug(string line)
        {
            InitializeDebugFile();
            if (AllowWritingFiles && Bot.Debug)
                File.AppendAllLines(DebugFile, new string[] { line });
        }

        public static void Register(string line)
        {
            InitializeResultsFile();
            if (AllowWritingFiles)
                File.AppendAllLines(ResultsFile, new string[] { line });
        }

        public static void WriteScoutLocation(string line)
        {
            InitializeScoutLocationFile();
            if (AllowWritingFiles)
                File.AppendAllLines(ScoutLocationFile, new string[] { line });
        }

        public static string[] ReadTournamentFile()
        {
            InitializeTournamentFile();
            return File.ReadAllLines(TournamentFile);
        }

        public static string[] ReadResultsFile()
        {
            InitializeResultsFile();
            return File.ReadAllLines(ResultsFile);
        }

        public static string[] ReadBuildFile()
        {
            if (!File.Exists(BuildFile))
                return new string[0];

            return File.ReadAllLines(BuildFile);
        }

        public static string[] ReadSettingsFile()
        {
            if (!File.Exists(SettingsFile))
                return new string[0];

            return File.ReadAllLines(SettingsFile);
        }

        public static string[] ReadDebugFile()
        {
            InitializeDebugFile();
            if (!File.Exists(DebugFile))
                return new string[0];

            return File.ReadAllLines(DebugFile);
        }

        public static string[] ReadScoutLocationFile()
        {
            InitializeScoutLocationFile();
            if (!File.Exists(ScoutLocationFile))
                return new string[0];

            return File.ReadAllLines(ScoutLocationFile);
        }

        private static void InitializeResultsFile()
        {
            if (ResultsFile == null)
            {
                if (Bot.Bot.OpponentID == null)
                    ResultsFile = DataFolder + Settings.ResultsFilePrefix() + Bot.Bot.EnemyRace + ".txt";
                else
                    ResultsFile = DataFolder + Settings.ResultsFilePrefix() + Bot.Bot.OpponentID + ".txt";

                if (AllowWritingFiles && !File.Exists(ResultsFile))
                {
                    Directory.CreateDirectory(DataFolder);
                    File.Create(ResultsFile).Close();
                }
            }
        }

        private static void InitializeLogFile()
        {
            if (LogFile == null)
            {
                LogFile = DataFolder + "Tyr.log";
                if (AllowWritingFiles && !File.Exists(LogFile))
                {
                    Directory.CreateDirectory(DataFolder);
                    File.Create(LogFile).Close();
                }
            }
        }

        private static void InitializeFile(string filename)
        {
            if (TournamentFile == null)
            {
                TournamentFile = DataFolder + filename;
                if (!File.Exists(TournamentFile))
                {
                    Directory.CreateDirectory(DataFolder);
                    File.Create(TournamentFile).Close();
                }
            }
        }

        private static void InitializeTournamentFile()
        {
            if (TournamentFile == null)
            {
                TournamentFile = DataFolder + "tounament.txt";
                if (!File.Exists(TournamentFile))
                {
                    Directory.CreateDirectory(DataFolder);
                    File.Create(TournamentFile).Close();
                }
            }
        }

        private static void InitializeDebugFile()
        {
            if (DebugFile == null)
            {
                DebugFile = DataFolder + "debug.txt";
                if (AllowWritingFiles && !File.Exists(DebugFile))
                {
                    Directory.CreateDirectory(DataFolder);
                    File.Create(DebugFile).Close();
                }
            }
        }

        private static void InitializeScoutLocationFile()
        {
            if (ScoutLocationFile == null)
            {
                ScoutLocationFile = Directory.GetCurrentDirectory() + "/scoutlocation.txt";
                if (AllowWritingFiles && !File.Exists(ScoutLocationFile))
                {
                    Directory.CreateDirectory(DataFolder);
                    File.Create(ScoutLocationFile).Close();
                }
            }
        }

        public static void WriteToFile(string filename, string text, bool overwrite)
        {
            string fullpath = DataFolder + filename;
            if (AllowWritingFiles)
            {
                if (overwrite)
                    File.WriteAllText(fullpath, text);
                else
                {
                    if (!File.Exists(fullpath))
                    {
                        Directory.CreateDirectory(DataFolder);
                        File.Create(fullpath).Close();
                    }
                    if (filename == "ArmyAnalysis.txt")
                        System.Console.WriteLine("writing analysis: " + text);
                    File.AppendAllLines(fullpath, new string[] { text });
                }
            }
        }

        public static string ReadFile(string filename)
        {
            string fullpath = DataFolder + filename;
            if (!File.Exists(fullpath))
                return null;

            return File.ReadAllText(fullpath);
        }
    }
}
