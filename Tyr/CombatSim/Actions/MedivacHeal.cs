namespace Tyr.CombatSim.Actions
{
    public class MedivacHeal : Action
    {
        public CombatUnit Target;
        public MedivacHeal(CombatUnit target)
        {
            Target = target;
        }

        public virtual void Perform(SimulationState state, CombatUnit unit)
        {
            if (Target == null)
                return;

            if (unit.DistSq(Target) > 4 * 4)
            {
                unit.Move(Target.Pos);
                return;
            }
            if (unit.Energy < 0.18516)
                return;
            if (!Target.HasAttribute(UnitAttribute.Biological))
                return;
            unit.Energy -= 0.18516f;
            Target.Health = System.Math.Min(Target.Health + 0.56f, Target.HealthMax);
        }
    }
}
