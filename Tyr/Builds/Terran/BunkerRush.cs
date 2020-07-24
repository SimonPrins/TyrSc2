
using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Terran
{
    public class BunkerRush : Build
    {
        private WallInCreator WallIn;
        private bool BaseTrade = false;

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
            BunkerRushTask.Enable();
            BunkerRushTask.Task.Stopped = true;
            BunkerDefendersTask.Enable();
            ArmyRavenTask.Enable();
        }

        public override string Name()
        {
            return "BunkerRush";
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new YamatoController());
            MicroControllers.Add(new TankController());
            MicroControllers.Add(new CycloneController());
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
                    + Bot.Main.UnitManager.Completed(UnitTypes.COMMAND_CENTER)
                    + Bot.Main.UnitManager.Completed(UnitTypes.BARRACKS) * 2
                    + Bot.Main.UnitManager.Completed(UnitTypes.FACTORY) * 2
                    + Bot.Main.UnitManager.Completed(UnitTypes.STARPORT) * 2
                    >= Build.ExpectedAvailableFood() - 2;
            });
            result.If(() => Count(UnitTypes.SUPPLY_DEPOT) >= 1);
            result += new BuildingStep(UnitTypes.SUPPLY_DEPOT);
            result.Goto(0);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.COMMAND_CENTER);
            result.Train(UnitTypes.SCV, 19, () => !BaseTrade);
            result.Building(UnitTypes.SUPPLY_DEPOT);
            result.If(() => Completed(UnitTypes.BUNKER) > 0 && Completed(UnitTypes.BARRACKS) > 0);
            result.Building(UnitTypes.REFINERY, 2);
            result.Train(UnitTypes.SCV, 23, () => !BaseTrade);
            result.Train(UnitTypes.ORBITAL_COMMAND);
            result.Building(UnitTypes.FACTORY);
            result.If(() => Count(UnitTypes.SIEGE_TANK) >= 2);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Train(UnitTypes.SCV, 30, () => !BaseTrade);
            result.Building(UnitTypes.REFINERY, 2, () => Count(UnitTypes.BATTLECRUISER) > 0);
            result.Train(UnitTypes.SCV, 34, () => !BaseTrade);
            result.Building(UnitTypes.FUSION_CORE);
            result.Building(UnitTypes.STARPORT, () => Count(UnitTypes.BATTLECRUISER) > 0);
            result.Upgrade(UpgradeType.YamatoCannon, () => Count(UnitTypes.BATTLECRUISER) > 0);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            BunkerRushTask.Task.Stopped = tyr.Frame < 22.4 * 10;
            BunkerRushTask.Task.DesiredWorkers = Count(UnitTypes.BARRACKS) >= 2 || Count(UnitTypes.BUNKER) > 0 ? 3 : 2;

            bool closeReaper = false;
            foreach (Unit enemy in tyr.Enemies())
            {
                if (enemy.UnitType == UnitTypes.REAPER && SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 20 * 20)
                {
                    closeReaper = true;
                    break;
                }
            }

            if (closeReaper && Completed(UnitTypes.MARINE) >= 12)
                BaseTrade = true;
            if (tyr.TargetManager.PotentialEnemyStartLocations.Count <= 1)
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if (BaseTrade)
                TimingAttackTask.Task.RequiredSize = 12;
            else
                TimingAttackTask.Task.RequiredSize = 50;
            if (BaseTrade)
                BunkerDefendersTask.Task.LeaveBunkers = true;
            bool bunkerExists = false;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType != UnitTypes.BUNKER)
                    continue;
                bunkerExists = true;
                IdleTask.Task.OverrideTarget = SC2Util.To2D(agent.Unit.Pos);
                break;
            }
            if (!bunkerExists)
                IdleTask.Task.OverrideTarget = BunkerRushTask.Task.GetHideLocation();

            IdleTask.Task.AttackMove = true;

            foreach (Task task in WorkerDefenseTask.Tasks)
                    task.Stopped = tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) >= 10;
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.BARRACKS)
            {
                if (Minerals() >= 50)
                    agent.Order(560);
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY)
            {
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    agent.Order(454);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
                {
                    if (Minerals() >= 150
                        && Gas() >= 125
                        && FoodLeft() >= 3
                        && Count(UnitTypes.SIEGE_TANK) < 3)
                        agent.Order(591);
                    else if (Minerals() >= 150
                        && Gas() >= 100
                        && FoodLeft() >= 3
                        && Count(UnitTypes.SIEGE_TANK) >= 3)
                        agent.Order(597);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.STARPORT)
            {
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Minerals() >= 50
                        && Gas() >= 50)
                        agent.Order(487);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_TECH_LAB)
                {
                    if (Minerals() > 100
                        && Gas() >= 200
                        && Count(UnitTypes.RAVEN) < 1
                        && FoodLeft() >= 2)
                    {
                        agent.Order(622);
                    }
                    else if (Minerals() > 150
                        && Gas() >= 75
                        && FoodLeft() >= 2
                        && Completed(UnitTypes.FUSION_CORE) == 0
                        && Count(UnitTypes.RAVEN) >= 1
                        && Count(UnitTypes.VIKING_FIGHTER) < 15)
                        agent.Order(624);
                    else if (Minerals() > 400
                        && Gas() >= 300
                        && FoodLeft() >= 2
                        && Completed(UnitTypes.FUSION_CORE) > 0
                        && Count(UnitTypes.RAVEN) >= 1)
                        agent.Order(623);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
            {
                if (Count(UnitTypes.CYCLONE) > 0
                    && Gas() >= 150
                    && Minerals() >= 150)
                    agent.Order(769);
            }
        }
    }
}
