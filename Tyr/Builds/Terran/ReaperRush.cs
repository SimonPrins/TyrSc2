using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Terran
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

        public override void OnStart(Bot tyr)
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
                    + Bot.Bot.UnitManager.Count(UnitTypes.COMMAND_CENTER)
                    + Bot.Bot.UnitManager.Count(UnitTypes.BARRACKS) * 2
                    + Bot.Bot.UnitManager.Count(UnitTypes.FACTORY) * 2
                    + Bot.Bot.UnitManager.Count(UnitTypes.STARPORT) * 2
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
                       Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0
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

        public override void OnFrame(Bot tyr)
        {
            TimingAttackTask.Task.RequiredSize = 1;
            TimingAttackTask.Task.RetreatSize = 0;

            tyr.Surrendered = true;
            tyr.SurrenderedFrame = tyr.Frame + 1000000;

            foreach (Agent agent in Bot.Bot.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.FACTORY)
                    agent.Order(485);


            foreach (Agent agent in Bot.Bot.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.FACTORY_FLYING && tyr.Frame % 22 == 0)
                {
                    Point2D a = SC2Util.Point(10, 10);
                    Point2D b = SC2Util.Point(tyr.GameInfo.StartRaw.MapSize.X - 10, tyr.GameInfo.StartRaw.MapSize.Y - 10);
                    if (SC2Util.DistanceSq(a, tyr.TargetManager.PotentialEnemyStartLocations[0]) < SC2Util.DistanceSq(b, tyr.TargetManager.PotentialEnemyStartLocations[0]))
                        agent.Order(Abilities.MOVE, b);
                    else
                        agent.Order(Abilities.MOVE, a);
                }
        }
    }
}
