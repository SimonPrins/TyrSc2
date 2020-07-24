using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Terran
{
    public class HellbatRush : Build
    {
        private WallInCreator WallIn;
        private bool LingRush = false;
        bool RoachDefense = false;

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
            return "HellbatRush";
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new DodgeBallController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.SUPPLY_DEPOT, UnitTypes.SUPPLY_DEPOT, UnitTypes.SUPPLY_DEPOT });
                WallIn.ReserveSpace();
            }

            Set += SupplyDepots();
            Set += Units();
            Set += MainBuild();
        }

        public BuildList SupplyDepots()
        {
            BuildList result = new BuildList();

            result.If(() => { return Count(UnitTypes.SUPPLY_DEPOT) >= 3; });
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

            result.Train(UnitTypes.SCV, 20);
            result.Train(UnitTypes.MARINE, 4, () => LingRush);
            result.Train(UnitTypes.ORBITAL_COMMAND);
            result.Train(UnitTypes.SCV, 40, () => Count(UnitTypes.COMMAND_CENTER) > 1);
            result.Train(UnitTypes.STARPORT_TECH_LAB);
            result.Train(UnitTypes.FACTORY_REACTOR, 1);
            result.Train(UnitTypes.FACTORY_TECH_LAB, 1);
            result.Upgrade(UpgradeType.InfernalPreigniter);
            result.Train(UnitTypes.BATTLECRUISER);
            result.Train(UnitTypes.HELLION, 20, () => Completed(UnitTypes.ARMORY) == 0);
            result.Train(UnitTypes.HELLBAT, 20, () => !RoachDefense);
            result.Train(UnitTypes.HELLBAT, 10, () => RoachDefense && Count(UnitTypes.FUSION_CORE) > 0);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.FACTORY, () => !LingRush || Count(UnitTypes.MARINE) > 0);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.FACTORY, () => !LingRush || Count(UnitTypes.MARINE) > 0);
            result.Building(UnitTypes.REFINERY);
            result.If(() => Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(19) || Bot.Main.UnitManager.ActiveOrders.Contains(761));
            result.Building(UnitTypes.ARMORY);
            result.If(() => Count(UnitTypes.HELLBAT) >= 16 || Minerals() >= 500 || RoachDefense);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.FUSION_CORE);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.STARPORT);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            TransformTask.Task.HellionsToHellbats();
            if (RoachDefense)
            {
                if (Completed(UnitTypes.BATTLECRUISER) >= 2)
                {
                    TimingAttackTask.Task.RequiredSize = 6;
                    TimingAttackTask.Task.RetreatSize = 0;
                }
                else
                {
                    TimingAttackTask.Task.RequiredSize = 20;
                    TimingAttackTask.Task.RetreatSize = 8;
                }
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = 12;
                TimingAttackTask.Task.RetreatSize = 4;
            }
            TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.HELLION);

            RepairTask.Task.WallIn = WallIn;

            if (!LingRush && tyr.Frame <= 22.4 * 150
                && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) >= 5)
                LingRush = true;

            if (!RoachDefense && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH) > 6)
                RoachDefense = true;
        }
    }
}