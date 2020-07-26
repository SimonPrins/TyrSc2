using SC2Sharp.CombatSim.Buffs;

namespace SC2Sharp.CombatSim.Actions
{
    public class AttackConcussiveShells : Attack
    {
        public AttackConcussiveShells(Attack attack) : base(attack.Target) { }

        public override void Perform(SimulationState state, CombatUnit unit)
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
            foreach (Buff buff in Target.Buffs)
                if (buff is ConcussiveShell)
                {
                    buff.ExpireFrame = state.SimulationFrame + 24;
                    return;
                }
            Target.AddBuff(new ConcussiveShell(state.SimulationFrame + 24));
        }
    }
}
