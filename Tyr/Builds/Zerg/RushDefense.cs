using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Zerg
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
            WorkerRushDefenseTask.Enable();
            DefenseTask.Enable();
            DefenseSquadTask.Enable(false, UnitTypes.HYDRALISK);
            //WorkerRushDefenseTask.Enable();
        }

        public override void OnStart(Bot bot)
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
            result.If(() => { return !StrategyAnalysis.WorkerRush.Get().Detected || (FoodUsed() == ExpectedAvailableFood() && Count(UnitTypes.ZERGLING) >= 4) || Count(UnitTypes.ZERGLING) >= 6; });
            result.If(() => { return Count(UnitTypes.SPAWNING_POOL) > 0 && FoodUsed() >= ExpectedAvailableFood() - 2; });
            result.Morph(UnitTypes.OVERLORD, 25);
            return result;
        }

        private BuildList DefendWorkerRush()
        {
            BuildList result = new BuildList();
            result.If(() =>
            {
                return StrategyAnalysis.WorkerRush.Get().Detected;
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

                if (!Lifting.Get().Detected)
                    return false;
                // First destroy non-lifted buildings.
                foreach (Unit enemy in Bot.Main.Enemies())
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
                return !StrategyAnalysis.WorkerRush.Get().Detected || Count(UnitTypes.ZERGLING) >= 10;
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
            result.Morph(UnitTypes.ROACH, 5, () => { return !ReaperRush.Get().Detected; });
            result.Morph(UnitTypes.OVERSEER);
            result.Building(UnitTypes.HYDRALISK_DEN);
            result.Morph(UnitTypes.ROACH, 4, () => { return !ReaperRush.Get().Detected; } );
            result.Morph(UnitTypes.HYDRALISK, 5);
            result.Morph(UnitTypes.OVERSEER);
            result.Morph(UnitTypes.HYDRALISK, 80);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            WorkerScoutTask.Task.Stopped = bot.Frame >= 40 * 22.4 || StrategyAnalysis.WorkerRush.Get().Detected;
            if (WorkerScoutTask.Task.Stopped)
                WorkerScoutTask.Task.Clear();

            if (StrategyAnalysis.WorkerRush.Get().Detected)
                foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                    task.DefenseRadius = 6;

            DefenseTask.GroundDefenseTask.Stopped = ReaperRush.Get().Detected && Completed(UnitTypes.HYDRALISK) + Completed(UnitTypes.ROACH) >= 25;
            if (DefenseTask.GroundDefenseTask.Stopped)
                DefenseTask.GroundDefenseTask.Clear();
            DefenseTask.AirDefenseTask.Stopped = ReaperRush.Get().Detected && Completed(UnitTypes.HYDRALISK) + Completed(UnitTypes.ROACH) >= 25;
            if (DefenseTask.AirDefenseTask.Stopped)
                DefenseTask.AirDefenseTask.Clear();

            TimingAttackTask.Task.RetreatSize = 5;
            TimingAttackTask.Task.RequiredSize = ReaperRush.Get().Detected && !TimingAttackTask.Task.AttackSent ? 15 : 25;
            TimingAttackTask.Task.DefendOtherAgents = false;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 55;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 20;
            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 55;

            GasWorkerTask.WorkersPerGas = 3;

            if (!TakeExpand)
                TakeExpand = Completed(UnitTypes.ROACH) + Completed(UnitTypes.HYDRALISK) + Completed(UnitTypes.QUEEN) >= TimingAttackTask.Task.RequiredSize;

            if (!TakeExpand)
            {
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.HATCHERY
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }
        }

        public override void Produce(Bot bot, Agent agent)
        {
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 150 && Completed(UnitTypes.QUEEN) < 2
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.QUEEN);
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
                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(134)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(1282);
            }
            else if (agent.Unit.UnitType == UnitTypes.EVOLUTION_CHAMBER)
            {
                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(59)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1192))
                    agent.Order(1192);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(56)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1189))
                    agent.Order(1189);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(60)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1193))
                    agent.Order(1193);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(57)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1190))
                    agent.Order(1190);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(61)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1194))
                    agent.Order(1194);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(58)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1191))
                    agent.Order(1191);
            }
        }
    }
}
