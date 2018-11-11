using System;
using System.Collections.Generic;
using System.Linq;
using SC2API_CSharp;
using SC2APIProtocol;
using Tyr.Builds;
using Tyr.Util;

namespace Tyr
{
    class Program
    {
        private static Race MyRace = Race.Random;
        static void Main(string[] args)
        {
            Tyr tyr = new Tyr();
            ReadBuildFile(tyr);
            
            
            string arguments = "Commandline args: ";
            foreach (string arg in args)
                arguments += arg;

            FileUtil.Log(arguments);

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

        private static void ReadBuildFile(Tyr tyr)
        {
            foreach (string line in FileUtil.ReadBuildFile())
            {
                if (line.StartsWith("#"))
                    continue;

                string[] words = line.Split(' ');
                if (words.Length < 2)
                    continue;

                if (words[0] == "Protoss")
                    MyRace = Race.Protoss;
                else if (words[0] == "Zerg")
                    MyRace = Race.Zerg;
                else if (words[0] == "Terran")
                    MyRace = Race.Terran;
                else if (words[0] == "Random")
                    MyRace = Race.Random;

                foreach (Type buildType in typeof(Build).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Build))))
                {
                    Build build = (Build)Activator.CreateInstance(buildType);
                    if (build.Name() == words[1])
                    {
                        FileUtil.AllowWritingFiles = false;
                        tyr.FixedBuild = build;
                        break;
                    }
                }
            }
        }
    }
}
