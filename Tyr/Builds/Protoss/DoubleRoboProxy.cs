using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class DoubleRoboProxy : Build
    {
        public int RequiredSize = 10;
        public bool DropInMain = true;
        private bool DefendReapers = false;
        private DefenseSquadTask ReaperDefenseTask;
        private DefenseSquadTask DefendProxyTask;
        StalkerAttackNaturalController StalkerAttackNaturalController = new StalkerAttackNaturalController();
        private StutterController StutterController = new StutterController();

        private int RequiredImmortals = 4;

        public override string Name()
        {
            return "DoubleRoboProxy";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            ArmyObserverTask.Enable();
            DefenseTask.Enable();
            //TimingAttackTask.Enable();
            if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ProxyTask.Enable(new List<ProxyBuilding>() {
                new ProxyBuilding() { UnitType = UnitTypes.PYLON },
                new ProxyBuilding() { UnitType = UnitTypes.ROBOTICS_FACILITY, Number = 2 }
            });
            if (DefendProxyTask == null)
            {
                DefendProxyTask = new DefenseSquadTask(Main);
                DefendProxyTask.DraftFromFarAway = true;
                DefendProxyTask.AlwaysNeeded = true;
                DefendProxyTask.MaxDefenders = 1000000;
                DefendProxyTask.Priority = 3;
            }
            DefenseSquadTask.Enable(DefendProxyTask);
            ReaperDefenseTask = new DefenseSquadTask(Main, UnitTypes.STALKER);
            ReaperDefenseTask.MaxDefenders = 0;
            DefenseSquadTask.Enable(ReaperDefenseTask);
            WarpPrismElevatorTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(StutterController);
            MicroControllers.Add(StalkerAttackNaturalController);

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) >= 2);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 21);
            result.Train(UnitTypes.IMMORTAL, 2);
            //if (Tyr.Bot.EnemyRace == Race.Terran)
            //    result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.IMMORTAL, 3);
            result.Train(UnitTypes.WARP_PRISM, 1, () => DropInMain);
            result.Train(UnitTypes.IMMORTAL, 4);
            //if (Tyr.Bot.EnemyRace != Race.Terran)
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.STALKER, 2);
            result.Train(UnitTypes.IMMORTAL, 6);
            result.Train(UnitTypes.STALKER, 4);
            result.Train(UnitTypes.IMMORTAL);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON, Main, () => Count(UnitTypes.PROBE) >= 13);
            result.Building(UnitTypes.GATEWAY, Main);
            result.If(() => Count(UnitTypes.GATEWAY) > 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.ROBOTICS_FACILITY) >= 2);
            result.Building(UnitTypes.ASSIMILATOR);

            return result;
        }

        bool printed = false;
        public override void OnFrame(Bot tyr)
        {
            if (Completed(UnitTypes.WARP_PRISM) == 0
                && (Count(UnitTypes.WARP_PRISM) == 1 || Count(UnitTypes.IMMORTAL) >= 3))
                tyr.NexusAbilityManager.PriotitizedAbilities.Remove(TrainingType.LookUp[UnitTypes.IMMORTAL].Ability);
            else
                tyr.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.IMMORTAL].Ability);
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.WARP_PRISM].Ability);
            ProxyTask.Task.UseCloseHideLocation = true;

            tyr.buildingPlacer.BuildCompact = true;
            tyr.TargetManager.PrefferDistant = false;
            tyr.TargetManager.TargetAllBuildings = true;

            Agent warpPrismPhasing = null;
            foreach (Agent agent in tyr.Units())
                if (agent.Unit.UnitType == UnitTypes.WARP_PRISM_PHASING)
                    warpPrismPhasing = agent;
            if (warpPrismPhasing != null)
                TrainStep.WarpInLocation = SC2Util.To2D(warpPrismPhasing.Unit.Pos);
            else
                TrainStep.WarpInLocation = ProxyTask.Task.GetHideLocation();
            
            if (!printed)
            foreach (Agent agent in Bot.Bot.Units())
            {
                if (agent.Unit.UnitType != UnitTypes.PYLON)
                    continue;
                if (agent.Unit.BuildProgress < 1)
                    continue;
                if (agent.DistanceSq(TrainStep.WarpInLocation) >= 30 * 30)
                    continue;
                printed = true;
            }


            if (StutterController.Toward == null
                && tyr.TargetManager.PotentialEnemyStartLocations.Count == 1
                && Completed(UnitTypes.WARP_PRISM) > 0)
            {
                Point2D enemyBaseCenter = new PotentialHelper(tyr.TargetManager.PotentialEnemyStartLocations[0], 12).To(WarpPrismElevatorTask.Task.StagingArea).Get();
                StutterController.Toward = new PotentialHelper(enemyBaseCenter, 6).From(tyr.MapAnalyzer.GetEnemyRamp()).Get();
            }


            if (Completed(UnitTypes.IMMORTAL) < RequiredImmortals)
            {
                TimingAttackTask.Task.RequiredSize = 8;
                TimingAttackTask.Task.RetreatSize = 4;
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = 4;
                TimingAttackTask.Task.RetreatSize = 0;
            }

            ProxyTask.Task.Stopped = Count(UnitTypes.GATEWAY) == 0;
            if (ProxyTask.Task.Stopped)
                ProxyTask.Task.Clear();

            if (DefendProxyTask.Base == Main)
            {
                Point2D proxyLocation = ProxyTask.Task.GetHideLocation();
                if (proxyLocation != null)
                {
                    DefendProxyTask.OverrideDefenseLocation = proxyLocation;
                    DefendProxyTask.OverrideIdleLocation = proxyLocation;
                    foreach (Base b in tyr.BaseManager.Bases)
                        if (SC2Util.DistanceSq(b.BaseLocation.Pos, proxyLocation) <= 20 * 20)
                            DefendProxyTask.Base = b;
                }
            }

            if (UpgradeType.LookUp[UpgradeType.WarpGate].Progress() >= 0.5
                && IdleTask.Task.OverrideTarget == null
                && (tyr.EnemyRace != Race.Protoss || tyr.Frame >= 22.4 * 4 * 60))
                DefendProxyTask.Stopped = false;
            else
                DefendProxyTask.Stopped = AdeptHarass.Get().DetectedPreviously;

            tyr.DrawText("DefendProxyTask.Stopped: " + DefendProxyTask.Stopped);

            if (DefendProxyTask.Stopped)
                DefendProxyTask.Clear();

            if (DropInMain &&
                (Completed(UnitTypes.WARP_PRISM) > 0 || tyr.Frame >= 22.4 * 6 * 60))
            {
                WarpPrismElevatorTask.Task.Stopped = false;
            }
            else
            {
                WarpPrismElevatorTask.Task.Stopped = true;
                WarpPrismElevatorTask.Task.Clear();
            }
            
            

            if (Completed(UnitTypes.OBSERVER) > 0)
                StalkerAttackNaturalController.Stopped = true;

            if (!DefendReapers && tyr.EnemyStrategyAnalyzer.Count(UnitTypes.REAPER) > 0 && UpgradeType.LookUp[UpgradeType.WarpGate].Done())
            {
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.REAPER)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 30 * 30)
                    {
                        DefendReapers = true;
                        ReaperDefenseTask.MaxDefenders = 1;
                        break;
                    }
                }
            }
        }
    }
}
