using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Terran
{
    public class TankPush : Build
    {
        private WallInCreator WallIn;
        private bool ReapersDetected = false;
        private Point2D OverrideDefenseTarget;

        private List<CustomController> AttackMicroControllers = new List<CustomController>();

        private List<DefenseSquadTask> TankDefenseTasks = new List<DefenseSquadTask>();
        private List<DefenseSquadTask> LiberatorDefenseTasks = new List<DefenseSquadTask>();
        private List<DefenseSquadTask> VikingDefenseTasks = new List<DefenseSquadTask>();
        private List<DefenseSquadTask> CycloneDefenseTasks;

        private bool ScanTimingsSet = false;

        private bool SuspectCloackedBanshees = false;

        private int DesiredMedivacs = 0;

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            SiegeTask.Enable();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            DefenseTask.Enable();
            BunkerDefendersTask.Enable();
            SupplyDepotTask.Enable();
            ArmyRavenTask.Enable();
            RepairTask.Enable();
            ReplenishBuildingSCVTask.Enable();
            ClearBlockedExpandsTask.Enable();
            HomeRepairTask.Enable();
            TransformTask.Enable();
            DefendClosestBaseTask.Enable();

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

            DefenseSquadTask.Enable(false, UnitTypes.CYCLONE);
            foreach (DefenseSquadTask task in DefenseSquadTask.Tasks)
                task.MaxDefenders = 2;
            if (CycloneDefenseTasks == null)
                CycloneDefenseTasks = DefenseSquadTask.GetDefenseTasks(UnitTypes.CYCLONE);

            DefenseSquadTask.Enable(CycloneDefenseTasks, false, false);
            foreach (DefenseSquadTask task in CycloneDefenseTasks)
            {
                task.MaxDefenders = 1;
                task.Priority = 8;
            }
        }

        public override string Name()
        {
            return "TankPush";
        }

        public override void OnStart(Bot bot)
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

            MicroControllers.Add(new MineController());
            MicroControllers.Add(new TankController());
            MicroControllers.Add(new LiberatorController());
            MicroControllers.Add(new VikingController());
            MicroControllers.Add(new MedivacController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new CycloneVsBansheeController());
            MicroControllers.Add(new DodgeBallController());

            OverrideDefenseTarget = bot.MapAnalyzer.Walk(NaturalDefensePos, bot.MapAnalyzer.EnemyDistances, 15);

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.SUPPLY_DEPOT, UnitTypes.SUPPLY_DEPOT, UnitTypes.BUNKER });
                WallIn.ReserveSpace();
            }

            Set += SupplyDepots();
            Set += Turrets();
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

        private BuildList Turrets()
        {
            BuildList result = new BuildList();

            result.If(() => { return SuspectCloackedBanshees; });
            result.If(() => Count(UnitTypes.CYCLONE) + Count(UnitTypes.THOR) > 0);
            result.Building(UnitTypes.ENGINEERING_BAY);
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                result.Building(UnitTypes.MISSILE_TURRET, b, b.MineralLinePos, () => { return b.Owner == Bot.Main.PlayerId && b.ResourceCenter != null; });
                result.Building(UnitTypes.MISSILE_TURRET, b, b.OppositeMineralLinePos, () => { return b.Owner == Bot.Main.PlayerId && b.ResourceCenter != null; });
            }
            result.Building(UnitTypes.MISSILE_TURRET, Main, 2);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.STARPORT, () => Count(UnitTypes.CYCLONE) >= 4);
            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.BARRACKS, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.SUPPLY_DEPOT, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.FACTORY);
            result.Building(UnitTypes.BARRACKS, () => { return FourRax.Get().Detected; });
            result.If(() => (!SuspectCloackedBanshees && !ReapersDetected) || Count(UnitTypes.CYCLONE) > 0);
            result.If(() => SuspectCloackedBanshees ||
                !FourRax.Get().Detected || 
                (Completed(UnitTypes.SIEGE_TANK) > 2 && Completed(UnitTypes.MARINE) > 10));
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY);
            result.Building(UnitTypes.FACTORY, () => { return !ReapersDetected || (Minerals() >= 400 && Gas() >= 400 && Count(UnitTypes.COMMAND_CENTER) >= 4); });
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.ARMORY, () => !SuspectCloackedBanshees);
            result.Building(UnitTypes.ENGINEERING_BAY);
            foreach (Base b in Bot.Main.BaseManager.Bases)
                if (b != Main && b != Natural)
                    result.Building(UnitTypes.SENSOR_TOWER, b, b.BaseLocation.Pos, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.9 && Count(UnitTypes.SENSOR_TOWER) < 2);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.FACTORY, () => { return ReapersDetected || (Minerals() >= 400 && Gas() >= 400 && Count(UnitTypes.COMMAND_CENTER) >= 4); });
            result.Building(UnitTypes.STARPORT);
            result.Building(UnitTypes.STARPORT, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BATTLECRUISER) > 0);
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 3);
            result.Building(UnitTypes.FACTORY, 2, () => { return !ReapersDetected || (Minerals() >= 400 && Gas() >= 400 && Count(UnitTypes.COMMAND_CENTER) >= 4); });
            result.Building(UnitTypes.COMMAND_CENTER);
            result.Building(UnitTypes.REFINERY, 2);
            result.Building(UnitTypes.FACTORY, () => { return !ReapersDetected; });

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            BalanceGas();

            if (bot.Observation.ActionErrors != null)
                foreach (ActionError error in bot.Observation.ActionErrors)
                    DebugUtil.WriteLine("Error with ability " + error.AbilityId + ": " + error.Result);

            TransformTask.Task.Priority = 10;
            if (Completed(UnitTypes.SIEGE_TANK) + Completed(UnitTypes.THOR) + Completed(UnitTypes.CYCLONE) >= 10)
                TransformTask.Task.HellionsToHellbats();
            TransformTask.Task.ThorsToSingleTarget();

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.CannonDefenseRadius = 20;
            
            if (bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) == 1
                && Completed(UnitTypes.SIEGE_TANK) > 0
                && bot.Frame <= 22.4 * 60 * 4
                && Count(UnitTypes.COMMAND_CENTER) < 3)
                IdleTask.Task.OverrideTarget = SC2Util.Point((bot.MapAnalyzer.GetMainRamp().X + Natural.BaseLocation.Pos.X) / 2f, (bot.MapAnalyzer.GetMainRamp().Y + Natural.BaseLocation.Pos.Y) / 2f);
            else if (Count(UnitTypes.COMMAND_CENTER) >= 3 && !SuspectCloackedBanshees)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else if (Count(UnitTypes.COMMAND_CENTER) >= 2 && SuspectCloackedBanshees)
                IdleTask.Task.OverrideTarget = Natural.BaseLocation.Pos;
            else
                IdleTask.Task.OverrideTarget = null;

            if (ReapersDetected)
            {
                SiegeTask.Task.Stopped = true;

                TimingAttackTask.Task.RequiredSize = 30;
                TimingAttackTask.Task.RetreatSize = 8;
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
                TimingAttackTask.Task.BeforeControllers = AttackMicroControllers;
            }
            /*
            else
            {
                TimingAttackTask.Task.Stopped = true;

                if (Completed(UnitTypes.LIBERATOR) >= 4
                    || FoodUsed() >= 198)
                    SiegeTask.Task.RequiredSize = 50;
                else
                    SiegeTask.Task.RequiredSize = 70;
                SiegeTask.Task.RetreatSize = 12;
                SiegeTask.Task.Stopped = false;
                SiegeTask.Task.CustomControllers = AttackMicroControllers;
            }*/

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

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 80;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 80;
            if (SuspectCloackedBanshees)
            {
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 15;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 15;
            }
            else if (Count(UnitTypes.COMMAND_CENTER) > 2)
            {
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 40;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 40;
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
            }
            else
            {
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            }

            if (StrategyAnalysis.Bio.Get().Detected
                || bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.SIEGE_TANK) + bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.SIEGE_TANK_SIEGED) > 0
                || bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) + bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE_BURROWED) >= 3)
                ReapersDetected = false;
            else if ((bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) > 6
                || bot.EnemyStrategyAnalyzer.Count(UnitTypes.BANSHEE) >= 1)
                && bot.Frame < 22.4 * 600)
                ReapersDetected = true;
            BunkerDefendersTask.Task.LeaveBunkers = !StrategyAnalysis.Bio.Get().Detected
                && bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 1
                && (bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 2 || bot.Frame <= 22.4 * 60 * 4);

            if (ReapersDetected)
                DefenseSquadTask.Enable(CycloneDefenseTasks, false, false);
            else
                foreach (DefenseSquadTask task in CycloneDefenseTasks)
                {
                    task.Stopped = true;
                    task.Clear();
                }

            if ((bot.EnemyStrategyAnalyzer.Count(UnitTypes.STARPORT_TECH_LAB) > 0 && bot.Frame <= 4 * 60 * 22.4)
                || bot.EnemyStrategyAnalyzer.Count(UnitTypes.BANSHEE) > 0
                || (bot.EnemyStrategyAnalyzer.Count(UnitTypes.STARPORT) > 0 && bot.Frame <= 3 * 60 * 22.4))
                SuspectCloackedBanshees = true;

            if (bot.TargetManager.PotentialEnemyStartLocations.Count == 1
                && !ScanTimingsSet)
            {
                ScanTimingsSet = true;
                bot.OrbitalAbilityManager.SaveEnergy = 50;
                bot.OrbitalAbilityManager.ScanCommands.Add(new ScanCommand()
                {
                    Pos = bot.TargetManager.PotentialEnemyStartLocations[0],
                    FromFrame = (int)(22.4 * 60 * 2.5)
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
                    && FoodLeft() >= 1)
                    agent.Order(524);
            }
            else if (agent.Unit.UnitType == UnitTypes.BARRACKS)
            {
                if (Completed(UnitTypes.FACTORY_TECH_LAB) > 0
                    && Count(UnitTypes.MARINE) >= 4
                    && Count(UnitTypes.THOR) + Count(UnitTypes.SIEGE_TANK) + Count(UnitTypes.CYCLONE) <= Completed(UnitTypes.THOR) + Completed(UnitTypes.SIEGE_TANK) + Completed(UnitTypes.CYCLONE)
                    && Minerals() <= 250)
                    return;

                if (Minerals() >= 50
                    && FoodLeft() >= 1
                    && (!SuspectCloackedBanshees || Count(UnitTypes.MARINE) < 4)
                    && (!SuspectCloackedBanshees || Count(UnitTypes.CYCLONE) == 0)
                    && (Count(UnitTypes.MARINE) < 4 || Count(UnitTypes.CYCLONE) + Count(UnitTypes.SIEGE_TANK) + Count(UnitTypes.THOR) > 0))
                    agent.Order(560);
            }
            else if (agent.Unit.UnitType == UnitTypes.FACTORY)
            {
                if (!bot.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.HELLION) + Count(UnitTypes.CYCLONE) == 0)
                    {
                        if (Minerals() >= 100
                            && FoodLeft() >= 2)
                            agent.Order(595);
                    } else //if (Count(UnitTypes.FACTORY_TECH_LAB) <= Count(UnitTypes.FACTORY_REACTOR) || ReapersDetected || Count(UnitTypes.FACTORY_REACTOR) >= 1)
                        agent.Order(454);
                    //else
                    //    agent.Order(455);
                }
                else if (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_TECH_LAB)
                {
                    if ((ReapersDetected || bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 1)
                        && (Count(UnitTypes.CYCLONE) == 0 || ReapersDetected)
                        && Minerals() >= 150
                        && Gas() >= 100
                        && FoodLeft() >= 3
                        && (Count(UnitTypes.CYCLONE) < 6 || Completed(UnitTypes.ARMORY) == 0)
                        && (Count(UnitTypes.CYCLONE) < 4 || Count(UnitTypes.SIEGE_TANK) >= 2 || SuspectCloackedBanshees))
                        agent.Order(597);
                    else if ((Count(UnitTypes.THOR) <= Count(UnitTypes.SIEGE_TANK) - 5 || SuspectCloackedBanshees)
                        && Completed(UnitTypes.ARMORY) >= 1) {
                        if (Minerals() >= 300
                            && Gas() >= 200
                            && FoodLeft() >= 6)
                                agent.Order(594);
                    }
                    else if (Minerals() >= 150
                        && Gas() >= 125
                        && !SuspectCloackedBanshees
                        && FoodLeft() >= 3)
                        agent.Order(591);
                }
                else if (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.FACTORY_REACTOR)
                {
                    if (Completed(UnitTypes.ARMORY) > 0
                        && Minerals() >= 75
                        && Gas() >= 25
                        && Count(UnitTypes.WIDOW_MINE) < 4
                        && FoodLeft() >= 2)
                        agent.Order(614);
                    else if (Completed(UnitTypes.ARMORY) > 0
                        && Minerals() >= 100
                        && FoodLeft() >= 2
                        && (Count(UnitTypes.HELLION) < 8 || Minerals() >= 400)
                        && Count(UnitTypes.HELLION) < 12)
                        agent.Order(596);
                    else if (Minerals() >= 100
                        && FoodLeft() >= 2
                        && (Count(UnitTypes.HELLION) < 8 || Minerals() >= 400)
                        && Count(UnitTypes.HELLION) < 12)
                        agent.Order(595);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.STARPORT)
            {
                if (!bot.UnitManager.Agents.ContainsKey(agent.Unit.AddOnTag))
                {
                    if (Count(UnitTypes.VIKING_FIGHTER) > 0)
                    {
                        if (Count(UnitTypes.STARPORT_TECH_LAB) > 0)
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
                    if (Count(UnitTypes.VIKING_FIGHTER) < 3 || ReapersDetected)
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
                else if (bot.UnitManager.Agents[agent.Unit.AddOnTag].Unit.UnitType == UnitTypes.STARPORT_TECH_LAB)
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
                        && Count(UnitTypes.RAVEN) >= 2
                        && !SuspectCloackedBanshees
                        && Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BATTLECRUISER) == 0)
                        agent.Order(626);
                    else if (Minerals() > 150
                            && Gas() >= 75
                            && FoodLeft() >= 2
                            && Count(UnitTypes.VIKING_FIGHTER) < 15
                            && (SuspectCloackedBanshees || Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BATTLECRUISER) > 0))
                        agent.Order(624);
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
                if (Count(UnitTypes.HELLBAT) + Count(UnitTypes.HELLION) > 0
                    && Gas() >= 150
                    && Minerals() >= 150
                    && !SuspectCloackedBanshees)
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
            else if (agent.Unit.UnitType == UnitTypes.ENGINEERING_BAY
                    && Gas() >= 400
                    && Minerals() >= 400
                    && Count(UnitTypes.PLANETARY_FORTRESS) > 0)
            {
                agent.Order(650);
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
