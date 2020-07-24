using Tyr.Agents;

namespace Tyr.Tasks
{
    class AMoveTask : Task
    {
        public static AMoveTask Task = new AMoveTask();

        public int UnitType = -1;

        public AMoveTask() : base(5)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit && (UnitType == -1 || agent.Unit.UnitType == UnitType);
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
