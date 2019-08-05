using Tyr.CombatSim.Actions;

namespace Tyr.CombatSim.ActionProcessors
{
    public interface ActionProcessor
    {
        Action Process(SimulationState state, CombatUnit unit, Action action);
    }
}
