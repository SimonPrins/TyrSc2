using Tyr.CombatSim.Actions;

namespace Tyr.CombatSim.CombatMicro
{
    public interface CombatMicro
    {
        Action Act(SimulationState state, CombatUnit unit);
    }
}
