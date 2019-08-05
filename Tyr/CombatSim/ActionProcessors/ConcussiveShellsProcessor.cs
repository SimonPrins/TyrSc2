using Tyr.CombatSim.Actions;

namespace Tyr.CombatSim.ActionProcessors
{
    public class ConcussiveShellsProcessor : ActionProcessor
    {
        public Action Process(SimulationState state, CombatUnit unit, Action action)
        {
            if ((action is Attack))
                return new AttackConcussiveShells((Attack)action);
            return action;
        }
    }
}
