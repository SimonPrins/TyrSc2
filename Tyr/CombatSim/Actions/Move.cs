namespace Tyr.CombatSim.Actions
{
    public class Move : Action
    {
        private Point Target;
        private bool Toward;
        public Move(Point target, bool toward)
        {
            Target = target;
            Toward = toward;
        }

        public void Perform(SimulationState state, CombatUnit unit)
        {
            if (Target == null)
                return;
            unit.Move(Target, Toward);
        }
    }
}
