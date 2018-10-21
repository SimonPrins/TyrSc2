using System;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Terran
{
    public class MarineRush : Build
    {
        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            DefenseTask.Enable();
        }

        public override string Name()
        {
            return "MarineRush";
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new DodgeBallController());
            Set += SupplyDepots();
            Set += MainBuild();
        }
        public static BuildList SupplyDepots()
        {
            BuildList result = new BuildList();

            result.If(() =>
            {
                return Build.FoodUsed()
                    + Tyr.Bot.UnitManager.Count(UnitTypes.COMMAND_CENTER)
                    + Tyr.Bot.UnitManager.Count(UnitTypes.BARRACKS) * 2
                    + Tyr.Bot.UnitManager.Count(UnitTypes.FACTORY) * 2
                    + Tyr.Bot.UnitManager.Count(UnitTypes.STARPORT) * 2
                    >= Build.ExpectedAvailableFood() - 2;
            });
            result += new BuildingStep(UnitTypes.SUPPLY_DEPOT);
            result.Goto(0);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.BARRACKS, 5);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
        }

        public override void Produce(Tyr tyr, Agent agent)
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
