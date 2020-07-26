using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
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
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ProxyTwoGateTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new StutterController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() => Bot.Main.Frame >= 22.4 * 90);
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

        public override void OnFrame(Bot bot)
        {
            TimingAttackTask.Task.RequiredSize = RequiredSize;
            TimingAttackTask.Task.RetreatSize = 0;
            //ProxyTwoGateTask.Task.Stopped = Count(UnitTypes.GATEWAY) == 0;
            if (ProxyFourGateTask.Task.Stopped)
                ProxyFourGateTask.Task.Clear();

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }

            IdleTask.Task.OverrideTarget = bot.MapAnalyzer.Walk(ProxyFourGateTask.Task.GetHideLocation(), bot.MapAnalyzer.EnemyDistances, 10);
            IdleTask.Task.AttackMove = bot.Frame <= 22.4 * 60 * 4.5;
        }

        public override void Produce(Bot bot, Agent agent)
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
