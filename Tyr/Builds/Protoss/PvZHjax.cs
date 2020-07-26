using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.BuildingPlacement;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class PvZHjax : Build
    {
        private Point2D OverrideDefenseTarget;
        private WallInCreator WallIn;
        private FallBackController FallBackController = new FallBackController() { ReturnFire = true };
        StutterForwardController StutterForwardController = new StutterForwardController() { MaxDist = 3 };
        private FearEnemyController FearSpinesController = new FearEnemyController(
            new HashSet<uint>() { UnitTypes.ZEALOT, UnitTypes.ARCHON, UnitTypes.ADEPT, UnitTypes.STALKER, UnitTypes.IMMORTAL },
            UnitTypes.SPINE_CRAWLER,
            12);
        private Point2D ShieldBatteryPos;
        private bool MassZerglings = false;
        private bool RoachRushDetected = false;
        public bool BlockExpand = false;
        public bool ProxyPylon = false;
        private bool InitialAttackDone = false;
        private bool InitialRoachCounterDone = false;
        private bool UseStorm = false;
        private bool UseColosus = true;
        private Point2D NydusPos;
        private bool ActiveHatchery = false;

        public bool CounterRoaches = true;
        public bool DefendNydus = true;

        private bool EnemyAttackPerformed = false;

        private bool RoboArmy = false;

        public override string Name()
        {
            return "PvZHjax";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (!BlockExpand)
                WorkerScoutTask.Enable();
            ArmyObserverTask.Enable();
            ObserverScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArchonMergeTask.Enable();
            ShieldRegenTask.Enable();
            HodorTask.Enable();
            AdeptHarassMainTask.Enable();
            WarpPrismTask.Enable();
            ScoutTask.Enable();
            if (BlockExpand)
                BlockExpandTask.Enable();
            if (ProxyPylon)
            {
                ProxyTask.Enable(new List<ProxyBuilding>() { new ProxyBuilding() { UnitType = UnitTypes.PYLON } });
                ProxyTask.Task.UseEnemyNatural = true;
            }
        }

        public override void OnStart(Bot bot)
        {
            OverrideDefenseTarget = bot.MapAnalyzer.Walk(NaturalDefensePos, bot.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(FearSpinesController);
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new AdeptKillWorkersController() { TargetTypes = new HashSet<uint> { UnitTypes.ZERGLING } });
            MicroControllers.Add(new AdeptKillWorkersController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new SoftLeashController(UnitTypes.ZEALOT, new HashSet<uint>() { UnitTypes.IMMORTAL, UnitTypes.ARCHON }, 6));
            MicroControllers.Add(new SoftLeashController(UnitTypes.ARCHON, UnitTypes.IMMORTAL, 6));
            MicroControllers.Add(StutterForwardController);
            MicroControllers.Add(FallBackController);
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new AdvanceController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.CreateNatural(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.GATEWAY, UnitTypes.ZEALOT, UnitTypes.GATEWAY});
                ShieldBatteryPos = DetermineShieldBatteryPos();
                WallIn.ReserveSpace();
                foreach (WallBuilding building in WallIn.Wall)
                    if (building.Type == UnitTypes.PYLON)
                        Bot.Main.buildingPlacer.ReservedLocation.Add(new ReservedBuilding() { Type = UnitTypes.NEXUS, Pos = building.Pos });
            }

            Base third = null;
            float dist = 1000000;
            foreach (Base b in bot.BaseManager.Bases)
            {
                if (b == Main
                    || b == Natural)
                    continue;
                float newDist = SC2Util.DistanceSq(b.BaseLocation.Pos, Main.BaseLocation.Pos);
                if (newDist > dist)
                    continue;
                dist = newDist;
                third = b;
            }
            NydusPos = new PotentialHelper(bot.MapAnalyzer.StartLocation, 18).To(third.BaseLocation.Pos).Get();

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) > 0
                && (Count(UnitTypes.CYBERNETICS_CORE) > 0 || EarlyPool.Get().Detected)
                && (Count(UnitTypes.GATEWAY) >= 2 || !EarlyPool.Get().Detected));
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuild();
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => { return !EarlyPool.Get().Detected; });
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.8);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 19);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3);
            result.Train(UnitTypes.PROBE, 80, () => Count(UnitTypes.NEXUS) >= 4);
            result.If(() => Count(UnitTypes.STALKER) < 5 || Count(UnitTypes.IMMORTAL) < 2 || Count(UnitTypes.NEXUS) >= 2);
            result.If(() => !EarlyPool.Get().Detected || Count(UnitTypes.ZEALOT) < 8 || Count(UnitTypes.NEXUS) >= 2);
            result.If(() => Completed(UnitTypes.CYBERNETICS_CORE) > 0 || (EarlyPool.Get().Detected && Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT) < 8));
            result.Train(UnitTypes.STALKER, 6, () => RoachRushDetected);
            result.Train(UnitTypes.ZEALOT, 1, () => EarlyPool.Get().Detected && !RoachRushDetected);
            result.Train(UnitTypes.ADEPT, 2, () => !AdeptHarassMainTask.Task.Sent && !RoachRushDetected);
            result.Upgrade(UpgradeType.WarpGate, () => !EarlyPool.Get().Detected || Count(UnitTypes.ADEPT) + Count(UnitTypes.ZEALOT) >= 4);
            result.Train(UnitTypes.ZEALOT, 1, () => Completed(UnitTypes.NEXUS) < 3);
            result.Train(UnitTypes.ZEALOT, 2, () => EarlyPool.Get().Detected && !RoachRushDetected);
            result.Train(UnitTypes.ADEPT, 5, () => EarlyPool.Get().Detected && !RoachRushDetected);
            result.Train(UnitTypes.ZEALOT, 10, () => EarlyPool.Get().Detected && !RoachRushDetected);
            //result.Train(UnitTypes.WARP_PRISM, 1, () => !InitialAttackDone && !RoachRushDetected);
            result.Train(UnitTypes.SENTRY, 1, () => !TimingAttackTask.Task.AttackSent && !RoachRushDetected && !EarlyPool.Get().Detected && Count(UnitTypes.ZEALOT) > 0);
            result.Train(UnitTypes.ZEALOT, 6, () => !InitialAttackDone && Count(UnitTypes.TEMPLAR_ARCHIVE) > 0);
            result.Train(UnitTypes.ZEALOT, 8, () => !InitialAttackDone && Count(UnitTypes.HIGH_TEMPLAR) + Count(UnitTypes.ARCHON) * 2 >= 4);
            result.Train(UnitTypes.IMMORTAL, 1);
            result.Train(UnitTypes.OBSERVER, 1, () => RoachRushDetected);
            result.Train(UnitTypes.IMMORTAL, 2);
            result.Train(UnitTypes.WARP_PRISM, 1, () => !RoachRushDetected || (Count(UnitTypes.ROBOTICS_FACILITY) >= 2 && Count(UnitTypes.NEXUS) >= 3));
            result.Train(UnitTypes.IMMORTAL, 3);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Upgrade(UpgradeType.ExtendedThermalLance, () => UseColosus);
            result.Train(UnitTypes.COLOSUS, 3, () => UseColosus);
            result.Train(UnitTypes.IMMORTAL, 6, () => !RoachRushDetected || Count(UnitTypes.NEXUS) >= 3);
            result.Train(UnitTypes.COLOSUS, 4, () => UseColosus && RoboArmy);
            result.Train(UnitTypes.IMMORTAL, 8, () => !RoachRushDetected || Count(UnitTypes.NEXUS) >= 3);
            result.Train(UnitTypes.COLOSUS, 5, () => UseColosus && RoboArmy);
            result.Train(UnitTypes.IMMORTAL, 10, () => !RoachRushDetected || Count(UnitTypes.NEXUS) >= 3);
            result.Train(UnitTypes.COLOSUS, 6, () => UseColosus && RoboArmy);
            result.Train(UnitTypes.IMMORTAL, 12, () => !RoachRushDetected || Count(UnitTypes.NEXUS) >= 3);
            result.Upgrade(UpgradeType.Charge, () => Count(UnitTypes.TEMPLAR_ARCHIVE) > 0 || (Gas() >= 200 && Minerals() >= 300));
            result.Train(UnitTypes.HIGH_TEMPLAR, 2, () => Gas() >= 75 && Count(UnitTypes.HIGH_TEMPLAR) == 1 && !RoboArmy);
            result.Train(UnitTypes.HIGH_TEMPLAR, () => Gas() >= 150 && Count(UnitTypes.ARCHON) * 2 + Count(UnitTypes.HIGH_TEMPLAR) < 6 && !RoboArmy);
            result.Train(UnitTypes.HIGH_TEMPLAR, 6, () => Gas() >= 150 && InitialAttackDone && UseStorm && !RoboArmy);
            result.Upgrade(UpgradeType.PsiStorm, () => InitialAttackDone && UseStorm && !RoboArmy);
            result.Train(UnitTypes.ZEALOT, 5, () => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) >= 2 && (Completed(UnitTypes.WARP_PRISM) == 0 || TimingAttackTask.Task.Units.Count == 0) && RoboArmy);
            result.Train(UnitTypes.ZEALOT, 10, () => Count(UnitTypes.IMMORTAL) >= 4 && Count(UnitTypes.COLOSUS) >= 2 && RoboArmy);
            result.Train(UnitTypes.ZEALOT, 5, () => !InitialAttackDone);
            result.Train(UnitTypes.ZEALOT, 10, () => !InitialAttackDone && Completed(UnitTypes.TWILIGHT_COUNSEL) == 1 && Count(UnitTypes.TEMPLAR_ARCHIVE) == 1 && Completed(UnitTypes.TEMPLAR_ARCHIVE) == 0);
            result.Train(UnitTypes.ZEALOT, 15, () => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) >= 2 && (Completed(UnitTypes.WARP_PRISM) == 0 || TimingAttackTask.Task.Units.Count == 0) && !RoboArmy);
            result.Train(UnitTypes.ZEALOT, 25, () => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) >= 2 && (Completed(UnitTypes.WARP_PRISM) == 0 || TimingAttackTask.Task.Units.Count == 0) && InitialAttackDone && !RoboArmy);
            result.Train(UnitTypes.ZEALOT, 35, () => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) >= 2 && InitialAttackDone && Minerals() >= 400 && !RoboArmy);
            result.Train(UnitTypes.STALKER, 35, () => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) >= 2 && InitialAttackDone && Minerals() >= 400 && RoboArmy);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.If(() => Bot.Main.Frame >= 22.4 * 17);
            if (WallIn.Wall.Count >= 5)
            {
                result.Building(UnitTypes.PYLON, Natural, WallIn.Wall[4].Pos, true);
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[3].Pos, true, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
                //result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[1].Pos, true, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
            } else
            {
                result.Building(UnitTypes.PYLON);
                result.Building(UnitTypes.GATEWAY, () => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.GATEWAY, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
            }
            result.If(() => Count(UnitTypes.GATEWAY) > 0);
            //result.Building(UnitTypes.ASSIMILATOR, () => !EarlyPool.Get().Detected || Expanded.Get().Detected || Count(UnitTypes.ZEALOT) >= 6);
            result.Building(UnitTypes.ASSIMILATOR, () => Bot.Main.Frame >= 22.4 * 53);
            result.Building(UnitTypes.NEXUS, () => (!EarlyPool.Get().Detected || Completed(UnitTypes.ADEPT) + Completed(UnitTypes.ZEALOT) >= 3 || RoachRushDetected) && (!RoachRushDetected || Completed(UnitTypes.IMMORTAL) > 0));
            if (WallIn.Wall.Count >= 5)
                result.Building(UnitTypes.CYBERNETICS_CORE, Natural, WallIn.Wall[1].Pos, true);
            else
                result.Building(UnitTypes.CYBERNETICS_CORE);
            //result.Building(UnitTypes.CYBERNETICS_CORE, () => EarlyPool.Get().Detected && Count(UnitTypes.ZEALOT) >= 6);
            result.If(() => Count(UnitTypes.ADEPT) >= 2 || AdeptHarassMainTask.Task.Sent);
            result.Building(UnitTypes.ASSIMILATOR, () => RoachRushDetected);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => RoboArmy);
            if (WallIn.Wall.Count >= 5)
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[3].Pos, true, () => !EarlyPool.Get().Detected);
            else
                result.Building(UnitTypes.GATEWAY, () => !EarlyPool.Get().Detected);
            if (ShieldBatteryPos == null)
                result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalDefensePos, () => (EarlyPool.Get().Detected && !RoachRushDetected) || (RoachRushDetected && Count(UnitTypes.STALKER) > 0));
            else
                result.Building(UnitTypes.SHIELD_BATTERY, Natural, ShieldBatteryPos, true, () => (EarlyPool.Get().Detected && !RoachRushDetected) || (RoachRushDetected && Count(UnitTypes.STALKER) > 0));
            result.Building(UnitTypes.ASSIMILATOR, 2, () => (!EarlyPool.Get().Detected || Count(UnitTypes.ZEALOT) >= 4) && (!RoachRushDetected || Count(UnitTypes.IMMORTAL) > 0));
            result.Building(UnitTypes.TWILIGHT_COUNSEL, () => !RoboArmy || Completed(UnitTypes.IMMORTAL) > 0);
            result.Building(UnitTypes.GATEWAY, Main, () => !EarlyPool.Get().Detected);
            result.Building(UnitTypes.TEMPLAR_ARCHIVE, () => !RoboArmy);
            result.Building(UnitTypes.GATEWAY, Main, () => !EarlyPool.Get().Detected);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.ZEALOT) >= 8 || Minerals() >= 450);
            result.Building(UnitTypes.GATEWAY, Main, () => !EarlyPool.Get().Detected);
            result.Building(UnitTypes.PYLON, Natural, () => Count(UnitTypes.CYBERNETICS_CORE) > 0 && Natural.ResourceCenter != null);
            result.Building(UnitTypes.NEXUS);
            result.If(() =>
                TimingAttackTask.Task.AttackSent ||
                InitialAttackDone ||
                Minerals() >= 600 ||
                RoachRushDetected ||
                (EarlyPool.Get().Detected && !Expanded.Get().Detected) ||
                (EarlyPool.Get().Detected && Count(UnitTypes.ARCHON) > 0));
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => (!EarlyPool.Get().Detected || Completed(UnitTypes.ADEPT) + Completed(UnitTypes.ZEALOT) >= 8) && !RoachRushDetected);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.FORGE, () => !RoboArmy || Count(UnitTypes.ROBOTICS_FACILITY) >= 2);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR, () => !RoboArmy || Minerals() >= 600);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.ROBOTICS_BAY, () => UseColosus && (TimingAttackTask.Task.AttackSent || RoachRushDetected));
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => Completed(UnitTypes.ADEPT) + Completed(UnitTypes.ZEALOT) >= 10 || InitialAttackDone || RoachRushDetected);
            result.Building(UnitTypes.ASSIMILATOR, 4, () => Minerals() >= 700 && Gas() < 200);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => InitialAttackDone || RoachRushDetected);
            result.If(() => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) >= 3 && Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) >= 10);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) + Count(UnitTypes.ADEPT) + Count(UnitTypes.COLOSUS) + Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) >= 15);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY, Main, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) + Count(UnitTypes.ADEPT) + Count(UnitTypes.COLOSUS) + Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) >= 40 || Minerals() >= 650);
            result.Building(UnitTypes.NEXUS);

            return result;
        }
        
        private Point2D DetermineShieldBatteryPos()
        {
            if (WallIn.Wall.Count < 5)
                return null;

            if (Bot.Main.Map == MapEnum.DiscoBloodbath
                && Main.BaseLocation.Pos.X <= 100)
                return new Point2D() { X = WallIn.Wall[4].Pos.X, Y = WallIn.Wall[4].Pos.Y + 2 };

            if (Bot.Main.Map == MapEnum.Zen)
            {
                if (Main.BaseLocation.Pos.X <= 100)
                    return new Point2D() { X = 64, Y = 58 };
                else
                    return new Point2D() { X = 128, Y = 115 };
            }

            Point2D pos = SC2Util.TowardCardinal(WallIn.Wall[4].Pos, Natural.BaseLocation.Pos, 2);
            if (Math.Abs(pos.X - Natural.BaseLocation.Pos.X) <= 3
                && Math.Abs(pos.Y - Natural.BaseLocation.Pos.Y) <= 3)
                return null;
            if (Bot.Main.buildingPlacer.CheckPlacement(pos, SC2Util.Point(2, 2), UnitTypes.PYLON, null, true))
                return pos;
            return null;
        }

        public override void OnFrame(Bot bot)
        {
            /*
            if (bot.Frame == 0)
            {
                FileUtil.WriteToFile("ArmyAnalysis.txt", "Started game against " + bot.OpponentID + " on map " + bot.GameInfo.MapName, false);
                FileUtil.WriteToFile("ArmyAnalysis.txt", "", false);
            }
            if (bot.Frame % 224 == 0)
            {
                List<uint> myUnitTypes = new List<uint>() { UnitTypes.ZEALOT, UnitTypes.ADEPT, UnitTypes.STALKER, UnitTypes.ARCHON, UnitTypes.IMMORTAL, UnitTypes.COLOSUS };
                List<uint> enemyUnitTypes = new List<uint>() { UnitTypes.ZERGLING, UnitTypes.QUEEN, UnitTypes.BANELING, UnitTypes.ROACH, UnitTypes.RAVAGER, UnitTypes.HYDRALISK, UnitTypes.MUTALISK, UnitTypes.CORRUPTOR, UnitTypes.BROOD_LORD, UnitTypes.LURKER};

                List<string> myUnitResults = new List<string>();
                foreach (uint myUnitType in myUnitTypes)
                {
                    int completed = Completed(myUnitType);
                    if (completed == 0)
                        continue;
                    myUnitResults.Add(UnitTypes.LookUp[myUnitType].Name + ":" + completed);
                }
                List<string> enemyUnitResults = new List<string>();
                foreach (uint enemyUnitType in enemyUnitTypes)
                {
                    int completed = EnemyCount(enemyUnitType);
                    if (completed == 0)
                        continue;
                    enemyUnitResults.Add(UnitTypes.LookUp[enemyUnitType].Name + ":" + completed);
                }


                FileUtil.WriteToFile("ArmyAnalysis.txt", "State at: " + (int)((bot.Frame / 22.4) / 60) + ":" + (int)(bot.Frame / 22.4) % 60, false);
                FileUtil.WriteToFile("ArmyAnalysis.txt", "MyUnits: {" + string.Join(", ", myUnitResults) + "}", false);
                FileUtil.WriteToFile("ArmyAnalysis.txt", "EnemyUnits: {" + string.Join(", ", enemyUnitResults) + "}", false);
                FileUtil.WriteToFile("ArmyAnalysis.txt", "SimulationResult me: " + bot.TaskManager.CombatSimulation.MyStartResources + "-> " + bot.TaskManager.CombatSimulation.MyFinalResources + " his: " + bot.TaskManager.CombatSimulation.EnemyStartResources + "-> " + bot.TaskManager.CombatSimulation.EnemyFinalResources, false);
                FileUtil.WriteToFile("ArmyAnalysis.txt", "", false);

            }
            */

            if (!EnemyAttackPerformed
                && (EarlyPool.Get().Detected || RoachRushDetected))
            {
                int enemyCount = 0;
                foreach (Unit enemy in bot.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.ZERGLING
                        && enemy.UnitType != UnitTypes.ROACH)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, bot.MapAnalyzer.StartLocation) <= 60 * 60)
                        enemyCount += enemy.UnitType == UnitTypes.ZERGLING ? 1 : 3;
                }
                if (enemyCount > 6)
                    EnemyAttackPerformed = true;
            }

            FearSpinesController.Stopped = Completed(UnitTypes.IMMORTAL) >= 2;

            ScoutTask.Task.ScoutType = UnitTypes.PHOENIX;
            ScoutTask.Task.Target = bot.TargetManager.PotentialEnemyStartLocations[0];

            if (!TimingAttackTask.Task.AttackSent)
            {
                foreach (Agent agent in bot.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.SENTRY)
                        continue;
                    if (agent.Unit.Energy < 75)
                        continue;
                    // Hallucinate scouting phoenix.
                    agent.Order(154);
                }
            }

            bot.DrawText("early pool detected: " + EarlyPool.Get().Detected);
            if (EarlyPool.Get().Detected)
                AdeptHarassMainTask.Task.Sent = true;


            if (!ActiveHatchery)
            {
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.HATCHERY)
                        continue;
                    if (enemy.IsActive)
                        ActiveHatchery = true;
                    break;
                }
            }

            if (!RoachRushDetected && CounterRoaches)
            {
                if (Bot.Main.Frame < 22.4 * 60 * 4.5
                    && TotalEnemyCount(UnitTypes.ROACH_WARREN) > 0)
                    RoachRushDetected = true;
                if (Bot.Main.Frame < 22.4 * 60 * 2.5
                    && TotalEnemyCount(UnitTypes.LAIR) > 0)
                    RoachRushDetected = true;
                if (Bot.Main.Frame < 22.4 * 60 * 2.5
                    && ActiveHatchery
                    && EarlyPool.Get().Detected)
                    RoachRushDetected = true;
                else if (Bot.Main.Frame < 22.4 * 60 * 5
                    && TotalEnemyCount(UnitTypes.ROACH) > 0)
                    RoachRushDetected = true;
                else if (Bot.Main.Frame < 22.4 * 60 * 5
                    && EnemyCount(UnitTypes.ROACH_WARREN) > 0)
                {
                    foreach (Unit enemy in bot.Enemies())
                    {
                        if (enemy.UnitType == UnitTypes.ROACH_WARREN
                            && enemy.BuildProgress > 0.99)
                        {
                            RoachRushDetected = true;
                            break;
                        }
                    }
                }
            }

            RoboArmy = RoachRushDetected || TotalEnemyCount(UnitTypes.ROACH) + TotalEnemyCount(UnitTypes.RAVAGER) + TotalEnemyCount(UnitTypes.HYDRALISK) >= 15;
            bot.DrawText("RoboArmy: " + RoboArmy);

            if (!WarpPrismTask.Task.WarpInObjectiveSet()
                && TimingAttackTask.Task.Units.Count > 0
                && Bot.Main.Frame % 112 == 0)
            {
                int desiredZealots = Math.Min(Minerals() / 100, Math.Min(Completed(UnitTypes.WARP_GATE), 10 - Count(UnitTypes.ZEALOT)));
                if (desiredZealots > 0)
                WarpPrismTask.Task.AddWarpInObjective(UnitTypes.ZEALOT, desiredZealots);
            }

            //if (Minerals() >= 600)
            //    InitialAttackDone = true;

            if (!InitialAttackDone
                && TimingAttackTask.Task.AttackSent)
            {
                int immortalArchonCount = 0;
                foreach (Agent agent in TimingAttackTask.Task.Units)
                {
                    if (agent.Unit.UnitType == UnitTypes.ARCHON
                        || agent.Unit.UnitType == UnitTypes.IMMORTAL)
                        immortalArchonCount++;
                }
                if (immortalArchonCount <= 1)
                {
                    InitialAttackDone = true;
                    for (int i = TimingAttackTask.Task.Units.Count - 1; i >= 0; i--)
                        if (TimingAttackTask.Task.Units[i].Unit.UnitType == UnitTypes.IMMORTAL
                            || TimingAttackTask.Task.Units[i].Unit.UnitType == UnitTypes.ARCHON)
                            TimingAttackTask.Task.ClearAt(i);
                }
            }

            if (!InitialRoachCounterDone
                && RoachRushDetected
                && TimingAttackTask.Task.AttackSent)
            {
                if (Completed(UnitTypes.STALKER) + Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.ARCHON) + Completed(UnitTypes.COLOSUS) < 8)
                    InitialRoachCounterDone = true;
            }

            ArchonMergeTask.Task.StopAndClear(InitialAttackDone && Count(UnitTypes.ARCHON) >= 3);

            for (int i = IdleTask.Task.Units.Count - 1; i >= 0; i--)
            {
                Agent agent = IdleTask.Task.Units[i];
                if (TimingAttackTask.Task.Units.Count > 0
                    && UnitTypes.CombatUnitTypes.Contains(agent.Unit.UnitType)
                    && agent.DistanceSq(Main.BaseLocation.Pos) >= 50 * 50)
                {
                    IdleTask.Task.RemoveAt(i);
                    TimingAttackTask.Task.Add(agent);
                }
            }

            //if (EnemyCount(UnitTypes.ZERGLING) >= 20)
                FallBackController.Stopped = true;

            if (RoachRushDetected)
                BalanceGas();
            else if (Bot.Main.Frame < 22.4 * 60 * 2
                && Count(UnitTypes.NEXUS) < 2)
                GasWorkerTask.WorkersPerGas = 1;
            else if (Bot.Main.Frame < 22.4 * 60 * 2
                && Count(UnitTypes.ASSIMILATOR) < 2)
                GasWorkerTask.WorkersPerGas = 2;
            else
                BalanceGas();

            bool gatewayExists = false;
            foreach (Agent agent in Bot.Main.Units())
                if (agent.Unit.UnitType == UnitTypes.GATEWAY
                    || agent.Unit.UnitType == UnitTypes.WARP_GATE)
                    gatewayExists = true;

            if (gatewayExists && Count(UnitTypes.ASSIMILATOR) == 0)
            {
                ConstructionTask.Task.DedicatedNaturalProbe = false;
                if (ConstructionTask.Task.NaturalProbe != null
                    && !WorkerScoutTask.Task.Done
                    && WorkerScoutTask.Task.Units.Count == 0)
                {
                    for (int i = 0; i < ConstructionTask.Task.Units.Count; i++)
                        if (ConstructionTask.Task.Units[i] == ConstructionTask.Task.NaturalProbe)
                        {
                            ConstructionTask.Task.RemoveAt(i);
                            WorkerScoutTask.Task.Add(ConstructionTask.Task.NaturalProbe);
                            ConstructionTask.Task.NaturalProbe = null;
                            break;
                        }
                }
            }
            else
                ConstructionTask.Task.DedicatedNaturalProbe = Count(UnitTypes.CYBERNETICS_CORE) == 0;

            if (ProxyPylon && ProxyTask.Task.UnitCounts.ContainsKey(UnitTypes.PYLON) && ProxyTask.Task.UnitCounts[UnitTypes.PYLON] > 0)
            {
                ProxyPylon = false;
                ProxyTask.Task.Stopped = true;
                ProxyTask.Task.Clear();
            }

            int wallDone = 0;
            foreach (WallBuilding building in WallIn.Wall)
            {
                if (!BuildingType.LookUp.ContainsKey(building.Type))
                {
                    wallDone++;
                    continue;
                }
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                {
                    if (agent.DistanceSq(building.Pos) <= 1 * 1)
                    {
                        wallDone++;
                        break;
                    }
                }
            }

            // Cancel nexus when the enemy has an early pool.
            if ((EarlyPool.Get().Detected && !Expanded.Get().Detected && Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.ADEPT) < 2 && !RoachRushDetected)
                || (RoachRushDetected && Completed(UnitTypes.IMMORTAL) == 0))
                CancelBuilding(UnitTypes.NEXUS);

            //StutterForwardController.Stopped = Count(UnitTypes.NEXUS) >= 3 || TimingAttackTask.Task.Units.Count > 0 || Completed(UnitTypes.ZEALOT) > 0;
            StutterForwardController.Stopped = true;
            HodorTask.Task.Stopped = Count(UnitTypes.NEXUS) >= 3 
                || TimingAttackTask.Task.Units.Count > 0
                || wallDone < WallIn.Wall.Count
                || Completed(UnitTypes.HIGH_TEMPLAR) + Count(UnitTypes.ARCHON) > 0
                || (Count(UnitTypes.ADEPT) >= 2 && !AdeptHarassMainTask.Task.Sent)
                || (EnemyCount(UnitTypes.ZERGLING) == 0 && EnemyCount(UnitTypes.ROACH) > 0
                || EnemyCount(UnitTypes.NYDUS_CANAL) > 0);
            if (HodorTask.Task.Stopped)
                HodorTask.Task.Clear();

            if (CounterRoaches)
                WorkerScoutTask.Task.StopAndClear((!gatewayExists || WorkerScoutTask.Task.BaseCircled()) && (!EarlyPool.Get().Detected || RoachRushDetected));
            else
                WorkerScoutTask.Task.StopAndClear((!gatewayExists || WorkerScoutTask.Task.BaseCircled()));

            if (WallIn.Wall.Count >= 5)
                HodorTask.Task.Target = WallIn.Wall[2].Pos;
            else
            {
                HodorTask.Task.Stopped = true;
                HodorTask.Task.Clear();
            }

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 3 
                    && TimingAttackTask.Task.Units.Count == 0
                    && (Count(UnitTypes.ADEPT) < 2 || AdeptHarassMainTask.Task.Sent))
                    agent.Order(Abilities.MOVE, Natural.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }

            if (!MassZerglings
                && !EarlyPool.Get().Detected
                && bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) >= 60
                && bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ROACH) == 0
                && bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.HYDRALISK) == 0)
            {
                MassZerglings = true;
                //TimingAttackTask.Task.Clear();
            }
            bot.DrawText("Zergling count: " + bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING));

            ObserverScoutTask.Task.Priority = 6;

            if (EarlyPool.Get().Detected)
            {
                bot.NexusAbilityManager.Stopped = Count(UnitTypes.ZEALOT) == 0;
                bot.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.ZEALOT].Ability);
            }
            else
            {
                bot.NexusAbilityManager.OnlyChronoPrioritizedUnits = Count(UnitTypes.CYBERNETICS_CORE) > 0 && !AdeptHarassMainTask.Task.Sent;
                bot.NexusAbilityManager.Stopped = Count(UnitTypes.ADEPT) + Count(UnitTypes.ZEALOT) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ARCHON) == 0
                   && Count(UnitTypes.CYBERNETICS_CORE) > 0;
                bot.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.ADEPT].Ability);
            }

            bot.DrawText("TimingAttackTask: " + TimingAttackTask.Task.Units.Count + "/" + TimingAttackTask.Task.RequiredSize);
            bot.DrawText("GroundDefense: " + DefenseTask.GroundDefenseTask.Units.Count);


            bot.DrawText("Roach rush: " + RoachRushDetected);
            bot.DrawText("InitialRoachCounterDone : " + InitialRoachCounterDone);
            if (RoachRushDetected)
            {
                if (InitialRoachCounterDone)
                    TimingAttackTask.Task.RequiredSize = 30;
                else
                    TimingAttackTask.Task.RequiredSize = 12;
            }
            else if (InitialAttackDone)
                TimingAttackTask.Task.RequiredSize = 30;
            else if (EarlyPool.Get().Detected
                && !Expanded.Get().Detected)
                TimingAttackTask.Task.RequiredSize = 24;
            else if (Completed(UnitTypes.ARCHON) < 3)
                TimingAttackTask.Task.RequiredSize = 24;
            else
                TimingAttackTask.Task.RequiredSize = 10;
            TimingAttackTask.Task.RetreatSize = 0;
            
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.ADEPT) >= 2;


            if (bot.Frame >= 22.4 * 60 * 2.75
                && bot.Frame <= 22.4 * 60 * 4.75
                && !EnemyAttackPerformed
                && (EarlyPool.Get().Detected || RoachRushDetected)
                && DefendNydus)
                IdleTask.Task.OverrideTarget = NydusPos;
            else if (Count(UnitTypes.NEXUS) >= 3
                || (Completed(UnitTypes.HIGH_TEMPLAR) + Count(UnitTypes.ARCHON) > 0)
                || (Count(UnitTypes.ADEPT) >= 2 && !AdeptHarassMainTask.Task.Sent))
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = NaturalDefensePos;

            ArchonMergeTask.Task.MergePos = OverrideDefenseTarget;

            if (RoachRushDetected
                && (Completed(UnitTypes.IMMORTAL) == 0 || Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.STALKER) < 6))
            {
                DefenseTask.GroundDefenseTask.BufferZone = 0;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;
            } else
            {
                DefenseTask.GroundDefenseTask.BufferZone = 5;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = EarlyPool.Get().Detected ? 50 : 30;
                DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;
            }

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
        }
    }
}
