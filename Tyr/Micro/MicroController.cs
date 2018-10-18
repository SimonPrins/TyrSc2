using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class MicroController
    {
        public void Attack(Agent agent, Point2D target)
        {
            foreach (CustomController customController in Tyr.Bot.Build.GetMicroControllers())
                if (customController.DetermineAction(agent, target))
                    return;
            agent.Order(Abilities.ATTACK, target);
        }
    }
}
