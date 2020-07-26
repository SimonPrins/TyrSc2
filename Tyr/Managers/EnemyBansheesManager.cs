using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Managers
{
    public class EnemyBansheesManager : Manager
    {
        public Point2D LastHitLocation;
        public int LastHitFrame = -1000000;

        public Point2D BansheeLocation;
        public int BansheeSeenFrame = -1000000;

        public void OnFrame(Bot bot)
        {
            foreach (Agent observer in bot.Units())
            {
                if (observer.Unit.UnitType != UnitTypes.OBSERVER)
                    continue;
                if (BansheeLocation != null
                    && Bot.Main.Frame - BansheeSeenFrame < 22.4 * 10
                    && observer.DistanceSq(BansheeLocation) <= 4 * 4)
                    BansheeSeenFrame = -1000000;
                if (LastHitLocation != null
                    && Bot.Main.Frame - LastHitFrame < 22.4 * 20
                    && observer.DistanceSq(LastHitLocation) <= 4 * 4)
                    LastHitFrame = -1000000;
            }

            float dist = 40 * 40;
            if (Bot.Main.Frame - BansheeSeenFrame < 22.4 * 10)
                dist = SC2Util.DistanceSq(BansheeLocation, bot.MapAnalyzer.StartLocation);
            foreach (Unit enemy in bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BANSHEE)
                    continue;
                float newDist = SC2Util.DistanceSq(enemy.Pos, bot.MapAnalyzer.StartLocation);
                if (newDist > dist)
                    continue;
                BansheeLocation = SC2Util.To2D(enemy.Pos);
                BansheeSeenFrame = bot.Frame;
                dist = newDist;
            }

            foreach (Agent agent in bot.Units())
            {
                if (agent.PreviousUnit == null)
                    continue;
                float damageTaken = agent.PreviousUnit.Health + agent.PreviousUnit.Shield - agent.Unit.Health - agent.Unit.Shield;
                if (damageTaken < 9)
                    continue;
                if (agent.DistanceSq(Bot.Main.MapAnalyzer.StartLocation) > 50 * 50)
                    continue;
                bool enemyClose = false;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                        continue;
                    if (agent.DistanceSq(enemy) > 15.5 * 15.5)
                        continue;
                    enemyClose = true;
                    break;
                }

                if (enemyClose)
                    continue;
                LastHitLocation = SC2Util.To2D(agent.Unit.Pos);
                LastHitFrame = bot.Frame;
            }
        }
    }
}
