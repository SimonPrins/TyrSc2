﻿using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.BuildingPlacement;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class TempestProxy : Build
    {
        public bool UseCloseHideLocation = true;
        public bool DefendingStalker = false;
        private WallInCreator WallIn;
        private Point2D ShieldBatteryPos;
        private int DepoweredStargates = 0;
        private TempestController TempestController = new TempestController();
        private Point2D HideLocation;

        public override string Name()
        {
            return "TempestProxy";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            ArmyObserverTask.Enable();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            DefendRegionTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ProxyTask.Enable(new List<ProxyBuilding>() {
                new ProxyBuilding() { UnitType = UnitTypes.PYLON },
                new ProxyBuilding() { UnitType = UnitTypes.STARGATE, Number = 1 , Test = () => Count(UnitTypes.CYBERNETICS_CORE) > 0},
                new ProxyBuilding() { UnitType = UnitTypes.PHOTON_CANNON, Number = 1, Test = () => CollectionUtil.Get(ProxyTask.Task.UnitCounts, UnitTypes.STARGATE) > 0 },
                new ProxyBuilding() { UnitType = UnitTypes.STARGATE, Number = 2, Test = () => Count(UnitTypes.FLEET_BEACON) > 0 },
                new ProxyBuilding() { UnitType = UnitTypes.PHOTON_CANNON, Number = 3 , Test = () => Minerals() >= 150 && Count(UnitTypes.FLEET_BEACON) > 0 && (Count(UnitTypes.TEMPEST) > 0 || Minerals() >= 600) },
                new ProxyBuilding() { UnitType = UnitTypes.PYLON, Number = 2 , Test = () => Minerals() >= 400 && Count(UnitTypes.PHOTON_CANNON) >= 3 },
                new ProxyBuilding() { UnitType = UnitTypes.SHIELD_BATTERY, Number = 3 , Test = () => Minerals() >= 400 && Count(UnitTypes.TEMPEST) > 0 },
                //new ProxyBuilding() { UnitType = UnitTypes.STARGATE, Number = 1, Test = () => Count(UnitTypes.TEMPEST) >= 2 }
            }, true);
        }

        public override void OnStart(Bot bot)
        {
            ProxyTask.Task.UseCloseHideLocation = UseCloseHideLocation;

            MicroControllers.Add(TempestController);
            MicroControllers.Add(new StutterController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY });
                WallIn.ReserveSpace();
                ShieldBatteryPos = SC2Util.TowardCardinal(WallIn.Wall[1].Pos, Main.BaseLocation.Pos, 2);
                Bot.Main.buildingPlacer.ReservedLocation.Add(new ReservedBuilding() { Type = UnitTypes.SHIELD_BATTERY, Pos = ShieldBatteryPos });
            }
            
            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) >= 2);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 21);
            if (DefendingStalker)
                result.Train(UnitTypes.STALKER, 1, () => Count(UnitTypes.STARGATE) > 0);
            result.Train(UnitTypes.TEMPEST);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            result.If(() => Count(UnitTypes.GATEWAY) > 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.FORGE);
            result.Building(UnitTypes.PHOTON_CANNON, Main, MainDefensePos, () => !DefendingStalker || Count(UnitTypes.FLEET_BEACON) > 0);
            result.Building(UnitTypes.STARGATE, 2, () => Bot.Main.Frame >= 22.4 * 60 * 4);
            result.Building(UnitTypes.STARGATE, 1, () => ProxyTask.Task.Units.Count == 0 && DepoweredStargates >= 1);
            result.Building(UnitTypes.STARGATE, 1, () => ProxyTask.Task.Units.Count == 0 && DepoweredStargates >= 2);
            //result.Building(UnitTypes.SHIELD_BATTERY, Main, ShieldBatteryPos, true, () => Minerals() >= 400);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.PYLON, Main, ShieldBatteryPos, true, () => Minerals() >= 400 && Bot.Main.Frame >= 22.4 * 60 * 5);
            result.Building(UnitTypes.PHOTON_CANNON, Main, MainDefensePos, 2, () => Minerals() >= 400 && Bot.Main.Frame >= 22.4 * 60 * 5);
            result.Building(UnitTypes.SHIELD_BATTERY, Main, MainDefensePos, 2, () => Minerals() >= 400 && Bot.Main.Frame >= 22.4 * 60 * 5);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (HideLocation == null)
                HideLocation = ProxyTask.Task.GetHideLocation();
            if (HideLocation != null)
            {
                if (ProxyTask.Task.UnitCounts.ContainsKey(UnitTypes.PYLON)
                    && ProxyTask.Task.UnitCounts[UnitTypes.PYLON] > 0
                    && ProxyTask.Task.UnitCounts.ContainsKey(UnitTypes.STARGATE)
                    && ProxyTask.Task.UnitCounts[UnitTypes.STARGATE] > 0)
                {
                    float dist = 10 * 10;
                    Unit fleeEnemy = null;
                    foreach (Unit enemy in bot.Enemies())
                    {
                        if (!UnitTypes.CanAttackAir(enemy.UnitType))
                            continue;
                        float newDist = SC2Util.DistanceSq(enemy.Pos, HideLocation);
                        if (newDist > dist)
                            continue;
                        dist = newDist;
                        fleeEnemy = enemy;
                    }
                    if (fleeEnemy != null)
                        TempestController.RetreatPos = new PotentialHelper(HideLocation, 6).From(fleeEnemy.Pos).Get();
                    else
                        TempestController.RetreatPos = HideLocation;
                }
                else TempestController.RetreatPos = null;
            }
            if (TempestController.RetreatPos == null)
                TempestController.RetreatPos = ProxyTask.Task.GetHideLocation();

            DepoweredStargates = 0;
            foreach (Agent agent in bot.Units())
                if (agent.Unit.UnitType == UnitTypes.STARGATE
                    && !agent.Unit.IsPowered
                    && agent.Unit.BuildProgress >= 0.99)
                    DepoweredStargates++;
            bot.DrawText("DepoweredStargates: " + DepoweredStargates);

            bot.NexusAbilityManager.PriotitizedAbilities.Add(1568);
            ProxyTask.Task.EvadeEnemies = true;
            
            bot.buildingPlacer.BuildCompact = true;
            bot.TargetManager.PrefferDistant = false;
            bot.TargetManager.TargetAllBuildings = true;


            TrainStep.WarpInLocation = ProxyTask.Task.GetHideLocation();
            DefendRegionTask.Task.DefenseLocation = ProxyTask.Task.GetHideLocation();
            
            
            TimingAttackTask.Task.RequiredSize = 1;
            TimingAttackTask.Task.RetreatSize = 0;
            TimingAttackTask.Task.UnitType = UnitTypes.TEMPEST;


            if (bot.Frame >= 22.4 * 60 * 4)
                ProxyTask.Task.Stopped = true;
            else
            {
                ProxyTask.Task.Stopped = Count(UnitTypes.GATEWAY) == 0;
                if (ProxyTask.Task.Stopped)
                    ProxyTask.Task.Clear();
            }
            if (UpgradeType.LookUp[UpgradeType.WarpGate].Progress() >= 0.5 
                && IdleTask.Task.OverrideTarget == null
                && (bot.EnemyRace != Race.Protoss || bot.Frame >= 22.4 * 4 * 60))
                IdleTask.Task.OverrideTarget = bot.MapAnalyzer.Walk(ProxyTask.Task.GetHideLocation(), bot.MapAnalyzer.EnemyDistances, 10);

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;
                
                agent.Order(Abilities.MOVE, agent.From(bot.MapAnalyzer.GetMainRamp(), 4));
            }
        }
    }
}
