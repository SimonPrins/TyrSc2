using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Tasks;

namespace Tyr.Micro
{
    public class DodgeBallController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (FleeFromEffects(agent))
                return true;

            if (FleeFromAlliedDisruptors(agent))
                return true;

            return false;
        }

        public bool FleeFromEffects(Agent agent)
        {
            PotentialHelper potential = new PotentialHelper(agent.Unit.Pos);
            potential.Magnitude = 4;
            bool flee = false;
            foreach (Managers.Effect effect in Bot.Main.EffectManager.Effects)
                if (effect.EffectId == 11 && agent.DistanceSq(effect.Pos) <= 3 * 3
                    && (Bot.Main.Frame - effect.FirstSeenFrame >= 34 || agent.Unit.UnitType != UnitTypes.ZERGLING))
                {
                    potential.From(effect.Pos);
                    flee = true;
                }

            if (!flee)
                return false;
            agent.Order(Abilities.MOVE, potential.Get());
            return true;
        }

        public bool FleeFromAlliedDisruptors(Agent agent)
        {
            if (agent.Unit.IsFlying)
                return false;
            PotentialHelper potential = new PotentialHelper(agent.Unit.Pos);
            potential.Magnitude = 4;
            bool flee = false;
            foreach (Agent disruptor in PhasedDisruptorTask.Task.Units)
            {
                if (!PhasedDisruptorTask.Task.PhasedFrame.ContainsKey(disruptor.Unit.Tag)
                    || Bot.Main.Frame - PhasedDisruptorTask.Task.PhasedFrame[disruptor.Unit.Tag] < 23)
                    continue;

                if (agent.DistanceSq(disruptor) <= 3 * 3)
                {
                    potential.From(disruptor.Unit.Pos);
                    flee = true;
                }
            }

            if (!flee)
                return false;
            agent.Order(Abilities.MOVE, potential.Get());
            return true;
        }
    }
}
