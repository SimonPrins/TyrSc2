using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Builds;
using Tyr.Builds.Protoss;
using Tyr.Builds.Terran;
using Tyr.Builds.Zerg;
using Tyr.Tasks;

namespace Tyr.buildSelection
{
    class VersusHumanBuildsProvider : BuildsProvider
    {
        private string[] Lines;
        public List<Build> GetBuilds(Bot tyr, string[] lines)
        {
            List<Build> options;

            Lines = lines;

            if (tyr.MyRace == Race.Protoss)
                options = ProtossBuilds(tyr);
            else if (tyr.MyRace == Race.Zerg)
                options = ZergBuilds(tyr);
            else if (tyr.MyRace == Race.Terran)
                options = TerranBuilds(tyr);
            else
                options = null;

            return options;
        }

        public List<Build> ZergBuilds(Bot tyr)
        {
            List<Build> options = new List<Build>();

            if (tyr.EnemyRace == Race.Protoss)
            {
                options.Add(new MassZergling() { AllowHydraTransition = true });
                options.Add(new MacroHydra());
            }
            else if (tyr.EnemyRace == Race.Terran)
            {
                options.Add(new MassZergling() { AllowHydraTransition = true });
                options.Add(new MacroHydra());
            }
            else if (tyr.EnemyRace == Race.Zerg)
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

        public List<Build> ProtossBuilds(Bot tyr)
        {
            List<Build> options = new List<Build>();
            if (tyr.EnemyRace == Race.Terran)
            {
                options.Add(new PvTStalkerImmortal() { BuildReaperWall = true, ProxyPylon = false, DelayObserver = true });
                options.Add(new DoubleRoboProxy());
            }
            else if (tyr.EnemyRace == Race.Zerg)
            {
                options.Add(new PvZStalkerImmortal() { BlockExpand = false });
                options.Add(new OneBaseStalkerImmortal() { StartZealots = true, ExpandCondition = () => Bot.Main.Frame >= 22.4 * 60 * 5, Scouting = false });
            }
            else if (tyr.EnemyRace == Race.Protoss)
            {
                options.Add(new PvPStalkerImmortal());
                options.Add(new OneBaseTempest() { DefendingStalker = true });
            }

            return options;
        }

        public List<Build> TerranBuilds(Bot tyr)
        {
            List<Build> options = new List<Build>();

            if (tyr.EnemyRace == Race.Terran)
            {
                options.Add(new BunkerRush());
                options.Add(new TankPushProbots());
                options.Add(new MarineRush());
            }
            else if (tyr.EnemyRace == Race.Zerg)
            {
                options.Add(new BunkerRush());
                options.Add(new MechTvZ());
                options.Add(new MarineRush());
            }
            else if (tyr.EnemyRace == Race.Protoss)
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
