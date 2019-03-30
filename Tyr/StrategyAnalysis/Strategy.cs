using System.Collections.Generic;

namespace Tyr.StrategyAnalysis
{
    public abstract class Strategy
    {
        public bool Detected = false;
        public bool DetectedPreviously = false;
        public abstract string Name();
        public abstract bool Detect();

        public void OnFrame()
        {
            if (!Detected && Detect())
            {
                Detected = true;
                if (!DetectedPreviously)
                {
                    Tyr.Bot.PreviousEnemyStrategies.Set(Name());
                    DetectedPreviously = true;
                }
            }
        }

        public void Load(HashSet<string> names)
        {
            if (names.Contains(Name()))
                DetectedPreviously = true;
        }
        
    }
}
