using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class DodgeBallController : CustomController
    {
        public bool DetermineAction(Agent agent, Point2D target)
        {
            PotentialHelper potential = new PotentialHelper(agent.Unit.Pos);
            potential.Magnitude = 4;
            bool flee = false;
            foreach (Managers.Effect effect in Tyr.Bot.EffectManager.Effects)
                if (effect.EffectId == 11 && agent.DistanceSq(effect.Pos) <= 3 * 3
                    && (Tyr.Bot.Frame - effect.FirstSeenFrame >= 34 || agent.Unit.UnitType != UnitTypes.ZERGLING))
                {
                    potential.From(effect.Pos);
                    flee = true;
                }

            if (!flee)
                return false;
            agent.Order(Abilities.MOVE, potential.Get());
            return true;
        }
    }
}
