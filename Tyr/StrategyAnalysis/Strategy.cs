using System.Collections.Generic;

namespace SC2Sharp.StrategyAnalysis
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
                    Bot.Main.EnemyStrategyAnalyzer.Set(Name());
                    DetectedPreviously = true;
                }
            }
        }

        public void Load(HashSet<string> names)
        {
            if (names.Contains(Name()))
                DetectedPreviously = true;
        }

        public int Count(uint unitType)
        {
            return Bot.Main.EnemyStrategyAnalyzer.Count(unitType);
        }

        public int TotalCount(uint unitType)
        {
            return Bot.Main.EnemyStrategyAnalyzer.TotalCount(unitType);
        }

    }
}
