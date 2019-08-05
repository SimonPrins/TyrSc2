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

        public override void OnStart(Tyr tyr)
        { }

        public override void OnFrame(Tyr tyr)
        { }

        public override void Produce(Tyr tyr, Agent agent)
        { }
    }
}
