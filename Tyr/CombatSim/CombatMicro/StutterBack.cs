using Tyr.CombatSim.Actions;

namespace Tyr.CombatSim.CombatMicro
{
    public class StutterBack : CombatMicro
    {
        public Action Act(SimulationState state, CombatUnit unit)
        {
            if (unit.FramesUntilNextAttack == 0)
                return new DoNothing();
            if (unit.PreviousAttackTarget == null)
                return new DoNothing();
            if (unit.AdditionalAttacksRemaining > 0)
                return new DoNothing();
            return new Move(unit.PreviousAttackTarget.Pos, false);
        }
    }
}
