using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class OneBaseRoach : Build
    {
        TimingAttackTask TimingAttackTask = new TimingAttackTask() { RequiredSize = 7 };
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

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new TargetFireController(GetPriorities()));
            tyr.TaskManager.Add(TimingAttackTask);
            if (tyr.EnemyRace != Race.Protoss)
                tyr.TaskManager.Add(new WorkerScoutTask());
            tyr.TaskManager.Add(new QueenInjectTask(Main));
            Set += MainBuild();
            Set += AntiLifting();
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Building(UnitTypes.EXTRACTOR);
            result.If(() => { return Count(UnitTypes.QUEEN) > 0; });
            result.Building(UnitTypes.ROACH_WARREN);
            return result;
        }

        private BuildList AntiLifting()
        {
            BuildList result = new BuildList();
            result.If(() => { return Tyr.Bot.EnemyStrategyAnalyzer.LiftingDetected; });
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Building(UnitTypes.SPIRE);
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (tyr.EnemyStrategyAnalyzer.FourRaxDetected
                || (tyr.Frame >= 22.4 * 85 && !tyr.EnemyStrategyAnalyzer.NoProxyTerranConfirmed && tyr.TargetManager.PotentialEnemyStartLocations.Count == 1)
                || tyr.EnemyStrategyAnalyzer.ReaperRushDetected)
            {
                SmellCheese = true;
            }

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) >= 8
                || tyr.EnemyStrategyAnalyzer.FourRaxDetected
                || SmellCheese)
            {
                TimingAttackTask.RequiredSize = 20;
                TimingAttackTask.Clear();
            }
            

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.LARVA)
                {
                    if (Count(UnitTypes.DRONE) >= 11 && Count(UnitTypes.SPAWNING_POOL) == 0)
                        break;
                    if (Minerals() >= 50
                        && ExpectedAvailableFood() > FoodUsed() + 2
                        && Count(UnitTypes.DRONE) < 17 - Completed(UnitTypes.EXTRACTOR))
                    {
                        agent.Order(1342);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.DRONE);
                    }
                    else if (Completed(UnitTypes.SPIRE) + Completed(UnitTypes.GREATER_SPIRE) > 0
                        && Minerals() >= 100
                        && Gas() >= 100
                        && ExpectedAvailableFood() > FoodUsed() + 4)
                    {
                        agent.Order(1346);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.MUTALISK);
                    }
                    else if (tyr.EnemyStrategyAnalyzer.LiftingDetected
                        && Count(UnitTypes.ROACH) >= 35)
                        break;
                    else if (Minerals() >= 75 && Gas() >= 25 && ExpectedAvailableFood() > FoodUsed() + 6)
                    {
                        agent.Order(1351);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.ROACH);
                    }
                    else if (Minerals() >= 100)
                    {
                        agent.Order(1344);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.OVERLORD);
                        tyr.UnitManager.FoodExpected += 8;
                    }
                }
            }
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 100 && Completed(UnitTypes.QUEEN) == 0
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                    agent.Order(1632);
                else if (Tyr.Bot.EnemyStrategyAnalyzer.LiftingDetected
                    && Minerals() >= 150 && Gas() >= 100)
                    agent.Order(1216);
            }
        }
    }
}
