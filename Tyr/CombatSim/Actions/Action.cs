namespace SC2Sharp.CombatSim.Actions
{
    public interface Action
    {
        void Perform(SimulationState state, CombatUnit unit);
    }
}
