﻿using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Terran
{
    public class Bio : Build
    {
        private Point2D OverrideDefenseTarget;

        private List<CustomController> AttackMicroControllers = new List<CustomController>();

        private List<DefenseSquadTask> TankDefenseTasks = new List<DefenseSquadTask>();
        private List<DefenseSquadTask> LiberatorDefenseTasks = new List<DefenseSquadTask>();
        private List<DefenseSquadTask> VikingDefenseTasks = new List<DefenseSquadTask>();

        private bool ScanTimingsSet = false;

        private bool SuspectCloackedBanshees = false;

        private int DesiredMedivacs = 0;
        private Base FarBase;

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            SiegeTask.Enable();
            TimingAttackTask.Enable();
            DefenseTask.Enable();
            BunkerDefendersTask.Enable();
            SupplyDepotTask.Enable();
            ArmyRavenTask.Enable();
            MechDestroyExpandsTask.Enable();
            RepairTask.Enable();
            ReplenishBuildingSCVTask.Enable();
            ClearBlockedExpandsTask.Enable();
            HomeRepairTask.Enable();
            TransformTask.Enable();
            ThorretTask.Enable();
            HideBuildingTask.Enable();
            HideUnitsTask.Enable();
            
            AttackTask.Enable();

            if (TankDefenseTasks.Count == 0)
            {
                foreach (Base b in Bot.Main.BaseManager.Bases)
                {
                    if (b == Natural
                        || b == Main)
                        continue;
                    TankDefenseTasks.Add(new DefenseSquadTask(b, UnitTypes.SIEGE_TANK) { MaxDefenders = 2 });
                    LiberatorDefenseTasks.Add(new DefenseSquadTask(b, UnitTypes.LIBERATOR) { MaxDefenders = 1 });
                    VikingDefenseTasks.Add(new DefenseSquadTask(b, UnitTypes.VIKING_FIGHTER) { MaxDefenders = 1 });
                }
            }

            foreach (DefenseSquadTask task in TankDefenseTasks)
                Task.Enable(task);
            foreach (DefenseSquadTask task in LiberatorDefenseTasks)
                Task.Enable(task);
            foreach (DefenseSquadTask task in VikingDefenseTasks)
                Task.Enable(task);
            
        }

        public override string Name()
        {
            return "Bio";
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new TankController());
            MicroControllers.Add(new LiberatorController());
            MicroControllers.Add(new VikingController());
            MicroControllers.Add(new MedivacController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new DodgeBallController());

            OverrideDefenseTarget = bot.MapAnalyzer.Walk(NaturalDefensePos, bot.MapAnalyzer.EnemyDistances, 15);


            double distance = 0;
            foreach (Base b in bot.BaseManager.Bases)
            {
                double newDist = Math.Sqrt(SC2Util.DistanceSq(b.BaseLocation.Pos, bot.BaseManager.Main.BaseLocation.Pos)) + Math.Sqrt(SC2Util.DistanceSq(b.BaseLocation.Pos, bot.TargetManager.PotentialEnemyStartLocations[0]));

                if (newDist > distance)
                {
                    FarBase = b;
                    distance = newDist;
                }
            }
            HideBuildingTask.Task.HideLocation = FarBase;
            HideUnitsTask.Task.Target = FarBase.BaseLocation.Pos;

            Set += SupplyDepots();
            Set += AntiBanshee();
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

        private BuildList AntiBanshee()
        {
            BuildList result = new BuildList();

            result.If(() => { return SuspectCloackedBanshees; });
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.ENGINEERING_BAY);
            result.If(() => Count(UnitTypes.HELLION) > 0 || Count(UnitTypes.THOR) > 0);
            result.Building(UnitTypes.MISSILE_TURRET, Main, 4);
            result.Building(UnitTypes.REFINERY);
            result.If(() => Count(UnitTypes.HELLION) >= 3);
            result.Building(UnitTypes.MISSILE_TURRET, Main, 4);
            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.If(() => !SuspectCloackedBanshees);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.BARRACKS);
            result.Building(UnitTypes.REFINERY);
            result.Upgrade(UpgradeType.Stim);
            result.Building(UnitTypes.ENGINEERING_BAY);
            result.Upgrade(UpgradeType.TerranInfantryWeapons);
            result.Upgrade(UpgradeType.TerranInfantryArmor);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.BARRACKS);
            result.Upgrade(UpgradeType.ConcussiveShells);
            result.Upgrade(UpgradeType.CombatShield);
            result.If(() => TimingAttackTask.Task.AttackSent);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.ENGINEERING_BAY);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.BARRACKS, 2);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.BARRACKS, 2);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.BARRACKS, 2);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            AttackTask.Task.LeaveAtHome = 2;
            AttackTask.Task.Priority = 10;
            AttackTask.Task.UnitType = UnitTypes.BATTLECRUISER;

            if (bot.Observation.ActionErrors != null)
                foreach (ActionError error in bot.Observation.ActionErrors)
                    DebugUtil.WriteLine("Error with ability " + error.AbilityId + ": " + error.Result);

            if (Count(UnitTypes.COMMAND_CENTER) == 0 
                && (Minerals() < 400 || Gas() < 300))
            {
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                {
                    if (agent.Unit.UnitType != UnitTypes.STARPORT)
                        continue;
                    if (agent.Unit.Orders == null || agent.Unit.Orders.Count == 0)
                        agent.Order(518);
                }
            }

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.CannonDefenseRadius = 20;

            TransformTask.Task.ThorsToSingleTarget();

            MechDestroyExpandsTask.Task.RequiredSize = 1;
            MechDestroyExpandsTask.Task.RetreatSize = 0;
            MechDestroyExpandsTask.Task.UnitType = UnitTypes.WIDOW_MINE;
            MechDestroyExpandsTask.Task.Stopped = bot.Frame >= 22.4 * 540;

            if (SuspectCloackedBanshees)
                IdleTask.Task.OverrideTarget = SC2Util.To2D(bot.MapAnalyzer.StartLocation);
            else if (bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) == 1
                && Completed(UnitTypes.SIEGE_TANK) > 0
                && bot.Frame <= 22.4 * 60 * 4
                && Count(UnitTypes.COMMAND_CENTER) < 3)
                IdleTask.Task.OverrideTarget = SC2Util.Point((bot.MapAnalyzer.GetMainRamp().X + Natural.BaseLocation.Pos.X) / 2f, (bot.MapAnalyzer.GetMainRamp().Y + Natural.BaseLocation.Pos.Y) / 2f);
            else if (Count(UnitTypes.COMMAND_CENTER) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            AttackTask.Task.Stopped = !SuspectCloackedBanshees;
            TimingAttackTask.Task.BeforeControllers = AttackMicroControllers;
            AttackTask.Task.BeforeControllers = AttackMicroControllers;
            if (SuspectCloackedBanshees)
            {
                SiegeTask.Task.Stopped = true;

                TimingAttackTask.Task.RequiredSize = 30;
                TimingAttackTask.Task.RetreatSize = 0;
                TimingAttackTask.Task.Stopped = false;
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = 35;
                SiegeTask.Task.Stopped = true;
                TimingAttackTask.Task.RetreatSize = 12;
                TimingAttackTask.Task.Stopped = false;
            }

            TimingAttackTask.Task.DefendOtherAgents = !SuspectCloackedBanshees;
            foreach (ThorretTask task in ThorretTask.Tasks)
                task.Stopped = !SuspectCloackedBanshees;

            bool attacking = (!TimingAttackTask.Task.Stopped && TimingAttackTask.Task.IsNeeded())
                || (!SiegeTask.Task.Stopped && SiegeTask.Task.IsNeeded());

            foreach (Task task in VikingDefenseTasks)
                task.Stopped = attacking;
            foreach (Task task in TankDefenseTasks)
                task.Stopped = attacking;
            foreach (Task task in LiberatorDefenseTasks)
                task.Stopped = attacking;

            int defendingTanksPerBase = 2;
            if (Completed(UnitTypes.SIEGE_TANK) >= 4 * (Count(UnitTypes.COMMAND_CENTER) - 1))
                defendingTanksPerBase = 4;
            else if (Completed(UnitTypes.SIEGE_TANK) >= 3 * (Count(UnitTypes.COMMAND_CENTER) - 1))
                defendingTanksPerBase = 3;

            foreach (DefenseSquadTask task in TankDefenseTasks)
                task.MaxDefenders = defendingTanksPerBase;

            foreach (DefenseSquadTask task in DefenseSquadTask.Tasks)
            {
                task.Priority = 4;
                task.MaxDefenders = 3;
                task.Stopped = task.Base != Main;
            }

            HomeRepairTask.Task.Stopped = SuspectCloackedBanshees;
            if (SuspectCloackedBanshees)
                HomeRepairTask.Task.Range = 10;
            else
                HomeRepairTask.Task.Range = 40;

            RepairTask.Task.RepairTurrets = SuspectCloackedBanshees;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 80;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 80;
            if (SuspectCloackedBanshees)
            {
                IdleTask.Task.IdleRange = 2;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 14;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 14;

            }
            else if (Count(UnitTypes.COMMAND_CENTER) > 2)
            {
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 40;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 40;
            }
            else
            {
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            }
            
            if ((Count(UnitTypes.HELLION) >= 6 || (Minerals() >= 250 && Gas() >= 200)) && Count(UnitTypes.MISSILE_TURRET) >= 4 && HideBuildingTask.Task.RequiredBuildings.Count == 0)
            {
                HideBuildingTask.Task.RequiredBuildings.Add(UnitTypes.STARPORT);
                HideBuildingTask.Task.RequiredBuildings.Add(UnitTypes.FUSION_CORE);
            }

            if (bot.Frame >= 22.4 * 60 * 2.5 && !SuspectCloackedBanshees)
            {
                HideBuildingTask.Task.Stopped = true;
                HideBuildingTask.Task.Clear();
            }

            if (bot.EnemyStrategyAnalyzer.Count(UnitTypes.FACTORY) > 0 && bot.Frame <= 22.4 * 60 * 2.5 + 22.4)  
                SuspectCloackedBanshees = true;

            if(bot.Frame >= 22.4 * 60 * 2.5)
                bot.OrbitalAbilityManager.SaveEnergy = 0;


            if (bot.TargetManager.PotentialEnemyStartLocations.Count == 1
                && !ScanTimingsSet)
            {
                ScanTimingsSet = true;
                bot.OrbitalAbilityManager.SaveEnergy = 50;
                bot.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = bot.TargetManager.PotentialEnemyStartLocations[0],
                    FromFrame = (int)(22.4 * 60 * 2.25)
                });
            }
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
                    && FoodLeft() >= 1
                    && (!SuspectCloackedBanshees || Count(UnitTypes.SCV) < 24))
                    agent.Order(524);
            }
            else if (agent.Unit.UnitType == UnitTypes.BARRACKS)
            {
                if (!bot.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.FACTORY) == 0)
                    {
                        if (Minerals() >= 50
                            && FoodLeft() >= 1)
                            agent.Order(560);
                        return;
                    }
                    if (Count(UnitTypes.BARRACKS_REACTOR) > 0)
                        agent.Order(421);
                    else
                        agent.Order(422);
                }
                else if (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.BARRACKS_TECH_LAB)
                {
                    if (Minerals() >= 100
                        && Gas() >= 25
                        && FoodLeft() >= 1)
                        agent.Order(563);
                }
                else
                {
                    if (Minerals() >= 50
                        && FoodLeft() >= 1)
                        agent.Order(560);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY)
            {
                if (!bot.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.HELLION) < 1 && (bot.Frame <= 22.4 * 60 * 2.5 + 22.4 || SuspectCloackedBanshees))
                    {
                        if (Minerals() >= 100
                            && FoodLeft() >= 2)
                            agent.Order(595);
                    }
                    else if (SuspectCloackedBanshees)
                    {
                        agent.Order(455);
                    }
                    else if (Count(UnitTypes.FACTORY_TECH_LAB) < 2 || Count(UnitTypes.FACTORY_REACTOR) >= 1)
                        agent.Order(454);
                    else
                        agent.Order(455);
                }
                else if (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
                {
                    if (Minerals() >= 150
                        && Gas() >= 100
                        && FoodLeft() >= 3)
                        agent.Order(597);
                }
                else if (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_REACTOR)
                {
                    if (SuspectCloackedBanshees)
                    {
                        if (Count(UnitTypes.HELLION) < 6
                            && Minerals() >= 100
                            && FoodLeft() >= 2)
                            agent.Order(595);
                        return;
                    }
                    else if (Completed(UnitTypes.ARMORY) > 0
                        && Minerals() >= 75
                        && Gas() >= 25
                        && !SuspectCloackedBanshees
                        && Count(UnitTypes.WIDOW_MINE) < 4
                        && FoodLeft() >= 2)
                        agent.Order(614);
                    else if (Completed(UnitTypes.ARMORY) > 0
                        && Minerals() >= 100
                        && FoodLeft() >= 2
                        && Count(UnitTypes.HELLION) < 12)
                        agent.Order(596);
                    else if (Minerals() >= 100
                        && FoodLeft() >= 2
                        && Count(UnitTypes.HELLION) < 12)
                        agent.Order(595);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.STARPORT)
            {
                if (!bot.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {

                    if (Count(UnitTypes.MEDIVAC) < 2)
                    {
                        if (Minerals() >= 100
                            && Gas() >= 100
                            && FoodLeft() >= 2)
                            agent.Order(620);
                        return;
                    }
                    if (Count(UnitTypes.VIKING_FIGHTER) > 0)
                    {
                        if ((Count(UnitTypes.STARPORT_REACTOR) == 0 || Count(UnitTypes.STARPORT_TECH_LAB) > 0)
                            && (!SuspectCloackedBanshees || Count(UnitTypes.STARPORT_TECH_LAB) > 0))
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
                else if (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_REACTOR)
                {
                    if (Count(UnitTypes.VIKING_FIGHTER) < 3 || SuspectCloackedBanshees)
                    {
                        if (Minerals() > 150
                            && Gas() >= 75
                            && FoodLeft() >= 2
                            && Count(UnitTypes.VIKING_FIGHTER) < 15)
                            agent.Order(624);
                    }
                    else if (Count(UnitTypes.MEDIVAC) < DesiredMedivacs)
                    {
                        if (Minerals() >= 100
                            && Gas() >= 100
                            && FoodLeft() >= 2)
                            agent.Order(620);
                    }
                }
                else if (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_TECH_LAB)
                {
                    if (SuspectCloackedBanshees)
                    {
                        if (Minerals() >= 400
                            && Gas() >= 300)
                            agent.Order(623);
                        return;
                    }
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
                if (SuspectCloackedBanshees)
                    return;
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
                if (Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) > 0
                    && !SuspectCloackedBanshees
                    && Gas() >= 150
                    && Minerals() >= 150)
                    agent.Order(761);
                else if (Count(UnitTypes.CYCLONE) > 0
                    && Gas() >= 150
                    && Minerals() >= 150)
                    agent.Order(769);
                else if (Count(UnitTypes.WIDOW_MINE) > 0
                    && Gas() >= 75
                    && Minerals() >= 75)
                    agent.Order(764);
            }
            else if (agent.Unit.UnitType == UnitTypes.ENGINEERING_BAY)
            {
                if (SuspectCloackedBanshees)
                {
                    if (Count(UnitTypes.HELLION) >= 2)
                    agent.Order(650);
                }
                else if (Gas() >= 100
                    && Minerals() >= 100
                    && Count(UnitTypes.PLANETARY_FORTRESS) > 0
                    && Count(UnitTypes.SIEGE_TANK) >= 4)
                    agent.Order(650);
            }
        }
    }
}
