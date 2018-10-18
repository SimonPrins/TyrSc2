using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class WorkerRush : Build
    {
        private WorkerRushTask WorkerRushTask = new WorkerRushTask();
        private int LastReinforcementsFrame = 0;
        private bool MessageSent = false;

        public override string Name()
        {
            return "WorkerRush";
        }

        public override void OnStart(Tyr tyr)
        {
            tyr.TaskManager.Add(WorkerRushTask);
            tyr.TaskManager.Add(new FlyerAttackTask() { RequiredSize = 3 });
            tyr.TaskManager.Add(new ElevatorChaserTask());

            Set += ProtossBuildUtil.Pylons();
            Set += BuildPylonForPower();
            Set += BuildStargatesAgainstLifters();
        }

        private BuildList BuildPylonForPower()
        {
            BuildList result = new BuildList();

            result.If(() => { return Tyr.Bot.EnemyStrategyAnalyzer.LiftingDetected; });
            result.If(() => { return Minerals() >= 300
                    && Count(UnitTypes.STARGATE) < 2
                    && Completed(UnitTypes.CYBERNETICS_CORE) > 0
                    && Count(UnitTypes.PYLON) == Completed(UnitTypes.PYLON); });
            result += new BuildingStep(UnitTypes.PYLON, 5);

            return result;
        }

        private BuildList BuildStargatesAgainstLifters()
        {
            BuildList result = new BuildList();

            result.If(() => { return Tyr.Bot.EnemyStrategyAnalyzer.LiftingDetected; });
            result += new BuildingStep(UnitTypes.GATEWAY);
            result += new BuildingStep(UnitTypes.ASSIMILATOR);
            result += new BuildingStep(UnitTypes.CYBERNETICS_CORE);
            result += new BuildingStep(UnitTypes.ASSIMILATOR);
            result += new BuildingStep(UnitTypes.STARGATE, 2);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (tyr.EnemyStrategyAnalyzer.LiftingDetected)
            {
                int surfaceEnemies = 0;
                foreach (Unit unit in tyr.Enemies())
                    if (!unit.IsFlying)
                        surfaceEnemies++;

                if (surfaceEnemies < 3 && WorkerTask.Task.Units.Count < 16)
                {
                    WorkerRushTask.Clear();
                    WorkerRushTask.Stopped = true;
                }
            }

            if (!MessageSent)
                    if (tyr.Enemies().Count > 0)
                    {
                        MessageSent = true;
                        tyr.Chat("Prepare to be TICKLED! :D");
                    }

            if (tyr.Frame - LastReinforcementsFrame >= 100
                && WorkerTask.Task.Units.Count >= (tyr.EnemyStrategyAnalyzer.LiftingDetected ? 22 : 12)
                && !tyr.EnemyStrategyAnalyzer.LiftingDetected)
            {
                LastReinforcementsFrame = tyr.Frame;
                WorkerRushTask.TakeWorkers += 6;
            }
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && (!WorkerRushTask.Stopped || Count(UnitTypes.PROBE) < 20))
                agent.Order(1006);

            if (agent.Unit.UnitType == UnitTypes.STARGATE
                && Minerals() >= 250
                && Gas() >= 150
                && FoodUsed() + 4 <= 200)
                agent.Order(950);
        }
    }
}
