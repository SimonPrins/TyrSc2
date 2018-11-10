using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds
{
    public abstract class Build
    {
        public abstract string Name();
        public abstract void OnStart(Tyr tyr);
        public abstract void OnFrame(Tyr tyr);
        public abstract void Produce(Tyr tyr, Agent agent);
        private Build PreviousOverrideBuild = null;

        protected List<CustomController> MicroControllers = new List<CustomController>();
        public List<CustomController> GetMicroControllers()
        {
            if (PreviousOverrideBuild != null)
                return PreviousOverrideBuild.MicroControllers;
            return MicroControllers;
        }

        public void ProduceOverride(Tyr tyr, Agent agent)
        {
            if (PreviousOverrideBuild != null)
                PreviousOverrideBuild.Produce(tyr, agent);
            else
                Produce(tyr, agent);
        }

        public void OnFrameBase(Tyr tyr)
        {
            Tyr.Bot.DrawText("Executing Build: " + Name());
            Build actualBuild = null;
            Build overrideBuild = this;
            while (overrideBuild != null)
            {
                actualBuild = overrideBuild;
                overrideBuild = overrideBuild.OverrideBuild();
            }

            if (PreviousOverrideBuild != null && PreviousOverrideBuild != actualBuild)
            {
                tyr.TaskManager.StopAll();
                actualBuild.InitializeTasks();
                tyr.TaskManager.ClearStopped();
            }
            actualBuild.OnFrame(tyr);
            PreviousOverrideBuild = actualBuild;
        }

        public virtual void InitializeTasks()
        {
            IdleTask.Enable();
            ProductionTask.Enable();
            MorphingTask.Enable();
        }

        public virtual Build OverrideBuild() { return null; }
        
        private static int gasConstructingFrame = -100;
        
        public int Minerals()
        {
            return (int)Tyr.Bot.Observation.Observation.PlayerCommon.Minerals - Tyr.Bot.ReservedMinerals;
        }

        public int Gas()
        {
            return (int)Tyr.Bot.Observation.Observation.PlayerCommon.Vespene - Tyr.Bot.ReservedGas;
        }
        
        public int Count(uint type)
        {
            return Tyr.Bot.UnitManager.Count(type);
        }

        public int Completed(uint type)
        {
            return Tyr.Bot.UnitManager.Completed(type);
        }
        
    }
}
