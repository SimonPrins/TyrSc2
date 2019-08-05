using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class ZealotRushProxy : Build
    {
        public int RequiredSize = 4;

        public override string Name()
        {
            return "ZealotRushProxy";
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
            ProxyTwoGateTask.Enable();
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new StutterController());

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
            result.Building(UnitTypes.GATEWAY, Main);
            result.Train(UnitTypes.ZEALOT);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            TimingAttackTask.Task.RequiredSize = RequiredSize;
            TimingAttackTask.Task.RetreatSize = 0;
            ProxyTwoGateTask.Task.Stopped = Count(UnitTypes.GATEWAY) == 0;
            if (ProxyFourGateTask.Task.Stopped)
                ProxyFourGateTask.Task.Clear();
            
            IdleTask.Task.OverrideTarget = tyr.MapAnalyzer.Walk(ProxyFourGateTask.Task.GetHideLocation(), tyr.MapAnalyzer.EnemyDistances, 10);
            IdleTask.Task.AttackMove = tyr.Frame <= 22.4 * 60 * 4.5;
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 17)
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
        }
    }
}
