using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Zerg
{
    public class OneBaseRoach : Build
    {
        TimingAttackTask TimingAttackTask = new TimingAttackTask() { RequiredSize = 12 };
        private bool SmellCheese = false;
        public override string Name()
        {
            return "OneBaseRoach";
        }

        public PriorityTargetting GetPriorities()
        {
            PriorityTargetting priorities = new PriorityTargetting();

            priorities.DefaultPriorities.MaxRange = 10;
            priorities.DefaultPriorities[UnitTypes.LARVA] = -1;

            priorities.DefaultPriorities[UnitTypes.BUNKER] = 1;
            priorities.DefaultPriorities[UnitTypes.SCV] = 2;
            priorities.DefaultPriorities[UnitTypes.MARAUDER] = 3;
            priorities.DefaultPriorities[UnitTypes.MARINE] = 4;
            priorities.DefaultPriorities[UnitTypes.REAPER] = 4;

            return priorities;
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new StutterController());
            //MicroControllers.Add(new TargetFireController(GetPriorities()));
            bot.TaskManager.Add(TimingAttackTask);
            if (bot.EnemyRace != Race.Protoss)
                bot.TaskManager.Add(new WorkerScoutTask());
            bot.TaskManager.Add(new QueenInjectTask(Main));
            Set += MainBuild();
            Set += AntiLifting();
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Building(UnitTypes.SPAWNING_POOL);
            //result.If(() => Completed(UnitTypes.SPAWNING_POOL) > 0);
            result.Building(UnitTypes.EXTRACTOR);
            result.If(() => { return Count(UnitTypes.QUEEN) > 0; });
            result.Building(UnitTypes.ROACH_WARREN);
            return result;
        }

        private BuildList AntiLifting()
        {
            BuildList result = new BuildList();
            result.If(() => { return Lifting.Get().Detected; });
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Building(UnitTypes.SPIRE);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            /*
            if (FourRax.Get().Detected
                || (bot.Frame >= 22.4 * 85 && !bot.EnemyStrategyAnalyzer.NoProxyTerranConfirmed && bot.TargetManager.PotentialEnemyStartLocations.Count == 1)
                || ReaperRush.Get().Detected)
            {
                SmellCheese = true;
            }

            if (bot.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) >= 8
                || FourRax.Get().Detected
                || SmellCheese)
            {
                TimingAttackTask.RequiredSize = 20;
                TimingAttackTask.Clear();
            }
            */

            if (Count(UnitTypes.DRONE) >= 17)
                GasWorkerTask.WorkersPerGas = 3;
            else
                GasWorkerTask.WorkersPerGas = 2;


            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.LARVA)
                {
                    if (Count(UnitTypes.DRONE) >= 11 && Count(UnitTypes.SPAWNING_POOL) == 0)
                        break;
                    if (Minerals() >= 50
                        && ExpectedAvailableFood() > FoodUsed() + 2
                        && Count(UnitTypes.DRONE) < 17 - Completed(UnitTypes.EXTRACTOR)
                        && (Count(UnitTypes.DRONE) < 14 || Completed(UnitTypes.ROACH) >= 11))
                    {
                        agent.Order(1342);
                        CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.DRONE);
                    }
                    else if (Completed(UnitTypes.SPIRE) + Completed(UnitTypes.GREATER_SPIRE) > 0
                        && Minerals() >= 100
                        && Gas() >= 100
                        && ExpectedAvailableFood() > FoodUsed() + 4)
                    {
                        agent.Order(1346);
                        CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.MUTALISK);
                    }
                    else if (Lifting.Get().Detected
                        && Count(UnitTypes.ROACH) >= 35)
                        break;
                    else if (Minerals() >= 75 && Gas() >= 25 && ExpectedAvailableFood() > FoodUsed() + 6)
                    {
                        agent.Order(1351);
                        CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.ROACH);
                    }
                    else if (Minerals() >= 100)
                    {
                        agent.Order(1344);
                        CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.OVERLORD);
                        bot.UnitManager.FoodExpected += 8;
                    }
                }
            }
        }

        public override void Produce(Bot bot, Agent agent)
        {
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 100 && Completed(UnitTypes.QUEEN) == 0
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                    agent.Order(1632);
                else if (Lifting.Get().Detected
                    && Minerals() >= 150 && Gas() >= 100)
                    agent.Order(1216);
            }
        }
    }
}
