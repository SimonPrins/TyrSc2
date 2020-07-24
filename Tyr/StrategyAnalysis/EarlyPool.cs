using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class EarlyPool : Strategy
    {
        private static Strategy Singleton = new EarlyPool();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) > 0 && Bot.Bot.Frame <= 22.4 * 90)
                return true;
            if (!Expanded.Get().Detected
                && Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SPAWNING_POOL) > 0 && Bot.Bot.Frame <= 22.4 * 120)
            {
                float hp = -1;
                foreach (Unit enemy in Bot.Bot.Enemies())
                    if (enemy.UnitType == UnitTypes.SPAWNING_POOL)
                        hp = enemy.Health;
                if ((22.4 * 120 - Bot.Bot.Frame) * 0.85 + hp >= 900)
                    return true;
            } else if (Bot.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.SPAWNING_POOL) > 0 && Bot.Bot.Frame <= 22.4 * 105)
            {
                float hp = -1;
                foreach (Unit enemy in Bot.Bot.Enemies())
                    if (enemy.UnitType == UnitTypes.SPAWNING_POOL)
                        hp = enemy.Health;
                if ((22.4 * 105 - Bot.Bot.Frame) * 0.85 + hp >= 900)
                    return true;
            }
            return false;
        }

        public override string Name()
        {
            return "EarlyPool";
        }
    }
}
