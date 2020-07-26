using SC2Sharp.Agents;

namespace SC2Sharp.Builds
{
    public class DoNothing : Build
    {
        public override string Name()
        {
            return "DoNothing";
        }

        public override void InitializeTasks()
        { }

        public override void OnStart(Bot bot)
        { }

        public override void OnFrame(Bot bot)
        {
            bot.NexusAbilityManager.Stopped = true;
            bot.OrbitalAbilityManager.SaveEnergy = 1000;
            bot.TaskManager.CombatSimulation.SimulationLength = 0;
            bot.AllowGG = false;
            bot.AllowChat = false;
        }

        public override void Produce(Bot bot, Agent agent)
        { }
    }
}
