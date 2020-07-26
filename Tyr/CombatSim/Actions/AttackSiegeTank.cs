namespace SC2Sharp.CombatSim.Actions
{
    public class AttackSiegeTank : Action
    {
        public CombatUnit Target;
        public AttackSiegeTank(CombatUnit target)
        {
            Target = target;
        }

        public virtual void Perform(SimulationState state, CombatUnit unit)
        {
            if (Target == null)
                return;
            if (unit.FramesUntilNextAttack > 0)
                return;
            
            CombatWeapon picked = unit.GetWeapon(Target);

            if (picked == null)
                return;

            if (unit.DistSq(Target) > picked.Range * picked.Range)
                return;

            foreach (CombatUnit damagedUnit in state.Player1Units)
                Attack(state, damagedUnit, picked, unit);
            foreach (CombatUnit damagedUnit in state.Player2Units)
                Attack(state, damagedUnit, picked, unit);
        }

        private void Attack(SimulationState state, CombatUnit damagedUnit, CombatWeapon picked, CombatUnit unit)
        {
            if (!damagedUnit.IsGround)
                return;

            float dist = Target.DistSq(damagedUnit);
            if (dist >= 1.25f * 1.25f)
                return;
            if (dist >= 0.7812f * 0.7812f)
            {
                unit.Attack(state, picked, damagedUnit, false, 0.25f);
                return;
            }
            if (dist >= 0.4687f * 0.4687f)
            {
                unit.Attack(state, picked, damagedUnit, false, 0.5f);
                return;
            }
            unit.Attack(state, picked, damagedUnit, false, 1);
        }
    }
}
