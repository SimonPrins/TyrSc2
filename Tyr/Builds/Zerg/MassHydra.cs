using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Zerg
{
    public class MassHydra : Build
    {
        TimingAttackTask TimingAttackTask = new TimingAttackTask() { RequiredSize = 60, RetreatSize = 20 };
        private bool SmellCheese = false;
        private static int RequiredZerglings = 14;
        private DefenseTask DefenseTask = new DefenseTask() { MainDefenseRadius = 20, ExpandDefenseRadius = 20, MaxDefenseRadius = 55 };
        private List<QueenInjectTask> QueenInjectTasks = new List<QueenInjectTask>();
        

        public override string Name()
        {
            return "MassHydra";
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new TargetFireController(GetPriorities()));
            bot.TaskManager.Add(TimingAttackTask);
            bot.TaskManager.Add(new WorkerScoutTask());

            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                QueenInjectTask queenInjectTask = new QueenInjectTask(b);
                bot.TaskManager.Add(queenInjectTask);
                QueenInjectTasks.Add(queenInjectTask);
                if (b != bot.BaseManager.Main && b != bot.BaseManager.Natural)
                    bot.TaskManager.Add(new DefenseSquadTask(b, UnitTypes.HYDRALISK));
            }
            bot.TaskManager.Add(new QueenDefenseTask());
            bot.TaskManager.Add(DefenseTask);
            Set += RushDefense();
            Set += AntiLifting();
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

        private BuildList RushDefense()
        {
            BuildList result = new BuildList();
            result.If(() => { return SmellCheese; });
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Building(UnitTypes.SPINE_CRAWLER, Main, MainDefensePos, 2);
            result.Building(UnitTypes.EXTRACTOR);
            result.If(() => { return Count(UnitTypes.QUEEN) > 0; });
            result.Building(UnitTypes.ROACH_WARREN);
            result.If(() => { return Count(UnitTypes.ROACH) >= 3; });
            result.Building(UnitTypes.EXTRACTOR);
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

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.If(() => { return !SmellCheese; });
            result.Building(UnitTypes.HATCHERY, 2);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.If(() => { return Count(UnitTypes.QUEEN) > 0; });
            result.If(() => { return Completed(UnitTypes.HATCHERY) + Completed(UnitTypes.LAIR) >= 2 && Bot.Main.Frame >= 22.4 * 60 * 2; });
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, 2);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, () => { return SmellCheese; });
            result.If(() => { return Count(UnitTypes.ZERGLING) >= RequiredZerglings && Count(UnitTypes.DRONE) >= 20; });
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Building(UnitTypes.HYDRALISK_DEN);
            result.Building(UnitTypes.EXTRACTOR, 4);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (FourRax.Get().Detected
                || (bot.EnemyRace == Race.Terran && bot.Frame >= 22.4 * 85 && !bot.EnemyStrategyAnalyzer.NoProxyTerranConfirmed && bot.TargetManager.PotentialEnemyStartLocations.Count == 1)
                || ReaperRush.Get().Detected)
            {
                SmellCheese = true;
                TimingAttackTask.RequiredSize = 20;
                TimingAttackTask.RetreatSize = 6;
                foreach (QueenInjectTask queenInjectTask in QueenInjectTasks)
                    queenInjectTask.Priority = 6;
                DefenseTask.ExpandDefenseRadius = 16;
            }

            if (SmellCheese)
            {

                foreach (Agent agent in bot.UnitManager.Agents.Values)
                {
                    if (Count(UnitTypes.HATCHERY) >= 2 && agent.Unit.UnitType == UnitTypes.HATCHERY
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
                    if (agent.Unit.UnitType == UnitTypes.LARVA)
                    {
                        if (Count(UnitTypes.DRONE) >= 11 && Count(UnitTypes.SPAWNING_POOL) == 0)
                            break;
                        if (Minerals() >= 50
                            && ExpectedAvailableFood() > FoodUsed() + 2
                            && Count(UnitTypes.DRONE) < 17 - Completed(UnitTypes.EXTRACTOR))
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
                        else if (Minerals() >= 75 && Gas() >= 25 && ExpectedAvailableFood() > FoodUsed() + 6
                            && (!Lifting.Get().Detected || Count(UnitTypes.ROACH) < 25))
                        {
                            agent.Order(1351);
                            CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.ROACH);
                        }
                        else if (Minerals() >= 100 && FoodUsed() > ExpectedAvailableFood() - 16)
                        {
                            agent.Order(1344);
                            CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.OVERLORD);
                            bot.UnitManager.FoodExpected += 8;
                        }
                    }
                }
                return;
            }
            
            if (Completed(UnitTypes.LAIR) > 0
                && Count (UnitTypes.OVERSEER) == 0)
                MorphingTask.Task.Morph(UnitTypes.OVERSEER);

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.LARVA)
                {
                    if (Count(UnitTypes.DRONE) >= 14 && Count(UnitTypes.SPAWNING_POOL) == 0)
                        break;
                    if (Minerals() >= 50
                        && ExpectedAvailableFood() > FoodUsed() + 2
                        && Count(UnitTypes.DRONE) < 45 - Completed(UnitTypes.EXTRACTOR)
                        && (Count(UnitTypes.DRONE) < 40 - Completed(UnitTypes.EXTRACTOR) || Count(UnitTypes.HATCHERY) + Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) >= 3)
                        && (Count(UnitTypes.ZERGLING) >= RequiredZerglings || Count(UnitTypes.DRONE) <= 18))
                    {
                        agent.Order(1342);
                        CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.DRONE);
                    }
                    else if (Completed(UnitTypes.SPAWNING_POOL) > 0
                        && Count(UnitTypes.ZERGLING) < RequiredZerglings
                        && Minerals() >= 50
                        && ExpectedAvailableFood() > FoodUsed() + 4
                        && (Count(UnitTypes.SPINE_CRAWLER) >= 2 || Minerals() >= 200))
                    {
                        agent.Order(1343);
                        CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.ZERGLING);
                        CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.ZERGLING);
                    }
                    else if (Completed(UnitTypes.HYDRALISK_DEN) > 0
                        && Minerals() >= 100
                        && Gas() >= 50
                        && ExpectedAvailableFood() > FoodUsed() + 4)
                    {
                        agent.Order(1345);
                        CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.HYDRALISK);
                    }
                    else if (Minerals() >= 100 && FoodUsed()
                        + Bot.Main.UnitManager.Count(UnitTypes.HATCHERY) * 2
                        + Bot.Main.UnitManager.Count(UnitTypes.LAIR) * 2
                        + Bot.Main.UnitManager.Count(UnitTypes.HIVE) * 2
                        >= ExpectedAvailableFood() - 2)
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
            if (SmellCheese)
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
                return;
            }
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 100 && Completed(UnitTypes.QUEEN) < 3
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Minerals() >= 100 && Completed(UnitTypes.QUEEN) < 5
                    && Count(UnitTypes.LAIR) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Count(UnitTypes.SPINE_CRAWLER) > 0
                    && Minerals() >= 150 && Gas() >= 100
                    && Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) == 0
                    && Count(UnitTypes.ZERGLING) >= RequiredZerglings)
                    agent.Order(1216);
            }
        }
    }
}
