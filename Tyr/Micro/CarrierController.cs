using SC2APIProtocol;
using SC2Sharp.Agents;

namespace SC2Sharp.Micro
{
    public class CarrierController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.CARRIER)
                return false;

            PotentialHelper potential = new PotentialHelper(agent.Unit.Pos);
            potential.Magnitude = 4;
            bool flee = false;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (UnitTypes.AirAttackTypes.Contains(enemy.UnitType)
                    && agent.DistanceSq(enemy) <= 8 * 8)
                {
                    potential.From(enemy.Pos);
                    flee = true;
                }
            }

            if (flee)
            {
                agent.Order(Abilities.MOVE, potential.Get());
                return true;
            }

            return false;
        }
    }
}
