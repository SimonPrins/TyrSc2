using Newtonsoft.Json;
using SC2Sharp.CombatSim.Buffs;

namespace SC2Sharp.CombatSim.DamageProcessors
{
    [JsonObject(MemberSerialization.Fields)]
    public class BarrierDamageProcessor : DamageProcessor
    {
        private float NextActivationFrame = 0;
        private float RemainingDamage = 0;
        private float BarrierExpireFrame = -1;

        public float Process(SimulationState state, CombatUnit unit, float damage)
        {
            if (damage <= 0)
                return damage;

            if (state.SimulationFrame <= BarrierExpireFrame && RemainingDamage > 0)
            {
                RemainingDamage -= damage;
                if (RemainingDamage <= 0)
                {
                    BarrierExpireFrame = -1;
                    return -RemainingDamage;
                }
                return 0;
            }

            if (state.SimulationFrame >= NextActivationFrame)
            {
                BarrierExpireFrame = state.SimulationFrame + 48;
                RemainingDamage = 100;
                NextActivationFrame = state.SimulationFrame + 720;
            }

            return damage;
        }
    }
}
