namespace SC2Sharp.CombatSim.Actions
{
    public class Attack : Action
    {
        public CombatUnit Target;
        public Attack(CombatUnit target)
        {
            Target = target;
        }

        public virtual void Perform(SimulationState state, CombatUnit unit)
        {
            if (Target == null)
                return;
            if (unit.FramesUntilNextAttack > 0)
                return;

            if (unit.AdditionalAttacksRemaining > 0 && unit.PreviousAttackTarget.Tag != Target.Tag)
                unit.AdditionalAttacksRemaining = 0;

            CombatWeapon picked = unit.GetWeapon(Target);

            if (picked == null)
                return;

            if (unit.DistSq(Target) > picked.Range * picked.Range)
            {
                unit.Move(Target.Pos);
                return;
            }
            unit.Attack(state, picked, Target, false);
        }
    }
}
