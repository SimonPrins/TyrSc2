using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.StrategyAnalysis
{
    public class CounterWorkerRush : Strategy
    {
        private static CounterWorkerRush Singleton = new CounterWorkerRush();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            float closest = 1000000;
            foreach (Agent agent in Bot.Main.Units())
            {
                if (!UnitTypes.WorkerTypes.Contains(agent.Unit.UnitType))
                    continue;
                float agentDist = agent.DistanceSq(Bot.Main.MapAnalyzer.StartLocation);
                if (agentDist <= 60 * 60)
                    continue;
                closest = Math.Min(closest, agentDist);
            }

            int closeEnemyWorkerCount = 0;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, Bot.Main.MapAnalyzer.StartLocation) < closest - 20)
                    closeEnemyWorkerCount++;
            }
            if (closeEnemyWorkerCount >= 6)
                return true;
            return false;
        }

        public override string Name()
        {
            return "CounterWorkerRush";
        }
    }
}
