using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class AntiMicro : Build
    {
        private KillTargetController KillCycloneController = new KillTargetController(UnitTypes.CYCLONE).ExcludeAttackerType(UnitTypes.PHOENIX);
        private KillTargetController KillBansheeController = new KillTargetController(UnitTypes.BANSHEE);
        private KillTargetController KillVikingController = new KillTargetController(UnitTypes.VIKING_FIGHTER);
        private KillTargetController KillMarauderController = new KillTargetController(UnitTypes.MARAUDER) { FocusDamaged = true, MaxDist = 6, MoveForwardWhenInRange = false };
        private AttackEnemyController PhoenixHuntAirController = new AttackEnemyController(UnitTypes.PHOENIX, new HashSet<uint> { UnitTypes.BANSHEE, UnitTypes.RAVEN, UnitTypes.MEDIVAC }, 60, true);
        private AttackEnemyController PhoenixHuntCycloneController = new AttackEnemyController(UnitTypes.PHOENIX, UnitTypes.CYCLONE, 60, true);
        private AttackEnemyController PhoenixHuntMarauderController = new AttackEnemyController(UnitTypes.PHOENIX, UnitTypes.MARAUDER, 60, true);
        private Point2D EnemyMain;
        private HashSet<ulong> RecordedProxies = new HashSet<ulong>();

        private Point2D RampDefensePoint;

        private bool MakeVoidray = false;
        private bool VoidrayDone = false;
        private WallInCreator WallIn = new WallInCreator();
        private List<Point2D> ScoutLocations = new List<Point2D>();
        private bool EnemyReaperClose = false;
        private bool DefendingStalkerClose = false;

        public bool HuntProxies = false;

        public bool ProxySuspected = false;
        public bool ProxyMarauderSuspected = false;

        public bool CounterProxyMarauder = true;
        private int SCVAttackFrame = 1000000;

        HuntProxyTask HuntProxyTask1 = new HuntProxyTask();
        HuntProxyTask HuntProxyTask2 = new HuntProxyTask();

        public override string Name()
        {
            return "AntiMicro";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1 || !HuntProxies)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            ForceFieldRampTask.Enable();
            //ObserverScoutTask.Enable();
            if (HuntProxies)
            {
                HuntProxyTask1.AddMidwayPoint = false;
                HuntProxyTask2.AddMidwayPoint = false;
                HuntProxyTask1.CloseBasesFirst = true;
                HuntProxyTask2.CloseBasesFirst = true;
                HuntProxyTask1.StartFrame = (int)(22.4 * 65);
                HuntProxyTask2.StartFrame = (int)(22.4 * 65);
                Task.Enable(HuntProxyTask1);
                //Task.Enable(HuntProxyTask2);
            }
            Task.Enable(PerUnitDefenseTask.GroundDefenseTask);
            PerUnitDefenseTask.GroundDefenseTask.Priority = 8;
            PerUnitDefenseTask.GroundDefenseTask.AllowedDefenderTypes.Add(UnitTypes.STALKER);
            PerUnitDefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.MARAUDER);
            PerUnitDefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.MARINE);
            PerUnitDefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.CYCLONE);
            PerUnitDefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.SIEGE_TANK);
            PerUnitDefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.SIEGE_TANK_SIEGED);
            PerUnitDefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.AUTO_TURRET);

            RampDefensePoint = new PotentialHelper(Bot.Main.MapAnalyzer.GetMainRamp(), 5).To(MainDefensePos).Get();
        }

        public override void OnStart(Bot bot)
        {
            DefenseTask.GroundDefenseTask.BeforeControllers.Add(new GravitonBeamController() { LiftReapers = true });
            DefenseTask.GroundDefenseTask.BeforeControllers.Add(KillMarauderController);
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new BlinkForwardController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new GravitonBeamController() { LiftMarauders = true });
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.MISSILE_TURRET, 11));
            MicroControllers.Add(new AttackEnemyController(UnitTypes.PHOENIX, new HashSet<uint> { UnitTypes.BANSHEE, UnitTypes.RAVEN }, 20, true));
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new StutterController() { Range = 3 });
            MicroControllers.Add(new KillTargetController(UnitTypes.SCV) { MaxDist = 4 }.ExcludeAttackerType(UnitTypes.PHOENIX));
            MicroControllers.Add(new KillTargetController((unit) => unit.BuffIds.Contains(5))); // Kill lifted units
            MicroControllers.Add(new AttackEnemyController(UnitTypes.PHOENIX, UnitTypes.CYCLONE, 15, true));
            //MicroControllers.Add(KillMarauderController);
            MicroControllers.Add(KillCycloneController);
            MicroControllers.Add(KillBansheeController);
            MicroControllers.Add(KillVikingController);
            MicroControllers.Add(new KillTargetController(UnitTypes.SCV, true).ExcludeAttackerType(UnitTypes.PHOENIX));
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(new StutterForwardController());
            MicroControllers.Add(PhoenixHuntAirController);
            MicroControllers.Add(PhoenixHuntCycloneController);
            MicroControllers.Add(PhoenixHuntMarauderController);
            MicroControllers.Add(new SoftLeashController(UnitTypes.PHOENIX, UnitTypes.STALKER, 5));
            TimingAttackTask.Task.BeforeControllers.Add(new SoftLeashController(UnitTypes.STALKER, UnitTypes.IMMORTAL, 7));

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            WallIn.CreateReaperWall(new List<uint> { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.CYBERNETICS_CORE });
            WallIn.ReserveSpace();

            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) >= 2 
                && Count(UnitTypes.CYBERNETICS_CORE) > 0
                && (!ProxyMarauderSuspected || Count(UnitTypes.SHIELD_BATTERY) >= 2)
                && (!ProxyMarauderSuspected || bot.Observation.Observation.PlayerCommon.FoodUsed >= 27));
            Set += Units();
            Set += MainBuildList();

            if (HuntProxies)
            {
                try
                {
                    DetermineProxyLocations();
                }
                catch (System.Exception e)
                {
                    Util.DebugUtil.WriteLine("Exception when generating map image: " + e.Message);
                }
            }
        }

            private void DetermineProxyLocations()
            {

            string[] scoutingLocations = Util.FileUtil.ReadScoutLocationFile();
            HashSet<string> existingLocations = new HashSet<string>();

            string[] debugLines = Util.FileUtil.ReadDebugFile();
            List<Point2D> fromCurrentStart = new List<Point2D>();
            List<Point2D> fromOtherStart = new List<Point2D>();
            string mapName = Bot.Main.GameInfo.MapName;

            string mapStartString = mapName + "(" + Bot.Main.MapAnalyzer.StartLocation.X + ", " + Bot.Main.MapAnalyzer.StartLocation.Y + "):";
            float dist;
            foreach (string line in scoutingLocations)
            {
                if (!line.StartsWith(mapName))
                    continue;
                existingLocations.Add(line);
            }

            /*
            foreach (string line in debugLines)
            {
                if (!line.StartsWith(mapName))
                    continue;
                string position = line.Substring(line.LastIndexOf("("));
                position = position.Replace(")", "").Replace("(", "");
                string[] pos = position.Split(',');
                Point2D point = new Point2D() { X = float.Parse(pos[0]), Y = float.Parse(pos[1]) };
                if (line.StartsWith(mapStartString))
                    fromCurrentStart.Add(point);
                else
                    fromOtherStart.Add(point);
                DrawMap(mapName + " from current", fromCurrentStart);
                DrawMap(mapName + " from other", fromOtherStart);

                Point2D basePos = null;
                dist = 1000000;
                foreach (Base b in Tyr.Bot.BaseManager.Bases)
                {
                    float newDist = SC2Util.DistanceSq(b.BaseLocation.Pos, point);
                    if (newDist > dist)
                        continue;
                    dist = newDist;
                    basePos = b.BaseLocation.Pos;
                }
                string locationString = line.Substring(0, line.LastIndexOf("(")) + "(" + basePos.X + "," + basePos.Y + ")";
                if (!existingLocations.Contains(locationString))
                {
                    existingLocations.Add(locationString);
                    FileUtil.WriteScoutLocation(locationString);
                }
            }
            */

            foreach (string line in scoutingLocations)
            {
                if (!line.StartsWith(mapStartString))
                    continue;

                string position = line.Substring(line.LastIndexOf("("));
                position = position.Replace(")", "").Replace("(", "");
                string[] pos = position.Split(',');
                Point2D point = new Point2D() { X = float.Parse(pos[0]), Y = float.Parse(pos[1]) };
                ScoutLocations.Add(point);
                DebugUtil.WriteLine("Found scout location: " + point);
            }
            Point2D furthestScoutLocation = null;
            dist = 0;
            foreach (Point2D scoutLocation in ScoutLocations)
            {
                float newDist = SC2Util.DistanceSq(scoutLocation, Main.BaseLocation.Pos);
                if (newDist > dist)
                {
                    dist = newDist;
                    furthestScoutLocation = scoutLocation;
                }
            }

            HuntProxyTask1.ScoutBases = new List<Point2D>();
            HuntProxyTask2.ScoutBases = new List<Point2D>();

            /*
            Point2D secondFarScoutLocation = null;
            dist = 1000 * 1000;
            foreach (Point2D scoutLocation in ScoutLocations)
            {
                if (scoutLocation == furthestScoutLocation)
                    continue;
                float newDist = SC2Util.DistanceSq(scoutLocation, furthestScoutLocation);
                if (newDist < dist)
                {
                    dist = newDist;
                    secondFarScoutLocation = scoutLocation;
                }
            }

            HuntProxyTask1.ScoutBases.Add(furthestScoutLocation);
            HuntProxyTask1.ScoutBases.Add(secondFarScoutLocation);
            foreach (Point2D scoutLocation in ScoutLocations)
                if (scoutLocation != furthestScoutLocation && scoutLocation != secondFarScoutLocation)
                    HuntProxyTask2.ScoutBases.Add(scoutLocation);
            */

            HuntProxyTask1.ScoutBases = ScoutLocations;
            /*

            Point2D secondFarScoutLocation = null;
            dist = 0;
            foreach (Point2D scoutLocation in ScoutLocations)
            {
                float newDist = SC2Util.DistanceSq(scoutLocation, furthestScoutLocation);
                if (newDist > dist)
                {
                    dist = newDist;
                    secondFarScoutLocation = scoutLocation;
                }
            }

            HashSet<Point2D> assignedLocations = new HashSet<Point2D>();
            assignedLocations.Add(furthestScoutLocation);
            assignedLocations.Add(secondFarScoutLocation);
            HuntProxyTask1.ScoutBases.Add(furthestScoutLocation);
            HuntProxyTask2.ScoutBases.Add(secondFarScoutLocation);
            while (assignedLocations.Count < ScoutLocations.Count)
            {
                dist = 1000 * 1000;
                Point2D nextLocation = null;
                foreach (Point2D scoutLocation in ScoutLocations)
                {
                    if (assignedLocations.Contains(scoutLocation))
                        continue;
                    float newDist = SC2Util.DistanceSq(scoutLocation, furthestScoutLocation);
                    if (newDist < dist)
                    {
                        dist = newDist;
                        nextLocation = scoutLocation;
                    }
                }
                assignedLocations.Add(nextLocation);
                HuntProxyTask1.ScoutBases.Add(nextLocation);

                if (assignedLocations.Count >= ScoutLocations.Count)
                    break;
                dist = 1000 * 1000;
                nextLocation = null;
                foreach (Point2D scoutLocation in ScoutLocations)
                {
                    if (assignedLocations.Contains(scoutLocation))
                        continue;
                    float newDist = SC2Util.DistanceSq(scoutLocation, secondFarScoutLocation);
                    if (newDist < dist)
                    {
                        dist = newDist;
                        nextLocation = scoutLocation;
                    }
                }
                assignedLocations.Add(nextLocation);
                HuntProxyTask2.ScoutBases.Add(nextLocation);
            }
            */

            HuntProxyTask1.KeepCycling = true;
            HuntProxyTask2.KeepCycling = true;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20, () => !ProxyMarauderSuspected || Count(UnitTypes.PROBE) < 18 || Minerals() >= 300);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.ZEALOT, 3, () => StrategyAnalysis.WorkerRush.Get().Detected);
            result.Train(UnitTypes.SENTRY, 1, () => FourRax.Get().Detected);
            result.If(() => !TimingAttackTask.Task.AttackSent || Count(UnitTypes.STALKER) < 10 || Count(UnitTypes.NEXUS) >= 2);
            result.If(() => !TimingAttackTask.Task.AttackSent || Count(UnitTypes.STALKER) < 10 || Completed(UnitTypes.TWILIGHT_COUNSEL) == 0 || UpgradeType.LookUp[UpgradeType.Blink].Started());
            result.Train(UnitTypes.OBSERVER, 1, () => Completed(UnitTypes.IMMORTAL) >= 2);
            result.Train(UnitTypes.STALKER, () => !DefendingStalkerClose && EnemyReaperClose);
            result.Train(UnitTypes.IMMORTAL, 1, () => Gas() >= 100 && !ProxyMarauderSuspected);
            result.Train(UnitTypes.IMMORTAL, 1, () => ProxyMarauderSuspected && Count(UnitTypes.SHIELD_BATTERY) >= 2 && Count(UnitTypes.STALKER) >= 3);
            result.Train(UnitTypes.OBSERVER, 2, () => Completed(UnitTypes.STALKER) + Completed(UnitTypes.IMMORTAL) >= 6 && Completed(UnitTypes.IMMORTAL) >= 2);
            result.Train(UnitTypes.VOID_RAY, 1, () => MakeVoidray && Gas() >= 100 && !VoidrayDone);
            result.Train(UnitTypes.STALKER, 1);
            result.Upgrade(UpgradeType.WarpGate, () => !ProxyMarauderSuspected || Count(UnitTypes.IMMORTAL) > 0);
            result.Train(UnitTypes.PHOENIX, 1, () => Gas() >= 100);
            result.Train(UnitTypes.STALKER, 3);
            result.Train(UnitTypes.TEMPEST, 1, () => Gas() >= 50);
            result.Train(UnitTypes.TEMPEST, 3, () => Gas() >= 100);
            result.Train(UnitTypes.PHOENIX, () => Gas() >= 100);
            result.Train(UnitTypes.IMMORTAL, 8, () => ProxyMarauderSuspected && Gas() >= 100 && (!ProxyMarauderSuspected || Count(UnitTypes.SHIELD_BATTERY) >= 2));
            result.Train(UnitTypes.STALKER, () => !ProxyMarauderSuspected || Count(UnitTypes.SHIELD_BATTERY) >= 2);
                
            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            if (WallIn.Wall.Count == 3)
            {
                System.Console.WriteLine("Found reaper wall, placing pylon and gateway.");
                System.Console.WriteLine("Pylon pos: " + WallIn.Wall[1].Pos);
                System.Console.WriteLine("Gateway pos: " + WallIn.Wall[0].Pos);

                result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
                result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            }
            else
            {
                System.Console.WriteLine("Did not find reaper, placing pylon and gateway.");
                result.Building(UnitTypes.PYLON, Main);
                result.If(() => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.GATEWAY, Main);
            }
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.ZEALOT) > 0 || !StrategyAnalysis.WorkerRush.Get().Detected);
            if (WallIn.Wall.Count == 3)
                result.Building(UnitTypes.CYBERNETICS_CORE, Main, WallIn.Wall[2].Pos, true);
            else
                result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.ZEALOT) > 0 || !StrategyAnalysis.WorkerRush.Get().Detected);
            result.Building(UnitTypes.PYLON, Main, MainDefensePos);
            result.Building(UnitTypes.GATEWAY, () => TotalEnemyCount(UnitTypes.MARAUDER) == 0 || Count(UnitTypes.IMMORTAL) >= 5);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => ProxyMarauderSuspected);
            result.Building(UnitTypes.STARGATE, () => !ProxyMarauderSuspected);
            result.Building(UnitTypes.SHIELD_BATTERY, Main, new PotentialHelper(Bot.Main.MapAnalyzer.GetMainRamp(), 7).To(MainDefensePos).Get(), 2, () => ProxyMarauderSuspected);
            //result.If(() => Count(UnitTypes.IMMORTAL) > 0);
            result.If(() => Count(UnitTypes.PHOENIX)  + Count(UnitTypes.IMMORTAL) >= 2);
            result.If(() => TimingAttackTask.Task.AttackSent);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos, () => !PerUnitDefenseTask.GroundDefenseTask.IsDefending() && !DefenseTask.GroundDefenseTask.IsNeeded() && !DefenseTask.AirDefenseTask.IsNeeded());
            //result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Upgrade(UpgradeType.Blink);
            result.Building(UnitTypes.FLEET_BEACON, () => Count(UnitTypes.PHOENIX) >= 6 && Count(UnitTypes.STALKER) >= 12);
            result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (WorkerScoutTask.Task.BaseCircled()
                && TotalEnemyCount(UnitTypes.BARRACKS) == 0)
                ProxySuspected = true;
            if (ProxySuspected
                && CounterProxyMarauder
                && EnemyCount(UnitTypes.REFINERY) == 1
                && Bot.Main.Frame < 22.4 * 60 * 2.5)
                ProxyMarauderSuspected = true;
            if (ProxyMarauderSuspected
                && Count(UnitTypes.NEXUS) < 2
                && Count(UnitTypes.IMMORTAL) == 0
                && Minerals() < 275)
                GasWorkerTask.WorkersPerGas = 2;
            else
                GasWorkerTask.WorkersPerGas = 3;

            if (Main.ResourceCenter != null)
                bot.DrawSphere(new Point() { X = RampDefensePoint.X, Y = RampDefensePoint.Y, Z = Main.ResourceCenter.Unit.Pos.Z});

            bot.NexusAbilityManager.Stopped = Completed(UnitTypes.CYBERNETICS_CORE) == 0;
            if (TotalEnemyCount(UnitTypes.MARAUDER) > 0)
            {
                bot.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.IMMORTAL].Ability);
            }
            if (ProxyMarauderSuspected)
            {
                bot.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.IMMORTAL].Ability);
                bot.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.STALKER].Ability);
                bot.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.VOID_RAY].Ability);
                bot.NexusAbilityManager.OnlyChronoPrioritizedUnits = Completed(UnitTypes.STALKER) < 4;
            }

            KillMarauderController.Stopped = !ProxyMarauderSuspected || Completed(UnitTypes.IMMORTAL) >= 2;

            DefendingStalkerClose = false;
            foreach (Agent agent in bot.Units())
                if (agent.Unit.UnitType == UnitTypes.STALKER
                    && agent.DistanceSq(Main.BaseLocation.Pos) <= 50 * 50)
                {
                    DefendingStalkerClose = true;
                    break;
                }
            EnemyReaperClose = false;
            foreach (Unit enemy in bot.Enemies())
                if (enemy.UnitType == UnitTypes.REAPER
                    && SC2Util.DistanceSq(enemy.Pos, Main.BaseLocation.Pos) <= 50 * 50)
                {
                    EnemyReaperClose = true;
                    break;
                }

            /*
            ArmyObserverTask.Task.Priority = 12;
            if (TotalEnemyCount(UnitTypes.MARAUDER) > 0
                && Tyr.Bot.Frame <= 4 * 60 * 22.4)
                MakeVoidray = true; 
            if (TotalEnemyCount(UnitTypes.BARRACKS_TECH_LAB) > 0
                && Tyr.Bot.Frame <= 3 * 60 * 22.4)
                MakeVoidray = true;
            if (Completed(UnitTypes.VOID_RAY) > 0)
                VoidrayDone = true;
            */
            PhoenixHuntAirController.Stopped = Completed(UnitTypes.PHOENIX) < 4;
            PhoenixHuntCycloneController.Stopped = Completed(UnitTypes.PHOENIX) < 4;
            PhoenixHuntMarauderController.Stopped = Completed(UnitTypes.PHOENIX) < 4;


            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY
                    && agent.Unit.UnitType != UnitTypes.ROBOTICS_FACILITY)
                    continue;

                agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }

            if (EnemyMain == null)
                EnemyMain = bot.TargetManager.PotentialEnemyStartLocations[0];
            foreach (Unit enemy in bot.Enemies())
            {
                if (RecordedProxies.Contains(enemy.Tag))
                    continue;
                if (enemy.UnitType != UnitTypes.BARRACKS
                    && enemy.UnitType != UnitTypes.FACTORY)
                    continue;
                if (enemy.IsFlying)
                    continue;
                if (Util.SC2Util.DistanceSq(enemy.Pos, EnemyMain) <= 40 * 40)
                    continue;
                RecordedProxies.Add(enemy.Tag);
                Util.FileUtil.Debug(bot.GameInfo.MapName + "(" + bot.MapAnalyzer.StartLocation.X + ", " + bot.MapAnalyzer.StartLocation.Y + "):(" + enemy.Pos.X + "," + enemy.Pos.Y + ")");
            }


            if (HuntProxies)
            {
                bool probeAttacked = false;
                foreach (Agent probe in HuntProxyTask1.Units)
                    if (probe.Unit.WeaponCooldown > 0)
                        probeAttacked = true;
                foreach (Agent probe in HuntProxyTask2.Units)
                    if (probe.Unit.WeaponCooldown > 0)
                        probeAttacked = true;
                if (SCVAttackFrame == 1000000
                    && probeAttacked)
                    SCVAttackFrame = bot.Frame;

                if (TotalEnemyCount(UnitTypes.REAPER) + TotalEnemyCount(UnitTypes.MARINE) + TotalEnemyCount(UnitTypes.CYCLONE) + TotalEnemyCount(UnitTypes.BANSHEE) + TotalEnemyCount(UnitTypes.MARAUDER) > 0
                    || bot.Frame >= 22.4 * 60 * 2
                    //|| (bot.Frame >= 22.4 * 60 * 1.25 && TotalEnemyCount(UnitTypes.SCV) + TotalEnemyCount(UnitTypes.BARRACKS) + TotalEnemyCount(UnitTypes.FACTORY) == 0)
                    || bot.Frame - SCVAttackFrame >= 22.4 * 20)
                {
                    HuntProxyTask1.StopAndClear(true);
                    HuntProxyTask2.StopAndClear(true);
                }
                else
                {
                    foreach (Unit enemy in bot.Enemies())
                    {
                        if (enemy.UnitType != UnitTypes.BARRACKS
                            && enemy.UnitType != UnitTypes.FACTORY
                            && enemy.UnitType != UnitTypes.SCV)
                            continue;
                        if (enemy.IsFlying)
                            continue;
                        if (Util.SC2Util.DistanceSq(enemy.Pos, EnemyMain) <= 40 * 40)
                            continue;
                        Point2D newLocation = new Point2D() { X = enemy.Pos.X, Y = enemy.Pos.Y };
                        ScoutLocations = new List<Point2D>() { newLocation };
                        HuntProxyTask1.ScoutBases = new List<Point2D>() { newLocation };
                        HuntProxyTask1.ClearNextRoundBases();
                        HuntProxyTask2.ScoutBases = new List<Point2D>() { newLocation };
                        HuntProxyTask2.ClearNextRoundBases();
                    }
                    Bot.Main.DrawText("ScoutLocations: " + ScoutLocations.Count);
                }
            }

            WorkerScoutTask.Task.StopAndClear(WorkerScoutTask.Task.BaseCircled());

            if (TotalEnemyCount(UnitTypes.BATTLECRUISER) > 0)
                KillVikingController.Stopped = true;

            bot.TargetManager.TargetAllBuildings = true;
            if (Completed(UnitTypes.IMMORTAL) >= 1)
                TimingAttackTask.Task.RequiredSize = 6;
            else if (Completed(UnitTypes.PHOENIX) + Completed(UnitTypes.VOID_RAY) + Completed(UnitTypes.IMMORTAL) >= 3)
                TimingAttackTask.Task.RequiredSize = 8;
            else if (Completed(UnitTypes.PHOENIX) + Completed(UnitTypes.IMMORTAL) >= 2)
                TimingAttackTask.Task.RequiredSize = 12;
            else
                TimingAttackTask.Task.RequiredSize = 20;      

            bot.buildingPlacer.BuildCompact = true;

            DefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.REAPER);
            DefenseTask.GroundDefenseTask.IncludePhoenixes = EnemyCount(UnitTypes.CYCLONE) > 0;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 
                TotalEnemyCount(UnitTypes.CYCLONE)
                + TotalEnemyCount(UnitTypes.BANSHEE) 
                + TotalEnemyCount(UnitTypes.MARAUDER) 
                + TotalEnemyCount(UnitTypes.BATTLECRUISER) > 0 ? 80 : 40;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = ProxyMarauderSuspected ? 20 : 30;
            DefenseTask.GroundDefenseTask.BufferZone = ProxyMarauderSuspected ? 0 : 5;
            PerUnitDefenseTask.GroundDefenseTask.MainDefenseRadius = ProxyMarauderSuspected ? 20 : 30;

            if (ProxyMarauderSuspected
                && Natural.ResourceCenter == null)
                IdleTask.Task.OverrideTarget = RampDefensePoint;
            else
                IdleTask.Task.OverrideTarget = null;
            IdleTask.Task.IdleRange = ProxyMarauderSuspected ? 3 : 5;
        }
        private void DrawMap(string name, List<Point2D> locations)
        {
            BoolGrid pathable = Bot.Main.MapAnalyzer.Pathable;
            if (!Bot.Debug)
                return;

            int width = Bot.Main.GameInfo.StartRaw.PathingGrid.Size.X;
            int height = Bot.Main.GameInfo.StartRaw.PathingGrid.Size.Y;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (pathable[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.White);
                    else
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
                }

            foreach (Managers.Base b in Bot.Main.BaseManager.Bases)
                bmp.SetPixel((int)b.BaseLocation.Pos.X, height - 1 - (int)b.BaseLocation.Pos.Y, System.Drawing.Color.Blue);

            foreach(Point2D location in locations)
                bmp.SetPixel((int)location.X, height - 1 - (int)location.Y, System.Drawing.Color.Red);

            bmp.Save(System.IO.Directory.GetCurrentDirectory() + "/data/" + name + ".png");
        }

    }
}
