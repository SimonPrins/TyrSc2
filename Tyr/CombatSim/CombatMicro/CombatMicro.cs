using SC2Sharp.CombatSim.Actions;

namespace SC2Sharp.CombatSim.CombatMicro
{
    public interface CombatMicro
    {
        Action Act(SimulationState state, CombatUnit unit);
    }
}
