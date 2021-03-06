﻿using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Zerg
{
    public class RoachRavager : Build
    {
        private bool SmellCheese = false;
        private bool NeedAntiAir = false;
        private bool SpinePushDetected = false;
        FearEnemyController FearSpinesController = new FearEnemyController(new HashSet<uint>() { UnitTypes.QUEEN, UnitTypes.ROACH, UnitTypes.RAVAGER }, UnitTypes.SPINE_CRAWLER, 10) { DefendHome = false };
        

        public override string Name()
        {
            return "RoachRavager";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            QueenInjectTask.Enable();
            QueenDefenseTask.Enable();
            ArmyOverseerTask.Enable();
            QueenTumorTask.Enable();
            DefenseTask.Enable();
            OverlordScoutTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new SwarmHostController());
            MicroControllers.Add(new InfestorController());
            MicroControllers.Add(new RavagerController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new QueenTransfuseController());
            MicroControllers.Add(FearSpinesController);
            MicroControllers.Add(new StutterController());
            //MicroControllers.Add(new TargetFireController(GetPriorities()));

            Set += ZergBuildUtil.Overlords();
            Set += MainBuild();
        }

        public PriorityTargetting GetPriorities()
        {
            PriorityTargetting priorities = new PriorityTargetting();

            priorities.DefaultPriorities.MaxRange = 10;
            priorities.DefaultPriorities[UnitTypes.LARVA] = -1;
            priorities.DefaultPriorities[UnitTypes.EGG] = -1;

            foreach (uint t in UnitTypes.CombatUnitTypes)
                priorities[UnitTypes.HYDRALISK][t] = 1;

            return priorities;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Morph(UnitTypes.DRONE, 14);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Building(UnitTypes.HATCHERY, 2, () => !SmellCheese || Count(UnitTypes.ROACH) >= 15);
            result.Morph(UnitTypes.DRONE, 4, () => !SmellCheese || Count(UnitTypes.ROACH) >= 15);
            result.Building(UnitTypes.SPINE_CRAWLER, Main, MainDefensePos, 2, () => SmellCheese);
            result.If(() => !SmellCheese || Completed(UnitTypes.SPINE_CRAWLER) >= 2);
            result.Building(UnitTypes.ROACH_WARREN, () => SmellCheese);
            //result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, 2, () => !SmellCheese && Natural.ResourceCenter != null && Natural.ResourceCenter.Unit.BuildProgress >= 0.99);
            result.Building(UnitTypes.EXTRACTOR, () => SmellCheese);
            result.Morph(UnitTypes.ROACH, 4, () => SmellCheese);
            result.Morph(UnitTypes.ZERGLING, 8, () => Completed(UnitTypes.ROACH) < 4 && SmellCheese);
            result.Morph(UnitTypes.ZERGLING, 6, () => Completed(UnitTypes.ROACH) < 4 && SmellCheese);
            result.Morph(UnitTypes.DRONE, 2);
            result.Building(UnitTypes.ROACH_WARREN, () => !SmellCheese);
            result.Building(UnitTypes.EXTRACTOR, () => !SmellCheese);
            result.Morph(UnitTypes.DRONE, 5, () => !SmellCheese || Count(UnitTypes.ROACH) >= 20);
            result.Morph(UnitTypes.ROACH, 4);
            result.Building(UnitTypes.SPINE_CRAWLER, Main, MainDefensePos, () => SmellCheese && Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SPINE_CRAWLER) == 0);
            result.Morph(UnitTypes.DRONE, 5);
            result.Building(UnitTypes.EVOLUTION_CHAMBER, 2, () => !SmellCheese || Count(UnitTypes.ROACH) >= 10);
            result.Upgrade(UpgradeType.ZergMissileWeapons, () => !SmellCheese || Count(UnitTypes.ROACH) >= 10);
            result.Upgrade(UpgradeType.ZergGroundArmor, () => !SmellCheese || Count(UnitTypes.ROACH) >= 10);
            result.Morph(UnitTypes.DRONE, 10, () => !SmellCheese || Count(UnitTypes.ROACH) >= 20);
            result.Building(UnitTypes.EXTRACTOR, () => !SmellCheese || SpinePushDetected);
            result.Morph(UnitTypes.ROACH, 10);
            result.Building(UnitTypes.INFESTATION_PIT, () => SpinePushDetected);
            result.Morph(UnitTypes.SWARM_HOST, 15, () => SpinePushDetected);
            result.Building(UnitTypes.HYDRALISK_DEN, () => NeedAntiAir);
            result.Building(UnitTypes.EXTRACTOR, 2, () => NeedAntiAir);
            result.Morph(UnitTypes.HYDRALISK, 10, () => NeedAntiAir);
            result.Upgrade(UpgradeType.GroovedSpines, () => NeedAntiAir);
            result.Upgrade(UpgradeType.MuscularAugments, () => NeedAntiAir);
            result.Building(UnitTypes.EXTRACTOR);
            result.Morph(UnitTypes.ROACH, 6, () => SmellCheese);
            result.Building(UnitTypes.EXTRACTOR, () => SmellCheese);
            result.If(() => !SmellCheese || Count(UnitTypes.ROACH) >= 20);
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.DRONE, 20);
            result.Morph(UnitTypes.OVERSEER, 2);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Building(UnitTypes.INFESTATION_PIT, () => !SpinePushDetected);
            result.Building(UnitTypes.EXTRACTOR, 4);
            result.Upgrade(UpgradeType.PathogenGlands);
            result.Morph(UnitTypes.INFESTOR, 3, () => Completed(UnitTypes.INFESTATION_PIT) > 0 && !SpinePushDetected);
            result.Morph(UnitTypes.ROACH, 10);
            result.Morph(UnitTypes.INFESTOR, 3, () => Completed(UnitTypes.INFESTATION_PIT) > 0 && !SpinePushDetected);
            result.Morph(UnitTypes.ROACH, 10, () => !NeedAntiAir);
            result.Building(UnitTypes.HATCHERY, 2);
            result.Morph(UnitTypes.HYDRALISK, 10, () => NeedAntiAir);
            result.Morph(UnitTypes.DRONE, 10);
            result.Building(UnitTypes.EXTRACTOR, 5);
            result.Morph(UnitTypes.ROACH, 100);
            result.Train(UnitTypes.HIVE);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (SpinePushDetected)
            {
                TimingAttackTask.Task.RequiredSize = 30;
                TimingAttackTask.Task.RetreatSize = 10;
            } else
            {
                TimingAttackTask.Task.RequiredSize = 45;
                TimingAttackTask.Task.RetreatSize = 15;
            }

            MorphingTask.Task.Priority = 10;

            if (!SmellCheese || Completed(UnitTypes.ROACH) >= 10 || Natural.Owner == bot.PlayerId)
                IdleTask.Task.OverrideTarget = null;
            else
                IdleTask.Task.OverrideTarget = Main.BaseLocation.Pos;

            if (SmellCheese && Completed(UnitTypes.ROACH) < 15)
            {
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
                DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 20;
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 20;
                DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;

                foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                {
                    task.DefenseRadius = 8;
                }
            }
            else
            {
                IdleTask.Task.OverrideTarget = null;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
                DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            }
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = SmellCheese && (Completed(UnitTypes.SPINE_CRAWLER) >= 2 || Completed(UnitTypes.ROACH) >= 4 || Completed(UnitTypes.ZERGLING) >= 10);

            if (!SpinePushDetected && SmellCheese)
            {
                foreach(Unit enemy in bot.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.QUEEN && enemy.UnitType != UnitTypes.SPINE_CRAWLER && enemy.UnitType != UnitTypes.SPINE_CRAWLER_UPROOTED)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, bot.MapAnalyzer.StartLocation) <= 50 * 50)
                    {
                        SpinePushDetected = true;
                        break;
                    }
                }
            }

            if (Completed(UnitTypes.DRONE) <= 10)
                GasWorkerTask.WorkersPerGas = 0;
            else
                BalanceGas();

            //SmellCheese = EarlyPool.Get().Detected && !Expanded.Get().Detected && Completed(UnitTypes.ROACH) < 2;
            if (Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) >= 5 && bot.Frame <= 22.4 * 60 * 2)
                SmellCheese = true;
            if (Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SPAWNING_POOL) > 0 && bot.Frame <= 22.4 * 60 * 1.4 && !Expanded.Get().Detected)
                SmellCheese = true;
            if (SmellCheese && Count(UnitTypes.ROACH) + Count(UnitTypes.RAVAGER) < 13)
            {
                /*
                TimingAttackTask.RetreatSize = 5;
                TimingAttackTask.RequiredSize = 25;
                */
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.HATCHERY
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }

            if (bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MUTALISK)
                    + bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BROOD_LORD)
                    + bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.CORRUPTOR)
                    + bot.EnemyStrategyAnalyzer.Count(UnitTypes.SPIRE)
                    + bot.EnemyStrategyAnalyzer.Count(UnitTypes.GREATER_SPIRE) > 0)
                NeedAntiAir = true;

            if (Completed(UnitTypes.ROACH_WARREN) > 0
                && Count(UnitTypes.RAVAGER) < 8
                && Count(UnitTypes.RAVAGER) < Completed(UnitTypes.ROACH) - 8
                && (!SmellCheese || Completed(UnitTypes.HATCHERY) >= 2))
                MorphingTask.Task.Morph(UnitTypes.RAVAGER);

            if (bot.EnemyStrategyAnalyzer.Count(UnitTypes.QUEEN)
                + bot.EnemyStrategyAnalyzer.Count(UnitTypes.HYDRALISK)
                + bot.EnemyStrategyAnalyzer.Count(UnitTypes.MUTALISK)
                + bot.EnemyStrategyAnalyzer.Count(UnitTypes.CORRUPTOR) > 0
                || bot.Frame >= 22.4 * 60 * 5)
            {
                OverlordScoutTask.Task.Stopped = true;
                OverlordScoutTask.Task.Clear();
            }

            bool saveForUpgrades = Completed(UnitTypes.EVOLUTION_CHAMBER) >= 2
                && Minerals() < 200
                && Gas() < 200
                && Count(UnitTypes.ROACH) >= 5
                && ((!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(57)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1190)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1189))
                    || (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(60)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1192)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1193)));
        }

        public override void Produce(Bot bot, Agent agent)
        {
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 150 && Completed(UnitTypes.QUEEN) < 3
                    && AvailableFood() >= 2
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Minerals() >= 150 && Completed(UnitTypes.QUEEN) < 5
                    && AvailableFood() >= 2
                    && Count(UnitTypes.LAIR) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Minerals() >= 150 && Gas() >= 100
                    && Count(UnitTypes.LAIR) == 0
                    && Count(UnitTypes.ROACH) >= 8)
                    agent.Order(1216);
            }
            else if (agent.Unit.UnitType == UnitTypes.ROACH_WARREN)
            {
                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(2)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(216);
            }
        }
    }
}