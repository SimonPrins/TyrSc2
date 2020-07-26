using SC2Sharp.CombatSim.Actions;
using SC2Sharp.CombatSim.Buffs;

namespace SC2Sharp.CombatSim.ActionProcessors
{
    public class ZealotChargeProcessor : ActionProcessor
    {
        public int NextChargeFrame = 0;
        public Action Process(SimulationState state, CombatUnit unit, Action action)
        {
            if (!(action is Attack))
                return action;
            if (state.SimulationFrame < NextChargeFrame)
                return action;
            if (((Attack)action).Target == null || unit.DistSq(((Attack)action).Target) > 4 * 4)
                return action;

            unit.AddBuff(new Charge(((Attack)action).Target, state.SimulationFrame + 78));
            NextChargeFrame = state.SimulationFrame + 224;
            return new DoNothing();
        }
    }
}
