using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class PvPMothershipSiege : Build
    {
        private StutterController StutterController = new StutterController();
        public bool Defensive = true;
        private bool EnemyStargateOpener = false;
        private bool EnemyVoidRayOpener = false;
        private bool EnemyTempestOpener = false;
        private BlinkForwardController BlinkForwardController = new BlinkForwardController();
        private bool ProxyDetected = false;
        private bool StalkerAggressionSuspected = false;

        public bool ProxyPylon = false;

        public override string Name()
        {
            return "PvPMothershipSiege";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();

            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            ObserverScoutTask.Enable();
            ArmyOracleTask.Enable();
            SaveWorkersTask.Enable();
            ObserverHunterTask.Enable();
            DTAttackTask.Enable();
            if (Defensive)
                HuntProxyTask.Enable();
            ScoutTask.Enable();
            ForceFieldRampTask.Enable();
            if (ProxyPylon)
            {
                ProxyTask.Enable(new List<ProxyBuilding>() { new ProxyBuilding() { UnitType = UnitTypes.PYLON } });
                ProxyTask.Task.UseEnemyNatural = true;
            }
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new TargetObserversController());
            TimingAttackTask.Task.BeforeControllers.Add(new SoftLeashController(new HashSet<uint> { UnitTypes.TEMPEST, UnitTypes.STALKER, UnitTypes.IMMORTAL }, UnitTypes.MOTHERSHIP, 6));
            MicroControllers.Add(BlinkForwardController);
            MicroControllers.Add(new DTController());
            MicroControllers.Add(new VoidrayController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new MothershipController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new GravitonBeamController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.STALKER, 6));
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(StutterController);

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() =>
                (Completed(UnitTypes.PYLON) >= (ProxyDetected ? 1 : 2) 
                && Count(UnitTypes.CYBERNETICS_CORE) > 0
                && Count(UnitTypes.STALKER) > 0) 
                && Count(UnitTypes.PYLON) < 3 || tyr.Frame >= 22.4 * 60 * 3.5);
            Set += ExpandBuildings();
            Set += ExtraAssimilators();
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();
            
            /*
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 450);
            }
            */

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
            result.Train(UnitTypes.SENTRY, 1, () => Defensive && ProxyDetected);
            result.Train(UnitTypes.IMMORTAL, 8, () => ProxyDetected && TotalEnemyCount(UnitTypes.PHOTON_CANNON) > 0);
            result.Train(UnitTypes.STALKER, 1);
            result.Train(UnitTypes.DARK_TEMPLAR, 4, () => EnemyVoidRayOpener && TotalEnemyCount(UnitTypes.PHOTON_CANNON) + TotalEnemyCount(UnitTypes.OBSERVER) == 0);
            result.Train(UnitTypes.STALKER, 20, () => EnemyStargateOpener && Completed(UnitTypes.TEMPEST) < 6);
            result.Train(UnitTypes.STALKER, 30, () => ProxyDetected);
            result.Train(UnitTypes.STALKER, 2, () => Defensive);
            result.Train(UnitTypes.SENTRY, 1, () => Defensive);
            result.Train(UnitTypes.OBSERVER, 1, () => EnemyCount(UnitTypes.DARK_SHRINE) + EnemyCount(UnitTypes.DARK_TEMPLAR) > 0);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.IMMORTAL) > 0);
            result.Upgrade(UpgradeType.ProtossAirArmor, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) >= (EnemyStargateOpener ? 6 : 1));
            result.Upgrade(UpgradeType.ProtossAirWeapons, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) >= (EnemyStargateOpener ? 6 : 1));
            result.Upgrade(UpgradeType.ProtossGroundArmor, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) > 0);
            result.Upgrade(UpgradeType.ProtossGroundWeapons, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) > 0);
            result.Upgrade(UpgradeType.ProtossShields, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) > 0);
            result.Train(UnitTypes.MOTHERSHIP, 1, () => Completed(UnitTypes.FLEET_BEACON) > 0 && (!EnemyStargateOpener || Count(UnitTypes.TEMPEST) >= 6));
            result.Train(UnitTypes.TEMPEST, 20);
            result.Train(UnitTypes.STALKER, 30, () => EnemyStargateOpener && Completed(UnitTypes.TEMPEST) < 6 && (!EnemyVoidRayOpener || Count(UnitTypes.SHIELD_BATTERY) >= 2) && (!EnemyVoidRayOpener || Count(UnitTypes.STARGATE) >= 3) && (!EnemyTempestOpener || Count(UnitTypes.TWILIGHT_COUNSEL) > 0));
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.PHOENIX) < 30 || Count(UnitTypes.NEXUS) >= 3);
            result.If(() => Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.STALKER) < 15);
            result.If(() => Count(UnitTypes.NEXUS) >= 3 || Count(UnitTypes.STALKER) < 20);
            result.Train(UnitTypes.IMMORTAL, 2, () => !EnemyStargateOpener || Count(UnitTypes.TEMPEST) >= 3);
            result.Train(UnitTypes.OBSERVER, 3);
            result.Train(UnitTypes.IMMORTAL, 8, () => !EnemyStargateOpener || Completed(UnitTypes.TEMPEST) >= 6);
            result.Train(UnitTypes.STALKER, 15);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON, Main, () => Defensive);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.ASSIMILATOR, () => Defensive);
            result.Building(UnitTypes.GATEWAY, Main, () => Defensive);
            result.Building(UnitTypes.ASSIMILATOR, () => ProxyDetected);
            result.Building(UnitTypes.PYLON, Main, () => ProxyDetected);
            result.Building(UnitTypes.GATEWAY, 2, () => ProxyDetected && TotalEnemyCount(UnitTypes.PHOTON_CANNON) == 0);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => ProxyDetected && TotalEnemyCount(UnitTypes.PHOTON_CANNON) > 0);
            result.If(() => !ProxyDetected || Completed(UnitTypes.STALKER) >= 20);
            result.If(() => !Defensive || Count(UnitTypes.STALKER) >= 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.ASSIMILATOR, () => !Defensive);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.TWILIGHT_COUNSEL, () => EnemyStargateOpener);
            result.Building(UnitTypes.GATEWAY, 2, () => EnemyStargateOpener || StalkerAggressionSuspected);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => EnemyVoidRayOpener && TotalEnemyCount(UnitTypes.PHOTON_CANNON) + TotalEnemyCount(UnitTypes.OBSERVER) == 0);
            result.Building(UnitTypes.DARK_SHRINE, () => EnemyVoidRayOpener && TotalEnemyCount(UnitTypes.PHOTON_CANNON) + TotalEnemyCount(UnitTypes.OBSERVER) == 0);
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalDefensePos, 2, () => EnemyVoidRayOpener || StalkerAggressionSuspected);
            result.If(() => !EnemyVoidRayOpener || TotalEnemyCount(UnitTypes.PHOTON_CANNON) + TotalEnemyCount(UnitTypes.OBSERVER) > 0 || Count(UnitTypes.DARK_TEMPLAR) > 0);
            result.If(() => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.STALKER) >= 7);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.FLEET_BEACON) > 0);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.STALKER) >= 12);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.TEMPEST) > 0);
            result.Building(UnitTypes.GATEWAY, Natural);
            result.Building(UnitTypes.STARGATE);
            result.Upgrade(UpgradeType.Blink);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.STARGATE, () => EnemyVoidRayOpener);
            result.Building(UnitTypes.FORGE, () => EnemyVoidRayOpener && Minerals() >= 500);
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 3, () => EnemyVoidRayOpener && Minerals() >= 600);
            result.If(() => !EnemyStargateOpener || Count(UnitTypes.TEMPEST) >= 6);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.FORGE, 2);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE, () => !EnemyVoidRayOpener);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 3);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.TWILIGHT_COUNSEL, () => !EnemyTempestOpener);
            result.Building(UnitTypes.GATEWAY, 2, () => Minerals() >= 500);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            BalanceGas();

            if (ProxyPylon && ProxyTask.Task.UnitCounts.ContainsKey(UnitTypes.PYLON) && ProxyTask.Task.UnitCounts[UnitTypes.PYLON] > 0)
            {
                ProxyPylon = false;
                ProxyTask.Task.Stopped = true;
                ProxyTask.Task.Clear();
            }

            IdleTask.Task.AttackMove = true;

            tyr.TargetManager.CloseTo = null;
            tyr.TargetManager.PrefferDistant = true;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.MOTHERSHIP)
                {
                    tyr.TargetManager.CloseTo = SC2Util.To2D(agent.Unit.Pos);
                    tyr.TargetManager.PrefferDistant = false;
                    tyr.TargetManager.IncludeAllEnemies = true;
                    break;
                }
            }

            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }


            if (!ProxyDetected && Defensive)
            {
                float proxyDist = 100 * 100;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (enemy.UnitType == UnitTypes.PYLON)
                    {
                        float dist = SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation);
                        if (dist < proxyDist)
                            ProxyDetected = true;
                    }
                }
            }

            ForceFieldRampTask.Task.Stopped = !ProxyDetected || Completed(UnitTypes.STALKER) >= 20;
            if (ForceFieldRampTask.Task.Stopped)
                ForceFieldRampTask.Task.Clear();

            if (TotalEnemyCount(UnitTypes.ZEALOT) + TotalEnemyCount(UnitTypes.PHOTON_CANNON) > 0)
            {
                HuntProxyTask.Task.Stopped = true;
                HuntProxyTask.Task.Clear();
            }

            if ((ProxyDetected) && Completed(UnitTypes.STALKER) < 15)
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

            BlinkForwardController.Stopped = !EnemyTempestOpener || Completed(UnitTypes.MOTHERSHIP) > 0;

            if (TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) + TotalEnemyCount(UnitTypes.ROBOTICS_BAY) > 0)
            {
                EnemyStargateOpener = false;
                EnemyTempestOpener = false;
                EnemyVoidRayOpener = false;
            }

            if (!StalkerAggressionSuspected
                && Defensive
                && tyr.Frame >= 22.4 * 60 * 4.5
                && Expanded.Get().Detected
                && TotalEnemyCount(UnitTypes.STARGATE) + TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) + TotalEnemyCount(UnitTypes.ROBOTICS_BAY) + TotalEnemyCount(UnitTypes.IMMORTAL) + TotalEnemyCount(UnitTypes.FLEET_BEACON) + +TotalEnemyCount(UnitTypes.VOID_RAY) +TotalEnemyCount(UnitTypes.TEMPEST) == 0)
                StalkerAggressionSuspected = true;
            if (TotalEnemyCount(UnitTypes.STARGATE) + TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) + TotalEnemyCount(UnitTypes.ROBOTICS_BAY) + TotalEnemyCount(UnitTypes.IMMORTAL) + TotalEnemyCount(UnitTypes.FLEET_BEACON) + +TotalEnemyCount(UnitTypes.VOID_RAY) + TotalEnemyCount(UnitTypes.TEMPEST) > 0)
                StalkerAggressionSuspected = false;

            if (!EnemyStargateOpener
                && Defensive 
                && TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) + TotalEnemyCount(UnitTypes.ROBOTICS_BAY) == 0
                && TotalEnemyCount(UnitTypes.STARGATE) + TotalEnemyCount(UnitTypes.VOID_RAY) + TotalEnemyCount(UnitTypes.FLEET_BEACON) + TotalEnemyCount(UnitTypes.TEMPEST) > 0
                && tyr.Frame <= 22.4 * 60 * 4.5)
            {
                EnemyStargateOpener = true;
            }

            if (!EnemyVoidRayOpener 
                && !EnemyTempestOpener 
                && EnemyStargateOpener 
                && (TotalEnemyCount(UnitTypes.VOID_RAY) > 0 || Expanded.Get().Detected)
                && TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) + TotalEnemyCount(UnitTypes.ROBOTICS_BAY) == 0)
                EnemyVoidRayOpener = true;
            if (!EnemyVoidRayOpener
                && !EnemyTempestOpener 
                && EnemyStargateOpener 
                && TotalEnemyCount(UnitTypes.TEMPEST) + TotalEnemyCount(UnitTypes.FLEET_BEACON) > 0
                && TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) + TotalEnemyCount(UnitTypes.ROBOTICS_BAY) == 0)
                EnemyTempestOpener = true;

            ObserverScoutTask.Task.Priority = 6;
            ArmyObserverTask.Task.IgnoreAllyUnitTypes.Add(UnitTypes.DARK_TEMPLAR);

            if (TotalEnemyCount(UnitTypes.STARGATE) + TotalEnemyCount(UnitTypes.DARK_SHRINE) + TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) + TotalEnemyCount(UnitTypes.VOID_RAY) == 0
                && !ProxyDetected)
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
            
            ScoutTask.Task.ScoutType = UnitTypes.PHOENIX;
            if (Completed(UnitTypes.PHOENIX) == 0)
                ScoutTask.Task.Target = tyr.TargetManager.PotentialEnemyStartLocations[0];
            else
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                {
                    if (agent.Unit.UnitType != UnitTypes.PHOENIX)
                        continue;
                    if (tyr.TargetManager.PotentialEnemyStartLocations.Count == 0 || agent.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]) >= 10 * 10)
                        continue;

                    ScoutTask.Task.Target = tyr.MapAnalyzer.GetEnemyNatural().Pos;
                    break;
                }
            }

            tyr.NexusAbilityManager.Stopped = Completed(UnitTypes.PYLON) == 0;
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(1006);


            SaveWorkersTask.Task.Stopped = tyr.Frame >= 22.4 * 60 * 7 || EnemyCount(UnitTypes.CYCLONE) == 0 || !Natural.UnderAttack;
            if (SaveWorkersTask.Task.Stopped)
                SaveWorkersTask.Task.Clear();

            DefenseTask.GroundDefenseTask.IncludePhoenixes = true;
            if (TimingAttackTask.Task.Units.Count > 0)
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Add(UnitTypes.OBSERVER);
            else
                DefenseTask.AirDefenseTask.IgnoreEnemyTypes.Remove(UnitTypes.OBSERVER);

            WorkerTask.Task.EvacuateThreatenedBases = true;
            

            TimingAttackTask.Task.DefendOtherAgents = false;

            if (EnemyVoidRayOpener && Completed(UnitTypes.TEMPEST) + Completed(UnitTypes.CARRIER) < 10)
            {
                TimingAttackTask.Task.RetreatSize = 15;
                TimingAttackTask.Task.RequiredSize = 45;
            }
            else if (TotalEnemyCount(UnitTypes.PHOTON_CANNON) > 0 && ProxyDetected && Completed(UnitTypes.IMMORTAL) >= 4)
            {
                TimingAttackTask.Task.RetreatSize = 6;
                TimingAttackTask.Task.RequiredSize = 15;
            }
            else if (Completed(UnitTypes.MOTHERSHIP) == 0 || Completed(UnitTypes.TEMPEST) + Completed(UnitTypes.CARRIER) < 6)
            {
                TimingAttackTask.Task.RetreatSize = 15;
                TimingAttackTask.Task.RequiredSize = 35;
            }
            else if (Completed(UnitTypes.MOTHERSHIP) == 1 && Completed(UnitTypes.TEMPEST) + Completed(UnitTypes.CARRIER) >= 8)
            {
                TimingAttackTask.Task.RetreatSize = 0;
                TimingAttackTask.Task.RequiredSize = 8;
            }
            else
            {
                TimingAttackTask.Task.RetreatSize = 10;
                TimingAttackTask.Task.RequiredSize = 25;
            }

            if (ProxyDetected && TotalEnemyCount(UnitTypes.PHOTON_CANNON) > 0 && Completed(UnitTypes.IMMORTAL) < 3)
            {
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 18;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 18;
                DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;
                DefenseTask.GroundDefenseTask.BufferZone = 0;

                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 18;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 18;
                DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
                DefenseTask.AirDefenseTask.BufferZone = 0;
            }
            else if (EnemyTempestOpener || TotalEnemyCount(UnitTypes.TEMPEST) > 0)
            {
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
                DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
                DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
            } else
            {
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
                DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 20;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 20;
                DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
                DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
            }

        }
    }
}
