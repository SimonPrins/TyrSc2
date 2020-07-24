using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Terran
{
    public class BansheeRush : Build
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
        }

        public override string Name()
        {
            return "BansheeRush";
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new TankController());
            MicroControllers.Add(new BansheeController());
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
            result.If(() => Completed(UnitTypes.BARRACKS) > 0);
            result.Train(UnitTypes.ORBITAL_COMMAND);
            result.Train(UnitTypes.SCV, 40);
            result.If(() => Completed(UnitTypes.FACTORY) > 0);
            result.Train(UnitTypes.HELLION, 1);
            result.Train(UnitTypes.HELLION, 2, () => Count(UnitTypes.STARPORT) > 0);
            result.Train(UnitTypes.STARPORT_TECH_LAB);
            result.Train(UnitTypes.BANSHEE, 20);
            result.Train(UnitTypes.BARRACKS_REACTOR, () => Minerals() >= 250 && Gas() >= 250);
            result.Train(UnitTypes.MARINE, () => Minerals() >= 250);
            result.Train(UnitTypes.FACTORY_TECH_LAB, () => Minerals() >= 250 && Gas() >= 250);
            result.Train(UnitTypes.SIEGE_TANK, () => Minerals() >= 250 && Gas() >= 250);
            result.Train(UnitTypes.VIKING_FIGHTER, 20);
            result.Train(UnitTypes.BANSHEE);
            result.Upgrade(UpgradeType.BansheeCloak, () => Count(UnitTypes.BANSHEE) >= 2);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.STARPORT);
            result.If(() => Count(UnitTypes.BANSHEE) >= 3);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.BARRACKS, 3, () => Minerals() >= 250);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.HELLION);
            if (Completed(UnitTypes.MARINE) >= 20)
            {
                TimingAttackTask.Task.ExcludeUnitTypes.Remove(UnitTypes.MARINE);
                TimingAttackTask.Task.ExcludeUnitTypes.Remove(UnitTypes.SIEGE_TANK);
            }
            else
            {
                TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.MARINE);
                TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.SIEGE_TANK);
            }
            TimingAttackTask.Task.RequiredSize = 3;
            TimingAttackTask.Task.RetreatSize = 0;
            
        }
    }
}
