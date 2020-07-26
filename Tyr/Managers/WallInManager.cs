using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Util;

namespace SC2Sharp.Managers
{
    public class WallInManager : Manager
    {
        public List<Base> Bases { get; internal set; } = new List<Base>();
        public int AvailableGasses { get; internal set; }
        public Point2D NaturalDefensePos { get; private set; }

        public void OnStart(Bot bot)
        {
            float dist = 0;
            Point2D crossSpawn = null;
            foreach (Point2D enemy in bot.TargetManager.PotentialEnemyStartLocations)
            {
                int enemyDist = (int)SC2Util.DistanceSq(enemy, bot.MapAnalyzer.StartLocation);
                if (enemyDist > dist)
                {
                    crossSpawn = enemy;
                    dist = enemyDist;
                }
            }

            int[,] enemyDistances = bot.MapAnalyzer.Distances(crossSpawn);
            
        }

        public void OnFrame(Bot bot)
        {
        }
    }
}
