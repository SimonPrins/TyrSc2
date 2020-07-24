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
    public class VoidrayPvP : Build
    {
        private Point2D OverrideDefenseTarget;
        private TimingAttackTask AttackTask = new TimingAttackTask();
        private TimedObserverTask TimedObserverTask = new TimedObserverTask();
        private WorkerScoutTask WorkerScoutTask = new WorkerScoutTask();
        private bool Attacking = false;
        private bool SmellCheese = false;
        private int DesiredImmortals = 20;
        private WallInCreator WallIn;
        private bool CannonDefenseDetected = false;
        private bool TempestDetected = false;

        bool BattlecruisersDetected = false;

        public override string Name()
        {
            return "VoidrayPvP";
        }

        public override void OnStart(Bot tyr)
        {
            WorkerScoutTask.StartFrame = 1200;

            DefenseTask.Enable();
            tyr.TaskManager.Add(AttackTask);
            tyr.TaskManager.Add(WorkerScoutTask);
            ArmyObserverTask.Enable();
            tyr.TaskManager.Add(new ObserverScoutTask() { Priority = 6 });
            tyr.TaskManager.Add(new AdeptScoutTask());
            if (tyr.BaseManager.Pocket != null)
                tyr.TaskManager.Add(new ScoutProxyTask(tyr.BaseManager.Pocket.BaseLocation.Pos));
            ArchonMergeTask.Enable();
            MechDestroyExpandsTask.Enable();

            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(new VoidrayController());
            MicroControllers.Add(new StalkerController());
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
            Set += ExpandBuildings();
            Set += MainBuild();
        }

        private BuildList CannonDefense()
        {
            BuildList result = new BuildList();

            result.If(() => Count(UnitTypes.STALKER) >= 10 && (Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0 || Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 3 || Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.PHOENIX) > 0) );
            result.Building(UnitTypes.FORGE);
            foreach (Base b in Bot.Bot.BaseManager.Bases)
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
            
            result.If(() => { return Count(UnitTypes.VOID_RAY) >= 3 && Completed(UnitTypes.NEXUS) >= 3; });
            foreach (Base b in Bot.Bot.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

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
            result.If(() => Count(UnitTypes.STALKER) > 0 || Minerals() >= 550);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.PYLON, Natural, () => Natural.Owner == Bot.Bot.PlayerId && !Natural.UnderAttack);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, 2, () => Bot.Bot.Frame >= 3 * 60 * 22.4 && !Expanded.Get().Detected && Completed(Natural, UnitTypes.PYLON) > 0 && !Natural.UnderAttack && !CannonDefenseDetected);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.ASSIMILATOR, 2);
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

            MechDestroyExpandsTask.Task.MaxSize = 3;
            MechDestroyExpandsTask.Task.RequiredSize = 3;
            MechDestroyExpandsTask.Task.RetreatSize = 0;
            MechDestroyExpandsTask.Task.UnitType = UnitTypes.ZEALOT;
            MechDestroyExpandsTask.Task.Stopped = !CannonDefenseDetected;

            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.BATTLECRUISER) > 0)
                BattlecruisersDetected = true;

            if (TempestDetected)
            {
                AttackTask.RequiredSize = 25;
                AttackTask.RetreatSize = 8;
            }
            else
            {
                AttackTask.RequiredSize = 25;
                AttackTask.RetreatSize = 8;
            }

            if (Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) >= AttackTask.RequiredSize)
                Attacking = true;

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
                DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
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

        public override void Produce(Bot tyr, Agent agent)
        {
            if (Count(UnitTypes.PROBE) >= 24
                && Count(UnitTypes.NEXUS) < 2
                && Minerals() < 450)
                return;
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && FoodLeft() >= 1
                && Count(UnitTypes.PROBE) < Math.Min(70, 20 * Completed(UnitTypes.NEXUS))
                && (Count(UnitTypes.PROBE) < 30 || Count(UnitTypes.STALKER) >= 15 || Count(UnitTypes.NEXUS) >= 3)
                && (Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.PROBE) < 18 + 2 * Completed(UnitTypes.ASSIMILATOR)))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (!RobosActive() && Minerals() < 400 && Gas() < 150 && !CannonDefenseDetected)
                    return;
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                if (Minerals() >= 100
                    && FoodLeft() >= 2
                    && CannonDefenseDetected
                    && Count(UnitTypes.ZEALOT) < 3
                    && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) < 4
                    && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.IMMORTAL) == 0
                    && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.STALKER) == 0
                    && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.VOID_RAY) == 0
                    && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.PHOENIX) == 0
                    && Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) >= 3)
                    agent.Order(916);
                else if (Gas() >= 50
                    && Minerals() >= 125
                    && FoodLeft() >= 2
                    && Completed(UnitTypes.CYBERNETICS_CORE) > 0
                    && ((Minerals() >= 300 && Gas() >= 250) || Completed(UnitTypes.STARGATE) == 0 || Completed(UnitTypes.FLEET_BEACON) == 0 || Count(UnitTypes.TEMPEST) > Completed(UnitTypes.TEMPEST))
                    && (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) >= 5 || (Minerals() >= 300 && Gas() >= 250) || Completed(UnitTypes.ROBOTICS_FACILITY) == 0 || Count(UnitTypes.DISRUPTOR) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.OBSERVER) + Count(UnitTypes.COLOSUS) > Completed(UnitTypes.DISRUPTOR) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.OBSERVER) + Completed(UnitTypes.COLOSUS)))
                    agent.Order(917);

            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_BAY)
            {
                if (Minerals() >= 150
                    && Gas() >= 150
                    && !Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(50)
                    && Count(UnitTypes.COLOSUS) > 0)
                {
                    agent.Order(1097);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_FACILITY)
            {
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                if (Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) >= 12
                    && Count(UnitTypes.IMMORTAL) >= 2
                    && Count(UnitTypes.NEXUS) < 2)
                    return;
                if (Count(UnitTypes.OBSERVER) < 2
                    && Count(UnitTypes.IMMORTAL) >= 3
                    && Minerals() >= 25
                    && Gas() >= 75
                    && FoodLeft() >= 1)
                {
                    agent.Order(977);
                }
                else if (Completed(UnitTypes.ROBOTICS_BAY) > 0
                    && Minerals() >= 150
                    && Gas() >= 150
                    && Count(UnitTypes.IMMORTAL) >= 6
                    && Count(UnitTypes.DISRUPTOR) < 4
                    && !TempestDetected
                    && FoodLeft() >= 3
                    && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BATTLECRUISER) == 0
                    && (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) < 10 || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARAUDER) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARINE) > tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) * 2))
                {
                    agent.Order(994);
                }
                else if (Minerals() >= 275
                    && Gas() >= 100
                    && !TempestDetected
                    && (Count(UnitTypes.DISRUPTOR) >= 4 || Completed(UnitTypes.ROBOTICS_BAY) == 0 || Count(UnitTypes.IMMORTAL) < 6)
                    && Count(UnitTypes.IMMORTAL) < DesiredImmortals
                    && (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) < 10 || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARAUDER) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARINE) > tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) * 2 || Count(UnitTypes.IMMORTAL) < 6)
                    && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) < 5
                    && FoodLeft() >= 4)
                {
                    agent.Order(979);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TEMPLAR_ARCHIVE)
            {
            }
            else if (agent.Unit.UnitType == UnitTypes.STARGATE)
            {
                if (Minerals() >= 250
                    && Gas() >= 150
                    && FoodLeft() >= 4
                    && Completed(UnitTypes.FLEET_BEACON) == 0)
                    agent.Order(950);
                else if (Completed(UnitTypes.FLEET_BEACON) > 0
                    && Minerals() >= 250
                    && Gas() >= 175
                    && FoodLeft() >= 5)
                    agent.Order(955);
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {

                if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(87)
                     && Minerals() >= 150
                     && Gas() >= 150
                    && Completed(UnitTypes.STALKER) > 0)
                    agent.Order(1593);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100
                    && Completed(UnitTypes.ADEPT) > 0)
                    agent.Order(1594);
            }
        }
    }
}
