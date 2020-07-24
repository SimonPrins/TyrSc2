﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SC2API_CSharp;
using SC2APIProtocol;
using Tyr.Builds;
using Tyr.buildSelection;
using Tyr.BuildSelection;
using Tyr.Util;

namespace Tyr
{
    public class Program
    {
        public static Race MyRace = Race.Protoss;
        private static DateTime ValidUntil = DateTime.Parse("07/24/2020", CultureInfo.InvariantCulture);
        public static void Run(string[] args)
        {/*
            if (args.Length == 0)
            {
                TestCombatSim.Test();
                return;
            }
            */

            Bot tyr = new Bot();
            string[] settings = FileUtil.ReadSettingsFile();
            foreach (string line in settings)
            {
                string[] setting = line.Split('=');
                if (setting.Length != 2)
                    continue;
                if (setting[0].Trim() != "debug")
                    continue;
                if (setting[1].Trim() == "true")
                    Bot.Debug = true;
                else if (setting[1].Trim() == "false")
                    Bot.Debug = false;
            }

            string now = DateTime.Now.ToShortDateString();
            if (ValidUntil != null && ValidUntil.Ticks < DateTime.Now.Ticks)
            {
                bool extension = false;
                foreach (string line in settings)
                {
                    string[] setting = line.Split('=');
                    if (setting.Length != 2)
                        continue;
                    if (setting[0].Trim() != "extendTime")
                        continue;
                    if (setting[1].Trim() == now)
                        extension = true;
                }
                foreach (string line in settings)
                {
                    string[] setting = line.Split('=');
                    if (setting.Length != 2)
                        continue;
                    if (setting[0].Trim() != "noTimeLimit")
                        continue;
                    if (setting[1].Trim() == "true")
                        extension = true;
                }
                if (!extension)
                {
                    DebugUtil.WriteLine("This version of Tyr is only valid until " + ValidUntil.ToShortDateString() + ". It is only intended for week " + tyr.VersionNumber + " of Probots. Are you sure you have the latest version? If you want to ignore this error for today you should set extendTime to " + now + " in the settings.txt file.");
                    FileUtil.Log("This version of Tyr is only valid until " + ValidUntil.ToShortDateString() + ". It is only intended for week " + tyr.VersionNumber + " of Probots. Are you sure you have the latest version? If you want to ignore this error for today you should set extendTime to " + now + " in the settings.txt file.");
                    System.Console.ReadLine();
                    throw new Exception("This version of Tyr is only valid until " + ValidUntil.ToShortDateString() + ". It is only intended for week " + tyr.VersionNumber + " of Probots. Are you sure you have the latest version? If you want to ignore this error for today you should set extendTime to " + now + " in the settings.txt file.");
                }
            }

            tyr.OnInitialize();

            ReadBuildFile(tyr);
            DetermineBuildsProvider(tyr);
            DetermineBuildSelector(tyr);
            DetermineProbotsChat(tyr);


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

            maps.Add(@"AutomatonLE.SC2Map");

            /*
            maps.Add(@"AcidPlantLE.SC2Map");
            maps.Add(@"BlueshiftLE.SC2Map");
            maps.Add(@"CeruleanFallLE.SC2Map");
            maps.Add(@"DreamcatcherLE.SC2Map");
            maps.Add(@"FractureLE.SC2Map");
            maps.Add(@"LostAndFoundLE.SC2Map");
            maps.Add(@"ParaSiteLE.SC2Map");
            */

            Random rand = new Random();
            return maps[rand.Next(maps.Count)];
        }

        private static void ReadBuildFile(Bot tyr)
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
                        tyr.FixedBuild = build;
                        break;
                    }
                }
            }
        }

        private static void DetermineBuildsProvider(Bot tyr)
        {
            string[] settings = FileUtil.ReadSettingsFile();
            foreach (string line in settings)
            {
                string[] setting = line.Split('=');
                if (setting.Length != 2)
                    continue;
                if (setting[0].Trim() != "BuildsProvider")
                    continue;
                string buildsProviderName = setting[1].Trim();


                foreach (Type buildsProviderType in typeof(BuildsProvider).Assembly.GetTypes().Where(type => (typeof(BuildsProvider)).IsAssignableFrom(type)))
                {
                    if (buildsProviderType.FullName.Substring(buildsProviderType.FullName.LastIndexOf('.') + 1) == buildsProviderName)
                    {
                        BuildsProvider buildsProvider = (BuildsProvider)Activator.CreateInstance(buildsProviderType);
                        tyr.BuildsProvider = buildsProvider;
                        DebugUtil.WriteLine("Found buildsProvider: " + buildsProviderName);
                        break;
                    }
                }
            }
        }

        private static void DetermineBuildSelector(Bot tyr)
        {
            string[] settings = FileUtil.ReadSettingsFile();
            foreach (string line in settings)
            {
                string[] setting = line.Split('=');
                if (setting.Length != 2)
                    continue;
                if (setting[0].Trim() != "BuildSelector")
                    continue;
                string buildSelectorName = setting[1].Trim();


                foreach (Type buildSelectorType in typeof(BuildSelector).Assembly.GetTypes().Where(type => (typeof(BuildSelector)).IsAssignableFrom(type)))
                {
                    if (buildSelectorType.FullName.Substring(buildSelectorType.FullName.LastIndexOf('.') + 1) == buildSelectorName)
                    {
                        BuildSelector buildSelector = (BuildSelector)Activator.CreateInstance(buildSelectorType);
                        tyr.BuildSelector = buildSelector;
                        break;
                    }
                }
            }
        }

        private static void DetermineProbotsChat(Bot tyr)
        {
            string[] settings = FileUtil.ReadSettingsFile();
            foreach (string line in settings)
            {
                string[] setting = line.Split('=');
                if (setting.Length != 2)
                    continue;
                if (setting[0].Trim() != "ProbotsChat")
                    continue;
                if (setting[1].Trim() == "true")
                    tyr.ProbotsChatMessages = true;
                else if (setting[1].Trim() == "false")
                    tyr.ProbotsChatMessages = false;
            }
        }
    }
}
