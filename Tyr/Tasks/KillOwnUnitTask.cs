using SC2Sharp.Agents;

namespace SC2Sharp.Tasks
{
    public class KillOwnUnitTask : Task
    {
        public static KillOwnUnitTask Task = new KillOwnUnitTask();
        public ulong TargetTag;

        public KillOwnUnitTask() : base(6)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.CanAttackGround() && !UnitTypes.WorkerTypes.Contains(agent.Unit.UnitType);
        }

        public override bool IsNeeded()
        {
            return Bot.Main.UnitManager.Agents.ContainsKey(TargetTag);
        }

        public override void OnFrame(Bot bot)
        {
            if (!Bot.Main.UnitManager.Agents.ContainsKey(TargetTag))
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
                agent.Order(Abilities.ATTACK, TargetTag);
        }
    }
}
