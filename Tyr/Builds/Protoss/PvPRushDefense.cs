using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class PvPRushDefense : Build
    {
        private Point2D OverrideDefenseTarget;
        private FallBackController FallBackController = new FallBackController() { ReturnFire = true, MainDist = 40 };
        private StutterController StutterController = new StutterController();
        private bool EarlyForge = false;
        private Point2D OverrideMainDefenseTarget;
        private bool FourGateDetected = false;
        private bool DoubleRoboAllIn = false;

        private bool SecondRobo = false;
        
        private int EnemyExpandFrame = 1000000;
        private WallInCreator WallIn;

        public override string Name()
        {
            return "PvPRushDefense";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            ArmyObserverTask.Enable();
            if (Bot.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ArchonMergeTask.Enable();
            ShieldRegenTask.Enable();
            WorkerRushDefenseTask.Enable();
            ScoutTask.Enable();
            KillOwnUnitTask.Enable();
            ForceFieldRampTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);
            OverrideMainDefenseTarget = new PotentialHelper(tyr.MapAnalyzer.GetMainRamp(), 6).
                To(tyr.MapAnalyzer.StartLocation)
                .Get();

            MicroControllers.Add(FallBackController);
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(StutterController);
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new AdvanceController());
            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY });
                WallIn.ReserveSpace();
            }

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) > 0 && (Count(UnitTypes.CYBERNETICS_CORE) > 0 || StrategyAnalysis.WorkerRush.Get().Detected));
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuild();
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

            result.If(() => DoubleRoboAllIn || FourGateDetected || Count(UnitTypes.STALKER) < 5 || Count(UnitTypes.IMMORTAL) < 2 || Count(UnitTypes.NEXUS) >= 2);
            result.If(() => !EarlyForge || Count(UnitTypes.FORGE) > 0);
            result.If(() => !EarlyForge || Completed(UnitTypes.FORGE) == 0 || Count(UnitTypes.PHOTON_CANNON) > 0);
            result.Train(UnitTypes.ZEALOT, 4, () => StrategyAnalysis.WorkerRush.Get().Detected);
            result.Train(UnitTypes.SENTRY, 1);
            result.Train(UnitTypes.STALKER, 3);
            result.Train(UnitTypes.TEMPEST);
            result.Train(UnitTypes.VOID_RAY);
            result.Train(UnitTypes.IMMORTAL, 1);
            result.Train(UnitTypes.OBSERVER, 1, () => !FourGateDetected && !DoubleRoboAllIn);
            result.Train(UnitTypes.IMMORTAL, 3);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.IMMORTAL, () => SecondRobo);
            result.Train(UnitTypes.STALKER);
            result.Train(UnitTypes.OBSERVER, 2, () => TotalEnemyCount(UnitTypes.DARK_SHRINE) + TotalEnemyCount(UnitTypes.DARK_TEMPLAR) > 0);
            result.Train(UnitTypes.IMMORTAL, 10, () => TotalEnemyCount(UnitTypes.CARRIER) + TotalEnemyCount(UnitTypes.TEMPEST) + TotalEnemyCount(UnitTypes.VOID_RAY) == 0);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main);
            result.Building(UnitTypes.GATEWAY, Main, () => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.ASSIMILATOR, () => !StrategyAnalysis.WorkerRush.Get().Detected || Count(UnitTypes.ZEALOT) >= 2);
            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) >= 2);
            result.Building(UnitTypes.CYBERNETICS_CORE, () => !StrategyAnalysis.WorkerRush.Get().Detected || Count(UnitTypes.ZEALOT) >= 2);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => (FourGateDetected && Bot.Bot.Frame >= 22.4 * 60 * 4) || DoubleRoboAllIn);
            result.Upgrade(UpgradeType.WarpGate);
            result.If(() => (!FourGateDetected && !DoubleRoboAllIn) || Completed(UnitTypes.IMMORTAL) >= 3);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.FORGE, () => EarlyForge);
            result.Upgrade(UpgradeType.ProtossGroundWeapons, () => EarlyForge);
            result.Building(UnitTypes.PHOTON_CANNON, () => EarlyForge);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => SecondRobo && !FourGateDetected);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !SecondRobo && !FourGateDetected && Bot.Bot.Frame >= 22.4 * 60 * 6
                && TotalEnemyCount(UnitTypes.STARGATE) + TotalEnemyCount(UnitTypes.VOID_RAY) + TotalEnemyCount(UnitTypes.ORACLE) == 0);
            result.Building(UnitTypes.ASSIMILATOR, () => SecondRobo);
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalDefensePos, 2, () => Completed(Natural, UnitTypes.PYLON) > 0);
            result.If(() => Completed(UnitTypes.IMMORTAL) > 0 && Completed(UnitTypes.STALKER) >= 20);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.FORGE, () => !EarlyForge);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => TotalEnemyCount(UnitTypes.CARRIER) + TotalEnemyCount(UnitTypes.TEMPEST) == 0);
            result.Building(UnitTypes.ASSIMILATOR, 4, () => Minerals() >= 700 && Gas() < 200);
            result.If(() => Count(UnitTypes.IMMORTAL) >= 3 && Count(UnitTypes.STALKER) >= 10);
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
            WorkerTask.Task.EvacuateThreatenedBases = true;

            if ((FourGateDetected || DoubleRoboAllIn) && Completed(UnitTypes.IMMORTAL) < 2)
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

            if (Completed(UnitTypes.NEXUS) == 1
                && TotalEnemyCount(UnitTypes.ROBOTICS_BAY) + TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) > 0)
                DoubleRoboAllIn = true;

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (tyr.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 2 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }
            
            if (EnemyCount(UnitTypes.PHOENIX) <= 3)
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.PHOENIX);
            else
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Remove(UnitTypes.PHOENIX);

            if (StrategyAnalysis.WorkerRush.Get().Detected
                && Count(UnitTypes.ZEALOT) < 2)
                GasWorkerTask.WorkersPerGas = 0;
            else
                BalanceGas();

            ForceFieldRampTask.Task.Stopped = !FourGateDetected;
            if (ForceFieldRampTask.Task.Stopped)
                ForceFieldRampTask.Task.Clear();


            SecondRobo = TotalEnemyCount(UnitTypes.STARGATE) + TotalEnemyCount(UnitTypes.VOID_RAY) + TotalEnemyCount(UnitTypes.ORACLE) == 0
                && (TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) + TotalEnemyCount(UnitTypes.ROBOTICS_BAY) + TotalEnemyCount(UnitTypes.COLOSUS) + TotalEnemyCount(UnitTypes.IMMORTAL) > 0
                || EnemyCount(UnitTypes.GATEWAY) + EnemyCount(UnitTypes.WARP_GATE) >= 3
                );

            ScoutTask.Task.ScoutType = UnitTypes.PHOENIX;
            ScoutTask.Task.Target = tyr.TargetManager.PotentialEnemyStartLocations[0];

            if (TotalEnemyCount(UnitTypes.STARGATE) 
                + TotalEnemyCount(UnitTypes.DARK_SHRINE)
                + TotalEnemyCount(UnitTypes.VOID_RAY)
                + TotalEnemyCount(UnitTypes.ORACLE)
                + TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY)
                == 0
                && !SecondRobo)
            {
                foreach (Agent agent in tyr.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.SENTRY)
                        continue;
                    if (agent.Unit.Energy < 75)
                        continue;
                    // Hallucinate scouting phoenix.
                    agent.Order(154);
                }
            }

            if (StrategyAnalysis.WorkerRush.Get().Detected
                && Count(UnitTypes.ZEALOT) < 2)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.ASSIMILATOR
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }

            if (!FourGateDetected
                && EnemyCount(UnitTypes.GATEWAY) + EnemyCount(UnitTypes.WARP_GATE) >= 3
                && tyr.Frame < 22.4 * 60 * 4)
                FourGateDetected = true;
            if (TotalEnemyCount(UnitTypes.VOID_RAY) + TotalEnemyCount(UnitTypes.STARGATE) + TotalEnemyCount(UnitTypes.ORACLE) > 0)
                FourGateDetected = false;

            tyr.TargetManager.TargetCannons = true;

            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            WorkerScoutTask.Task.StartFrame = 600;
            ObserverScoutTask.Task.Priority = 6;

            tyr.NexusAbilityManager.Stopped = Count(UnitTypes.STALKER) == 0 && tyr.Frame >= 120 * 22.4;
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);

            if (EnemyExpandFrame >= 1000000
                && Expanded.Get().Detected)
                EnemyExpandFrame = tyr.Frame;
            
            if ((DoubleRoboAllIn || FourGateDetected)
                && Completed(UnitTypes.IMMORTAL) >= 4)
                TimingAttackTask.Task.RequiredSize = 10;
            else
                TimingAttackTask.Task.RequiredSize = 24;

            if (Count(UnitTypes.NEXUS) <= 1)
                IdleTask.Task.OverrideTarget = OverrideMainDefenseTarget;
            else if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            if (FourGateDetected)
            {
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 25;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 25;
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

            if (TotalEnemyCount(UnitTypes.TEMPEST) > 0)
            {
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 50;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 50;
                DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

            }
            else
            {
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
                DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
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
