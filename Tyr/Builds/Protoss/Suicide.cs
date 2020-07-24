using Tyr.Agents;
using Tyr.Managers;

namespace Tyr.Builds.Protoss
{
    public class Suicide : Build
    {
        public override string Name()
        {
            return "Suicide";
        }

        public override void OnStart(Bot tyr)
        {
            tyr.TaskManager = new TaskManager();
        }

        public override void OnFrame(Bot tyr)
        {
            Agent main = null;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
                if (agent.IsResourceCenter)
                {
                    main = agent;
                    break;
                }
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
                if (agent.IsWorker)
                    agent.Order(Abilities.ATTACK, main.Unit.Tag);
        }

        public override void Produce(Bot tyr, Agent agent)
        {
        }
    }
}
