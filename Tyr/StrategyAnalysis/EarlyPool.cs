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
            if (Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) > 0 && Bot.Main.Frame <= 22.4 * 90)
                return true;
            if (!Expanded.Get().Detected
                && Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SPAWNING_POOL) > 0 && Bot.Main.Frame <= 22.4 * 120)
            {
                float hp = -1;
                foreach (Unit enemy in Bot.Main.Enemies())
                    if (enemy.UnitType == UnitTypes.SPAWNING_POOL)
                        hp = enemy.Health;
                if ((22.4 * 120 - Bot.Main.Frame) * 0.85 + hp >= 900)
                    return true;
            } else if (Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.SPAWNING_POOL) > 0 && Bot.Main.Frame <= 22.4 * 105)
            {
                float hp = -1;
                foreach (Unit enemy in Bot.Main.Enemies())
                    if (enemy.UnitType == UnitTypes.SPAWNING_POOL)
                        hp = enemy.Health;
                if ((22.4 * 105 - Bot.Main.Frame) * 0.85 + hp >= 900)
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
