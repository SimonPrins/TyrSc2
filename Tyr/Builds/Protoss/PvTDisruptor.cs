using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class PvTDisruptor : Build
    {
        private FallBackController FallBackController = new FallBackController() { ReturnFire = true,  MainDist = 40 };
        private Point2D OverrideDefenseTarget;
        private Point2D OverrideMainDefenseTarget;
        private int DesiredImmortals = 20;

        private bool BattlecruisersDetected = false;
        private bool FourRaxSuspected = false;
        private bool CloakedBanshee = false;

        private WaitForDetectionController WaitForDetectionController = new WaitForDetectionController();

        public override string Name()
        {
            return "PvTDisruptor";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            ShieldRegenTask.Enable();
            ArmyObserverTask.Enable();
            ObserverScoutTask.Enable();
            if (Bot.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ArchonMergeTask.Enable();
            ForceFieldRampTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);
            OverrideMainDefenseTarget = new PotentialHelper(tyr.MapAnalyzer.GetMainRamp(), 6).
                To(tyr.MapAnalyzer.StartLocation)
                .Get();
            
            MicroControllers.Add(FallBackController);
            MicroControllers.Add(WaitForDetectionController);
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.MISSILE_TURRET, 10));
            MicroControllers.Add(new AttackEnemyController(UnitTypes.PHOENIX, UnitTypes.BANSHEE, 15, true));
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new AdvanceController());

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += EmergencyGateways();
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuild();
        }

        private BuildList CannonDefense()
        {
            BuildList result = new BuildList();

            result.If(() => Count(UnitTypes.STALKER) >= 6 && (Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0 || Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 3));
            result.Building(UnitTypes.FORGE);
            result.If(() => Count(UnitTypes.STALKER) >= 8);
            foreach (Base b in Bot.Bot.BaseManager.Bases)
            {
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.PHOTON_CANNON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1);
            }
            result.Building(UnitTypes.PHOTON_CANNON, Main, 2, () => Count(UnitTypes.STALKER) >= 10 && Count(UnitTypes.OBSERVER) > 0);
            result.Building(UnitTypes.PHOTON_CANNON, Main, () => Count(UnitTypes.STALKER) >= 15 && Count(UnitTypes.OBSERVER) > 0);

            return result;
        }

        private BuildList EmergencyGateways()
        {
            BuildList result = new BuildList();

            result.If(() => { return EarlyPool.Get().Detected && !Expanded.Get().Detected; });
            result.Building(UnitTypes.PYLON, Main);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.GATEWAY, Main);
            result.If(() => Count(UnitTypes.ZEALOT) >= 4);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.If(() => Count(UnitTypes.ZEALOT) >= 6);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => Count(UnitTypes.ZEALOT) >= 8);
            result.Building(UnitTypes.GATEWAY, Main);

            return result;
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => { return !EarlyPool.Get().Detected; });
            foreach (Base b in Bot.Bot.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PHOENIX, () => TotalEnemyCount(UnitTypes.BANSHEE) > 0);
            result.Train(UnitTypes.TEMPEST);
            result.Train(UnitTypes.SENTRY, 1, () => FourRaxSuspected && TotalEnemyCount(UnitTypes.BARRACKS) == 0);
            result.Train(UnitTypes.STALKER, 2, () => FourRaxSuspected);
            result.Train(UnitTypes.SENTRY, 1, () => FourRaxSuspected);
            result.Train(UnitTypes.STALKER, () => TotalEnemyCount(UnitTypes.BANSHEE) >= 5);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.OBSERVER, 2, () => FourRaxSuspected);
            result.Train(UnitTypes.OBSERVER, 3, () => TotalEnemyCount(UnitTypes.BANSHEE) >= 3);
            result.Train(UnitTypes.IMMORTAL, 3, () => TotalEnemyCount(UnitTypes.BANSHEE) == 0);
            result.Train(UnitTypes.OBSERVER, 2);
            result.Train(UnitTypes.IMMORTAL, 6, () => TotalEnemyCount(UnitTypes.BANSHEE) == 0);
            result.Train(UnitTypes.DISRUPTOR, 4, () => 
                TotalEnemyCount(UnitTypes.BATTLECRUISER) == 0 
                && (TotalEnemyCount(UnitTypes.WIDOW_MINE) < 10 || TotalEnemyCount(UnitTypes.MARAUDER) + TotalEnemyCount(UnitTypes.MARINE) > TotalEnemyCount(UnitTypes.WIDOW_MINE) * 2) );
            result.Train(UnitTypes.IMMORTAL, () => 
                Count(UnitTypes.IMMORTAL) < DesiredImmortals
                && TotalEnemyCount(UnitTypes.BANSHEE) == 0
                && (TotalEnemyCount(UnitTypes.WIDOW_MINE) < 10 || TotalEnemyCount(UnitTypes.MARAUDER) + TotalEnemyCount(UnitTypes.MARINE) > TotalEnemyCount(UnitTypes.WIDOW_MINE) * 2));
            result.Train(UnitTypes.STALKER, () => TotalEnemyCount(UnitTypes.BANSHEE) < 5);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.NEXUS, () => !FourRaxSuspected || Completed(UnitTypes.STALKER) >= 20);
            result.Building(UnitTypes.GATEWAY, Main, () => FourRaxSuspected);
            result.Building(UnitTypes.ASSIMILATOR, () => FourRaxSuspected);
            result.Building(UnitTypes.SHIELD_BATTERY, Main, MainDefensePos, 2, () => FourRaxSuspected && Completed(UnitTypes.CYBERNETICS_CORE) > 0);
            result.Building(UnitTypes.GATEWAY, Main, () => Count(UnitTypes.STALKER) > 0 && (!FourRaxSuspected || Completed(UnitTypes.STALKER) >= 2));
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.STALKER) + Count(UnitTypes.SENTRY) + Count(UnitTypes.ADEPT) + Count(UnitTypes.ZEALOT) >= 3);
            result.Building(UnitTypes.PYLON, Natural, () => Count(UnitTypes.CYBERNETICS_CORE) > 0 && Natural.ResourceCenter != null);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => TotalEnemyCount(UnitTypes.BANSHEE) > 0);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => FourRaxSuspected && Count(UnitTypes.STALKER) + Count(UnitTypes.ADEPT) >= 10);
            result.Building(UnitTypes.ASSIMILATOR, () => Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0 && Count(UnitTypes.OBSERVER) > 0);
            result.Building(UnitTypes.STARGATE, () => TotalEnemyCount(UnitTypes.BANSHEE) > 0 && Count(UnitTypes.OBSERVER) > 0);
            result.Building(UnitTypes.GATEWAY, Natural, () => FourRaxSuspected && Completed(Natural, UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.ASSIMILATOR, () => !FourRaxSuspected || Minerals() >= 200);
            result.Building(UnitTypes.ASSIMILATOR, () => FourRaxSuspected && Minerals() >= 600);
            result.Building(UnitTypes.ASSIMILATOR, () => FourRaxSuspected && Minerals() >= 800);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => TotalEnemyCount(UnitTypes.BANSHEE) == 0 && !FourRaxSuspected);
            result.Building(UnitTypes.GATEWAY, Main, () => !FourRaxSuspected);
            result.If(() => !FourRaxSuspected || Count(UnitTypes.STALKER) + Count(UnitTypes.ADEPT) >= 15);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Minerals() >= 400);
            result.If(() => Count(UnitTypes.IMMORTAL) >= 2 && Count(UnitTypes.IMMORTAL) + Count(UnitTypes.STALKER) >= 15);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) >= 3);
            result.Building(UnitTypes.TWILIGHT_COUNSEL, () => Count(UnitTypes.PHOENIX) > 0 || TotalEnemyCount(UnitTypes.BANSHEE) == 0);
            result.Building(UnitTypes.FORGE, () => Count(UnitTypes.PHOENIX) > 0 || TotalEnemyCount(UnitTypes.BANSHEE) == 0);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.FORGE, () => Count(UnitTypes.PHOENIX) > 0 || TotalEnemyCount(UnitTypes.BANSHEE) == 0);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.PYLON);
            result.If(() => TotalEnemyCount(UnitTypes.BANSHEE) == 0 || (Completed(UnitTypes.STALKER) >= 10 && Completed(UnitTypes.PHOTON_CANNON) >= 2));
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !BattlecruisersDetected && Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) == 0);
            result.Building(UnitTypes.ASSIMILATOR, 4, () => Minerals() >= 700 && Gas() < 200);
            result.If(() => Count(UnitTypes.IMMORTAL) >= 3 && Count(UnitTypes.STALKER) >= 10); 
            result.Building(UnitTypes.ROBOTICS_BAY, () => !BattlecruisersDetected && Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) == 0);
            result.Building(UnitTypes.STARGATE, () => BattlecruisersDetected || Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0);
            result.Building(UnitTypes.FLEET_BEACON, () => Completed(UnitTypes.STARGATE) > 0);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.ADEPT) >= 15);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Completed(UnitTypes.STALKER) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.DISRUPTOR) + Completed(UnitTypes.TEMPEST) + Count(UnitTypes.ADEPT) >= 40 || Minerals() >= 650);
            result.Building(UnitTypes.NEXUS);

            return result;
        }
        
        public override void OnFrame(Bot tyr)
        {
            if (FourRaxSuspected && Gas() >= 200)
                GasWorkerTask.WorkersPerGas = 1;
            else if (FourRaxSuspected && Minerals() <= 200 && Count(UnitTypes.STALKER) < 10)
                GasWorkerTask.WorkersPerGas = 2;
            else
                BalanceGas();


            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            CloakedBanshee = TotalEnemyCount(UnitTypes.BANSHEE) > 0;

            WaitForDetectionController.Stopped = !CloakedBanshee;

            tyr.DrawText("CloakedBanshee: " + CloakedBanshee);

            WorkerScoutTask.Task.StartFrame = 600;
            ObserverScoutTask.Task.Priority = 6;

            /*
            if (TotalEnemyCount(UnitTypes.BANSHEE) > 0 || FourRaxSuspected)
                ForwardProbeTask.Task.Stopped = true;
            else if (Completed(UnitTypes.IMMORTAL) >= 2)
                ForwardProbeTask.Task.Stopped = false;
            else if (Completed(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 4)
                ForwardProbeTask.Task.Stopped = true;
            
            if (ForwardProbeTask.Task.Stopped)
                ForwardProbeTask.Task.Clear();
                */

            tyr.NexusAbilityManager.Stopped = Count(UnitTypes.STALKER) == 0 && tyr.Frame >= 120 * 22.4;
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.BATTLECRUISER) > 0)
                BattlecruisersDetected = true;

            if (FourRax.Get().Detected)
                FourRaxSuspected = true;
            if (ProxyDetected.Get().Detected)
                FourRaxSuspected = true;
            if (WorkerScoutTask.Task.BaseCircled()
                && tyr.Frame < 22.4 * 60 * 2
                && TotalEnemyCount(UnitTypes.BARRACKS) == 0)
                FourRaxSuspected = true;
            if (!FourRaxSuspected)
            {
                Point2D enemyRamp = tyr.MapAnalyzer.GetEnemyRamp();
                int enemyBarrackWallCount = 0;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (enemyRamp == null)
                        break;
                    if (enemy.UnitType == UnitTypes.BARRACKS && SC2Util.DistanceSq(enemy.Pos, enemyRamp) <= 5.5 * 5.5)
                        enemyBarrackWallCount++;
                }
                if (enemyBarrackWallCount >= 2)
                {
                    WorkerScoutTask.Task.Stopped = true;
                    WorkerScoutTask.Task.Clear();
                    FourRaxSuspected = true;
                }
            }

            if (FourRaxSuspected)
            {
                tyr.TargetManager.TargetAllBuildings = true;
                FallBackController.Stopped = true;
            }
            tyr.TargetManager.SkipPlanetaries = true;

            ForceFieldRampTask.Task.Stopped = !FourRaxSuspected || Completed(UnitTypes.STALKER) >= 18;
            if (ForceFieldRampTask.Task.Stopped)
                ForceFieldRampTask.Task.Clear();

            if (FourRaxSuspected && Completed(UnitTypes.STALKER) < 10)
            {
                foreach (Agent agent in tyr.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.NEXUS)
                        continue;
                    if (agent.Unit.BuildProgress >= 0.99)
                        continue;
                    agent.Order(Abilities.CANCEL);
                }
            }


            tyr.DrawText("Defending units: " + DefenseTask.GroundDefenseTask.Units.Count);
            tyr.DrawText("Is defending: " + DefenseTask.GroundDefenseTask.IsDefending());

            if (FourRaxSuspected)
                TimingAttackTask.Task.RequiredSize = 18;
            else
            {
                TimingAttackTask.Task.RequiredSize = 4;
                TimingAttackTask.Task.RetreatSize = 0;
            }
            
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = Completed(UnitTypes.ZEALOT) >= 5;

            
            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 3 || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0)
                TimingAttackTask.Task.DefendOtherAgents = false;

            if (Count(UnitTypes.NEXUS) <= 1)
                IdleTask.Task.OverrideTarget = OverrideMainDefenseTarget;
            else if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            if (FourRaxSuspected && Completed(UnitTypes.STALKER) < 18)
            {
                DefenseTask.GroundDefenseTask.BufferZone = 0;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
                DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;
            }
            else
            {
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;
            }

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

            if (EarlyPool.Get().Detected && !Expanded.Get().Detected && Completed(UnitTypes.ZEALOT) < 2)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.NEXUS
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (Count(UnitTypes.PROBE) >= 24
                && Count(UnitTypes.NEXUS) < 2
                && Minerals() < 450)
                return;
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < Math.Min(70, 20 * Completed(UnitTypes.NEXUS))
                && (Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.PROBE) < 18 + 2 * Completed(UnitTypes.ASSIMILATOR)))
            {
                if (Count(UnitTypes.PROBE) >= 16
                    && Count(UnitTypes.STALKER) < 4
                    && FourRaxSuspected)
                    return;
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_BAY)
            {
                if (Minerals() >= 150
                    && Gas() >= 150
                    && !Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(50)
                    && Count(UnitTypes.COLOSUS) > 0)
                {
                    agent.Order(1097);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {

                if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(87)
                     && Minerals() >= 150
                     && Gas() >= 150
                    && Completed(UnitTypes.STALKER) > 0)
                    agent.Order(1593);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100
                    && Completed(UnitTypes.ADEPT) > 0)
                    agent.Order(1594);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                         && Minerals() >= 100
                         && Gas() >= 100
                         && Completed(UnitTypes.ZEALOT) > 0)
                    agent.Order(1592);
            }
        }
    }
}
