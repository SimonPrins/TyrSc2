using SC2Sharp.Agents;

namespace SC2Sharp.Builds.Protoss
{
    public class Expand : Build
    {
        public override string Name()
        {
            return "Expand";
        }

        public override void OnStart(Bot bot)
        {
        }

        public override void OnFrame(Bot bot)
        {
            Construct(UnitTypes.ASSIMILATOR);
            Construct(UnitTypes.NEXUS);
        }

        public override void Produce(Bot bot, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 16)
                agent.Order(1006);
        }
    }
}
