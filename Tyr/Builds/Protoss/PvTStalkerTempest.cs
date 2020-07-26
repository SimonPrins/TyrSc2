using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.BuildingPlacement;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class PvTStalkerTempest : Build
    {
        private TempestController TempestController = new TempestController();

        public override string Name()
        {
            return "PvTStalkerTempest";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            ArmyObserverTask.Enable();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(TempestController);
            MicroControllers.Add(new StutterController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) >= 2);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.STALKER);
            result.Train(UnitTypes.TEMPEST);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => Count(UnitTypes.GATEWAY) > 0);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.PROBE) >= 32);
            result.Building(UnitTypes.STARGATE, () => Count(UnitTypes.TEMPEST) > 0);
            result.Building(UnitTypes.STARGATE, () => Count(UnitTypes.TEMPEST) >= 3);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            bot.NexusAbilityManager.PriotitizedAbilities.Add(1568);

            if (bot.Frame == (int)(22.4 * (LogLabel.FoundStrelok ? 60 : 30)))
                bot.Chat(LogLabel.FoundMechSweep ? "Fun isn't something one considers when balancing the universe" : "I am inevitable.");

            bot.buildingPlacer.BuildCompact = true;
            bot.TargetManager.PrefferDistant = false;
            bot.TargetManager.TargetAllBuildings = true;

            if (Completed(UnitTypes.TEMPEST) >= 4)
            {
                TimingAttackTask.Task.RequiredSize = 4;
            }
            else if (Completed(UnitTypes.TEMPEST) <= 2)
            {
                TimingAttackTask.Task.RequiredSize = 20;
            }
            TimingAttackTask.Task.RetreatSize = 0;
        }
    }
}
