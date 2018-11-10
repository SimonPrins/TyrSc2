
using Tyr.Agents;
using Tyr.Tasks;

namespace Tyr.Builds
{
    public class MicroBuild : Build
    {
        public override string Name()
        {
            return "Micro";
        }

        public override void InitializeTasks()
        {
            IdleTask.Enable();
            MicroAttackTask.Enable();
        }

        public override void OnFrame(Tyr tyr)
        { }

        public override void OnStart(Tyr tyr)
        {
            
        }

        public override void Produce(Tyr tyr, Agent agent)
        { }
    }
}
