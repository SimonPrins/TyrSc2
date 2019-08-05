using SC2APIProtocol;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class FourGateProxy : Build
    {
        public int RequiredSize = 10;
        private bool DefendReapers = false;
        private DefenseSquadTask ReaperDefenseTask;
        StalkerAttackNaturalController StalkerAttackNaturalController = new StalkerAttackNaturalController();

        public override string Name()
        {
            return "FourGateProxy";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            ArmyObserverTask.Enable();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Tyr.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Tyr.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ProxyFourGateTask.Enable();
            ReaperDefenseTask = new DefenseSquadTask(Main, UnitTypes.STALKER);
            ReaperDefenseTask.MaxDefenders = 0;
            DefenseSquadTask.Enable(ReaperDefenseTask);
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(StalkerAttackNaturalController);
            //MicroControllers.Add(new TargetRepairingSCVController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons();
            Set += MainBuildList();
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON, Main, () => Count(UnitTypes.PROBE) >= 13);
            result.Building(UnitTypes.GATEWAY, Main);
            result.If(() => Count(UnitTypes.GATEWAY) > 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Upgrade(UpgradeType.WarpGate);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.ASSIMILATOR);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(1568);
            if (Completed(UnitTypes.OBSERVER) > 0
                && Completed(UnitTypes.IMMORTAL) == 0
                && Count(UnitTypes.IMMORTAL) > 0)
            {
                TimingAttackTask.Task.RequiredSize = 20;
                TimingAttackTask.Task.RetreatSize = 4;
            }
            else if (Completed(UnitTypes.OBSERVER) == 0
                && Count(UnitTypes.ROBOTICS_FACILITY) > 0)
            {
                TimingAttackTask.Task.RequiredSize = 20;
                TimingAttackTask.Task.RetreatSize = 4;
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = RequiredSize;
                TimingAttackTask.Task.RetreatSize = 4;
            }
            ProxyFourGateTask.Task.Stopped = Count(UnitTypes.CYBERNETICS_CORE) == 0;
            if (ProxyFourGateTask.Task.Stopped)
                ProxyFourGateTask.Task.Clear();
            if (UpgradeType.LookUp[UpgradeType.WarpGate].Progress() >= 0.5 && IdleTask.Task.OverrideTarget == null)
                IdleTask.Task.OverrideTarget = tyr.MapAnalyzer.Walk(ProxyFourGateTask.Task.GetHideLocation(), tyr.MapAnalyzer.EnemyDistances, 10);
            IdleTask.Task.AttackMove = tyr.Frame <= 22.4 * 60 * 4.5;

            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.WIDOW_MINE) >= 2)
                ProxyFourGateTask.Task.BuildRobo = true;

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

        private bool RobosActive()
        {
            int robos = Completed(UnitTypes.ROBOTICS_FACILITY);
            int alreadyBuilt = Completed(UnitTypes.OBSERVER) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.WARP_PRISM) + Completed(UnitTypes.DISRUPTOR);
            int total = Count(UnitTypes.OBSERVER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.WARP_PRISM) + Count(UnitTypes.DISRUPTOR);
            return total - alreadyBuilt >= robos;
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 23 - Completed(UnitTypes.ASSIMILATOR))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                    && Minerals() >= 125
                    && Gas() >= 50
                    && UpgradeType.LookUp[UpgradeType.WarpGate].Progress() < 0.7
                    && Count(UnitTypes.STALKER) < 10 || UpgradeType.LookUp[UpgradeType.Blink].Done())
                    agent.Order(917);
            }
            else if (agent.Unit.UnitType == UnitTypes.WARP_GATE)
            {
                if (ProxyFourGateTask.Task.BuildRobo && Count(UnitTypes.ROBOTICS_FACILITY) == 0 && (Minerals() < 300 || Gas() < 150))
                    return;
                if (Completed(UnitTypes.ROBOTICS_FACILITY) > 0 && !RobosActive() && (Minerals() < 350 || Gas() < 150) && (Count(UnitTypes.IMMORTAL) < 2 || Count(UnitTypes.STALKER) >= 7))
                    return;
                if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                    && Minerals() >= 125
                    && Gas() >= 50
                    && DefendReapers
                    && ReaperDefenseTask.Units.Count == 0)
                {
                    Point2D aroundTile = SC2Util.To2D(tyr.MapAnalyzer.StartLocation);
                    Point2D placement = WarpInPlacer.FindPlacement(aroundTile, UnitTypes.STALKER);
                    if (placement != null)
                        agent.Order(1414, placement);
                }
                else if (Completed(UnitTypes.STALKER) >= 4
                    && Minerals() >= 100
                    && Tyr.Bot.EnemyRace == Race.Zerg
                    && !Roach.Get().DetectedPreviously
                    && Count(UnitTypes.ZEALOT) < Count(UnitTypes.STALKER))
                {
                    Point2D aroundTile = ProxyFourGateTask.Task.GetHideLocation();
                    if (aroundTile == null)
                        aroundTile = SC2Util.To2D(tyr.MapAnalyzer.StartLocation);
                    Point2D placement = WarpInPlacer.FindPlacement(aroundTile, UnitTypes.ZEALOT);
                    if (placement != null)
                        agent.Order(1413, placement);
                }
                else if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                    && Minerals() >= 125
                    && Gas() >= 50)
                {
                    Point2D aroundTile = ProxyFourGateTask.Task.GetHideLocation();
                    if (aroundTile == null)
                        aroundTile = SC2Util.To2D(tyr.MapAnalyzer.StartLocation);
                    Point2D placement = WarpInPlacer.FindPlacement(aroundTile, UnitTypes.STALKER);
                    if (placement != null)
                        agent.Order(1414, placement);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_FACILITY)
            {
                if (Count(UnitTypes.OBSERVER) == 0
                    && Minerals() >= 25
                    && Gas() >= 75
                    && FoodLeft() >= 1)
                {
                    agent.Order(977);
                }
                else if (Minerals() >= 275
                    && Gas() >= 100
                    && FoodLeft() >= 4)
                {
                    agent.Order(979);
                }
            }
        }
    }
}
