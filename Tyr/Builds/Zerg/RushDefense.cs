using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class RushDefense : Build
    {
        private bool TakeExpand = false;
        public override string Name()
        {
            return "RushDefense";
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
            HydraDefenseTask.Enable(false);
            //WorkerRushDefenseTask.Enable();
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new RavagerController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new QueenTransfuseController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new TargetFireController(GetPriorities()));

            Set += Overlords();
            Set += DefendWorkerRush();
            Set += AntiLifting();
            Set += Expand();
            Set += MainBuild();
        }

        public PriorityTargetting GetPriorities()
        {
            PriorityTargetting priorities = new PriorityTargetting();

            priorities.DefaultPriorities.MaxRange = 10;
            priorities.DefaultPriorities[UnitTypes.LARVA] = -1;
            priorities.DefaultPriorities[UnitTypes.EGG] = -1;

            return priorities;
        }

        private BuildList Overlords()
        {
            BuildList result = new BuildList();
            result.If(() => { return !Tyr.Bot.EnemyStrategyAnalyzer.WorkerRushDetected || (FoodUsed() == ExpectedAvailableFood() && Count(UnitTypes.ZERGLING) >= 4) || Count(UnitTypes.ZERGLING) >= 6; });
            result.If(() => { return Count(UnitTypes.SPAWNING_POOL) > 0 && FoodUsed() >= ExpectedAvailableFood() - 2; });
            result.Morph(UnitTypes.OVERLORD, 25);
            return result;
        }

        private BuildList DefendWorkerRush()
        {
            BuildList result = new BuildList();
            result.If(() =>
            {
                return Tyr.Bot.EnemyStrategyAnalyzer.WorkerRushDetected;
            });
            result.Morph(UnitTypes.DRONE, 2);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Morph(UnitTypes.ZERGLING, 10);
            return result;
        }

        private BuildList AntiLifting()
        {
            BuildList result = new BuildList();
            result.If(() => {

                if (!Tyr.Bot.EnemyStrategyAnalyzer.LiftingDetected)
                    return false;
                // First destroy non-lifted buildings.
                foreach (Unit enemy in Tyr.Bot.Enemies())
                    if (UnitTypes.BuildingTypes.Contains(enemy.UnitType) && !enemy.IsFlying)
                        return false;
                return true;
            });
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Building(UnitTypes.SPIRE);
            result.Morph(UnitTypes.MUTALISK, 10);
            return result;
        }

        private BuildList Expand()
        {
            BuildList result = new BuildList();
            result.If(() => { return TakeExpand; });
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.DRONE, 22);
            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.If(() =>
            {
                return !Tyr.Bot.EnemyStrategyAnalyzer.WorkerRushDetected || Count(UnitTypes.ZERGLING) > 10;
            });
            result.Morph(UnitTypes.DRONE, 14);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Building(UnitTypes.SPINE_CRAWLER, Main, MainDefensePos, 2);
            result.Building(UnitTypes.EXTRACTOR);
            result.Morph(UnitTypes.DRONE, 4);
            result.If(() => { return Count(UnitTypes.QUEEN) > 0; });
            result.Building(UnitTypes.ROACH_WARREN);
            result.Morph(UnitTypes.ROACH, 6);
            result.If(() => { return Count(UnitTypes.LAIR) > 0; });
            result.Building(UnitTypes.EXTRACTOR);
            result.Morph(UnitTypes.ROACH, 5, () => { return !Tyr.Bot.EnemyStrategyAnalyzer.ReaperRushDetected; });
            result.Morph(UnitTypes.OVERSEER);
            result.Building(UnitTypes.HYDRALISK_DEN);
            result.Morph(UnitTypes.ROACH, 4, () => { return !Tyr.Bot.EnemyStrategyAnalyzer.ReaperRushDetected; } );
            result.Morph(UnitTypes.HYDRALISK, 5);
            result.Morph(UnitTypes.OVERSEER);
            result.Morph(UnitTypes.HYDRALISK, 80);
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            WorkerScoutTask.Task.Stopped = tyr.Frame >= 40 * 22.4 || tyr.EnemyStrategyAnalyzer.WorkerRushDetected;
            if (WorkerScoutTask.Task.Stopped)
                WorkerScoutTask.Task.Clear();

            if (tyr.EnemyStrategyAnalyzer.WorkerRushDetected)
                foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                    task.DefenseRadius = 6;
            
            DefenseTask.Task.Stopped = tyr.EnemyStrategyAnalyzer.ReaperRushDetected && Completed(UnitTypes.HYDRALISK) + Completed(UnitTypes.ROACH) >= 25;
            if (DefenseTask.Task.Stopped)
                DefenseTask.Task.Clear();

            TimingAttackTask.Task.RetreatSize = 5;
            TimingAttackTask.Task.RequiredSize = tyr.EnemyStrategyAnalyzer.ReaperRushDetected && !TimingAttackTask.Task.AttackSent ? 15 : 25;
            TimingAttackTask.Task.DefendOtherAgents = false;

            DefenseTask.Task.MainDefenseRadius = 20;
            DefenseTask.Task.ExpandDefenseRadius = 20;
            DefenseTask.Task.MaxDefenseRadius = 55;
            
            BaseWorkers.WorkersPerGas = 3;

            if (!TakeExpand)
                TakeExpand = Completed(UnitTypes.ROACH) + Completed(UnitTypes.HYDRALISK) + Completed(UnitTypes.QUEEN) >= TimingAttackTask.Task.RequiredSize;

            if (!TakeExpand)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.HATCHERY
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 150 && Completed(UnitTypes.QUEEN) < 2
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Count(UnitTypes.SPINE_CRAWLER) > 0
                    && Minerals() >= 150 && Gas() >= 100
                    && Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) == 0
                    && Count(UnitTypes.ROACH) >= 6)
                    agent.Order(1216);
            }
            else if (agent.Unit.UnitType == UnitTypes.ROACH_WARREN)
            {
                /*
                if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(2)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(216);
                    */
            }
            else if (agent.Unit.UnitType == UnitTypes.HYDRALISK_DEN)
            {
                if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(134)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(1282);
            }
            else if (agent.Unit.UnitType == UnitTypes.EVOLUTION_CHAMBER)
            {
                if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(59)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1192))
                    agent.Order(1192);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(56)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1189))
                    agent.Order(1189);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(60)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1193))
                    agent.Order(1193);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(57)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1190))
                    agent.Order(1190);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(61)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1194))
                    agent.Order(1194);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(58)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1191))
                    agent.Order(1191);
            }
        }
    }
}
