using SC2Sharp.Agents;
using SC2Sharp.Managers;

namespace SC2Sharp.Builds.Protoss
{
    public class Suicide : Build
    {
        public override string Name()
        {
            return "Suicide";
        }

        public override void OnStart(Bot bot)
        {
            bot.TaskManager = new TaskManager();
        }

        public override void OnFrame(Bot bot)
        {
            Agent main = null;
            foreach (Agent agent in bot.UnitManager.Agents.Values)
                if (agent.IsResourceCenter)
                {
                    main = agent;
                    break;
                }
            foreach (Agent agent in bot.UnitManager.Agents.Values)
                if (agent.IsWorker)
                    agent.Order(Abilities.ATTACK, main.Unit.Tag);
        }

        public override void Produce(Bot bot, Agent agent)
        {
        }
    }
}
