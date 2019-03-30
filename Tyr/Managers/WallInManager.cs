using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Util;

namespace Tyr.Managers
{
    public class WallInManager : Manager
    {
        public List<Base> Bases { get; internal set; } = new List<Base>();
        public int AvailableGasses { get; internal set; }
        public Point2D NaturalDefensePos { get; private set; }

        public void OnStart(Tyr tyr)
        {
            float dist = 0;
            Point2D crossSpawn = null;
            foreach (Point2D enemy in tyr.TargetManager.PotentialEnemyStartLocations)
            {
                int enemyDist = (int)SC2Util.DistanceSq(enemy, tyr.MapAnalyzer.StartLocation);
                if (enemyDist > dist)
                {
                    crossSpawn = enemy;
                    dist = enemyDist;
                }
            }

            int[,] enemyDistances = tyr.MapAnalyzer.Distances(crossSpawn);
            
        }

        public void OnFrame(Tyr tyr)
        {
        }
    }
}
