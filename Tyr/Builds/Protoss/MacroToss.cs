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
    public class MacroToss : Build
    {
        private Point2D OverrideDefenseTarget;
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 50, RetreatSize = 20 };
        private TimedObserverTask TimedObserverTask = new TimedObserverTask();
        private WorkerScoutTask WorkerScoutTask = new WorkerScoutTask() { StartFrame = 600 };
        private bool Attacking = false;
        private bool SmellCheese = false;
        private bool SpinePushDetected = false;
        private FearEnemyController FearSpinesController = new FearEnemyController(new HashSet<uint>() { UnitTypes.ADEPT , UnitTypes.STALKER }, UnitTypes.SPINE_CRAWLER, 10);
        private int DesiredImmortals = 20;
        private WallInCreator WallIn;

        private int DesiredStalkers;

        public override string Name()
        {
            return "MacroToss";
        }

        public override void OnStart(Bot tyr)
        {
            DefenseTask.Enable();
            tyr.TaskManager.Add(attackTask);
            tyr.TaskManager.Add(WorkerScoutTask);
            tyr.TaskManager.Add(new ObserverScoutTask());
            tyr.TaskManager.Add(new ArmyObserverTask());
            tyr.TaskManager.Add(new AdeptScoutTask());
            tyr.TaskManager.Add(TimedObserverTask);
            PhasedDisruptorTask.Enable();
            if (tyr.BaseManager.Pocket != null)
                tyr.TaskManager.Add(new ScoutProxyTask(tyr.BaseManager.Pocket.BaseLocation.Pos));
            ArchonMergeTask.Enable();

            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(FearSpinesController);
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new DisruptorController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY});
                WallIn.ReserveSpace();
            }

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) > 0);
            Set += EmergencyGateways();
            Set += MutaCannons();
            Set += ExpandBuildings();
            Set += Nexii();
            Set += MainBuild();
        }

        private BuildList Nexii()
        {
            BuildList result = new BuildList();

            result.If(() => { return Bot.Main.EnemyRace != Race.Terran || Count(UnitTypes.GATEWAY) >= 2; });
            result.If(() => { return Bot.Main.EnemyRace != Race.Zerg || Count(UnitTypes.GATEWAY) >= 1; });
            if (Bot.Main.EnemyRace == Race.Zerg)
                result.If(() => { return !EarlyPool.Get().Detected || Expanded.Get().Detected || Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.ADEPT) >= 15; });
            result.Building(UnitTypes.NEXUS, 2);
            result.If(() => { return Attacking; });
            result.Building(UnitTypes.NEXUS);

            return result;
        }

        private BuildList MutaCannons()
        {
            BuildList result = new BuildList();

            result.If(() => { return Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MUTALISK) + Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.SPIRE) > 0; });
            result.Building(UnitTypes.FORGE);
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.PHOTON_CANNON, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1);
            }

            return result;
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => !EarlyPool.Get().Detected);
            result.If(() => Count(UnitTypes.NEXUS) >= 4 || Minerals() >= 600);
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350 && RobosActive());
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

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
            if (Bot.Main.EnemyRace != Race.Terran)
                result.If(() => { return !EarlyPool.Get().Detected || Expanded.Get().Detected || Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.ADEPT) >= 15; });
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => RobosActive() || Minerals() >= 600 || FoodUsed() >= 190);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.ROBOTICS_BAY, () => RobosActive() || Count(UnitTypes.IMMORTAL) >= 3);
            result.If(() => Count(UnitTypes.IMMORTAL) >= 3 && Count(UnitTypes.ZEALOT) >= 10);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 6);
            result.If(() => Count(UnitTypes.IMMORTAL) >= 10 && Count(UnitTypes.ZEALOT) >= 15);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);

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
            BalanceGas();

            TimedObserverTask.Target = tyr.TargetManager.PotentialEnemyStartLocations[0];
            TimedObserverTask.Stopped = tyr.Frame < 22.4 * 60 * 6 || tyr.Frame >= 22.4 * 60 * 7 || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.SPIRE) > 0;
            if (TimedObserverTask.Stopped)
                TimedObserverTask.Clear();

            if (Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) >= attackTask.RequiredSize)
                Attacking = true;

            foreach (WallBuilding building in WallIn.Wall)
                tyr.DrawSphere(new Point() { X = building.Pos.X, Y = building.Pos.Y, Z = tyr.MapAnalyzer.StartLocation.Z });

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = Completed(UnitTypes.ZEALOT) >= 5;
            
            if (Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) >= 5 && tyr.Frame <= 22.4 * 60 * 2)
                SmellCheese = true;
            if (Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SPAWNING_POOL) > 0 && tyr.Frame <= 22.4 * 60 * 1.4 && !Expanded.Get().Detected)
                SmellCheese = true;
            if (!SpinePushDetected && SmellCheese)
            {
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.QUEEN && enemy.UnitType != UnitTypes.SPINE_CRAWLER && enemy.UnitType != UnitTypes.SPINE_CRAWLER_UPROOTED)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 50 * 50)
                    {
                        SpinePushDetected = true;
                        break;
                    }
                }
            }

            FearSpinesController.Stopped = !SpinePushDetected;

            if (tyr.EnemyRace == Race.Zerg)
            {
                if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MUTALISK)
                    + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BROOD_LORD)
                    + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.CORRUPTOR)
                    + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.SPIRE)
                    + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.GREATER_SPIRE) > 0)
                    DesiredStalkers = 15;
                else
                    DesiredStalkers = 2;
            }

            if (StrategyAnalysis.CannonRush.Get().Detected)
                attackTask.RequiredSize = 5;
            else if (SmellCheese)
                attackTask.RequiredSize = 30;
            
            if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 25;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 25;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

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
                && Count(UnitTypes.PROBE) < Math.Min(70, 20 * Completed(UnitTypes.NEXUS))
                && (Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.PROBE) < 18 + 2 * Completed(UnitTypes.ASSIMILATOR)))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Count(UnitTypes.ZEALOT) >= 6
                    && Count(UnitTypes.NEXUS) < 2
                    && Minerals() < 500)
                    return;
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                if (SmellCheese && !Expanded.Get().Detected)
                {
                    if (Minerals() >= 100
                        && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Math.Max(8, Count(UnitTypes.ADEPT))))
                        agent.Order(916);
                    else if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                        && Minerals() >= 100
                        && Gas() >= 25)
                        agent.Order(922);
                }
                else
                {
                    if (Gas() >= 50
                        && Minerals() >= 125
                        && Count(UnitTypes.STALKER) < DesiredStalkers
                        && Completed(UnitTypes.CYBERNETICS_CORE) > 0)
                        agent.Order(917);
                    else if (Minerals() >= 450
                        && Gas() < 100
                        && Count(UnitTypes.ZEALOT) < 20
                        && Completed(UnitTypes.ROBOTICS_FACILITY) > 0)
                        agent.Order(916);
                    else if (Minerals() >= 50
                        && Gas() >= 150
                        && Completed(UnitTypes.TEMPLAR_ARCHIVE) > 0)
                        agent.Order(919);
                    else if (Minerals() >= 350
                        && Count(UnitTypes.ZEALOT) < 20)
                        agent.Order(916);
                    else if (Minerals() >= 100
                        && Count(UnitTypes.ZEALOT) < 10)
                        agent.Order(916);
                    else if (Minerals() >= 450  )
                        agent.Order(916);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_FACILITY)
            {
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                if ((Count(UnitTypes.OBSERVER) == 0 || (Count(UnitTypes.IMMORTAL) >= 10 && Count(UnitTypes.DISRUPTOR) >= 4))
                    && Count(UnitTypes.OBSERVER) < 2
                    && Minerals() >= 25
                    && Gas() >= 75)
                {
                    agent.Order(977);
                }
                else if (Minerals() >= 150
                    && Gas() >= 150
                    && Count(UnitTypes.IMMORTAL) >= 3
                    && Count(UnitTypes.DISRUPTOR) < 4)
                {
                    agent.Order(994);
                }
                else if (Minerals() >= 275
                    && Gas() >= 100
                    && Count(UnitTypes.IMMORTAL) < DesiredImmortals)
                {
                    agent.Order(979);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TEMPLAR_ARCHIVE)
            {
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {
                    if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                        && Minerals() >= 100
                        && Gas() >= 100
                        && Completed(UnitTypes.ADEPT) > 0)
                        agent.Order(1594);
                    else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                         && Minerals() >= 100
                         && Gas() >= 100)
                        agent.Order(1592);
            }
        }
    }
}
