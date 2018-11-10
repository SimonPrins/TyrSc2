using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Managers
{
    public class TargetManager : Manager
    {
        public List<Point2D> PotentialEnemyStartLocations = new List<Point2D>();
        private ulong targetUnitTag = 0;
        bool enemyMainFound = false;
        public bool PrefferDistant { get; set; } = true;
        public void OnFrame(Tyr tyr)
        {
            AttackTarget = PotentialEnemyStartLocations[0];
        }

        public void OnStart(Tyr tyr)
        {
            foreach (Point2D location in tyr.GameInfo.StartRaw.StartLocations)
                if (SC2Util.DistanceGrid(tyr. MapAnalyzer.StartLocation, location) > 20)
                    PotentialEnemyStartLocations.Add(location);
            Console.WriteLine("Enemy locations: " + PotentialEnemyStartLocations.Count);
        }

        public Point2D AttackTarget { get; internal set; }
    }
}
