using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class OneBaseAdept : Build
    {
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 8 };

        public override string Name()
        {
            return "OneBaseAdept";
        }

        public override void OnStart(Tyr tyr)
        {
            tyr.TaskManager.Add(new DefenseTask());
            tyr.TaskManager.Add(attackTask);
            tyr.TaskManager.Add(new WorkerScoutTask());
            if (tyr.BaseManager.Pocket != null)
                tyr.TaskManager.Add(new ScoutProxyTask(tyr.BaseManager.Pocket.BaseLocation.Pos));
            MicroControllers.Add(new StutterController());
            
            Set += ProtossBuildUtil.Pylons();
            Set += MainBuildList();
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result += new BuildingStep(UnitTypes.GATEWAY);
            result += new BuildingStep(UnitTypes.ASSIMILATOR);
            result += new BuildingStep(UnitTypes.GATEWAY);
            result += new BuildingStep(UnitTypes.CYBERNETICS_CORE);
            result += new BuildingStep(UnitTypes.GATEWAY, 2);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        { }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 19 - Completed(UnitTypes.ASSIMILATOR))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                    && Minerals() >= 100
                    && Gas() >= 25)
                    agent.Order(922);
            }
        }
    }
}
