using System;
using System.IO;

namespace Tyr.Util
{
    public class FileUtil
    {
        private static string ResultsFile;
        private static string DataFolder = Directory.GetCurrentDirectory() + "/data/Tyr/";
        private static string LogFile;
        private static string BuildFile = AppDomain.CurrentDomain.BaseDirectory + "build.txt";
        public static bool AllowWritingFiles = true;

        public static void Log(string line)
        {
            InitializeLogFile();
            if (AllowWritingFiles)
                File.AppendAllLines(LogFile, new string[] { line });
        }

        public static void Register(string line)
        {
            InitializeResultsFile();
            if (AllowWritingFiles)
                File.AppendAllLines(ResultsFile, new string[] { line });
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
    }
}
