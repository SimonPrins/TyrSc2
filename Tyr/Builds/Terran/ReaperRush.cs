using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Terran
{
    public class ReaperRush : Build
    {
        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            DefenseTask.Enable();
            SupplyDepotTask.Enable();
            RepairTask.Enable();
            ReplenishBuildingSCVTask.Enable();
            ClearBlockedExpandsTask.Enable();
            HomeRepairTask.Enable();
            TransformTask.Enable();
            WorkerScoutTask.Enable();
        }

        public override string Name()
        {
            return "ReaperRush";
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new ReaperHarassController());
            MicroControllers.Add(new DodgeBallController());

            Set += SupplyDepots();
            Set += Units();
            Set += MainBuild();
        }

        public BuildList SupplyDepots()
        {
            BuildList result = new BuildList();

            result.If(() => { return Count(UnitTypes.SUPPLY_DEPOT) >= 1; });
            result.If(() =>
            {
                return Build.FoodUsed()
                    + Bot.Main.UnitManager.Count(UnitTypes.COMMAND_CENTER)
                    + Bot.Main.UnitManager.Count(UnitTypes.BARRACKS) * 2
                    + Bot.Main.UnitManager.Count(UnitTypes.FACTORY) * 2
                    + Bot.Main.UnitManager.Count(UnitTypes.STARPORT) * 2
                    >= Build.ExpectedAvailableFood() - 2
                    && Build.ExpectedAvailableFood() < 200;
            });
            result += new BuildingStep(UnitTypes.SUPPLY_DEPOT);
            result.Goto(0);

            return result;
        }

        public BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.SCV, 16);
            result.Train(UnitTypes.ORBITAL_COMMAND);
            result.Train(UnitTypes.REAPER, 6);
            result.Train(UnitTypes.VIKING_FIGHTER, 10);
            result.If(() => Count(UnitTypes.REAPER) >= 2);
            result.Train(UnitTypes.MARINE, () => 
                       Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0
                    || Gas() < 42
                    || Lifting.Get().Detected);
            result.Train(UnitTypes.REAPER, 16);
            result.Train(UnitTypes.MARINE);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.BARRACKS);
            result.If(() => Count(UnitTypes.REAPER) > 0 && Minerals() >= 200);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.BARRACKS);
            result.If(() => Lifting.Get().Detected || (Gas() >= 200 && Minerals() >= 250));
            result.Building(UnitTypes.FACTORY, () => Completed(UnitTypes.FACTORY_FLYING) == 0);
            result.Building(UnitTypes.STARPORT);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            TimingAttackTask.Task.RequiredSize = 1;
            TimingAttackTask.Task.RetreatSize = 0;

            bot.Surrendered = true;
            bot.SurrenderedFrame = bot.Frame + 1000000;

            foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.FACTORY)
                    agent.Order(485);


            foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.FACTORY_FLYING && bot.Frame % 22 == 0)
                {
                    Point2D a = SC2Util.Point(10, 10);
                    Point2D b = SC2Util.Point(bot.GameInfo.StartRaw.MapSize.X - 10, bot.GameInfo.StartRaw.MapSize.Y - 10);
                    if (SC2Util.DistanceSq(a, bot.TargetManager.PotentialEnemyStartLocations[0]) < SC2Util.DistanceSq(b, bot.TargetManager.PotentialEnemyStartLocations[0]))
                        agent.Order(Abilities.MOVE, b);
                    else
                        agent.Order(Abilities.MOVE, a);
                }
        }
    }
}
