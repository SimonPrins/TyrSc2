using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class MacroPvP : Build
    {
        private Point2D OverrideDefenseTarget;
        private TimingAttackTask AttackTask = new TimingAttackTask();
        private WorkerScoutTask WorkerScoutTask = new WorkerScoutTask();
        private bool SmellCheese = false;
        private WallInCreator WallIn;
        private bool CannonDefenseDetected = false;
        private bool TempestDetected = false;
        private FallBackController FallBackController = new FallBackController() { ReturnFire = true, MainDist = 40 };

        bool BattlecruisersDetected = false;

        public override string Name()
        {
            return "MacroPvP";
        }

        public override void OnStart(Bot tyr)
        {
            WorkerScoutTask.StartFrame = 1200;

            DefenseTask.Enable();
            tyr.TaskManager.Add(AttackTask);
            tyr.TaskManager.Add(WorkerScoutTask);
            ArmyObserverTask.Enable();
            tyr.TaskManager.Add(new ObserverScoutTask() { Priority = 6 });
            if (tyr.BaseManager.Pocket != null)
                tyr.TaskManager.Add(new ScoutProxyTask(tyr.BaseManager.Pocket.BaseLocation.Pos));
            ArchonMergeTask.Enable();

            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(FallBackController);
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new SoftLeashController(UnitTypes.STALKER, UnitTypes.IMMORTAL, 6));
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new TempestController());
            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY});
                WallIn.ReserveSpace();
            }

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) > 0);
            Set += CannonDefense();
            Set += EmergencyGateways();
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuild();
        }

        private BuildList CannonDefense()
        {
            BuildList result = new BuildList();

            result.If(() => Count(UnitTypes.STALKER) >= 10 && (Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0 || Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 3));
            result.Building(UnitTypes.FORGE);
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.PHOTON_CANNON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1);
            }
            result.Building(UnitTypes.PHOTON_CANNON, Main, 2, () => Count(UnitTypes.STALKER) >= 10 && Count(UnitTypes.OBSERVER) > 0);
            result.Building(UnitTypes.PHOTON_CANNON, Main, () => Count(UnitTypes.STALKER) >= 15 && Count(UnitTypes.OBSERVER) > 0);

            return result;
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();
            
            result.If(() => { return Count(UnitTypes.IMMORTAL) >= 2 && Completed(UnitTypes.NEXUS) >= 3; });
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }

        private BuildList EmergencyGateways()
        {
            BuildList result = new BuildList();

            result.If(() => { return EarlyPool.Get().Detected && !Expanded.Get().Detected; });
            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main);
            result.If(() => Count(UnitTypes.ZEALOT) >= 4);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.If(() => Count(UnitTypes.ZEALOT) >= 6);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => Count(UnitTypes.ZEALOT) >= 8);
            result.Building(UnitTypes.GATEWAY, Main);

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 70, () => Count(UnitTypes.NEXUS) >= 4 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.STALKER, 3);
            result.Train(UnitTypes.IMMORTAL, 2);
            result.Train(UnitTypes.OBSERVER, 2);
            result.Train(UnitTypes.IMMORTAL);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.NEXUS);
            result.Upgrade(UpgradeType.WarpGate);
            result.Building(UnitTypes.GATEWAY, Main, () => Count(UnitTypes.IMMORTAL) > 0);
            result.Building(UnitTypes.PYLON, Natural, () => Natural.Owner == Bot.Main.PlayerId && !Natural.UnderAttack);
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, 2, () => Bot.Main.Frame >= 3 * 60 * 22.4 && !Expanded.Get().Detected && Completed(Natural, UnitTypes.PYLON) > 0 && !Natural.UnderAttack && !CannonDefenseDetected);
            result.If(() => Count(UnitTypes.STALKER) >= 5);
            result.Building(UnitTypes.GATEWAY, Main, () => !Expanded.Get().Detected);
            result.If(() => Count(UnitTypes.STALKER) >= 10);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.STARGATE, 2, () => TempestDetected);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !Expanded.Get().Detected && !TempestDetected);
            result.If(() => Count(UnitTypes.IMMORTAL) >= 2);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Upgrade(UpgradeType.Blink);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => Expanded.Get().Detected && !TempestDetected);
            result.Building(UnitTypes.FORGE);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.If(() => Count(UnitTypes.IMMORTAL) >= 3 && Count(UnitTypes.STALKER) >= 10);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ROBOTICS_BAY);
            result.Building(UnitTypes.ASSIMILATOR, 4, () => Minerals() >= 700 && Gas() < 200);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Count(UnitTypes.STALKER) >= 20);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.NEXUS);

            return result;
        }
        
        private bool RobosActive()
        {
            int robos = Completed(UnitTypes.ROBOTICS_FACILITY);
            int alreadyBuilt = Completed(UnitTypes.OBSERVER) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.WARP_PRISM) + Completed(UnitTypes.DISRUPTOR);
            int total = Count(UnitTypes.OBSERVER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.WARP_PRISM) + Count(UnitTypes.DISRUPTOR);
            return total - alreadyBuilt >= robos;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Count(UnitTypes.PROBE) <= 10)
                GasWorkerTask.WorkersPerGas = 0;
            else if (Count(UnitTypes.NEXUS) < 3)
                GasWorkerTask.WorkersPerGas = 2;
            else BalanceGas();

            if (!CannonDefenseDetected && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.PHOTON_CANNON) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.FORGE) >= 0 && tyr.Frame < 22.4 * 60 * 4)
                CannonDefenseDetected = true;

            if (!TempestDetected && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.TEMPEST) > 0)
                TempestDetected = true;

            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.BATTLECRUISER) > 0)
                BattlecruisersDetected = true;

            if (TempestDetected)
            {
                AttackTask.RequiredSize = 25;
                AttackTask.RetreatSize = 8;
            }
            else if (BattlecruisersDetected)
            {
                AttackTask.RequiredSize = 40;
                AttackTask.RetreatSize = 10;
            }
            else
            {
                AttackTask.RequiredSize = 15;
                AttackTask.RetreatSize = 6;
            }

            foreach (WallBuilding building in WallIn.Wall)
                tyr.DrawSphere(new Point() { X = building.Pos.X, Y = building.Pos.Y, Z = tyr.MapAnalyzer.StartLocation.Z });

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = Completed(UnitTypes.ZEALOT) >= 5;
            
            if (StrategyAnalysis.CannonRush.Get().Detected)
                AttackTask.RequiredSize = 5;
            else if (SmellCheese)
                AttackTask.RequiredSize = 30;
            
            if (Natural.Owner == tyr.PlayerId && Count(UnitTypes.NEXUS) < 3 && Completed(UnitTypes.STALKER) < 15)
                IdleTask.Task.OverrideTarget = SC2Util.Point((tyr.MapAnalyzer.GetMainRamp().X + Natural.BaseLocation.Pos.X) / 2f, (tyr.MapAnalyzer.GetMainRamp().Y + Natural.BaseLocation.Pos.Y) / 2f);
            else if (Count(UnitTypes.NEXUS) >= 4)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 25;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;


            if (TempestDetected && Count(UnitTypes.STALKER) + Count(UnitTypes.VOID_RAY) >= 12)
            {
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 40;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 40;
                DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.AirDefenseTask.DrawDefenderRadius = 120;
            }
            else
            {
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 25;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
                DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
            }

            if (EarlyPool.Get().Detected && !Expanded.Get().Detected && Completed(UnitTypes.ZEALOT) < 2)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.NEXUS
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }
        }
    }
}
