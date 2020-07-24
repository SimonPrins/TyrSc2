using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Terran
{
    public class MarineRush : Build
    {
        private WallInCreator WallIn;

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            WorkerRushDefenseTask.Enable();
            DefenseTask.Enable();
            SupplyDepotTask.Enable();
            RepairTask.Enable();
            ReplenishBuildingSCVTask.Enable();
        }

        public override string Name()
        {
            return "MarineRush";
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new DodgeBallController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.SUPPLY_DEPOT, UnitTypes.SUPPLY_DEPOT, UnitTypes.BARRACKS });
                WallIn.ReserveSpace();
            }

            Set += SupplyDepots();
            Set += MainBuild();
        }

        public BuildList SupplyDepots()
        {
            BuildList result = new BuildList();

            result.If(() =>
            {
                return Build.FoodUsed()
                    + Bot.Bot.UnitManager.Completed(UnitTypes.COMMAND_CENTER)
                    + Bot.Bot.UnitManager.Completed(UnitTypes.BARRACKS) * 2
                    + Bot.Bot.UnitManager.Completed(UnitTypes.FACTORY) * 2
                    + Bot.Bot.UnitManager.Completed(UnitTypes.STARPORT) * 2
                    >= Build.ExpectedAvailableFood() - 2;
            });
            result.If(() => Count(UnitTypes.SUPPLY_DEPOT) >= 2);
            result += new BuildingStep(UnitTypes.SUPPLY_DEPOT);
            result.Goto(0);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.BARRACKS, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.BARRACKS, 3);
            result.Building(UnitTypes.BARRACKS, () => { return TimingAttackTask.Task.AttackSent; });

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            RepairTask.Task.WallIn = WallIn;
            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) >= 20)
                TimingAttackTask.Task.RequiredSize = 20;
            else
                TimingAttackTask.Task.RequiredSize = 10;
            foreach (Task task in WorkerDefenseTask.Tasks)
                task.Stopped = tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) >= 10;
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Count(UnitTypes.SCV) < 16
                    && Minerals() >= 50)
                    agent.Order(524);
            } else if (agent.Unit.UnitType == UnitTypes.BARRACKS)
            {
                if (Minerals() >= 50)
                    agent.Order(560);
            }
        }
    }
}
