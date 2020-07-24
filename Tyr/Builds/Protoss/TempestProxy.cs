using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Builds.BuildLists;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
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
        private bool ChatMessageSent = false;

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
            if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Bot.BaseManager.Pocket.BaseLocation.Pos);
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

        public override void OnStart(Bot tyr)
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
                Bot.Bot.buildingPlacer.ReservedLocation.Add(new ReservedBuilding() { Type = UnitTypes.SHIELD_BATTERY, Pos = ShieldBatteryPos });
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
            result.Building(UnitTypes.STARGATE, 2, () => Bot.Bot.Frame >= 22.4 * 60 * 4);
            result.Building(UnitTypes.STARGATE, 1, () => ProxyTask.Task.Units.Count == 0 && DepoweredStargates >= 1);
            result.Building(UnitTypes.STARGATE, 1, () => ProxyTask.Task.Units.Count == 0 && DepoweredStargates >= 2);
            //result.Building(UnitTypes.SHIELD_BATTERY, Main, ShieldBatteryPos, true, () => Minerals() >= 400);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.PYLON, Main, ShieldBatteryPos, true, () => Minerals() >= 400 && Bot.Bot.Frame >= 22.4 * 60 * 5);
            result.Building(UnitTypes.PHOTON_CANNON, Main, MainDefensePos, 2, () => Minerals() >= 400 && Bot.Bot.Frame >= 22.4 * 60 * 5);
            result.Building(UnitTypes.SHIELD_BATTERY, Main, MainDefensePos, 2, () => Minerals() >= 400 && Bot.Bot.Frame >= 22.4 * 60 * 5);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (tyr.Frame == (int)(22.4 * 60) && LogLabel.FoundMM)
                tyr.Chat("These Tempests are perfectly balanced. As all things should be.");
            if (!ChatMessageSent)
            {
                if (Completed(UnitTypes.TEMPEST) > 0 && !LogLabel.FoundMM)
                {
                    tyr.Chat("This build is dedicated to Andyman!");
                    ChatMessageSent = true;
                }
            }

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
                    foreach (Unit enemy in tyr.Enemies())
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
            foreach (Agent agent in tyr.Units())
                if (agent.Unit.UnitType == UnitTypes.STARGATE
                    && !agent.Unit.IsPowered
                    && agent.Unit.BuildProgress >= 0.99)
                    DepoweredStargates++;
            tyr.DrawText("DepoweredStargates: " + DepoweredStargates);

            tyr.NexusAbilityManager.PriotitizedAbilities.Add(1568);
            ProxyTask.Task.EvadeEnemies = true;
            
            tyr.buildingPlacer.BuildCompact = true;
            tyr.TargetManager.PrefferDistant = false;
            tyr.TargetManager.TargetAllBuildings = true;


            TrainStep.WarpInLocation = ProxyTask.Task.GetHideLocation();
            DefendRegionTask.Task.DefenseLocation = ProxyTask.Task.GetHideLocation();
            
            
            TimingAttackTask.Task.RequiredSize = 1;
            TimingAttackTask.Task.RetreatSize = 0;
            TimingAttackTask.Task.UnitType = UnitTypes.TEMPEST;


            if (tyr.Frame >= 22.4 * 60 * 4)
                ProxyTask.Task.Stopped = true;
            else
            {
                ProxyTask.Task.Stopped = Count(UnitTypes.GATEWAY) == 0;
                if (ProxyTask.Task.Stopped)
                    ProxyTask.Task.Clear();
            }
            if (UpgradeType.LookUp[UpgradeType.WarpGate].Progress() >= 0.5 
                && IdleTask.Task.OverrideTarget == null
                && (tyr.EnemyRace != Race.Protoss || tyr.Frame >= 22.4 * 4 * 60))
                IdleTask.Task.OverrideTarget = tyr.MapAnalyzer.Walk(ProxyTask.Task.GetHideLocation(), tyr.MapAnalyzer.EnemyDistances, 10);

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (tyr.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;
                
                agent.Order(Abilities.MOVE, agent.From(tyr.MapAnalyzer.GetMainRamp(), 4));
            }
        }
    }
}
