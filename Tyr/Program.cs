using System;
using System.Collections.Generic;
using System.IO;
using SC2API_CSharp;
using SC2APIProtocol;

namespace Tyr
{
    class Program
    {
        private static Race MyRace = Race.Protoss;
        static void Main(string[] args)
        {
            Tyr tyr = new Tyr();
            if (Tyr.AllowWritingFiles && !File.Exists(Directory.GetCurrentDirectory() + "/data/Tyr/Tyr.log"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/data/Tyr");
                File.Create(Directory.GetCurrentDirectory() + "/data/Tyr/Tyr.log").Close();
            }
            
            string arguments = "Commandline args: ";
            foreach (string arg in args)
                arguments += arg;
            if (Tyr.AllowWritingFiles)
                File.AppendAllLines(Directory.GetCurrentDirectory() + "/Data/Tyr/Tyr.log", new string[] { arguments });

            GameConnection gameConnection = new GameConnection();
            tyr.GameConnection = gameConnection;



            if (args.Length == 0)
                gameConnection.RunSinglePlayer(tyr, RandomMap(), MyRace, Race.Terran, Difficulty.VeryHard).Wait();
            else
                gameConnection.RunLadder(tyr, MyRace, args).Wait();
        }

        private static string RandomMap()
        {
            List<string> maps = new List<string>();

            maps.Add(@"AcidPlantLE.SC2Map");
            maps.Add(@"BlueshiftLE.SC2Map");
            maps.Add(@"CeruleanFallLE.SC2Map");
            maps.Add(@"DreamcatcherLE.SC2Map");
            maps.Add(@"FractureLE.SC2Map");
            maps.Add(@"LostAndFoundLE.SC2Map");
            maps.Add(@"ParaSiteLE.SC2Map");

            Random rand = new Random();
            return maps[rand.Next(maps.Count)];
        }
    }
}
