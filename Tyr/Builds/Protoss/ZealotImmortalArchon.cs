using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class ZealotImmortalArchon : Build
    {
        private Point2D OverrideDefenseTarget;
        private TimingAttackTask attackTask = new TimingAttackTask();
        private TimedObserverTask TimedObserverTask = new TimedObserverTask();
        private WorkerScoutTask WorkerScoutTask = new WorkerScoutTask() { StartFrame = 600 };
        private bool Attacking = false;
        private bool SmellCheese = false;
        private bool SpinePushDetected = false;
        private FearEnemyController FearSpinesController = new FearEnemyController(new HashSet<uint>() { UnitTypes.ADEPT , UnitTypes.STALKER }, UnitTypes.SPINE_CRAWLER, 10);
        private WallInCreator WallIn;
        private bool BansheeHarassDetected = false;
        private List<DefenseSquadTask> StalkerDefenseSquads;

        private bool BattlecruisersDetected = false;
        private bool FourRaxSuspected = false;

        private int DesiredStalkers;

        public override string Name()
        {
            return "ZealotImmortalArchon";
        }

        public override void OnStart(Bot bot)
        {
            DefenseTask.Enable();
            bot.TaskManager.Add(attackTask);
            bot.TaskManager.Add(WorkerScoutTask);
            ArmyObserverTask.Enable();
            bot.TaskManager.Add(new ObserverScoutTask() { Priority = 6 });
            bot.TaskManager.Add(new AdeptScoutTask());
            if (bot.BaseManager.Pocket != null)
                bot.TaskManager.Add(new ScoutProxyTask(bot.BaseManager.Pocket.BaseLocation.Pos));
            ArchonMergeTask.Enable();

            if (StalkerDefenseSquads == null)
                StalkerDefenseSquads = DefenseSquadTask.GetDefenseTasks(UnitTypes.STALKER);
            else
                foreach (DefenseSquadTask task in StalkerDefenseSquads)
                    Bot.Main.TaskManager.Add(task);
            DefenseSquadTask.Enable(StalkerDefenseSquads, true, true);

            OverrideDefenseTarget = bot.MapAnalyzer.Walk(NaturalDefensePos, bot.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(new SpreadOutController() { SpreadTypes = new HashSet<uint>() { UnitTypes.IMMORTAL, UnitTypes.ARCHON, UnitTypes.STALKER } });
            MicroControllers.Add(FearSpinesController);
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new FallBackController());
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

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) > 0 && (Completed(Natural, UnitTypes.PYLON) > 0 || Count(UnitTypes.PYLON) < 2 || Minerals() >= 500));
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

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => { return !EarlyPool.Get().Detected; });
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

        private BuildList Nexii()
        {
            BuildList result = new BuildList();

            result.If(() => { return Bot.Main.EnemyRace != Race.Terran || Count(UnitTypes.GATEWAY) >= 2; });
            result.If(() => { return Bot.Main.EnemyRace != Race.Zerg || Count(UnitTypes.GATEWAY) >= 1; });
            result.Building(UnitTypes.NEXUS, 2);
            result.If(() => { return Attacking; });
            result.Building(UnitTypes.NEXUS);

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.TEMPEST);
            result.Train(UnitTypes.ZEALOT, 1, () => TotalEnemyCount(UnitTypes.BANSHEE) < 5);
            result.Train(UnitTypes.STALKER, () => TotalEnemyCount(UnitTypes.BANSHEE) >= 5);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.OBSERVER, 3, () => TotalEnemyCount(UnitTypes.BANSHEE) >= 3);
            result.Train(UnitTypes.IMMORTAL, 10, () => TotalEnemyCount(UnitTypes.BANSHEE) < 5);
            result.Train(UnitTypes.STALKER, 2, () => TotalEnemyCount(UnitTypes.BANSHEE) < 5 && Count(UnitTypes.IMMORTAL) < 2);
            result.Train(UnitTypes.ZEALOT, 15, () => TotalEnemyCount(UnitTypes.BANSHEE) < 5 && Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) > 0);
            result.Train(UnitTypes.HIGH_TEMPLAR, () => Gas() >= 150);
            result.Train(UnitTypes.ZEALOT, () => TotalEnemyCount(UnitTypes.BANSHEE) < 5 && Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) > 0);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
            if (Bot.Main.EnemyRace != Race.Terran)
                result.If(() => { return !EarlyPool.Get().Detected || Expanded.Get().Detected || Completed(UnitTypes.STALKER) + Completed(UnitTypes.ADEPT) >= 5; });
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Upgrade(UpgradeType.WarpGate);
            result.Building(UnitTypes.PYLON, Natural, () => FourRaxSuspected);
            result.Building(UnitTypes.GATEWAY, Natural, () => FourRaxSuspected && Completed(Natural, UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, 2, () => FourRaxSuspected && Completed(Natural, UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.FORGE, () => FourRaxSuspected && Count(UnitTypes.GATEWAY) >= 3);
            result.Building(UnitTypes.PHOTON_CANNON, Natural, 4, () => FourRaxSuspected);
            result.Building(UnitTypes.ASSIMILATOR, () => FourRaxSuspected && Minerals() >= 600);
            result.Building(UnitTypes.ASSIMILATOR, () => FourRaxSuspected && Minerals() >= 800);
            //result.If(() => !FourRaxSuspected || Count(UnitTypes.STALKER) >= 10);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.PYLON, Natural, () => !FourRaxSuspected);
            result.Building(UnitTypes.ASSIMILATOR, () => !FourRaxSuspected || Count(UnitTypes.PHOTON_CANNON) >= 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) >= 3);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.FORGE, () => !FourRaxSuspected);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.TEMPLAR_ARCHIVE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !BattlecruisersDetected && Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) == 0);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ASSIMILATOR, 4, () => Minerals() >= 500 && Gas() < 200);
            //result.Building(UnitTypes.STARGATE);
            //result.Building(UnitTypes.FLEET_BEACON);
            //result.If(() => Count(UnitTypes.IMMORTAL) >= 3 && Count(UnitTypes.STALKER) >= 10); 
            //result.Building(UnitTypes.ROBOTICS_BAY, () => !BattlecruisersDetected && Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) == 0);
            result.Building(UnitTypes.STARGATE, () => BattlecruisersDetected || Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0);
            result.Building(UnitTypes.FLEET_BEACON, () => Completed(UnitTypes.STARGATE) > 0);
            //result.If(() => Count(UnitTypes.STALKER) >= 20);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY, Main, 2, () => Minerals() >= 500);
            result.Building(UnitTypes.GATEWAY, Main, 2, () => Minerals() >= 500);
            result.Building(UnitTypes.GATEWAY, Main, 2, () => Minerals() >= 500);
            result.If(() => Count(UnitTypes.ZEALOT) >= 20 && Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) >= 10);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.DISRUPTOR) + Completed(UnitTypes.ARCHON) + Completed(UnitTypes.TEMPEST) >= 40 || Minerals() >= 650);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);

            return result;
        }
        
        public override void OnFrame(Bot bot)
        {
            BalanceGas();

            if (bot.Observation.ActionErrors != null && bot.Observation.ActionErrors.Count > 0)
            {
                FileUtil.Debug("Errors for frame: " + bot.Frame);
                foreach (ActionError error in bot.Observation.ActionErrors)
                {
                    FileUtil.Debug(error.Result + " ability: " + error.AbilityId);
                }
                FileUtil.Debug("");
            }

            bot.NexusAbilityManager.PriotitizedAbilities.Add(917);

            if (bot.EnemyStrategyAnalyzer.Count(UnitTypes.BATTLECRUISER) > 0)
                BattlecruisersDetected = true;

            if (FourRax.Get().Detected)
                FourRaxSuspected = true;
            if (!FourRaxSuspected)
            {
                Point2D enemyRamp = bot.MapAnalyzer.GetEnemyRamp();
                int enemyBarrackWallCount = 0;
                foreach (Unit enemy in bot.Enemies())
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

            /*
            if (BattlecruisersDetected || 
                (bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) >= 12 && bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARINE) < 80))
            {
                attackTask.RequiredSize = 30;
                attackTask.RetreatSize = 8;
            }
            else if (Count(UnitTypes.ARCHON) + Count(UnitTypes.IMMORTAL) >= 14)
            {
                attackTask.RequiredSize = 35;
                attackTask.RetreatSize = 10;
            }
            else
            {
                attackTask.RequiredSize = 40;
                attackTask.RetreatSize = 10;
            }
            */
            attackTask.RequiredSize = 4;

            if (Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) >= attackTask.RequiredSize)
                Attacking = true;

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = Completed(UnitTypes.ZEALOT) >= 5;


            foreach (DefenseSquadTask task in StalkerDefenseSquads)
            {
                task.Stopped = (bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) < 3 && bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) == 0 || bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARINE) + bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MARAUDER) >= 10);
                task.MaxDefenders = Math.Min(5, Completed(UnitTypes.STALKER) / Math.Max(1, Count(UnitTypes.NEXUS)));
                task.Priority = 10;
            }

            if (bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 3 || bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0)
            {
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.VIKING_FIGHTER);
                DefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.REAPER);
                bot.buildingPlacer.SpreadCannons = true;
                attackTask.DefendOtherAgents = false;
            }

            FearSpinesController.Stopped = !SpinePushDetected;

            if (bot.EnemyRace == Race.Zerg)
            {
                if (bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MUTALISK)
                    + bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BROOD_LORD)
                    + bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.CORRUPTOR)
                    + bot.EnemyStrategyAnalyzer.Count(UnitTypes.SPIRE)
                    + bot.EnemyStrategyAnalyzer.Count(UnitTypes.GREATER_SPIRE) > 0)
                    DesiredStalkers = 15;
                else
                    DesiredStalkers = 2;
            }
            
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

            if (EarlyPool.Get().Detected && !Expanded.Get().Detected && Completed(UnitTypes.ZEALOT) < 2)
            {
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.NEXUS
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }
        }

        public override void Produce(Bot bot, Agent agent)
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
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_BAY)
            {
                if (Minerals() >= 150
                    && Gas() >= 150
                    && !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(50)
                    && Count(UnitTypes.COLOSUS) > 0)
                {
                    agent.Order(1097);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {

                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(87)
                     && Minerals() >= 150
                     && Gas() >= 150
                    && Completed(UnitTypes.STALKER) > 3)
                    agent.Order(1593);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100
                    && Completed(UnitTypes.ADEPT) > 0)
                    agent.Order(1594);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                         && Minerals() >= 100
                         && Gas() >= 100
                         && Completed(UnitTypes.ZEALOT) > 0)
                    agent.Order(1592);
            }
        }
    }
}
