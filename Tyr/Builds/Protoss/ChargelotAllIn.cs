using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class ChargelotAllIn : Build
    {
        private StutterController StutterController = new StutterController();
        private StutterForwardController StutterForwardController = new StutterForwardController();

        private WallInCreator WallIn = new WallInCreator();

        public override string Name()
        {
            return "ChargelotAllIn";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();

            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            SaveWorkersTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new GravitonBeamController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.MISSILE_TURRET, 11));
            MicroControllers.Add(new AttackEnemyController(UnitTypes.PHOENIX, UnitTypes.BANSHEE, 15, true));
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(StutterController);
            MicroControllers.Add(StutterForwardController);

            WallIn.CreateReaperWall(new List<uint> { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.CYBERNETICS_CORE});
            WallIn.ReserveSpace();

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() => (Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0 && Count(UnitTypes.STALKER) > 0) || tyr.Frame >= 22.4 * 60 * 3.5);
            Set += Units();
            Set += ExpandBuildings();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 70, () => Count(UnitTypes.NEXUS) >= 4 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.STALKER, 1);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.STALKER) > 0);
            result.Upgrade(UpgradeType.Charge);
            //result.Train(UnitTypes.STALKER, () => Count(UnitTypes.ZEALOT) >= Count(UnitTypes.STALKER));
            result.Train(UnitTypes.ZEALOT);

            return result;
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
            }

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.GATEWAY, Main, 2);
            result.If(() => TimingAttackTask.Task.AttackSent);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.PROBE) >= 34);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.FORGE, 2);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            BalanceGas();

            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }
            

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (tyr.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 3 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }

            tyr.NexusAbilityManager.Stopped = Count(UnitTypes.STALKER) == 0;
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);

            if (TotalEnemyCount(UnitTypes.BANSHEE) > 0)
            {
                StutterForwardController.Stopped = false;
                StutterController.Stopped = true;
            }
            else
            {
                StutterForwardController.Stopped = true;
                StutterController.Stopped = false;
            }

            SaveWorkersTask.Task.Stopped = tyr.Frame >= 22.4 * 60 * 7 || EnemyCount(UnitTypes.CYCLONE) == 0 || !Natural.UnderAttack;
            if (SaveWorkersTask.Task.Stopped)
                SaveWorkersTask.Task.Clear();

            WorkerTask.Task.EvacuateThreatenedBases = true;
            
            TimingAttackTask.Task.DefendOtherAgents = false;

            TimingAttackTask.Task.RequiredSize = 12;
            TimingAttackTask.Task.RetreatSize = 0;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

            tyr.TargetManager.SkipPlanetaries = true;
        }
    }
}
