﻿using System;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Terran
{
    public class CycloneRush : Build
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
        }

        public override string Name()
        {
            return "CycloneRush";
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new DodgeBallController());
            
            Set += SupplyDepots();
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

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.FACTORY);
            result.If(() => Minerals() >= 300);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.BARRACKS);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            TimingAttackTask.Task.RequiredSize = 12;
            TimingAttackTask.Task.RetreatSize = 4;
        }

        public override void Produce(Bot bot, Agent agent)
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
                if (!bot.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    agent.Order(454);
                }
                else if (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
                {
                    if (Minerals() >= 150
                        && Gas() >= 100
                        && FoodLeft() >= 3)
                        agent.Order(597);
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
                if (Count(UnitTypes.CYCLONE) > 0
                    && Gas() >= 150
                    && Minerals() >= 150)
                    agent.Order(761);
            }
        }
    }
}
