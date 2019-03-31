using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class MicroController
    {
        public void Attack(Agent agent, Point2D target)
        {
            if (!TryAttack(agent, target))
                agent.Order(Abilities.ATTACK, target);
        }

        public bool TryAttack(Agent agent, Point2D target)
        {
            foreach (CustomController customController in Tyr.Bot.Build.GetMicroControllers())
                if (!customController.Stopped && customController.DetermineAction(agent, target))
                    return true;
            return false;
        }

        public bool TryAttack(Agent agent, Point2D target, List<CustomController> customControllers)
        {
            foreach (CustomController customController in customControllers)
                if (!customController.Stopped && customController.DetermineAction(agent, target))
                    return true;
            return false;
        }
    }
}
