using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class StealUnitTask : Task
    {
        private uint UnitType = UnitTypes.ADEPT;
        public StealUnitTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitType && units.Count == 0;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.Frame >= 800;
        }

        public override void Add(Agent agent)
        {
            base.Add(agent);
            agent.Order(Abilities.MOVE, SC2Util.Point(Bot.Main.GameInfo.StartRaw.MapSize.X / 2, Bot.Main.GameInfo.StartRaw.MapSize.Y / 2));
        }

        public override void OnFrame(Bot tyr)
        {
        }
    }
}
