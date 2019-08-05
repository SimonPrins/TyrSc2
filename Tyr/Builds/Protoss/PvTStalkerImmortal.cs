using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class PvTStalkerImmortal : Build
    {
        private bool BansheesDetected = false;

        private bool BuildTempest = false;
        private Point2D ScoutingPylonPos;
        private Point2D DefendDropsPos;
        private Base ScoutingPylonBase;
        private bool MedivacsSpotted = false;
        private int MedivacsSpottedFrame = 1000000;

        private bool LiftedTextSent = false;
        private int SendAirUnitsText = -1;

        private StutterController StutterController = new StutterController();
        private StutterForwardController StutterForwardController = new StutterForwardController();
        private DefenseSquadTask ReaperDefenseTask;

        public override string Name()
        {
            return "PvTStalkerImmortal";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();

            if (ReaperDefenseTask == null)
                ReaperDefenseTask = new DefenseSquadTask(Main, UnitTypes.STALKER) { MaxDefenders = 2, Priority = 10 };
            DefenseSquadTask.Enable(ReaperDefenseTask);

            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Tyr.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Tyr.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            TimedObserverTask.Enable();
            SaveWorkersTask.Enable();
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new FallBackController() { ReturnFire = true, MainDist = 40 });
            //MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new GravitonBeamController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.MISSILE_TURRET, 11));
            MicroControllers.Add(new AttackEnemyController(UnitTypes.PHOENIX, UnitTypes.BANSHEE, 15, true));
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(StutterController);
            MicroControllers.Add(StutterForwardController);

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            GetScoutingPylonPos();

            Set += ProtossBuildUtil.Pylons(() => (Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0 && Count(UnitTypes.STALKER) > 0) || tyr.Frame >= 22.4 * 60 * 3.5);
            Set += BaseCannons();
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuildList();
        }

        private void GetScoutingPylonPos()
        {
            Point2D main = Main.BaseLocation.Pos;
            Point2D natural = Natural.BaseLocation.Pos;
            float dist = 1000000;
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                float topDist = b.BaseLocation.Pos.Y;
                float leftDist = b.BaseLocation.Pos.X;
                float bottomDist = Tyr.Bot.GameInfo.StartRaw.MapSize.Y - b.BaseLocation.Pos.Y;
                float rightDist = Tyr.Bot.GameInfo.StartRaw.MapSize.X - b.BaseLocation.Pos.X;
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
                ScoutingPylonPos = new PotentialHelper(waypoint, 40).To(Tyr.Bot.MapAnalyzer.GetEnemyNatural().Pos).Get();
                ScoutingPylonBase = b;
            }
        }

        private BuildList BaseCannons()
        {
            BuildList result = new BuildList();

            result.If(() => BansheesDetected && Count(UnitTypes.PHOENIX) > 0);
            result.Building(UnitTypes.FORGE);
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
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
            
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
            }

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 70, () => Count(UnitTypes.NEXUS) >= 4 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.STALKER, 1);
            result.Train(UnitTypes.OBSERVER, 2, () => BansheesDetected || Count(UnitTypes.IMMORTAL) > 0);
            result.Train(UnitTypes.TEMPEST, 2);
            result.Train(UnitTypes.PHOENIX, 10);
            result.Upgrade(UpgradeType.WarpGate);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.PHOENIX) < 30 || Count(UnitTypes.NEXUS) >= 3);
            result.If(() => !BuildTempest || Count(UnitTypes.FLEET_BEACON) > 0 || Count(UnitTypes.STALKER) < 20);
            result.If(() => Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 15);
            result.If(() => Count(UnitTypes.NEXUS) >= 3 || Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 20);
            result.Train(UnitTypes.IMMORTAL, 5, () => !BansheesDetected);
            result.Train(UnitTypes.COLOSUS, 4, () => !BansheesDetected);
            result.Train(UnitTypes.IMMORTAL, () => !BansheesDetected);
            result.Train(UnitTypes.STALKER, 5);
            result.Train(UnitTypes.SENTRY, 1);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.STARGATE, () => BansheesDetected);
            result.Building(UnitTypes.FLEET_BEACON, () => Completed(UnitTypes.STARGATE) > 0 && BuildTempest);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.NEXUS);
            //result.Building(UnitTypes.PYLON, ScoutingPylonBase, ScoutingPylonPos, () => !BansheesDetected);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY, () => BansheesDetected);
            result.Building(UnitTypes.ROBOTICS_BAY, () => !BansheesDetected);
            result.Upgrade(UpgradeType.ExtendedThermalLance);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !BansheesDetected);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            BalanceGas();

            ReaperDefenseTask.Stopped = Completed(UnitTypes.STALKER) < 4 || TimingAttackTask.Task.AttackSent || Completed(UnitTypes.PHOENIX) > 0;
            if (ReaperDefenseTask.Stopped)
                ReaperDefenseTask.Clear();
            if (ReaperDefenseTask.Stopped)
                DefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Remove(UnitTypes.REAPER);
            else
                DefenseTask.GroundDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.REAPER);

            tyr.NexusAbilityManager.Stopped = Count(UnitTypes.STALKER) == 0;
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);

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

            SaveWorkersTask.Task.Stopped = tyr.Frame >= 22.4 * 60 * 7 || EnemyCount(UnitTypes.CYCLONE) == 0 || !Natural.UnderAttack;
            if (SaveWorkersTask.Task.Stopped)
                SaveWorkersTask.Task.Clear();

            if (tyr.Observation.Chat != null && SendAirUnitsText == -1)
            {
                foreach (ChatReceived chat in tyr.Observation.Chat)
                {
                    if (chat.PlayerId == tyr.PlayerId)
                        continue;
                    if (chat.Message.ToLower().Contains("air"))
                        SendAirUnitsText = tyr.Frame + 67;
                }
            }

            if (tyr.Frame == SendAirUnitsText)
                tyr.Chat("Yes, I am going for air units! How do you like my Phoenixes? :D");

            foreach (Agent agent in tyr.Units())
            {
                if (LiftedTextSent)
                    break;
                if (agent.Unit.UnitType != UnitTypes.PHOENIX)
                    continue;
                if (agent.PreviousUnit == null)
                    continue;
                if (agent.PreviousUnit.Energy - agent.Unit.Energy >= 45)
                {
                    LiftedTextSent = true;
                    tyr.Chat("I love your micro! Those flying Cyclones look especially scary!");
                }
            }
            
            

            foreach (ActionError error in tyr.Observation.ActionErrors)
                DebugUtil.WriteLine("Error with ability " + error.AbilityId + ": " + error.Result);
            

            if (!MedivacsSpotted && EnemyCount(UnitTypes.MEDIVAC) > 0)
            {
                MedivacsSpotted = true;
                MedivacsSpottedFrame = tyr.Frame;
            }

            DefenseTask.GroundDefenseTask.IncludePhoenixes = EnemyCount(UnitTypes.CYCLONE) > 0;

            if (MedivacsSpotted && tyr.Frame - MedivacsSpottedFrame <= 22.4 * 30)
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
            
            if (!BansheesDetected
                && tyr.Frame >= 22.4 * 60 * 6
                && tyr.Frame < 22.4 * 60 * 9.5
                && !MedivacsSpotted)
            {
                TimedObserverTask.Task.Target = ScoutingPylonPos;
                TimedObserverTask.Task.Priority = 11;
            }
            else
            {
                if (Tyr.Bot.Frame - tyr.EnemyBansheesManager.BansheeSeenFrame <= 22.4 * 10
                    && tyr.EnemyBansheesManager.BansheeLocation != null)
                    TimedObserverTask.Task.Target = tyr.EnemyBansheesManager.BansheeLocation;
                else if (Tyr.Bot.Frame - tyr.EnemyBansheesManager.LastHitFrame <= 22.4 * 20)
                    TimedObserverTask.Task.Target = tyr.EnemyBansheesManager.LastHitLocation;
                else
                    TimedObserverTask.Task.Target = SC2Util.To2D(tyr.MapAnalyzer.StartLocation);
                TimedObserverTask.Task.Priority = 3;
            }

            TimingAttackTask.Task.DefendOtherAgents = false;

            TimingAttackTask.Task.RequiredSize = 25;
            TimingAttackTask.Task.RetreatSize = 0;

            if (!BansheesDetected && TotalEnemyCount(UnitTypes.BANSHEE) > 0)
                BansheesDetected = true;
            if (!BansheesDetected
                && TotalEnemyCount(UnitTypes.STARPORT_TECH_LAB) + TotalEnemyCount(UnitTypes.STARPORT) > 0
                && tyr.Frame < 22.4 * 60 * 5)
                BansheesDetected = true;
            
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

            if (BansheesDetected && EnemyCount(UnitTypes.VIKING_ASSUALT) + EnemyCount(UnitTypes.VIKING_FIGHTER) >= 3)
                BuildTempest = true;

            tyr.TargetManager.SkipPlanetaries = true;
        }
    }
}
