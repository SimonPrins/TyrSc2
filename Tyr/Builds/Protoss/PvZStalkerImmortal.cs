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
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class PvZStalkerImmortal : Build
    {
        private Point2D OverrideDefenseTarget;
        private WallInCreator WallIn;
        private FallBackController FallBackController = new FallBackController() { ReturnFire = true };
        StutterForwardController StutterForwardController = new StutterForwardController() { MaxDist = 3 };
        private Point2D ShieldBatteryPos;
        private bool MassZerglings = false;
        public bool BlockExpand = false;
        public bool ProxyPylon = false;

        public override string Name()
        {
            return "PvZStalkerImmortal";
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
            if (Bot.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ArchonMergeTask.Enable();
            ForwardProbeTask.Enable();
            ShieldRegenTask.Enable();
            HodorTask.Enable();
            if (BlockExpand)
                BlockExpandTask.Enable();
            if (ProxyPylon)
            {
                ProxyTask.Enable(new List<ProxyBuilding>() { new ProxyBuilding() { UnitType = UnitTypes.PYLON } });
                ProxyTask.Task.UseEnemyNatural = true;
            }
        }

        public override void OnStart(Bot tyr)
        {
            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(StutterForwardController);
            MicroControllers.Add(FallBackController);
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new AdvanceController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.CreateNatural(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.GATEWAY, UnitTypes.ZEALOT, UnitTypes.GATEWAY});
                ShieldBatteryPos = DetermineShieldBatteryPos();
                WallIn.ReserveSpace();
            }

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

            result.If(() => Count(UnitTypes.STALKER) < 5 || Count(UnitTypes.IMMORTAL) < 2 || Count(UnitTypes.NEXUS) >= 2);
            result.If(() => !EarlyPool.Get().Detected || Count(UnitTypes.ZEALOT) < 8 || Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.ZEALOT, 1, () => Completed(UnitTypes.NEXUS) < 3);
            result.Train(UnitTypes.ZEALOT, 2, () => Completed(UnitTypes.NEXUS) < 3 && TimingAttackTask.Task.Units.Count == 0 && Count(UnitTypes.STALKER) >= 5);
            result.Train(UnitTypes.ZEALOT, 10, () => EarlyPool.Get().Detected || MassZerglings);
            result.Train(UnitTypes.TEMPEST);
            result.Train(UnitTypes.COLOSUS, 3, () => TotalEnemyCount(UnitTypes.ZERGLING) >= 40 && TotalEnemyCount(UnitTypes.MUTALISK) + TotalEnemyCount(UnitTypes.CORRUPTOR) == 0);
            result.Train(UnitTypes.IMMORTAL, 3, () => TotalEnemyCount(UnitTypes.ZERGLING) <= 60 || TotalEnemyCount(UnitTypes.ROACH) + TotalEnemyCount(UnitTypes.HYDRALISK) > 0);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.IMMORTAL, 10, () => TotalEnemyCount(UnitTypes.ZERGLING) <= 60 || TotalEnemyCount(UnitTypes.ROACH) + TotalEnemyCount(UnitTypes.HYDRALISK) > 0);
            //result.Train(UnitTypes.ADEPT, 4);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            if (WallIn.Wall.Count >= 5)
            {
                result.Building(UnitTypes.PYLON, Natural, WallIn.Wall[4].Pos, true);
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[3].Pos, true, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[1].Pos, true, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
            } else
            {
                result.Building(UnitTypes.PYLON);
                result.Building(UnitTypes.GATEWAY, () => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.GATEWAY, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
                result.Building(UnitTypes.GATEWAY, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
            }
            result.Building(UnitTypes.NEXUS, () => !EarlyPool.Get().Detected || Completed(UnitTypes.ADEPT) + Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.STALKER) >= 8);
            if (WallIn.Wall.Count >= 5)
                result.Building(UnitTypes.CYBERNETICS_CORE, Natural, WallIn.Wall[1].Pos, true, () => !EarlyPool.Get().Detected);
            else
                result.Building(UnitTypes.CYBERNETICS_CORE, () => !EarlyPool.Get().Detected);
            result.Building(UnitTypes.CYBERNETICS_CORE, () => EarlyPool.Get().Detected && Count(UnitTypes.ZEALOT) >= 6);
            result.Building(UnitTypes.ASSIMILATOR, () => !EarlyPool.Get().Detected || Count(UnitTypes.ZEALOT) >= 6);
            if (WallIn.Wall.Count >= 5)
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[3].Pos, true, () => !EarlyPool.Get().Detected);
            else
                result.Building(UnitTypes.GATEWAY, () => !EarlyPool.Get().Detected);
            if (ShieldBatteryPos == null)
                result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalDefensePos);
            else
                result.Building(UnitTypes.SHIELD_BATTERY, Natural, ShieldBatteryPos, true);
            result.Building(UnitTypes.ASSIMILATOR, () => !EarlyPool.Get().Detected || Count(UnitTypes.ZEALOT) >= 6);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !EarlyPool.Get().Detected || Completed(UnitTypes.ADEPT) + Completed(UnitTypes.STALKER) >= 8);
            result.Upgrade(UpgradeType.WarpGate);
            result.Building(UnitTypes.ASSIMILATOR, () => !EarlyPool.Get().Detected || Count(UnitTypes.ZEALOT) >= 6);
            result.Building(UnitTypes.PYLON, Natural, () => Count(UnitTypes.CYBERNETICS_CORE) > 0 && Natural.ResourceCenter != null);
            result.Building(UnitTypes.ROBOTICS_BAY, () => TotalEnemyCount(UnitTypes.ZERGLING) >= 40 && TotalEnemyCount(UnitTypes.MUTALISK) + TotalEnemyCount(UnitTypes.CORRUPTOR) == 0);
            result.If(() => Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.COLOSUS) > 0 && Completed(UnitTypes.STALKER) >= 6);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.ASSIMILATOR, 4, () => Minerals() >= 700 && Gas() < 200);
            result.If(() => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.COLOSUS) >= 3 && Count(UnitTypes.STALKER) >= 10);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.COLOSUS) + Count(UnitTypes.ADEPT) >= 15);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Completed(UnitTypes.STALKER) + Completed(UnitTypes.COLOSUS) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.DISRUPTOR) + Completed(UnitTypes.TEMPEST) + Count(UnitTypes.ADEPT) >= 40 || Minerals() >= 650);
            result.Building(UnitTypes.NEXUS);

            return result;
        }
        
        private Point2D DetermineShieldBatteryPos()
        {
            if (WallIn.Wall.Count < 5)
                return null;
            Point2D pos = SC2Util.TowardCardinal(WallIn.Wall[4].Pos, Natural.BaseLocation.Pos, 2);
            if (Bot.Bot.buildingPlacer.CheckPlacement(pos, SC2Util.Point(2, 2), UnitTypes.PYLON, null, true))
                return pos;
            return null;
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

            int wallDone = 0;
            foreach (WallBuilding building in WallIn.Wall)
            {
                if (!BuildingType.LookUp.ContainsKey(building.Type))
                {
                    wallDone++;
                    continue;
                }
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                {
                    if (agent.DistanceSq(building.Pos) <= 1 * 1)
                    {
                        wallDone++;
                        break;
                    }
                }
            }

            FallBackController.Stopped = (Count(UnitTypes.NEXUS) < 3 && TimingAttackTask.Task.Units.Count == 0)
                || EarlyPool.Get().Detected
                || MassZerglings;
            StutterForwardController.Stopped = Count(UnitTypes.NEXUS) >= 3 || TimingAttackTask.Task.Units.Count > 0 || Completed(UnitTypes.ZEALOT) > 0;
            HodorTask.Task.Stopped = Count(UnitTypes.NEXUS) >= 3 
                || TimingAttackTask.Task.Units.Count > 0 
                || (EarlyPool.Get().Detected && Completed(UnitTypes.ZEALOT) >= 2)
                || wallDone < WallIn.Wall.Count;
            if (HodorTask.Task.Stopped)
                HodorTask.Task.Clear();

            if (EarlyPool.Get().Detected || tyr.Frame >= 1800)
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if (WallIn.Wall.Count >= 5)
                HodorTask.Task.Target = WallIn.Wall[2].Pos;
            else
            {
                HodorTask.Task.Stopped = true;
                HodorTask.Task.Clear();
            }

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (tyr.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 3 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Natural.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }

            if (!MassZerglings
                && !EarlyPool.Get().Detected
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) >= 60
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ROACH) == 0
                && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.HYDRALISK) == 0)
            {
                MassZerglings = true;
                TimingAttackTask.Task.Clear();
            }
            tyr.DrawText("Zergling count: " + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING));

            //WorkerScoutTask.Task.StartFrame = 600;
            ObserverScoutTask.Task.Priority = 6;

            ForwardProbeTask.Task.Stopped = Completed(UnitTypes.STALKER) + Count(UnitTypes.ADEPT) + Count(UnitTypes.IMMORTAL) < 12;

            if (ForwardProbeTask.Task.Stopped)
                ForwardProbeTask.Task.Clear();


            if (EarlyPool.Get().Detected)
            {
                tyr.NexusAbilityManager.Stopped = Count(UnitTypes.ZEALOT) == 0;
                tyr.NexusAbilityManager.PriotitizedAbilities.Add(916);
            }
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);

            if (EarlyPool.Get().Detected
                || MassZerglings)
                TimingAttackTask.Task.RequiredSize = 24;
            else
                TimingAttackTask.Task.RequiredSize = 12;
            TimingAttackTask.Task.RetreatSize = 0;
            
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.STALKER) + Completed(UnitTypes.IMMORTAL) >= 3;
            
            
            if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = NaturalDefensePos;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = EarlyPool.Get().Detected ? 50 : 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
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
