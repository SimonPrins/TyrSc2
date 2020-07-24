using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Managers
{
    public class EnemyBansheesManager : Manager
    {
        public Point2D LastHitLocation;
        public int LastHitFrame = -1000000;

        public Point2D BansheeLocation;
        public int BansheeSeenFrame = -1000000;

        public void OnFrame(Bot tyr)
        {
            foreach (Agent observer in tyr.Units())
            {
                if (observer.Unit.UnitType != UnitTypes.OBSERVER)
                    continue;
                if (BansheeLocation != null
                    && Bot.Bot.Frame - BansheeSeenFrame < 22.4 * 10
                    && observer.DistanceSq(BansheeLocation) <= 4 * 4)
                    BansheeSeenFrame = -1000000;
                if (LastHitLocation != null
                    && Bot.Bot.Frame - LastHitFrame < 22.4 * 20
                    && observer.DistanceSq(LastHitLocation) <= 4 * 4)
                    LastHitFrame = -1000000;
            }

            float dist = 40 * 40;
            if (Bot.Bot.Frame - BansheeSeenFrame < 22.4 * 10)
                dist = SC2Util.DistanceSq(BansheeLocation, tyr.MapAnalyzer.StartLocation);
            foreach (Unit enemy in tyr.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BANSHEE)
                    continue;
                float newDist = SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation);
                if (newDist > dist)
                    continue;
                BansheeLocation = SC2Util.To2D(enemy.Pos);
                BansheeSeenFrame = tyr.Frame;
                dist = newDist;
            }

            foreach (Agent agent in tyr.Units())
            {
                if (agent.PreviousUnit == null)
                    continue;
                float damageTaken = agent.PreviousUnit.Health + agent.PreviousUnit.Shield - agent.Unit.Health - agent.Unit.Shield;
                if (damageTaken < 9)
                    continue;
                if (agent.DistanceSq(Bot.Bot.MapAnalyzer.StartLocation) > 50 * 50)
                    continue;
                bool enemyClose = false;
                foreach (Unit enemy in Bot.Bot.Enemies())
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
                LastHitFrame = tyr.Frame;
            }
        }
    }
}
