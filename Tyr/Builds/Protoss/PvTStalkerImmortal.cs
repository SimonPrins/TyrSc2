using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class PvTStalkerImmortal : Build
    {
        private bool BansheesDetected = false;

        private bool BuildTempest = false;
        private Point2D ScoutingPylonPos;
        private Point2D DefendDropsPos;
        private Base ScoutingPylonBase;
        private bool InterceptMedivacs = false;
        private bool MedivacsSpotted = false;
        private int MedivacsSpottedFrame = 1000000;
        public bool SendScout = false;

        private StutterController StutterController = new StutterController();
        private StutterForwardController StutterForwardController = new StutterForwardController();
        private FearEnemyController FearTanksController = new FearEnemyController(new HashSet<uint>() { UnitTypes.STALKER, UnitTypes.IMMORTAL, UnitTypes.COLOSUS }, UnitTypes.SIEGE_TANK_SIEGED, 20) { CourageCount = 100 };

        private WallInCreator WallIn = new WallInCreator();

        public bool BuildReaperWall = true;
        public bool ProxyPylon = false;

        private bool FourRaxSuspected = false;

        public bool DelayObserver = false;
        public bool MassTanksDetected = false;

        public bool UseColosus = true;

        public override string Name()
        {
            return "PvTStalkerImmortalProbots";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();

            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1 || SendScout)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            TimedObserverTask.Enable();
            ObserverScoutTask.Enable();
            SaveWorkersTask.Enable();
            ForceFieldRampTask.Enable();
            if (ProxyPylon)
            {
                ProxyTask.Enable(new List<ProxyBuilding>() { new ProxyBuilding() { UnitType = UnitTypes.PYLON }});
                ProxyTask.Task.UseEnemyNatural = true;
                ProxyTask.Task.Stopped = true;
            }
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(FearTanksController);
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new StalkerController());
            //MicroControllers.Add(new FallBackController() { ReturnFire = true, MainDist = 40 });
            //MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new GravitonBeamController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.MISSILE_TURRET, 11));
            MicroControllers.Add(new AttackEnemyController(UnitTypes.PHOENIX, UnitTypes.BANSHEE, 15, true));
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.MARINE, 11));
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(StutterController);
            MicroControllers.Add(StutterForwardController);

            WallIn.CreateReaperWall(new List<uint> { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.CYBERNETICS_CORE});
            WallIn.ReserveSpace();

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            GetScoutingPylonPos();

            Set += ProtossBuildUtil.Pylons(() => 
            (Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0 && Count(UnitTypes.STALKER) > 0) && Count(UnitTypes.PYLON) < 3 || bot.Frame >= 22.4 * 60 * 3.5);
            Set += BaseCannons();
            Set += ExpandBuildings();
            Set += ExtraAssimilators();
            Set += Units();
            Set += MainBuildList();
        }

        private void GetScoutingPylonPos()
        {
            Point2D main = Main.BaseLocation.Pos;
            Point2D natural = Natural.BaseLocation.Pos;
            float dist = 1000000;
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                float topDist = b.BaseLocation.Pos.Y;
                float leftDist = b.BaseLocation.Pos.X;
                float bottomDist = Bot.Main.GameInfo.StartRaw.MapSize.Y - b.BaseLocation.Pos.Y;
                float rightDist = Bot.Main.GameInfo.StartRaw.MapSize.X - b.BaseLocation.Pos.X;
                float edgeDist = System.Math.Min(System.Math.Min(topDist, leftDist), System.Math.Min(bottomDist, rightDist));
                if (edgeDist >= 50)
                    continue;
                float mainDist = SC2Util.DistanceSq(b.BaseLocation.Pos, main);
                if (mainDist > dist)
                    continue;
                if (mainDist <= 2 * 2)
                    continue;
                float naturalDist = SC2Util.DistanceSq(b.BaseLocation.Pos, natural);
                if (mainDist > naturalDist)
                    continue;
                dist = mainDist;
                DefendDropsPos = new PotentialHelper(main, 15).To(b.BaseLocation.Pos).Get();
                Point2D waypoint = new PotentialHelper(main, 50).To(b.BaseLocation.Pos).Get();
                ScoutingPylonPos = new PotentialHelper(waypoint, 40).To(Bot.Main.MapAnalyzer.GetEnemyNatural().Pos).Get();
                ScoutingPylonBase = b;
            }
        }

        private BuildList BaseCannons()
        {
            BuildList result = new BuildList();

            result.If(() => BansheesDetected && Count(UnitTypes.PHOENIX) > 0);
            result.Building(UnitTypes.FORGE);
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.PHOTON_CANNON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1);
            }

            return result;
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => Count(UnitTypes.NEXUS) >= 3);
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 450);
            }

            return result;
        }

        private BuildList ExtraAssimilators()
        {
            BuildList result = new BuildList();

            result.If(() => Minerals() >= 600 && Completed(UnitTypes.NEXUS) >= 3 && Gas() < 100 && Bot.Main.Frame % 10 == 0);
            result.Building(UnitTypes.ASSIMILATOR, 6);
            result.If(() => Minerals() >= 800);
            result.Building(UnitTypes.ASSIMILATOR, 7);
            result.If(() => Minerals() >= 900);
            result.Building(UnitTypes.ASSIMILATOR, 8);
            result.If(() => Minerals() >= 1000);
            result.Building(UnitTypes.ASSIMILATOR, 10);

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 70, () => Count(UnitTypes.NEXUS) >= 4 && Count(UnitTypes.PYLON) > 2);
            result.If(() => !FourRaxSuspected || Completed(UnitTypes.STALKER) < 15 || Count(UnitTypes.NEXUS) >= 2);
            //result.If(() => FourRaxSuspected || Count(UnitTypes.SHIELD_BATTERY) >= 2 || Tyr.Bot.Frame < 22.4 * 60 * 6 || Tyr.Bot.Frame >= 22.4 * 60 * 7 || Completed(UnitTypes.STALKER) < 8);
            result.Train(UnitTypes.SENTRY, 1, () => FourRaxSuspected);
            result.Train(UnitTypes.STALKER, () => FourRaxSuspected);
            result.Train(UnitTypes.OBSERVER, 1, () => !DelayObserver);
            result.Train(UnitTypes.STALKER, 1);
            result.Train(UnitTypes.VOID_RAY, 10, () => FourRaxSuspected && Completed(UnitTypes.STALKER) >= 20);
            result.Train(UnitTypes.OBSERVER, 2, () => BansheesDetected || (Count(UnitTypes.IMMORTAL) > 0 && TotalEnemyCount(UnitTypes.CYCLONE) > 0) || Bot.Main.Frame >= 22.4 * 60 * 8);
            //result.Train(UnitTypes.MOTHERSHIP, 1, () => Completed(UnitTypes.FLEET_BEACON) > 0);
            result.Train(UnitTypes.TEMPEST, 10, () => Gas() >= 150 || Count(UnitTypes.NEXUS) >= 5);
            result.Train(UnitTypes.PHOENIX, 10, () => BansheesDetected);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.STALKER) > 0 && (Count(UnitTypes.ROBOTICS_FACILITY) > 0 || FourRaxSuspected));
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.PHOENIX) < 30 || Count(UnitTypes.NEXUS) >= 3);
            result.If(() => !BuildTempest || Count(UnitTypes.FLEET_BEACON) > 0 || Count(UnitTypes.STALKER) < 25);
            result.If(() => Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 15);
            result.If(() => Count(UnitTypes.NEXUS) >= 3 || Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 20);
            result.If(() => Count(UnitTypes.VOID_RAY) > 0 || Count(UnitTypes.STALKER) < 25 || !FourRaxSuspected);
            result.Train(UnitTypes.IMMORTAL, 3, () => !BansheesDetected);
            result.Train(UnitTypes.OBSERVER, 1, () => DelayObserver);
            if (UseColosus)
            {
                result.Upgrade(UpgradeType.ExtendedThermalLance, () => !BansheesDetected && TotalEnemyCount(UnitTypes.THOR) + TotalEnemyCount(UnitTypes.THOR_SINGLE_TARGET) == 0 && TotalEnemyCount(UnitTypes.CYCLONE) < 8 && TotalEnemyCount(UnitTypes.VIKING_FIGHTER) == 0 && Count(UnitTypes.COLOSUS) > 0);
                result.Train(UnitTypes.COLOSUS, 4, () => !BansheesDetected && TotalEnemyCount(UnitTypes.THOR) + TotalEnemyCount(UnitTypes.THOR_SINGLE_TARGET) == 0 && TotalEnemyCount(UnitTypes.CYCLONE) < 8 && TotalEnemyCount(UnitTypes.VIKING_FIGHTER) == 0);
            }
            //result.Train(UnitTypes.DISRUPTOR, 4, () => !BansheesDetected && TotalEnemyCount(UnitTypes.THOR) + TotalEnemyCount(UnitTypes.THOR_SINGLE_TARGET) == 0 && TotalEnemyCount(UnitTypes.CYCLONE) < 8);
            result.Train(UnitTypes.IMMORTAL, () => TotalEnemyCount(UnitTypes.THOR) + TotalEnemyCount(UnitTypes.THOR_SINGLE_TARGET) > 0  || (!BansheesDetected && Completed(UnitTypes.ROBOTICS_BAY) > 0) || (!BansheesDetected && !UseColosus));
            result.Train(UnitTypes.STALKER, 5);
            result.Train(UnitTypes.SENTRY, 1);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            if (BuildReaperWall)
            {
                result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
                result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            } else
            {
                result.Building(UnitTypes.PYLON, Main);
                result.If(() => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.GATEWAY, Main);
            }
            result.Building(UnitTypes.ASSIMILATOR);
            if (BuildReaperWall)
                result.Building(UnitTypes.CYBERNETICS_CORE, Main, WallIn.Wall[2].Pos, true);
            else
                result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.PYLON, Main, () => FourRaxSuspected);
            result.Building(UnitTypes.ASSIMILATOR, () => FourRaxSuspected);
            result.Building(UnitTypes.GATEWAY, 2, () => FourRaxSuspected);
            result.If(() => !FourRaxSuspected || Completed(UnitTypes.STALKER) >= 15);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.STARGATE, () => BansheesDetected || (Count(UnitTypes.STALKER) >= 20 || FourRaxSuspected));
            result.Building(UnitTypes.FLEET_BEACON, () => Completed(UnitTypes.STARGATE) > 0 && BuildTempest);
            result.Building(UnitTypes.GATEWAY, () => Completed(UnitTypes.IMMORTAL) > 0 || Bot.Main.Frame >= 22.4 * 60 * 4);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            if(UseColosus)
                result.Building(UnitTypes.ROBOTICS_BAY, () => !BansheesDetected && Completed(UnitTypes.IMMORTAL) >= 2);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.IMMORTAL) >= 1 || Count(UnitTypes.STALKER) >= 7);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.STARGATE, () => !BansheesDetected && Count(UnitTypes.IMMORTAL) + Count(UnitTypes.STALKER) + Count(UnitTypes.COLOSUS) >= 25);
            result.Building(UnitTypes.FORGE, 2, () => Count(UnitTypes.COLOSUS) > 0 || Count(UnitTypes.IMMORTAL) >= 4 || Count(UnitTypes.PHOENIX) > 0);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.STARGATE) > 0);
            //result.Building(UnitTypes.PYLON, ScoutingPylonBase, ScoutingPylonPos, () => !BansheesDetected);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.DISRUPTOR) > 0 || Count(UnitTypes.STALKER) >= 12);
            result.Building(UnitTypes.GATEWAY, () => BansheesDetected);
            //result.If(() => Tyr.Bot.Frame >= 22.4 * 60 * 7.5);
            result.Building(UnitTypes.TWILIGHT_COUNSEL, () => BansheesDetected || TotalEnemyCount(UnitTypes.CYCLONE) > 0 || Completed(UnitTypes.FORGE) >= 2);
            result.Upgrade(UpgradeType.Blink);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR, () => Minerals() >= 600);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.COLOSUS) >= 25);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.STARGATE, 2);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            //result.Building(UnitTypes.ROBOTICS_FACILITY, () => !BansheesDetected && TotalEnemyCount(UnitTypes.THOR) + TotalEnemyCount(UnitTypes.THOR_SINGLE_TARGET) == 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => Completed(UnitTypes.STALKER) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.COLOSUS) + Completed(UnitTypes.PHOENIX) + 2 * Completed(UnitTypes.TEMPEST) >= 30);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 3);
            result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            BalanceGas();

            MassTanksDetected = MassTank.Get().Detected;
            FearTanksController.Stopped = !MassTanksDetected;

            if (!FourRaxSuspected
                && bot.Frame <= 22.4 * 60 * 3
                && SendScout)
            {
                if (EnemyCount(UnitTypes.BARRACKS) >= 3)
                    FourRaxSuspected = true;
                if (bot.Frame >= 22.4 * 90
                    && EnemyCount(UnitTypes.BARRACKS) >= 2
                    && EnemyCount(UnitTypes.REFINERY) == 0
                    && EnemyCount(UnitTypes.COMMAND_CENTER) < 2)
                    FourRaxSuspected = true;
            }

            ForceFieldRampTask.Task.Stopped = !FourRaxSuspected || Completed(UnitTypes.STALKER) >= 15;
            if (ForceFieldRampTask.Task.Stopped)
                ForceFieldRampTask.Task.Clear();

            if (ProxyPylon && ProxyTask.Task.Stopped && bot.Frame >= 400)
                ProxyTask.Task.Stopped = false;

            if (ProxyPylon && ProxyTask.Task.UnitCounts.ContainsKey(UnitTypes.PYLON) && ProxyTask.Task.UnitCounts[UnitTypes.PYLON] > 0)
            {
                ProxyPylon = false;
                ProxyTask.Task.Stopped = true;
                ProxyTask.Task.Clear();
            }

            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }
            

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 3 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }

            bot.NexusAbilityManager.Stopped = Completed(UnitTypes.PYLON) == 0;
            bot.NexusAbilityManager.PriotitizedAbilities.Add(1006);

            if (TotalEnemyCount(UnitTypes.BANSHEE) > 0)
            {
                StutterForwardController.Stopped = false;
                StutterController.Stopped = true;
            }
            else
            {
                StutterForwardController.Stopped = true;
                StutterController.Stopped = false;
            }

            SaveWorkersTask.Task.Stopped = bot.Frame >= 22.4 * 60 * 7 || EnemyCount(UnitTypes.CYCLONE) == 0 || !Natural.UnderAttack;
            if (SaveWorkersTask.Task.Stopped)
                SaveWorkersTask.Task.Clear();


            if (!MedivacsSpotted && EnemyCount(UnitTypes.MEDIVAC) > 0)
            {
                MedivacsSpotted = true;
                MedivacsSpottedFrame = bot.Frame;
            }

            DefenseTask.GroundDefenseTask.IncludePhoenixes = EnemyCount(UnitTypes.CYCLONE) > 0;

            if (MedivacsSpotted && bot.Frame - MedivacsSpottedFrame <= 22.4 * 30 && InterceptMedivacs)
            {
                IdleTask.Task.OverrideTarget = DefendDropsPos;
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.MEDIVAC);
            }
            else
            {
                IdleTask.Task.OverrideTarget = null;
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Remove(UnitTypes.MEDIVAC);
            }

            WorkerTask.Task.EvacuateThreatenedBases = true;
            
            if (BansheesDetected)
            {
                ObserverScoutTask.Task.Stopped = true;
                ObserverScoutTask.Task.Clear();
            }

            if (!BansheesDetected
                && bot.Frame >= 22.4 * 60 * 6
                && bot.Frame < 22.4 * 60 * 9.5
                && !MedivacsSpotted
                && InterceptMedivacs)
            {
                TimedObserverTask.Task.Target = ScoutingPylonPos;
                TimedObserverTask.Task.Priority = 11;
            }
            else
            {
                if (Bot.Main.Frame - bot.EnemyBansheesManager.BansheeSeenFrame <= 22.4 * 10
                    && bot.EnemyBansheesManager.BansheeLocation != null)
                    TimedObserverTask.Task.Target = bot.EnemyBansheesManager.BansheeLocation;
                else if (Bot.Main.Frame - bot.EnemyBansheesManager.LastHitFrame <= 22.4 * 20)
                    TimedObserverTask.Task.Target = bot.EnemyBansheesManager.LastHitLocation;
                else
                    TimedObserverTask.Task.Target = SC2Util.To2D(bot.MapAnalyzer.StartLocation);
                TimedObserverTask.Task.Priority = 3;
            }

            TimingAttackTask.Task.DefendOtherAgents = false;

            if (FourRaxSuspected)
            {
                TimingAttackTask.Task.RetreatSize = 0;
                TimingAttackTask.Task.RequiredSize = 20;
            }
            else if (TotalEnemyCount(UnitTypes.CYCLONE) > 0 && TotalEnemyCount(UnitTypes.BANSHEE) > 0)
            {
                TimingAttackTask.Task.RetreatSize = 0;
                TimingAttackTask.Task.RequiredSize = 25;
            }
            else
            {
                TimingAttackTask.Task.RetreatSize = 15;
                TimingAttackTask.Task.RequiredSize = 35;
            }

            if (!BansheesDetected && TotalEnemyCount(UnitTypes.BANSHEE) > 0)
                BansheesDetected = true;
            if (!BansheesDetected
                && TotalEnemyCount(UnitTypes.STARPORT_TECH_LAB) + TotalEnemyCount(UnitTypes.STARPORT) > 0
                && bot.Frame < 22.4 * 60 * 5)
                BansheesDetected = true;
            
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

            //if (BansheesDetected && EnemyCount(UnitTypes.VIKING_ASSUALT) + EnemyCount(UnitTypes.VIKING_FIGHTER) >= 3)
                BuildTempest = true;

            bot.TargetManager.SkipPlanetaries = true;
        }
    }
}
