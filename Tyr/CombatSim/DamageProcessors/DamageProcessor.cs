namespace Tyr.CombatSim.DamageProcessors
{
    public interface DamageProcessor
    {
        float Process(SimulationState state, CombatUnit unit, float damage);
    }
}
