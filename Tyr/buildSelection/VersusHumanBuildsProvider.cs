using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Builds;
using SC2Sharp.Builds.Protoss;
using SC2Sharp.Builds.Terran;
using SC2Sharp.Builds.Zerg;
using SC2Sharp.Tasks;

namespace SC2Sharp.buildSelection
{
    class VersusHumanBuildsProvider : BuildsProvider
    {
        private string[] Lines;
        public List<Build> GetBuilds(Bot bot, string[] lines)
        {
            List<Build> options;

            Lines = lines;

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
                options.Add(new MassZergling() { AllowHydraTransition = true });
                options.Add(new MacroHydra());
            }
            else if (bot.EnemyRace == Race.Zerg)
            {
                options.Add(new RoachRavager());
                options.Add(new MacroHydra());
            }
            else
            {
                options.Add(new MassZergling() { AllowHydraTransition = true });
                options.Add(new MacroHydra());
            }

            return options;
        }

        public List<Build> ProtossBuilds(Bot bot)
        {
            List<Build> options = new List<Build>();
            if (bot.EnemyRace == Race.Terran)
            {
                options.Add(new PvTStalkerImmortal() { BuildReaperWall = true, ProxyPylon = false, DelayObserver = true });
                options.Add(new DoubleRoboProxy());
            }
            else if (bot.EnemyRace == Race.Zerg)
            {
                options.Add(new PvZStalkerImmortal() { BlockExpand = false });
                options.Add(new OneBaseStalkerImmortal() { StartZealots = true, ExpandCondition = () => Bot.Main.Frame >= 22.4 * 60 * 5, Scouting = false });
            }
            else if (bot.EnemyRace == Race.Protoss)
            {
                options.Add(new PvPStalkerImmortal());
                options.Add(new OneBaseTempest() { DefendingStalker = true });
            }

            return options;
        }

        public List<Build> TerranBuilds(Bot bot)
        {
            List<Build> options = new List<Build>();

            if (bot.EnemyRace == Race.Terran)
            {
                options.Add(new BunkerRush());
                options.Add(new TankPushProbots());
                options.Add(new MarineRush());
            }
            else if (bot.EnemyRace == Race.Zerg)
            {
                options.Add(new BunkerRush());
                options.Add(new MechTvZ());
                options.Add(new MarineRush());
            }
            else if (bot.EnemyRace == Race.Protoss)
            {
                options.Add(new BunkerRush());
                options.Add(new TankPushTvPProbots());
                options.Add(new MarineRush());
            }
            else
            {
                options.Add(new BunkerRush());
                options.Add(new TankPushProbots());
                options.Add(new MarineRush());
            }

            return options;
        }
    }
}
