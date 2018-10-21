using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Terran
{
    public class TankPush : Build
    {
        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            DefenseTask.Enable();
            BunkerDefendersTask.Enable();
            SupplyDepotTask.Enable();
        }

        public override string Name()
        {
            return "TankPush";
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new TankController());
            MicroControllers.Add(new VikingController());
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

            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.BUNKER, Natural, NaturalDefensePos);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.ARMORY);
            result.Building(UnitTypes.FACTORY, 2);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.COMMAND_CENTER);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            TimingAttackTask.Task.RequiredSize = 50;
            TimingAttackTask.Task.RetreatSize = 12;
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER
                && Completed(UnitTypes.BARRACKS) > 0
                && Count(UnitTypes.SCV) >= 16)
            {
                if (Minerals() >= 150)
                    agent.Order(1516);
            }
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Count(UnitTypes.SCV) < 40
                    && Minerals() >= 50)
                    agent.Order(524);
            }
            else if (agent.Unit.UnitType == UnitTypes.BARRACKS)
            {
                if (Minerals() >= 50)
                    agent.Order(560);
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY)
            {
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.FACTORY_TECH_LAB) <= Count(UnitTypes.FACTORY_REACTOR))
                        agent.Order(454);
                    else
                        agent.Order(455);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB
                    && Minerals() >= 150
                    && Gas() >= 125)
                    agent.Order(591);
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_REACTOR
                    && Minerals() >= 100)
                {
                    if (Completed(UnitTypes.ARMORY) > 0)
                        agent.Order(596);
                    else
                        agent.Order(595);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.STARPORT)
            {
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.VIKING_FIGHTER) > 0)
                        agent.Order(488);
                    else if (Minerals() > 150
                        && Gas() >= 75)
                        agent.Order(624);
                } else if(Minerals() > 150
                      && Gas() >= 75)
                {
                    agent.Order(624);
                }
            }
        }
    }
}
