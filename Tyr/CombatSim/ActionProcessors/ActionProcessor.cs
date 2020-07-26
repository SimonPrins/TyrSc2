using SC2Sharp.CombatSim.Actions;

namespace SC2Sharp.CombatSim.ActionProcessors
{
    public interface ActionProcessor
    {
        Action Process(SimulationState state, CombatUnit unit, Action action);
    }
}
