using SC2APIProtocol;
using System;
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
    public class PvTDisruptor : Build
    {
        private Point2D OverrideDefenseTarget;
        private TimingAttackTask attackTask = new TimingAttackTask();
        private TimedObserverTask TimedObserverTask = new TimedObserverTask();
        private WorkerScoutTask WorkerScoutTask = new WorkerScoutTask() { StartFrame = 600 };
        private bool Attacking = false;
        private bool SmellCheese = false;
        private bool SpinePushDetected = false;
        private FearEnemyController FearSpinesController = new FearEnemyController(new HashSet<uint>() { UnitTypes.ADEPT , UnitTypes.STALKER }, UnitTypes.SPINE_CRAWLER, 10);
        private int DesiredImmortals = 20;
        private WallInCreator WallIn;
        private bool BansheeHarassDetected = false;
        private List<DefenseSquadTask> StalkerDefenseSquads;

        private bool BattlecruisersDetected = false;
        private bool FourRaxSuspected = false;

        private int DesiredStalkers;

        public override string Name()
        {
            return "PvTDisruptor";
        }

        public override void OnStart(Tyr tyr)
        {
            DefenseTask.Enable();
            tyr.TaskManager.Add(attackTask);
            tyr.TaskManager.Add(WorkerScoutTask);
            ArmyObserverTask.Enable();
            tyr.TaskManager.Add(new ObserverScoutTask() { Priority = 6 });
            tyr.TaskManager.Add(new AdeptScoutTask());
            if (tyr.BaseManager.Pocket != null)
                tyr.TaskManager.Add(new ScoutProxyTask(tyr.BaseManager.Pocket.BaseLocation.Pos));
            ArchonMergeTask.Enable();

            if (StalkerDefenseSquads == null)
                StalkerDefenseSquads = DefenseSquadTask.GetDefenseTasks(UnitTypes.STALKER);
            else
                foreach (DefenseSquadTask task in StalkerDefenseSquads)
                    Tyr.Bot.TaskManager.Add(task);
            DefenseSquadTask.Enable(StalkerDefenseSquads, true, true);

            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(FearSpinesController);
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new TempestController());
            if (WallIn == null)
            {
                System.Console.WriteLine("Creating wall.");
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY});
                WallIn.ReserveSpace();
                System.Console.WriteLine("Wall size: " + WallIn.Wall.Count);
                foreach (WallBuilding building in WallIn.Wall)
                    System.Console.WriteLine("Building pos: " + building.Pos);
            }

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) > 0);
            Set += CannonDefense();
            Set += EmergencyGateways();
            Set += ExpandBuildings();
            Set += Nexii();
            Set += Units();
            Set += MainBuild();
        }

        private BuildList CannonDefense()
        {
            BuildList result = new BuildList();

            result.If(() => Count(UnitTypes.STALKER) >= 10 && (Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0 || Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 3));
            result.Building(UnitTypes.FORGE);
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.PHOTON_CANNON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1);
            }
            result.Building(UnitTypes.PHOTON_CANNON, Main, 2, () => Count(UnitTypes.STALKER) >= 10 && Count(UnitTypes.OBSERVER) > 0);
            result.Building(UnitTypes.PHOTON_CANNON, Main, () => Count(UnitTypes.STALKER) >= 15 && Count(UnitTypes.OBSERVER) > 0);

            return result;
        }

        private BuildList EmergencyGateways()
        {
            BuildList result = new BuildList();

            result.If(() => { return Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool && !Tyr.Bot.EnemyStrategyAnalyzer.Expanded; });
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

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => { return !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool; });
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }

        private BuildList Nexii()
        {
            BuildList result = new BuildList();

            result.If(() => { return Tyr.Bot.EnemyRace != Race.Terran || Count(UnitTypes.GATEWAY) >= 2; });
            result.If(() => { return Tyr.Bot.EnemyRace != Race.Zerg || Count(UnitTypes.GATEWAY) >= 1; });
            result.Building(UnitTypes.NEXUS, 2);
            result.If(() => { return Attacking; });
            result.Building(UnitTypes.NEXUS);

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.TEMPEST);
            result.Train(UnitTypes.STALKER, () => TotalEnemyCount(UnitTypes.BANSHEE) >= 5);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.OBSERVER, 3, () => TotalEnemyCount(UnitTypes.BANSHEE) >= 3);
            result.Train(UnitTypes.IMMORTAL, 6, () => TotalEnemyCount(UnitTypes.BANSHEE) < 5);
            result.Train(UnitTypes.DISRUPTOR, 4, () => 
                TotalEnemyCount(UnitTypes.BATTLECRUISER) == 0 
                && (TotalEnemyCount(UnitTypes.WIDOW_MINE) < 10 || TotalEnemyCount(UnitTypes.MARAUDER) + TotalEnemyCount(UnitTypes.MARINE) > TotalEnemyCount(UnitTypes.WIDOW_MINE) * 2) );
            result.Train(UnitTypes.IMMORTAL, () => 
                Count(UnitTypes.IMMORTAL) < DesiredImmortals
                && TotalEnemyCount(UnitTypes.BANSHEE) < 5
                && (TotalEnemyCount(UnitTypes.WIDOW_MINE) < 10 || TotalEnemyCount(UnitTypes.MARAUDER) + TotalEnemyCount(UnitTypes.MARINE) > TotalEnemyCount(UnitTypes.WIDOW_MINE) * 2));
            result.Train(UnitTypes.STALKER, () => TotalEnemyCount(UnitTypes.BANSHEE) < 5);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
            if (Tyr.Bot.EnemyRace != Race.Terran)
                result.If(() => { return !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool || Tyr.Bot.EnemyStrategyAnalyzer.Expanded || Completed(UnitTypes.STALKER) + Completed(UnitTypes.ADEPT) >= 5; });
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.ASSIMILATOR, () => !FourRaxSuspected || Count(UnitTypes.PHOTON_CANNON) >= 2);
            result.Upgrade(UpgradeType.WarpGate);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.GATEWAY, Natural, () => FourRaxSuspected && Completed(Natural, UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, 2, () => FourRaxSuspected && Completed(Natural, UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.FORGE, () => FourRaxSuspected && Count(UnitTypes.GATEWAY) >= 3);
            result.Building(UnitTypes.PHOTON_CANNON, Natural, 4, () => FourRaxSuspected);
            result.Building(UnitTypes.ASSIMILATOR, () => FourRaxSuspected && Minerals() >= 600);
            result.Building(UnitTypes.ASSIMILATOR, () => FourRaxSuspected && Minerals() >= 800);
            result.If(() => !FourRaxSuspected || Count(UnitTypes.STALKER) >= 10);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) >= 3);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.FORGE, () => !FourRaxSuspected);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !BattlecruisersDetected && Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) == 0);
            result.Building(UnitTypes.ASSIMILATOR, 4, () => Minerals() >= 700 && Gas() < 200);
            //result.Building(UnitTypes.STARGATE);
            //result.Building(UnitTypes.FLEET_BEACON);
            result.If(() => Count(UnitTypes.IMMORTAL) >= 3 && Count(UnitTypes.STALKER) >= 10); 
            result.Building(UnitTypes.ROBOTICS_BAY, () => !BattlecruisersDetected && Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) == 0);
            result.Building(UnitTypes.STARGATE, () => BattlecruisersDetected || Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0);
            result.Building(UnitTypes.FLEET_BEACON, () => Completed(UnitTypes.STARGATE) > 0);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Count(UnitTypes.STALKER) >= 20);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Completed(UnitTypes.STALKER) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.DISRUPTOR) + Completed(UnitTypes.TEMPEST) >= 40 || Minerals() >= 650);
            result.Building(UnitTypes.NEXUS);

            return result;
        }
        
        public override void OnFrame(Tyr tyr)
        {
            BalanceGas();
            
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.BATTLECRUISER) > 0)
                BattlecruisersDetected = true;

            if (tyr.EnemyStrategyAnalyzer.FourRaxDetected)
                FourRaxSuspected = true;
            if (!FourRaxSuspected)
            {
                Point2D enemyRamp = tyr.MapAnalyzer.GetEnemyRamp();
                int enemyBarrackWallCount = 0;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (enemyRamp == null)
                        break;
                    if (enemy.UnitType == UnitTypes.BARRACKS && SC2Util.DistanceSq(enemy.Pos, enemyRamp) <= 5.5 * 5.5)
                        enemyBarrackWallCount++;
                }
                if (enemyBarrackWallCount >= 2)
                {
                    WorkerScoutTask.Stopped = true;
                    WorkerScoutTask.Clear();
                    FourRaxSuspected = true;
                }
            }

            if (BattlecruisersDetected || 
                (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) >= 12 && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARINE) < 80))
            {
                attackTask.RequiredSize = 30;
                attackTask.RetreatSize = 8;
            }
            else
            {
                attackTask.RequiredSize = 50;
                attackTask.RetreatSize = 15;
            }

            if (Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) >= attackTask.RequiredSize)
                Attacking = true;

            foreach (WallBuilding building in WallIn.Wall)
                tyr.DrawSphere(new Point() { X = building.Pos.X, Y = building.Pos.Y, Z = tyr.MapAnalyzer.StartLocation.Z });

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = Completed(UnitTypes.ZEALOT) >= 5;


            foreach (DefenseSquadTask task in StalkerDefenseSquads)
            {
                task.Stopped = (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) < 3 && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) == 0 || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARINE) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARAUDER) >= 10);
                task.MaxDefenders = Math.Min(5, Completed(UnitTypes.STALKER) / Math.Max(1, Count(UnitTypes.NEXUS)));
                task.Priority = 10;
            }

            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 3 || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0)
            {
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.VIKING_FIGHTER);
                DefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.REAPER);
                tyr.buildingPlacer.SpreadCannons = true;
                attackTask.DefendOtherAgents = false;
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

            if (tyr.EnemyStrategyAnalyzer.CannonRushDetected)
                attackTask.RequiredSize = 5;
            else if (SmellCheese)
                attackTask.RequiredSize = 30;
            
            if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

            if (Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool && !Tyr.Bot.EnemyStrategyAnalyzer.Expanded && Completed(UnitTypes.ZEALOT) < 2)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.NEXUS
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }
        }

        public override void Produce(Tyr tyr, Agent agent)
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
            /*
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                else
                {
                    if (Gas() >= 50
                        && Minerals() >= 125
                        && Completed(UnitTypes.CYBERNETICS_CORE) > 0
                        && ((Minerals() >= 300 && Gas() >= 250) || Completed(UnitTypes.STARGATE) == 0 || Completed(UnitTypes.FLEET_BEACON) == 0 || Count(UnitTypes.TEMPEST) > Completed(UnitTypes.TEMPEST))
                        && (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) >= 5 || (Minerals() >= 300 && Gas() >= 250) || Completed(UnitTypes.ROBOTICS_FACILITY) == 0 || Count(UnitTypes.DISRUPTOR) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.OBSERVER) + Count(UnitTypes.COLLOSUS) > Completed(UnitTypes.DISRUPTOR) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.OBSERVER) + Completed(UnitTypes.COLLOSUS)))
                        agent.Order(917);
                }
            }
            */
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_BAY)
            {
                if (Minerals() >= 150
                    && Gas() >= 150
                    && !Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(50)
                    && Count(UnitTypes.COLLOSUS) > 0)
                {
                    agent.Order(1097);
                }
            }
            /*
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_FACILITY)
            {
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                if ((Count(UnitTypes.OBSERVER) == 0 || Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) >= 3)
                    && Count(UnitTypes.OBSERVER) < 3
                    && Minerals() >= 25
                    && Gas() >= 75)
                {
                    agent.Order(977);
                }
                else if (Completed(UnitTypes.ROBOTICS_BAY) > 0
                    && Minerals() >= 150
                    && Gas() >= 150
                    && Count(UnitTypes.IMMORTAL) >= 6
                    && Count(UnitTypes.DISRUPTOR) < 4
                    && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BATTLECRUISER) == 0
                    && (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) < 10 || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARAUDER) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARINE) > tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) * 2))
                {
                    agent.Order(994);
                }
                else if (Minerals() >= 275
                    && Gas() >= 100
                    && (Count(UnitTypes.DISRUPTOR) >= 4 || Completed(UnitTypes.ROBOTICS_BAY) == 0 || Count(UnitTypes.IMMORTAL) < 6)
                    && Count(UnitTypes.IMMORTAL) < DesiredImmortals
                    && (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) < 10 || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARAUDER) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARINE) > tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) * 2 || Count(UnitTypes.IMMORTAL) < 6)
                    && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) < 5)
                {
                    agent.Order(979);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TEMPLAR_ARCHIVE)
            {
            }
            else if (agent.Unit.UnitType == UnitTypes.STARGATE)
            {
                if (Completed(UnitTypes.FLEET_BEACON) > 0
                    && Minerals() >= 250
                    && Gas() >= 175)
                    agent.Order(955);
            }
            */
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {

                if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(87)
                     && Minerals() >= 150
                     && Gas() >= 150
                    && Completed(UnitTypes.STALKER) > 0)
                    agent.Order(1593);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100
                    && Completed(UnitTypes.ADEPT) > 0)
                    agent.Order(1594);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                         && Minerals() >= 100
                         && Gas() >= 100
                         && Completed(UnitTypes.ZEALOT) > 0)
                    agent.Order(1592);
            }
        }
    }
}
