using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class TurtleLords : Build
    {
        //private bool SmellCheese;
        private bool CannonRush = false;
        private bool StalkerDefense = false;
        

        public override string Name()
        {
            return "TurtleLords";
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
        }

        public override Build OverrideBuild()
        {
            return ZergBuildUtil.GetDefenseBuild();
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new CorruptorController());
            MicroControllers.Add(new QueenTransfuseController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new TargetFireController(GetPriorities()));
            //Set += DefendZealots();
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

        /*
        private BuildList DefendZealots()
        {
            BuildList result = new BuildList();
            result.If(() => { return SmellCheese; });
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Building(UnitTypes.SPINE_CRAWLER, Main, MainDefensePos, 2, () => { return !CannonRush; });
            result.Building(UnitTypes.ROACH_WARREN);
            result.Building(UnitTypes.EXTRACTOR);
            result.Building(UnitTypes.EXTRACTOR, () => { return Count(UnitTypes.ROACH) >= 5; });
            return result;
        }
        */

        private BuildList MainBuild()
        {
            Point2D spinePos = Tyr.Bot.MapAnalyzer.Walk(NaturalDefensePos, Tyr.Bot.MapAnalyzer.EnemyDistances, 5);

            BuildList result = new BuildList();
            //result.If(() => { return !SmellCheese; });
            result.Building(UnitTypes.HATCHERY, 2, () => { return Count(UnitTypes.HATCHERY) + Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) < 2; });
            result.Building(UnitTypes.SPAWNING_POOL);
            result.If(() => { return Count(UnitTypes.QUEEN) > 0; });
            result.If(() => { return Completed(UnitTypes.HATCHERY) + Completed(UnitTypes.LAIR) >= 2 && Tyr.Bot.Frame >= 22.4 * 60 * 2; });
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, spinePos, 2);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, spinePos, 2, () => { return StalkerDefense; });
            result.Building(UnitTypes.ROACH_WARREN);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.If(() => { return Count(UnitTypes.ROACH) >= 8; });
            result.Building(UnitTypes.SPIRE, () => { return Count(UnitTypes.GREATER_SPIRE) == 0; });
            result.Building(UnitTypes.INFESTATION_PIT);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, spinePos, 2, () => { return !StalkerDefense; });
            result.Building(UnitTypes.EXTRACTOR, 2);
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            TimingAttackTask.Task.RequiredSize = 32;
            TimingAttackTask.Task.RetreatSize = 10;

            DefenseTask.Task.MainDefenseRadius = 20;
            DefenseTask.Task.ExpandDefenseRadius = 20;
            DefenseTask.Task.MaxDefenseRadius = 55;
            /*
            if ((!SmellCheese && tyr.Frame >= 22.4 * 60 * 1.5
                && !tyr.EnemyStrategyAnalyzer.NoProxyGatewayConfirmed)
                || (!SmellCheese && tyr.Frame < 22.4 * 60 * 1.5 && tyr.EnemyStrategyAnalyzer.ThreeGateDetected))
            {
                SmellCheese = true;
                TimingAttackTask.RequiredSize = 18;
                TimingAttackTask.Stopped = false;
            }
            */


            if (tyr.Frame < 22.4 * 60 * 1.5 && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.FORGE) > 0)
                CannonRush = true;

            if (tyr.Frame < 22.4 * 60 * 2 && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.CYBERNETICS_CORE) > 0
                && tyr.EnemyStrategyAnalyzer.ThreeGateDetected)
            {
                StalkerDefense = true;
                DefenseTask.Task.ExpandDefenseRadius = 13;
            }

            //if (SmellCheese && CannonRush)
            //    TimingAttackTask.RequiredSize = 12;

            if (Completed(UnitTypes.LAIR) > 0
                && Count(UnitTypes.OVERSEER) == 0)
                MorphingTask.Task.Morph(UnitTypes.OVERSEER);

            /*
            if (SmellCheese)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                {
                    if (Count(UnitTypes.HATCHERY) >= 2 && agent.Unit.UnitType == UnitTypes.HATCHERY
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);

                    if (agent.Unit.UnitType == UnitTypes.LARVA)
                    {
                        if (Count(UnitTypes.DRONE) >= 14 && Count(UnitTypes.SPAWNING_POOL) == 0)
                            break;
                        if (Minerals() >= 75 && Gas() >= 25
                            && ExpectedAvailableFood() > FoodUsed() + 2
                            && Completed(UnitTypes.ROACH_WARREN) > 0)
                        {
                            agent.Order(1351);
                            CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.ROACH);
                        }
                        else if (Minerals() >= 50
                            && ExpectedAvailableFood() > FoodUsed() + 2
                            && (Count(UnitTypes.DRONE) < 16 - Completed(UnitTypes.EXTRACTOR) || Count(UnitTypes.ROACH) >= 5)
                            && Count(UnitTypes.DRONE) < 22 - Completed(UnitTypes.EXTRACTOR))
                        {
                            agent.Order(1342);
                            CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.DRONE);
                        }
                        else if (Minerals() >= 100 && FoodUsed()
                            + Tyr.Bot.UnitManager.Count(UnitTypes.HATCHERY) * 2
                            + Tyr.Bot.UnitManager.Count(UnitTypes.LAIR) * 2
                            + Tyr.Bot.UnitManager.Count(UnitTypes.HIVE) * 2
                            >= ExpectedAvailableFood() - 2)
                        {
                            agent.Order(1344);
                            CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.OVERLORD);
                            tyr.UnitManager.FoodExpected += 8;
                        }
                    }
                }

                return;
            }
            
            */
            
            TimingAttackTask.Task.Stopped = Completed(UnitTypes.BROOD_LORD) < 6;

            if (Count(UnitTypes.CORRUPTOR) > 10 && Completed(UnitTypes.GREATER_SPIRE) > 0)
                MorphingTask.Task.Morph(UnitTypes.BROOD_LORD);

            if (Completed(UnitTypes.LAIR) > 0
                && Count(UnitTypes.OVERSEER) < 2)
                MorphingTask.Task.Morph(UnitTypes.OVERSEER);


            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.LARVA)
                {
                    if (Count(UnitTypes.DRONE) >= 14 && Count(UnitTypes.SPAWNING_POOL) == 0)
                        break;
                    if (Minerals() >= 50 && StalkerDefense 
                        && ExpectedAvailableFood() > FoodUsed() + 2
                        && Count(UnitTypes.ZERGLING) < 0
                        && Completed(UnitTypes.SPAWNING_POOL) > 0
                        && (Count(UnitTypes.SPINE_CRAWLER) >= 2 || Minerals() >= 200))
                    {
                        agent.Order(1343);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.ZERGLING);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.ZERGLING);
                    }
                    if (Minerals() >= 75 && Gas() >= 25 && ExpectedAvailableFood() > FoodUsed() + 6
                        && Count(UnitTypes.ROACH) < 8
                        && Completed(UnitTypes.ROACH_WARREN) > 0)
                    {
                        agent.Order(1351);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.ROACH);
                    }
                    else if (Minerals() >= 50
                        && ExpectedAvailableFood() > FoodUsed() + 2
                        && Count(UnitTypes.DRONE) < 40 - Completed(UnitTypes.EXTRACTOR)
                        && (Count(UnitTypes.ROACH) >= 8 || Count(UnitTypes.DRONE) <= 24))
                    {
                        agent.Order(1342);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.DRONE);
                    }
                    else if (Minerals() >= 150
                        && Gas() >= 100
                        && Completed(UnitTypes.SPIRE) + Completed(UnitTypes.GREATER_SPIRE) > 0
                        && (Completed(UnitTypes.GREATER_SPIRE) > 0 || Count(UnitTypes.CORRUPTOR) < 8)
                        && Count(UnitTypes.CORRUPTOR) < 16
                        && ExpectedAvailableFood() > FoodUsed() + 4)
                    {
                        agent.Order(1353);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.CORRUPTOR);
                    }
                    else if (Minerals() >= 100 && FoodUsed()
                        + Tyr.Bot.UnitManager.Count(UnitTypes.HATCHERY) * 2
                        + Tyr.Bot.UnitManager.Count(UnitTypes.LAIR) * 2
                        + Tyr.Bot.UnitManager.Count(UnitTypes.HIVE) * 2
                        >= ExpectedAvailableFood() - 2)
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
                if (Minerals() >= 100 && Completed(UnitTypes.QUEEN) < 2
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Minerals() >= 100
                    && Completed(UnitTypes.QUEEN) < 3
                    && (Count(UnitTypes.LAIR) > 0))
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Minerals() >= 150
                    && Completed(UnitTypes.QUEEN) < 6
                    && StalkerDefense)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Completed(UnitTypes.QUEEN) < 5
                    && Count(UnitTypes.CORRUPTOR) >= 10
                    && Minerals() >= 400)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Count(UnitTypes.CORRUPTOR) >= 8
                    && Count(UnitTypes.HIVE) > 0
                    && Minerals() >= 600)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Count(UnitTypes.SPINE_CRAWLER) > 0
                    && Minerals() >= 150 && Gas() >= 100
                    && Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) == 0
                    && Count(UnitTypes.ROACH) >= 8)
                    agent.Order(1216);
                else if (agent.Unit.UnitType == UnitTypes.LAIR
                    && Completed(UnitTypes.INFESTATION_PIT) > 0
                    && Minerals() >= 200 && Gas() >= 150
                    && Count(UnitTypes.ROACH) >= 8)
                    agent.Order(1218);
            }
            else if (agent.Unit.UnitType == UnitTypes.SPIRE)
            {
                if (Completed(UnitTypes.HIVE) > 0
                    && Minerals() >=100
                    && Gas() >= 150)
                {
                    agent.Order(1220);
                }
            }
        }
    }
}