using Tyr.Agents;

namespace Tyr.Builds.Protoss
{
    public class Expand : Build
    {
        public override string Name()
        {
            return "Expand";
        }

        public override void OnStart(Tyr tyr)
        {
        }

        public override void OnFrame(Tyr tyr)
        {
            Construct(UnitTypes.ASSIMILATOR);
            Construct(UnitTypes.NEXUS);
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 16)
                agent.Order(1006);
        }
    }
}
