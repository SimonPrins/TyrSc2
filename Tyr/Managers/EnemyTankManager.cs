using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Managers
{
    public class EnemyTankManager : Manager
    {
        public List<UnitLocation> Tanks = new List<UnitLocation>();
        public Dictionary<ulong, int> SiegeFrame = new Dictionary<ulong, int>();
        
        public void OnFrame(Tyr tyr)
        {
            Cleanup(tyr);
            Update(tyr);
        }

        private void Cleanup(Tyr tyr)
        {
            HashSet<ulong> unsiegedTanks = new HashSet<ulong>();
            foreach (Unit enemy in tyr.Enemies())
                if (enemy.UnitType == UnitTypes.SIEGE_TANK)
                    unsiegedTanks.Add(enemy.Tag);

            for (int i = Tanks.Count - 1; i >= 0; i--)
            {
                UnitLocation tank = Tanks[i];
                if (unsiegedTanks.Contains(tank.Tag))
                {
                    Remove(i);
                    continue;
                }
                bool removed = false;
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                {
                    float sightRange = UnitTypes.LookUp[agent.Unit.UnitType].SightRange;
                    if (agent.DistanceSq(tank.Pos) <= sightRange * sightRange - 4)
                    {
                        Remove(i);
                        removed = true;
                        break;
                    }
                }
                if (removed)
                    continue;
            }
        }

        private void Remove(int i)
        {
            Tanks[i] = Tanks[Tanks.Count - 1];
            Tanks.RemoveAt(Tanks.Count - 1);
        }

        public void Update(Tyr tyr)
        {
            Dictionary<ulong, UnitLocation> existingTanks = new Dictionary<ulong, UnitLocation>();
            foreach (UnitLocation tank in Tanks)
                existingTanks.Add(tank.Tag, tank);

            HashSet<ulong> removeTanks = new HashSet<ulong>();

            foreach (Unit enemy in tyr.Enemies())
            {
                if (enemy.UnitType == UnitTypes.SIEGE_TANK)
                {
                    if (SiegeFrame.ContainsKey(enemy.Tag))
                        SiegeFrame[enemy.Tag] = tyr.Frame;
                    else
                        SiegeFrame.Add(enemy.Tag, tyr.Frame);
                }
                if (existingTanks.ContainsKey(enemy.Tag))
                {
                    if (enemy.UnitType == UnitTypes.SIEGE_TANK_SIEGED)
                    {
                        existingTanks[enemy.Tag].Pos = enemy.Pos;
                        existingTanks[enemy.Tag].LastSeenFrame = tyr.Frame;
                    }
                    else
                        removeTanks.Add(enemy.Tag);
                    continue;
                }

                if (enemy.UnitType != UnitTypes.SIEGE_TANK_SIEGED)
                    continue;

                Tanks.Add(new UnitLocation() { Tag = enemy.Tag, UnitType = enemy.UnitType, LastSeenFrame = tyr.Frame, Pos = enemy.Pos });
            }

            for (int i = Tanks.Count - 1; i >= 0; i--)
                if (removeTanks.Contains(Tanks[i].Tag))
                    Remove(i);
        }
    }
}
