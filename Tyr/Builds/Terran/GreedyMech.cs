using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Terran
{
    public class GreedyMech : Build
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
            //WorkerScoutTask.Enable();
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
            //AttackTask.Enable();
            
            AttackTask.Enable();

            if (TankDefenseTasks.Count == 0)
            {
                foreach (Base b in Bot.Bot.BaseManager.Bases)
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
            return "GreedyMech";
        }

        public override void OnStart(Bot tyr)
        {
            AttackMicroControllers.Add(new LeashController(
                new HashSet<uint>() { UnitTypes.LIBERATOR, UnitTypes.MEDIVAC, UnitTypes.HELLBAT, UnitTypes.WIDOW_MINE, UnitTypes.MARINE },
                new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED, UnitTypes.THOR, UnitTypes.THOR_SINGLE_TARGET },
                4));
            AttackMicroControllers.Add(new SoftLeashController(
                new HashSet<uint>() { UnitTypes.LIBERATOR, UnitTypes.MEDIVAC, UnitTypes.HELLBAT, UnitTypes.WIDOW_MINE, UnitTypes.MARINE },
                new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED, UnitTypes.THOR, UnitTypes.THOR_SINGLE_TARGET },
                12));
            AttackMicroControllers.Add(new LeashController(
                new HashSet<uint>() { UnitTypes.THOR, UnitTypes.THOR_SINGLE_TARGET },
                new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED },
                8));
            AttackMicroControllers.Add(new SoftLeashController(
                new HashSet<uint>() { UnitTypes.THOR, UnitTypes.THOR_SINGLE_TARGET },
                new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED },
                12));

            MicroControllers.Add(new FearEnemyController(new HashSet<uint> { UnitTypes.HELLION, UnitTypes.MARINE }, UnitTypes.BANSHEE, 10));
            MicroControllers.Add(new BCAggressiveTeleportController());
            MicroControllers.Add(new MineController());
            MicroControllers.Add(new TankController());
            MicroControllers.Add(new LiberatorController());
            MicroControllers.Add(new VikingController());
            MicroControllers.Add(new MedivacController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new DodgeBallController());

            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);


            double distance = 0;
            foreach (Base b in tyr.BaseManager.Bases)
            {
                double newDist = Math.Sqrt(SC2Util.DistanceSq(b.BaseLocation.Pos, tyr.BaseManager.Main.BaseLocation.Pos)) + Math.Sqrt(SC2Util.DistanceSq(b.BaseLocation.Pos, tyr.TargetManager.PotentialEnemyStartLocations[0]));

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
            result.Building(UnitTypes.FACTORY);
            result.If(() => Bot.Bot.Frame >= 22.4 * 60 * 2.5 + 22.4);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.FACTORY, () => Count(UnitTypes.CYCLONE) + Count(UnitTypes.SIEGE_TANK) + Count(UnitTypes.THOR) > 0);
            result.Building(UnitTypes.REFINERY, 1, () => Count(UnitTypes.CYCLONE) + Count(UnitTypes.SIEGE_TANK) + Count(UnitTypes.THOR) > 0);
            result.If(() => Completed(UnitTypes.SIEGE_TANK) >= 3);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.ARMORY);
            result.Building(UnitTypes.ENGINEERING_BAY);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.FACTORY);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            AttackTask.Task.LeaveAtHome = 2;
            AttackTask.Task.Priority = 10;
            AttackTask.Task.UnitType = UnitTypes.BATTLECRUISER;

            if (tyr.Observation.ActionErrors != null)
                foreach (ActionError error in tyr.Observation.ActionErrors)
                    DebugUtil.WriteLine("Error with ability " + error.AbilityId + ": " + error.Result);

            if (Count(UnitTypes.COMMAND_CENTER) == 0 
                && (Minerals() < 400 || Gas() < 300))
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
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
            MechDestroyExpandsTask.Task.Stopped = tyr.Frame >= 22.4 * 540;

            if (SuspectCloackedBanshees)
                IdleTask.Task.OverrideTarget = SC2Util.To2D(tyr.MapAnalyzer.StartLocation);
            else if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) == 1
                && Completed(UnitTypes.SIEGE_TANK) > 0
                && tyr.Frame <= 22.4 * 60 * 4
                && Count(UnitTypes.COMMAND_CENTER) < 3)
                IdleTask.Task.OverrideTarget = SC2Util.Point((tyr.MapAnalyzer.GetMainRamp().X + Natural.BaseLocation.Pos.X) / 2f, (tyr.MapAnalyzer.GetMainRamp().Y + Natural.BaseLocation.Pos.Y) / 2f);
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
                SiegeTask.Task.Stopped = true;

                if (Completed(UnitTypes.LIBERATOR) >= 4
                    || FoodUsed() >= 198)
                    TimingAttackTask.Task.RequiredSize = 50;
                else
                    TimingAttackTask.Task.RequiredSize = 70;
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

            if (tyr.Frame >= 22.4 * 60 * 2.5 && !SuspectCloackedBanshees)
            {
                HideBuildingTask.Task.Stopped = true;
                HideBuildingTask.Task.Clear();
            }

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.FACTORY) > 0 && tyr.Frame <= 22.4 * 60 * 2.5 + 22.4)  
                SuspectCloackedBanshees = true;

            if(tyr.Frame >= 22.4 * 60 * 2.5)
                tyr.OrbitalAbilityManager.SaveEnergy = 0;


            if (tyr.TargetManager.PotentialEnemyStartLocations.Count == 1
                && !ScanTimingsSet)
            {
                ScanTimingsSet = true;
                tyr.OrbitalAbilityManager.SaveEnergy = 50;
                tyr.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = tyr.TargetManager.PotentialEnemyStartLocations[0],
                    FromFrame = (int)(22.4 * 60 * 2.25)
                });
            }
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
                    && FoodLeft() >= 1
                    && (!SuspectCloackedBanshees || Count(UnitTypes.SCV) < 24))
                    agent.Order(524);
            }
            else if (agent.Unit.UnitType == UnitTypes.BARRACKS)
            {
                if (Completed(UnitTypes.FACTORY_TECH_LAB) > 0
                    && Count(UnitTypes.MARINE) >= 3
                    && Count(UnitTypes.THOR) + Count(UnitTypes.SIEGE_TANK) + Count(UnitTypes.CYCLONE) <= Completed(UnitTypes.THOR) + Completed(UnitTypes.SIEGE_TANK) + Completed(UnitTypes.CYCLONE)
                    && Minerals() <= 250)
                    return;

                if (SuspectCloackedBanshees
                    && (Count(UnitTypes.MARINE) >= 3 || Completed(UnitTypes.HELLION) >= 3))
                {
                    return;
                }
                if (Minerals() >= 50
                    && FoodLeft() >= 1)
                    agent.Order(560);
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY)
            {
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.HELLION) < 1 && (tyr.Frame <= 22.4 * 60 * 2.5 + 22.4 || SuspectCloackedBanshees))
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
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
                {
                    if (SuspectCloackedBanshees)
                    {
                        if (Count(UnitTypes.HELLION) < 6
                            && Minerals() >= 100
                            && FoodLeft() >= 2)
                            agent.Order(595);
                        return;
                    }
                    /*
                    if (SuspectCloackedBanshees)
                    {
                        if (Minerals() >= 300
                            && Gas() >= 200
                            && FoodLeft() >= 6
                            && Completed(UnitTypes.ARMORY) >= 1)
                            agent.Order(594);
                            return;
                    }
                    */
                    else if ((Count(UnitTypes.CYCLONE) == 0 || SuspectCloackedBanshees)
                        && Minerals() >= 150
                        && Gas() >= 100
                        && FoodLeft() >= 3
                        && (Count(UnitTypes.CYCLONE) < 4 || Count(UnitTypes.SIEGE_TANK) >= 2))
                        agent.Order(597);
                    else if (Count(UnitTypes.THOR) <= Count(UnitTypes.SIEGE_TANK) - 5
                        && Completed(UnitTypes.ARMORY) >= 1) {
                        if (Minerals() >= 300
                            && Gas() >= 200
                            && FoodLeft() >= 6)
                                agent.Order(594);
                    }
                    else if (Minerals() >= 150
                        && Gas() >= 125
                        && FoodLeft() >= 3)
                        agent.Order(591);
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_REACTOR)
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
                if (!tyr.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (SuspectCloackedBanshees)
                    {
                        if (Minerals() >= 50
                            && Gas() >= 25)
                            agent.Order(487);
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
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_REACTOR)
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
                    else
                    {
                        if (Minerals() >= 150
                            && Gas() >= 150
                            && FoodLeft() >= 3)
                            agent.Order(626);
                    }
                }
                else if (tyr.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_TECH_LAB)
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
                if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(116)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(864);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(30)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(855);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(117)
                    && Gas() >= 175
                    && Minerals() >= 175)
                    agent.Order(865);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(31)
                    && Gas() >= 175
                    && Minerals() >= 175)
                    agent.Order(856);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(118)
                    && Gas() >= 250
                    && Minerals() >= 250)
                    agent.Order(866);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(32)
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
