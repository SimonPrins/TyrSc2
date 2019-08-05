using System;
using System.IO;

namespace Tyr.Util
{
    public class FileUtil
    {
        private static string TournamentFile;
        private static string ResultsFile;
        private static string DataFolder = Directory.GetCurrentDirectory() + "/data/Tyr/";
        private static string LogFile;
        private static string DebugFile;
        private static string BuildFile = AppDomain.CurrentDomain.BaseDirectory + "build.txt";
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
            if (AllowWritingFiles && Tyr.Debug)
                File.AppendAllLines(DebugFile, new string[] { line });
        }

        public static void Register(string line)
        {
            InitializeResultsFile();
            if (AllowWritingFiles)
                File.AppendAllLines(ResultsFile, new string[] { line });
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

        private static void InitializeResultsFile()
        {
            if (ResultsFile == null)
            {
                if (Tyr.Bot.OpponentID == null)
                    ResultsFile = DataFolder + Tyr.Bot.EnemyRace + ".txt";
                else
                    ResultsFile = DataFolder + Tyr.Bot.OpponentID + ".txt";

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
                        Directory.CreateDirectory(DataFolder);
                    File.Create(fullpath).Close();
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
