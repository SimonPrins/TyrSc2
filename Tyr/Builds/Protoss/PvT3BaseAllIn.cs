using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class PvT3BaseAllIn : Build
    {
        private StutterController StutterController = new StutterController();
        private StutterForwardController StutterForwardController = new StutterForwardController();

        private WallInCreator WallIn = new WallInCreator();

        public override string Name()
        {
            return "PvT3BaseAllIn";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();

            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            SaveWorkersTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new SoftLeashController(UnitTypes.COLOSUS, UnitTypes.IMMORTAL, 12));
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new FallBackController() { ReturnFire = true, MainDist = 40 });
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
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.PYLON) >= 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3 && Count(UnitTypes.PYLON) >= 2);
            result.Train(UnitTypes.PROBE, 70, () => Count(UnitTypes.NEXUS) >= 4 && Count(UnitTypes.PYLON) >= 2);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.STALKER, 1);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.STALKER) > 0);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.PHOENIX) < 30 || Count(UnitTypes.NEXUS) >= 3);
            result.If(() => Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 15);
            result.If(() => Count(UnitTypes.NEXUS) >= 3 || Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 20);
            result.Train(UnitTypes.IMMORTAL, 2);
            result.Train(UnitTypes.COLOSUS, 2, () => TotalEnemyCount(UnitTypes.VIKING_FIGHTER) == 0);
            result.Train(UnitTypes.IMMORTAL);
            result.Train(UnitTypes.STALKER, 5);
            result.Train(UnitTypes.SENTRY, 1);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();
            
            foreach (Base b in Bot.Bot.BaseManager.Bases)
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
            //result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.ROBOTICS_BAY, () => Count(UnitTypes.IMMORTAL) >= 4);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.GATEWAY);
            result.Upgrade(UpgradeType.ExtendedThermalLance);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => Count(UnitTypes.IMMORTAL) >= 2);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Count(UnitTypes.IMMORTAL) >= 3);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Count(UnitTypes.IMMORTAL) >= 3 && Minerals() >= 300);
            result.If(() => Completed(UnitTypes.COLOSUS) >= 2 || TimingAttackTask.Task.AttackSent);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.FORGE, 2);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
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

            TimingAttackTask.Task.RequiredSize = 25;
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
