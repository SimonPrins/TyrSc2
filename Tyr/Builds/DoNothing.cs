using Tyr.Agents;

namespace Tyr.Builds
{
    public class DoNothing : Build
    {
        public override string Name()
        {
            return "DoNothing";
        }

        public override void InitializeTasks()
        { }

        public override void OnStart(Bot tyr)
        { }

        public override void OnFrame(Bot tyr)
        {
            tyr.NexusAbilityManager.Stopped = true;
            tyr.OrbitalAbilityManager.SaveEnergy = 1000;
            tyr.TaskManager.CombatSimulation.SimulationLength = 0;
            tyr.AllowGG = false;
            tyr.AllowChat = false;
        }

        public override void Produce(Bot tyr, Agent agent)
        { }
    }
}
