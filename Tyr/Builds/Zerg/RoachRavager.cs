using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class RoachRavager : Build
    {
        //private bool SmellCheese = false;
        private static int RequiredZerglings;
        

        public override string Name()
        {
            return "RoachRavager";
        }

        public override Build OverrideBuild()
        {
            return ZergBuildUtil.GetDefenseBuild();
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

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new RavagerController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new QueenTransfuseController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new TargetFireController(GetPriorities()));
            //Set += RushDefense();
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
        private BuildList RushDefense()
        {
            BuildList result = new BuildList();
            result.If(() => { return SmellCheese; });
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Building(UnitTypes.SPINE_CRAWLER, Main, MainDefensePos, 2);
            result.If(() => { return Count(UnitTypes.ZERGLING) >= RequiredZerglings; });
            result.Building(UnitTypes.EXTRACTOR);
            result.If(() => { return Count(UnitTypes.QUEEN) > 0; });
            result.Building(UnitTypes.ROACH_WARREN);
            result.If(() => { return Count(UnitTypes.ROACH) >= 3; });
            result.Building(UnitTypes.EXTRACTOR);
            return result;
        }
        */

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            //result.If(() => { return !SmellCheese; });
            result.Building(UnitTypes.HATCHERY, 2);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.If(() => { return Count(UnitTypes.QUEEN) > 0; });
            result.If(() => { return Completed(UnitTypes.HATCHERY) + Completed(UnitTypes.LAIR) >= 2 && Tyr.Bot.Frame >= 22.4 * 60 * 2; });
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, 2);
            result.If(() => { return Count(UnitTypes.ZERGLING) >= RequiredZerglings && Count(UnitTypes.DRONE) >= 20; });
            result.Building(UnitTypes.ROACH_WARREN);
            result.Building(UnitTypes.EXTRACTOR, 3);
            result.If(() => { return Count(UnitTypes.ROACH) >= 4; });
            result.Building(UnitTypes.EVOLUTION_CHAMBER, 2);
            result.If(() => { return Count(UnitTypes.ROACH) >= 8; });
            result.Building(UnitTypes.EXTRACTOR);
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            TimingAttackTask.Task.RequiredSize = 45;
            TimingAttackTask.Task.RetreatSize = 20;

            DefenseTask.Task.MainDefenseRadius = 20;
            DefenseTask.Task.ExpandDefenseRadius = 20;
            DefenseTask.Task.MaxDefenseRadius = 55;
            /*
            SmellCheese = Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool && !Tyr.Bot.EnemyStrategyAnalyzer.Expanded && Completed(UnitTypes.ROACH) < 2;
            if (SmellCheese)
            {
                TimingAttackTask.RetreatSize = 5;
                TimingAttackTask.RequiredSize = 25;
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.HATCHERY
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }
            */

            if (Completed(UnitTypes.LAIR) > 0
                && Count(UnitTypes.OVERSEER) < 2)
                MorphingTask.Task.Morph(UnitTypes.OVERSEER);

            if (Completed(UnitTypes.ROACH_WARREN) > 0
                && Count(UnitTypes.RAVAGER) < 8
                && Count(UnitTypes.RAVAGER) < Completed(UnitTypes.ROACH) - 8)
                MorphingTask.Task.Morph(UnitTypes.RAVAGER);

            if (Completed(UnitTypes.ROACH_WARREN) > 0)
                RequiredZerglings = 0;
            else
                RequiredZerglings = 14;

            bool saveForUpgrades = Completed(UnitTypes.EVOLUTION_CHAMBER) >= 2
                && Minerals() < 200
                && Gas() < 200
                && ((!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(57)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1190)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1189))
                    || (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(60)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1192)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1193)));

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.LARVA)
                {
                    if (Count(UnitTypes.DRONE) >= 14 && Count(UnitTypes.SPAWNING_POOL) == 0)
                        break;
                    if (Minerals() >= 50
                        && AvailableFood() > FoodUsed() + 1
                        && Count(UnitTypes.DRONE) < 50 - Completed(UnitTypes.EXTRACTOR)
                        && (Count(UnitTypes.DRONE) < 40 - Completed(UnitTypes.EXTRACTOR) || Count(UnitTypes.HATCHERY) + Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) >= 3)
                        && (Count(UnitTypes.ZERGLING) >= RequiredZerglings || Count(UnitTypes.DRONE) <= 18 || Completed(UnitTypes.ROACH_WARREN) > 0))
                    {
                        agent.Order(1342);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.DRONE);
                    }
                    else if (Completed(UnitTypes.SPAWNING_POOL) > 0
                        && Count(UnitTypes.ZERGLING) < RequiredZerglings
                        && Minerals() >= 50
                        && AvailableFood() > FoodUsed() + 2
                        && (Count(UnitTypes.SPINE_CRAWLER) >= 2 || Minerals() >= 200)
                        && Completed(UnitTypes.ROACH_WARREN) == 0)
                    {
                        agent.Order(1343);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.ZERGLING);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.ZERGLING);
                    }
                    else if (Completed(UnitTypes.ROACH_WARREN) > 0
                        && Minerals() >= 75
                        && Gas() >= 25
                        && !saveForUpgrades
                        && AvailableFood() > FoodUsed() + 2)
                    {
                        agent.Order(1351);
                        CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.ROACH);
                    }
                    else if (Minerals() >= 100 && FoodUsed()
                        + Tyr.Bot.UnitManager.Count(UnitTypes.HATCHERY) * 2
                        + Tyr.Bot.UnitManager.Count(UnitTypes.LAIR) * 2
                        + Tyr.Bot.UnitManager.Count(UnitTypes.HIVE) * 2
                        >= ExpectedAvailableFood() - (Minerals() > 300 ? 8 : 2))
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
                if (Minerals() >= 150 && Completed(UnitTypes.QUEEN) < 3
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Minerals() >= 150 && Completed(UnitTypes.QUEEN) < 5
                    && Count(UnitTypes.LAIR) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Count(UnitTypes.SPINE_CRAWLER) > 0
                    && Minerals() >= 150 && Gas() >= 100
                    && Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) == 0
                    && Count(UnitTypes.ROACH) >= 8)
                    agent.Order(1216);
            }
            else if (agent.Unit.UnitType == UnitTypes.ROACH_WARREN)
            {
                if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(2)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(216);
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
