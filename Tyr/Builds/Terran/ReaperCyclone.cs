using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Terran
{
    public class ReaperCyclone : Build
    {
        private bool SiegeTanks = false;
        private bool RetreatAgainstReapers = false;
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
            ArmyRavenTask.Enable();
        }

        public override string Name()
        {
            return "ReaperCyclone";
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new YamatoController());
            MicroControllers.Add(new TankController());
            MicroControllers.Add(new MineController());
            MicroControllers.Add(new CycloneController());
            MicroControllers.Add(new LiberatorController());
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
            result.Train(UnitTypes.SCV, 30, () => Count(UnitTypes.COMMAND_CENTER) >= 2);
            result.Train(UnitTypes.SCV, 50, () => Count(UnitTypes.COMMAND_CENTER) >= 3);
            result.Train(UnitTypes.ORBITAL_COMMAND);
            result.Train(UnitTypes.REAPER, 6);
            result.Train(UnitTypes.FACTORY_TECH_LAB, 1);
            if (SiegeTanks)
                result.Train(UnitTypes.SIEGE_TANK, 10);
            else
                result.Train(UnitTypes.CYCLONE, 10);
            result.Train(UnitTypes.STARPORT_TECH_LAB, () => Completed(UnitTypes.FUSION_CORE) > 0);
            result.Train(UnitTypes.RAVEN, 1, () => Completed(UnitTypes.BATTLECRUISER) >= 2);
            result.Train(UnitTypes.BATTLECRUISER, () => Completed(UnitTypes.FUSION_CORE) > 0);
            result.Upgrade(UpgradeType.YamatoCannon, () => Completed(UnitTypes.BATTLECRUISER) >= 2);
            result.Train(UnitTypes.VIKING_FIGHTER, 10, () => Lifting.Get().Detected);
            result.Train(UnitTypes.VIKING_FIGHTER, 10, () => Lifting.Get().Detected);
            result.Train(UnitTypes.LIBERATOR, 10);
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
            result.Building(UnitTypes.COMMAND_CENTER, () => Minerals() >= 550);
            result.Building(UnitTypes.REFINERY, 2);
            result.If(() => Lifting.Get().Detected || Count(UnitTypes.REAPER) >= 5 || Gas() >= 150);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.COMMAND_CENTER, () => Minerals() >= 550);
            result.Building(UnitTypes.COMMAND_CENTER, () => Minerals() >= 550);
            result.Building(UnitTypes.REFINERY, 4);
            result.If(() => Lifting.Get().Detected || (Gas() >= 150 && Count(UnitTypes.COMMAND_CENTER) >= 2));
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.FUSION_CORE, () => Completed(UnitTypes.STARPORT) > 0 && Gas() >= 300 && Minerals() >= 300);
            result.Building(UnitTypes.STARPORT, () => Count(UnitTypes.COMMAND_CENTER) >= 4 && Count(UnitTypes.FUSION_CORE) > 0 && Count(UnitTypes.BATTLECRUISER) > 0);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            tyr.OrbitalAbilityManager.SaveEnergy = 200;

            if (tyr.Frame % 448 == 0)
            {
                if (tyr.OrbitalAbilityManager.ScanCommands.Count == 0)
                {
                    foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    {
                        if (agent.Unit.UnitType != UnitTypes.REAPER
                            && agent.Unit.UnitType != UnitTypes.CYCLONE)
                            continue;

                        Unit scanTarget = null;
                        foreach (Unit enemy in Bot.Bot.Enemies())
                        {
                            if (!UnitTypes.CanAttackGround(enemy.UnitType))
                                continue;
                            if (agent.DistanceSq(enemy) >= 13 * 13)
                                continue;

                            scanTarget = enemy;
                            break;
                        }
                        if (scanTarget != null)
                        {
                            tyr.OrbitalAbilityManager.ScanCommands.Add(new Managers.ScanCommand() { FromFrame = tyr.Frame, Pos = SC2Util.To2D(scanTarget.Pos) });
                            break;
                        }
                    }
                }
            }
            if (tyr.Frame % 448 == 20)
                tyr.OrbitalAbilityManager.ScanCommands.Clear();

            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 2 && RetreatAgainstReapers)
            {
                TimingAttackTask.Task.RequiredSize = 10;
                TimingAttackTask.Task.RetreatSize = 5;
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = 1;
                TimingAttackTask.Task.RetreatSize = 0;
            }
        }
    }
}