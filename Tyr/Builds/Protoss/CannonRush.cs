using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class CannonRush : Build
    {
        public override string Name()
        {
            return "CannonRush";
        }

        public override void OnStart(Tyr tyr)
        {
            tyr.TaskManager.Add(new DefenseTask());
            tyr.TaskManager.Add(new CannonRushTask());

            Set += MainBuild();
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.FORGE);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        { }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 18
                && Count(UnitTypes.PYLON) > 0)
            {
                agent.Order(1006);
            }
        }
    }
}
