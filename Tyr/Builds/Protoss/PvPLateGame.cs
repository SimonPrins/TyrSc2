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

namespace SC2Sharp.Builds.Protoss
{
    public class PvPLateGame : Build
    {
        private Point2D OverrideDefenseTarget;
        private bool ZealotRushSuspected;
        private FallBackController FallBackController = new FallBackController() { ReturnFire = true, MainDist = 40 };
        private StutterController StutterController = new StutterController();
        private bool VoidraysDetected;
        private bool CannonDefenseDetected;
        private bool StargateDetected;
        private bool EarlyForge = false;
        private Point2D OverrideMainDefenseTarget;

        private bool EarlyForgeDetected = false;
        private bool EarlyNexus = true;
        private int EnemyExpandFrame = 1000000;
        private bool EarlyExpand = false;
        private bool LowGroundCannons = false;
        private WallInCreator WallIn;
        private bool StopAdeptHarass = false;

        private KillTargetController KillImmortals = new KillTargetController(UnitTypes.IMMORTAL);

        public override string Name()
        {
            return "PvPLateGame";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            ArmyObserverTask.Enable();
            ObserverScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArchonMergeTask.Enable();
            WorkerRushDefenseTask.Enable();
            ScoutTask.Enable();
            KillOwnUnitTask.Enable();
            AdeptHarassExpandsTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            OverrideDefenseTarget = bot.MapAnalyzer.Walk(NaturalDefensePos, bot.MapAnalyzer.EnemyDistances, 15);
            OverrideMainDefenseTarget = new PotentialHelper(bot.MapAnalyzer.GetMainRamp(), 6).
                To(bot.MapAnalyzer.StartLocation)
                .Get();

            MicroControllers.Add(new AdeptKillWorkersController());
            MicroControllers.Add(FallBackController);
            MicroControllers.Add(KillImmortals);
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
            foreach (Base b in Bot.Main.BaseManager.Bases)
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

            result.If(() => Count(UnitTypes.STALKER) < 5 || Count(UnitTypes.IMMORTAL) < 2 || Count(UnitTypes.NEXUS) >= 2 || EarlyExpand);
            result.If(() => !EarlyForge || Count(UnitTypes.FORGE) > 0 || EarlyExpand);
            result.If(() => !EarlyForge || Completed(UnitTypes.FORGE) == 0 || Count(UnitTypes.PHOTON_CANNON) > 0 || EarlyExpand);
            result.Train(UnitTypes.ZEALOT, 4, () => StrategyAnalysis.WorkerRush.Get().Detected);
            result.Train(UnitTypes.SENTRY, 1, () => (VoidraysDetected && !CannonDefenseDetected));
            result.Train(UnitTypes.SENTRY, 1, () => !EarlyExpand && Count(UnitTypes.STALKER) >= 4);
            result.Train(UnitTypes.STALKER, () => VoidraysDetected || TotalEnemyCount(UnitTypes.CARRIER) + TotalEnemyCount(UnitTypes.TEMPEST) > 0);
            result.Train(UnitTypes.TEMPEST);
            result.Train(UnitTypes.VOID_RAY);
            result.Train(UnitTypes.OBSERVER, 1, () => Bot.Main.Frame >= 22.4 * 60 * 5 && (Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) >= 12 || !EarlyExpand));
            result.Train(UnitTypes.IMMORTAL, 1, () => TotalEnemyCount(UnitTypes.CARRIER) + TotalEnemyCount(UnitTypes.TEMPEST) == 0 && (!CannonDefenseDetected || !StargateDetected));
            result.Train(UnitTypes.IMMORTAL, 3, () => !CannonDefenseDetected && TotalEnemyCount(UnitTypes.CARRIER) + TotalEnemyCount(UnitTypes.TEMPEST) + TotalEnemyCount(UnitTypes.FORGE) + TotalEnemyCount(UnitTypes.PHOTON_CANNON) == 0);
            result.Train(UnitTypes.IMMORTAL, 3, () => EarlyExpand);
            result.Train(UnitTypes.OBSERVER, 2);
            result.Train(UnitTypes.OBSERVER, 3, () => TotalEnemyCount(UnitTypes.DARK_SHRINE) + TotalEnemyCount(UnitTypes.DARK_TEMPLAR) > 0);
            result.Train(UnitTypes.IMMORTAL, 10, () => TotalEnemyCount(UnitTypes.CARRIER) + TotalEnemyCount(UnitTypes.TEMPEST) == 0 && (!CannonDefenseDetected || !StargateDetected));
            result.Train(UnitTypes.ADEPT, 6, () => !StopAdeptHarass && Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) >= 18);
            result.Train(UnitTypes.STALKER);

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
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[2].Pos, true, () => ZealotRushSuspected && Count(UnitTypes.STALKER) >= 2 && Completed(UnitTypes.STALKER) <= 8);
            result.If(() => !ZealotRushSuspected || Count(UnitTypes.STALKER) >= 6);
            result.Building(UnitTypes.NEXUS, () => EarlyNexus && (!EarlyExpand || TimingAttackTask.Task.AttackSent));
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => (!ZealotRushSuspected || Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) >= 12));
            result.Building(UnitTypes.ASSIMILATOR);
            result.Upgrade(UpgradeType.WarpGate, () => !ZealotRushSuspected || Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) >= 12);
            result.If(() => !ZealotRushSuspected || Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) >= 14);
            result.Building(UnitTypes.NEXUS, () => !EarlyNexus && (!EarlyExpand || TimingAttackTask.Task.AttackSent));
            result.Building(UnitTypes.FORGE, () => EarlyForge);
            result.Upgrade(UpgradeType.ProtossGroundWeapons, () => EarlyForge);
            result.Building(UnitTypes.PHOTON_CANNON, () => EarlyForge);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) >= 12);
            result.Building(UnitTypes.PYLON, Natural, () => Count(UnitTypes.CYBERNETICS_CORE) > 0 && !CannonDefenseDetected);
            result.Building(UnitTypes.ASSIMILATOR, () => CannonDefenseDetected && StargateDetected);
            result.If(() => (Completed(UnitTypes.IMMORTAL) > 0 || StargateDetected) && Completed(UnitTypes.STALKER) >= 6);
            result.If(() => !EarlyExpand || TimingAttackTask.Task.AttackSent);
            result.Building(UnitTypes.NEXUS);
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
            result.If(() => (Count(UnitTypes.IMMORTAL) >= 3 || StargateDetected) && Count(UnitTypes.STALKER) >= 10);
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
        
        public override void OnFrame(Bot bot)
        {
            ArmyObserverTask.Task.IgnoreAllyUnitTypes.Add(UnitTypes.ADEPT);

            ObserverScoutTask.Task.Priority = 6;
            WorkerTask.Task.EvacuateThreatenedBases = true;

            TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.ADEPT);

            if (Count(UnitTypes.ADEPT) >= 6)
                StopAdeptHarass = true;

            bot.DrawText("ZealotRushSuspected: " + ZealotRushSuspected);

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 2 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }

            KillImmortals.Stopped = TotalEnemyCount(UnitTypes.ZEALOT) >= 3 || EnemyCount(UnitTypes.PHOTON_CANNON) > 0;


            if (EnemyCount(UnitTypes.PHOENIX) <= 3)
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.PHOENIX);
            else
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Remove(UnitTypes.PHOENIX);

            if (StrategyAnalysis.WorkerRush.Get().Detected
                && Count(UnitTypes.ZEALOT) < 2)
                GasWorkerTask.WorkersPerGas = 0;
            else
                BalanceGas();

            if (TotalEnemyCount(UnitTypes.FORGE) > 0 && bot.Frame <= 22.4 * 60 * 2)
                EarlyForgeDetected = true;

            ScoutTask.Task.ScoutType = UnitTypes.PHOENIX;
            ScoutTask.Task.Target = bot.TargetManager.PotentialEnemyStartLocations[0];
            
            if (!EarlyExpand
                && !ZealotRushSuspected
                && TotalEnemyCount(UnitTypes.STARGATE) + TotalEnemyCount(UnitTypes.DARK_SHRINE) + TotalEnemyCount(UnitTypes.VOID_RAY) == 0)
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

            if (!VoidraysDetected && TotalEnemyCount(UnitTypes.VOID_RAY) > 0)
            {
                VoidraysDetected = true;
                if (TimingAttackTask.Task.Units.Count < 12)
                    TimingAttackTask.Task.Clear();
            }

            if (!ZealotRushSuspected && !StrategyAnalysis.WorkerRush.Get().Detected)
            {
                if ((Bot.Main.Frame >= 22.4 * 60 * 1.5
                    && !Bot.Main.EnemyStrategyAnalyzer.NoProxyGatewayConfirmed
                    && TotalEnemyCount(UnitTypes.ASSIMILATOR) + TotalEnemyCount(UnitTypes.CYBERNETICS_CORE) == 0)
                    || (Bot.Main.Frame < 22.4 * 60 * 1.5 && ThreeGate.Get().Detected))
                    ZealotRushSuspected = true;
            }

            if (!LowGroundCannons)
            {
                foreach (Unit enemy in bot.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.PHOTON_CANNON)
                        continue;
                    if (bot.MapAnalyzer.MapHeight((int)enemy.Pos.X, (int)enemy.Pos.Y) < bot.MapAnalyzer.MapHeight((int)bot.TargetManager.PotentialEnemyStartLocations[0].X, (int)bot.TargetManager.PotentialEnemyStartLocations[0].Y))
                    {
                        LowGroundCannons = true;
                        break;
                    }
                }
            }

            if (!CannonDefenseDetected
                && TotalEnemyCount(UnitTypes.PHOTON_CANNON) + TotalEnemyCount (UnitTypes.FORGE) > 0
                && bot.Frame < 22.4 * 60 * 4
                && (EarlyForgeDetected || Expanded.Get().Detected || LowGroundCannons))
            {
                CannonDefenseDetected = true;
                if (TimingAttackTask.Task.Units.Count < 12)
                    TimingAttackTask.Task.Clear();
            }
            
            if (StrategyAnalysis.WorkerRush.Get().Detected
                && Count(UnitTypes.ZEALOT) < 2)
            {
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.ASSIMILATOR
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }

            bot.TargetManager.TargetCannons = true;
            if (ZealotRushSuspected)
            {
                bot.TargetManager.TargetGateways = true;
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if (ZealotRushSuspected
                || Completed(UnitTypes.IMMORTAL) > 0)
            {
                foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                {
                    task.Stopped = true;
                    task.Clear();
                }
            }

            if (ZealotRushSuspected && Completed(UnitTypes.STALKER) >= 12)
            {
                foreach (Agent agent in bot.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                        continue;
                    if (agent.DistanceSq(WallIn.Wall[2].Pos) >= 2)
                        continue;
                    KillOwnUnitTask.Task.TargetTag = agent.Unit.Tag;
                    break;
                }
            }

            WorkerScoutTask.Task.StartFrame = 600;
            ObserverScoutTask.Task.Priority = 6;

            bot.NexusAbilityManager.Stopped = Count(UnitTypes.STALKER) == 0 && bot.Frame >= 120 * 22.4;
            bot.NexusAbilityManager.PriotitizedAbilities.Add(917);

            if (EnemyExpandFrame >= 1000000
                && Expanded.Get().Detected)
                EnemyExpandFrame = bot.Frame;

            if (EnemyExpandFrame < 90 * 22.4)
                EarlyExpand = true;

            if (EarlyExpand)
                TimingAttackTask.Task.RequiredSize = 12;
            else if (VoidraysDetected
                || (TotalEnemyCount(UnitTypes.PHOTON_CANNON) + TotalEnemyCount(UnitTypes.FORGE) > 0 && !LowGroundCannons && !EarlyExpand))
                TimingAttackTask.Task.RequiredSize = 36;
            else if (ZealotRushSuspected
                || (CannonDefenseDetected && EarlyExpand))
                TimingAttackTask.Task.RequiredSize = 12;
            else
                TimingAttackTask.Task.RequiredSize = 36;
            TimingAttackTask.Task.RetreatSize = 0;


            if (TotalEnemyCount(UnitTypes.PHOTON_CANNON) + TotalEnemyCount(UnitTypes.FORGE) > 0
                && !LowGroundCannons 
                && !EarlyExpand
                && TimingAttackTask.Task.Units.Count < 18
                && TimingAttackTask.Task.RequiredSize > 18)
                TimingAttackTask.Task.Clear();
            
            FallBackController.Stopped = ZealotRushSuspected;

            ForwardProbeTask.Task.Stopped = 
                Completed(UnitTypes.STALKER) + Count(UnitTypes.ADEPT) + Completed(UnitTypes.IMMORTAL) < Math.Max(8, TimingAttackTask.Task.RequiredSize)
                && (!CannonDefenseDetected || !StargateDetected || EarlyExpand);

            if (ForwardProbeTask.Task.Stopped)
                ForwardProbeTask.Task.Clear();


            if (Count(UnitTypes.NEXUS) <= 1)
                IdleTask.Task.OverrideTarget = OverrideMainDefenseTarget;
            else if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            if (ZealotRushSuspected)
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

            StargateDetected = TotalEnemyCount(UnitTypes.STARGATE)
                + TotalEnemyCount(UnitTypes.VOID_RAY)
                + TotalEnemyCount(UnitTypes.TEMPEST)
                + TotalEnemyCount(UnitTypes.CARRIER)
                + TotalEnemyCount(UnitTypes.FLEET_BEACON) > 0
                || TotalEnemyCount(UnitTypes.PHOENIX) >= 3;

            EarlyNexus = !EarlyExpand;
        }

        public override void Produce(Bot bot, Agent agent)
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
                    && !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(50)
                    && Count(UnitTypes.COLOSUS) > 0)
                {
                    agent.Order(1097);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {

                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(87)
                     && Minerals() >= 150
                     && Gas() >= 150
                    && Completed(UnitTypes.STALKER) > 0)
                    agent.Order(1593);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100
                    && Completed(UnitTypes.ADEPT) > 0)
                    agent.Order(1594);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                         && Minerals() >= 100
                         && Gas() >= 100
                         && Completed(UnitTypes.ZEALOT) > 0)
                    agent.Order(1592);
            }
        }
    }
}
