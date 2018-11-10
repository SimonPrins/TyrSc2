using System.Collections.Generic;
using SC2APIProtocol;

namespace Tyr.Managers
{
    public class EnemyManager : Manager
    {
        private List<Unit> Enemies;
        private int EnemiesFrame = -1;
        public Queue<RecentlyDeceased> RecentlyDeceased = new Queue<RecentlyDeceased>();

        public List<Unit> GetEnemies()
        {
            Update();
            return Enemies;
        }

        public Queue<RecentlyDeceased> GetRecentlyDeceased()
        {
            Update();
            return RecentlyDeceased;
        }

        private void Update()
        {
            if (Tyr.Bot.Frame > EnemiesFrame)
            {
                if (Tyr.Bot.Observation.Observation.RawData.Event != null
                    && Tyr.Bot.Observation.Observation.RawData.Event.DeadUnits != null)
                    foreach (ulong tag in Tyr.Bot.Observation.Observation.RawData.Event.DeadUnits)
                    {
                        foreach (Unit unit in Enemies)
                            if (unit.Tag == tag)
                            {
                                RecentlyDeceased.Enqueue(new RecentlyDeceased() { UnitType = unit.UnitType, Pos = unit.Pos, Frame = Tyr.Bot.Frame });
                                break;
                            }
                    }

                while (RecentlyDeceased.Count > 0 && RecentlyDeceased.Peek().Frame < Tyr.Bot.Frame - 66)
                    RecentlyDeceased.Dequeue();

                EnemiesFrame = Tyr.Bot.Frame;

                Enemies = new List<Unit>();

                foreach (Unit unit in Tyr.Bot.Observation.Observation.RawData.Units)
                    if (unit.Alliance == Alliance.Enemy)
                        Enemies.Add(unit);
            }
        }

        public void OnFrame(Tyr tyr)
        {
        }
    }
}
