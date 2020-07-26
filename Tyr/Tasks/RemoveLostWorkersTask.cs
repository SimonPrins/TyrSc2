using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class RemoveLostWorkersTask : Task
    {
        public RemoveLostWorkersTask() : base(5)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && !Bot.Main.MapAnalyzer.MainAndPocketArea[SC2Util.To2D(agent.Unit.Pos)] && Bot.Main.MapAnalyzer.Placement[SC2Util.To2D(agent.Unit.Pos)];
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            foreach (Agent agent in units)
                agent.Order(Abilities.ATTACK, bot.TargetManager.AttackTarget);
        }
    }
}
