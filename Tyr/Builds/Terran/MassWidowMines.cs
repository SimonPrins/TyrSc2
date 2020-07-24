using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Terran
{
    public class MassWidowMines : Build
    {
        private WallInCreator WallIn;

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            DefenseTask.Enable();
            BunkerDefendersTask.Enable();
            SupplyDepotTask.Enable();
            ArmyRavenTask.Enable();
            RepairTask.Enable();
            ReplenishBuildingSCVTask.Enable();
        }

        public override string Name()
        {
            return "MassWidowMines";
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new MineController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new DodgeBallController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.SUPPLY_DEPOT, UnitTypes.SUPPLY_DEPOT, UnitTypes.BUNKER });
                WallIn.ReserveSpace();
            }

            Set += SupplyDepots();
            Set += MainBuild();
        }

        public BuildList SupplyDepots()
        {
            BuildList result = new BuildList();

            result.If(() => { return Count(UnitTypes.SUPPLY_DEPOT) >= 2; });
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

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.BUNKER, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.ARMORY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.ENGINEERING_BAY);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.STARPORT);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            TimingAttackTask.Task.RequiredSize = 10;
            TimingAttackTask.Task.RetreatSize = 0;

            if (Completed(UnitTypes.MARINE) + Completed(UnitTypes.THOR) * 3 < 20)
                TimingAttackTask.Task.UnitType = UnitTypes.WIDOW_MINE;
            else
                TimingAttackTask.Task.UnitType = 0;
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER
                && Completed(UnitTypes.BARRACKS) > 0
                && Count(UnitTypes.SCV) >= 16
                && (agent.Base == Main
                    || agent.Base == Natural))
            {
                if (Minerals() >= 150)
                    agent.Order(1516);
            }
            else if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER
                && Completed(UnitTypes.ENGINEERING_BAY) > 0
                && Count(UnitTypes.SCV) >= 16
                && Gas() >= 150
                && agent.Base != Main
                && agent.Base != Natural)
            {
                if (Minerals() >= 150)
                    agent.Order(1450);
            }
            else if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Count(UnitTypes.SCV) < Math.Min(60, 20 * Count(UnitTypes.COMMAND_CENTER))
                    && Minerals() >= 50
                    && FoodLeft() >= 1)
                    agent.Order(524);
            }
            else if (agent.Unit.UnitType == UnitTypes.BARRACKS)
            {
                if (Minerals() >= 50
                    && FoodLeft() >= 1)
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
                else
                {
                    if (Minerals() >= 75
                        && Gas() >= 25
                        && FoodLeft() >= 2
                        && Count(UnitTypes.WIDOW_MINE) < 10)
                        agent.Order(614);
                    else if (Count(UnitTypes.WIDOW_MINE) >= 10)
                    {
                        if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
                        {
                            if (Completed(UnitTypes.ARMORY) > 0
                                && Minerals() >= 300
                                && Gas() >= 200
                                && FoodLeft() >= 6)
                                agent.Order(594);
                        }
                    }
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.STARPORT)
            {
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.VIKING_FIGHTER) > 0)
                    {
                        if (Count(UnitTypes.STARPORT_REACTOR) == 0 || Count(UnitTypes.STARPORT_TECH_LAB) > 0)
                        {
                            if (Minerals() >= 50
                                && Gas() >= 25)
                            agent.Order(488);
                        }
                        else if (Minerals() >= 50
                            && Gas() >= 50)
                            agent.Order(487);
                    }
                    else if (Minerals() > 150
                        && Gas() >= 75
                        && FoodLeft() >= 2)
                        agent.Order(624);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_REACTOR)
                {
                    if (Minerals() > 150
                        && Gas() >= 75
                        && FoodLeft() >= 2
                        && Count(UnitTypes.VIKING_FIGHTER) < 10)
                        agent.Order(624);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_TECH_LAB)
                {
                    if (Minerals() > 100
                        && Gas() >= 200
                        && Count(UnitTypes.RAVEN) < 2
                        && FoodLeft() >= 2)
                    {
                        agent.Order(622);
                    }
                    else if (Minerals() > 150
                        && Gas() >= 150
                        && FoodLeft() >= 3
                        && Count(UnitTypes.RAVEN) >= 2)
                        agent.Order(626);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ARMORY)
            {
                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(116)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(864);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(30)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(855);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(117)
                    && Gas() >= 175
                    && Minerals() >= 175)
                    agent.Order(865);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(31)
                    && Gas() >= 175
                    && Minerals() >= 175)
                    agent.Order(856);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(118)
                    && Gas() >= 250
                    && Minerals() >= 250)
                    agent.Order(866);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(32)
                    && Gas() >= 250
                    && Minerals() >= 250)
                    agent.Order(857);
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
            {
                if (Count(UnitTypes.WIDOW_MINE) > 0
                    && Gas() >= 75
                    && Minerals() >= 75)
                    agent.Order(764);
            }
            else if (agent.Unit.UnitType == UnitTypes.STARPORT_TECH_LAB
                    && Gas() >= 150
                    && Minerals() >= 150)
            {
                agent.Order(805);
            }
        }
    }
}