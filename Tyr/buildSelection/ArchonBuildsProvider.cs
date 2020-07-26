using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Builds;
using SC2Sharp.Builds.Protoss;
using SC2Sharp.Builds.Terran;
using SC2Sharp.Builds.Zerg;
using SC2Sharp.StrategyAnalysis;

namespace SC2Sharp.buildSelection
{
    class ArchonBuildsProvider : BuildsProvider
    {
        public List<Build> GetBuilds(Bot bot, string[] lines)
        {
            List<Build> options;

            if (bot.MyRace == Race.Protoss)
                options = ProtossBuilds(bot);
            else if (bot.MyRace == Race.Zerg)
                options = ZergBuilds(bot);
            else if (bot.MyRace == Race.Terran)
                options = TerranBuilds(bot);
            else
                options = null;

            return options;
        }

        public List<Build> ZergBuilds(Bot bot)
        {
            List<Build> options = new List<Build>();

            if (bot.EnemyRace == Race.Protoss)
            {
                options.Add(new MassZergling() { AllowHydraTransition = true });
                options.Add(new MacroHydra());
            }
            else if (bot.EnemyRace == Race.Terran)
            {
                options.Add(new MacroHydra());
            }
            else if (bot.EnemyRace == Race.Zerg)
            {
                options.Add(new RoachRavager());
            }
            else
            {
                options.Add(new MacroHydra());
            }

            return options;
        }

        public List<Build> ProtossBuilds(Bot bot)
        {
            List<Build> options = new List<Build>();

            if (bot.EnemyRace == Race.Terran)
            {
                options.Add(new PvTStalkerImmortal() { BuildReaperWall = true, ProxyPylon = false, DelayObserver = true, UseColosus = true });
            }
            else if (bot.EnemyRace == Race.Zerg)
            {
                options.Add(new PvZHjax());
            }
            else if (bot.EnemyRace == Race.Protoss)
            {
                options.Add(new PvPStalkerImmortal());
            }

            return options;
        }

        public List<Build> TerranBuilds(Bot bot)
        {
            List<Build> options = new List<Build>();

            if (bot.EnemyRace == Race.Terran)
            {
                options.Add(new TankPushProbots());
            }
            else if (bot.EnemyRace == Race.Zerg)
            {
                options.Add(new MechTvZ());
            }
            else if (bot.EnemyRace == Race.Protoss)
            {
                options.Add(new TankPushTvPProbots());
            }

            return options;
        }
    }
}
