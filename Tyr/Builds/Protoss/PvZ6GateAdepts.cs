using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class PvZ6GateAdepts : Build
    {
        private Point2D OverrideDefenseTarget;
        private WallInCreator WallIn;
        private Point2D ShieldBatteryPos;
        private bool AttackMessageSent = false;

        public override string Name()
        {
            return "PvZ6GateAdepts";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            ArmyObserverTask.Enable();
            HodorTask.Enable();
            WarpPrismTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(new HTController());
            MicroControllers.Add(new AdeptPhaseEnemyMainController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new SoftLeashController(UnitTypes.ZEALOT, new HashSet<uint>() { UnitTypes.IMMORTAL, UnitTypes.ARCHON }, 6));
            MicroControllers.Add(new SoftLeashController(UnitTypes.ARCHON, UnitTypes.IMMORTAL, 6));
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new AdeptKillWorkersController());
            MicroControllers.Add(new AdeptKillWorkersController() { TargetTypes = new HashSet<uint> { UnitTypes.ZERGLING } });
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

            Base third = null;
            float dist = 1000000;
            foreach (Base b in tyr.BaseManager.Bases)
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

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) >= 4
                && (Count(UnitTypes.CYBERNETICS_CORE) > 0 || EarlyPool.Get().Detected)
                && (Count(UnitTypes.GATEWAY) >= 2 || !EarlyPool.Get().Detected));
            Set += Units();
            Set += MainBuild();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 19);
            result.Train(UnitTypes.PROBE, 30, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.PROBE, 45, () => Count(UnitTypes.NEXUS) >= 3);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 4);
            result.Train(UnitTypes.PROBE, 80, () => Count(UnitTypes.NEXUS) >= 5);
            result.Train(UnitTypes.STALKER, 1);
            result.Upgrade(UpgradeType.ResonatingGlaives);
            result.Train(UnitTypes.WARP_PRISM, 1);
            result.Train(UnitTypes.OBSERVER, 1);
            result.If(() => Count(UnitTypes.ROBOTICS_FACILITY) > 0);
            result.Train(UnitTypes.ADEPT, 3);
            result.Train(UnitTypes.ADEPT, () => Completed(UnitTypes.WARP_PRISM) == 0);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.If(() => Count(UnitTypes.PROBE) >= 13);
            if (WallIn.Wall.Count >= 5)
            {
                result.Building(UnitTypes.PYLON, Natural, WallIn.Wall[4].Pos, true);
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) > 0 && Count(UnitTypes.PROBE) >= 16);
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[3].Pos, true, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
                //result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[1].Pos, true, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
            } else
            {
                result.Building(UnitTypes.PYLON);
                result.Building(UnitTypes.GATEWAY, () => Completed(UnitTypes.PYLON) > 0 && Count(UnitTypes.PROBE) >= 16);
                result.Building(UnitTypes.GATEWAY, () => Completed(UnitTypes.PYLON) > 0 && EarlyPool.Get().Detected);
            }
            result.If(() => Count(UnitTypes.GATEWAY) > 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.NEXUS, () => (!EarlyPool.Get().Detected || Completed(UnitTypes.ADEPT) + Completed(UnitTypes.ZEALOT) >= 3) && Count(UnitTypes.PROBE) >= 19);
            result.If(() => Count(UnitTypes.NEXUS) >= 2);
            if (WallIn.Wall.Count >= 5)
                result.Building(UnitTypes.CYBERNETICS_CORE, Natural, WallIn.Wall[1].Pos, true);
            else
                result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON);
            result.If(() => Count(UnitTypes.STALKER) > 0);
            result.Upgrade(UpgradeType.WarpGate);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            if (WallIn.Wall.Count >= 5)
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[3].Pos, true, () => !EarlyPool.Get().Detected);
            else
                result.Building(UnitTypes.GATEWAY, () => !EarlyPool.Get().Detected);
            result.Building(UnitTypes.GATEWAY, Main, 2);
            result.Building(UnitTypes.GATEWAY, Main, 2, () => Count(UnitTypes.WARP_PRISM) > 0);
            result.Building(UnitTypes.PYLON, () => Count(UnitTypes.GATEWAY) >= 4);
            result.Building(UnitTypes.PYLON, () => Count(UnitTypes.GATEWAY) >= 5 && Count(UnitTypes.ADEPT) >= 3);

            return result;
        }
        
        private Point2D DetermineShieldBatteryPos()
        {
            if (WallIn.Wall.Count < 5)
                return null;

            if (Bot.Bot.Map == MapEnum.DiscoBloodbath
                && Main.BaseLocation.Pos.X <= 100)
                return new Point2D() { X = WallIn.Wall[4].Pos.X, Y = WallIn.Wall[4].Pos.Y + 2 };

            if (Bot.Bot.Map == MapEnum.Zen)
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
            if (Bot.Bot.buildingPlacer.CheckPlacement(pos, SC2Util.Point(2, 2), UnitTypes.PYLON, null, true))
                return pos;
            return null;
        }

        public override void OnFrame(Bot tyr)
        {
            WarpPrismTask.Task.ArmyUnitTypes.Add(UnitTypes.ADEPT);

            if (tyr.Frame == (int)(22.4 * 60 * 3))
                tyr.Chat("Use your stalker to clear the scouting overlord from the third if it is there.");
            if (!AttackMessageSent && Completed(UnitTypes.WARP_PRISM) > 0)
            {
                AttackMessageSent = true;
                tyr.Chat("Send your adepts straight to the enemy base.");
            }

            if (!WarpPrismTask.Task.WarpInObjectiveSet()
                && TimingAttackTask.Task.Units.Count > 0
                && Bot.Bot.Frame % 22 == 0)
            {
                int warpInsReady = 0;
                RequestQuery query = new RequestQuery();
                foreach (Agent agent in tyr.Units())
                    if (agent.Unit.UnitType == UnitTypes.WARP_GATE)
                        query.Abilities.Add(new RequestQueryAvailableAbilities() { UnitTag = agent.Unit.Tag });
                Task<ResponseQuery> task = tyr.GameConnection.SendQuery(query);
                task.Wait();
                ResponseQuery response = task.Result;
                foreach (ResponseQueryAvailableAbilities availableAbilities in response.Abilities)
                {
                    foreach (AvailableAbility ability in availableAbilities.Abilities)
                    {
                        if (ability.AbilityId == TrainingType.LookUp[UnitTypes.ZEALOT].WarpInAbility)
                            warpInsReady++;
                    }
                }
                if (Completed(UnitTypes.WARP_GATE) == warpInsReady)
                {
                    int desiredAdepts = Math.Min(Minerals() / 100, Math.Min(Completed(UnitTypes.WARP_GATE), 10 - Count(UnitTypes.ZEALOT)));
                    if (desiredAdepts > 0)
                        WarpPrismTask.Task.AddWarpInObjective(UnitTypes.ADEPT, desiredAdepts);
                }
            }

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

            if (Count(UnitTypes.STALKER) > 0)
                BalanceGas();
            else
                GasWorkerTask.WorkersPerGas = 2;

            bool gatewayExists = false;
            foreach (Agent agent in Bot.Bot.Units())
                if (agent.Unit.UnitType == UnitTypes.GATEWAY
                    || agent.Unit.UnitType == UnitTypes.WARP_GATE)
                    gatewayExists = true;

            WorkerScoutTask.Task.StartFrame = 2240;
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

            HodorTask.Task.Stopped = Count(UnitTypes.NEXUS) >= 3 
                || TimingAttackTask.Task.Units.Count > 0
                || wallDone < WallIn.Wall.Count
                || Completed(UnitTypes.HIGH_TEMPLAR) + Count(UnitTypes.ARCHON) > 0
                || (Count(UnitTypes.ADEPT) >= 2 && !AdeptHarassMainTask.Task.Sent)
                || (EnemyCount(UnitTypes.ZERGLING) == 0 && EnemyCount(UnitTypes.ROACH) > 0
                || EnemyCount(UnitTypes.NYDUS_CANAL) > 0);
            if (HodorTask.Task.Stopped)
                HodorTask.Task.Clear();

            WorkerScoutTask.Task.StopAndClear(WorkerScoutTask.Task.BaseCircled());

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

                if (Count(UnitTypes.NEXUS) < 3 
                    && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Natural.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }
            if (EarlyPool.Get().Detected)
            {
                tyr.NexusAbilityManager.Stopped = Count(UnitTypes.ZEALOT) == 0;
                tyr.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.ZEALOT].Ability);
            }
            else
            {
                tyr.NexusAbilityManager.OnlyChronoPrioritizedUnits = Completed(UnitTypes.WARP_PRISM) == 0;
                tyr.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.WARP_PRISM].Ability);
                tyr.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.STALKER].Ability);
                tyr.NexusAbilityManager.PriotitizedAbilities.Add(UpgradeType.LookUp[UpgradeType.ResonatingGlaives].Ability);
            }

            if (Completed(UnitTypes.WARP_PRISM) > 0)
                TimingAttackTask.Task.RequiredSize = 4;
            else
                TimingAttackTask.Task.RequiredSize = 30;
            TimingAttackTask.Task.RetreatSize = 0;
            
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.ADEPT) >= 2;


            if (Count(UnitTypes.NEXUS) >= 3
                || (Completed(UnitTypes.HIGH_TEMPLAR) + Count(UnitTypes.ARCHON) > 0))
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = NaturalDefensePos;

            ArchonMergeTask.Task.MergePos = OverrideDefenseTarget;

            DefenseTask.GroundDefenseTask.BufferZone = 5;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = EarlyPool.Get().Detected ? 50 : 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;
            

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;
        }
    }
}
