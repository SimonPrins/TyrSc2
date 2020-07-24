using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class RemoveLostWorkersTask : Task
    {
        public RemoveLostWorkersTask() : base(5)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && !Bot.Bot.MapAnalyzer.MainAndPocketArea[SC2Util.To2D(agent.Unit.Pos)] && Bot.Bot.MapAnalyzer.Placement[SC2Util.To2D(agent.Unit.Pos)];
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            foreach (Agent agent in units)
                agent.Order(Abilities.ATTACK, tyr.TargetManager.AttackTarget);
        }
    }
}
