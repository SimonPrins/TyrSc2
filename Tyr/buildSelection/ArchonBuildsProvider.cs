using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Builds;
using Tyr.Builds.Protoss;
using Tyr.Builds.Terran;
using Tyr.Builds.Zerg;
using Tyr.StrategyAnalysis;

namespace Tyr.buildSelection
{
    class ArchonBuildsProvider : BuildsProvider
    {
        public List<Build> GetBuilds(Bot tyr, string[] lines)
        {
            List<Build> options;

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
                options.Add(new MacroHydra());
            }
            else if (tyr.EnemyRace == Race.Zerg)
            {
                options.Add(new RoachRavager());
            }
            else
            {
                options.Add(new MacroHydra());
            }

            return options;
        }

        public List<Build> ProtossBuilds(Bot tyr)
        {
            List<Build> options = new List<Build>();

            if (tyr.EnemyRace == Race.Terran)
            {
                options.Add(new PvTStalkerImmortal() { BuildReaperWall = true, ProxyPylon = false, DelayObserver = true, UseColosus = true });
            }
            else if (tyr.EnemyRace == Race.Zerg)
            {
                options.Add(new PvZHjax());
            }
            else if (tyr.EnemyRace == Race.Protoss)
            {
                options.Add(new PvPStalkerImmortal());
            }

            return options;
        }

        public List<Build> TerranBuilds(Bot tyr)
        {
            List<Build> options = new List<Build>();

            if (tyr.EnemyRace == Race.Terran)
            {
                options.Add(new TankPushProbots());
            }
            else if (tyr.EnemyRace == Race.Zerg)
            {
                options.Add(new MechTvZ());
            }
            else if (tyr.EnemyRace == Race.Protoss)
            {
                options.Add(new TankPushTvPProbots());
            }

            return options;
        }
    }
}
