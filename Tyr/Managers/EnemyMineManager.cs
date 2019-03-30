using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Managers
{
    public class EnemyMineManager : Manager
    {
        public List<UnitLocation> Mines = new List<UnitLocation>();
        public Dictionary<ulong, int> BurrowFrame = new Dictionary<ulong, int>();
        
        public void OnFrame(Tyr tyr)
        {
            Cleanup(tyr);
            Update(tyr);
        }

        private void Cleanup(Tyr tyr)
        {
            HashSet<ulong> unburrowedMines = new HashSet<ulong>();
            foreach (Unit enemy in tyr.Enemies())
                if (enemy.UnitType == UnitTypes.WIDOW_MINE)
                    unburrowedMines.Add(enemy.Tag);

            for (int i = Mines.Count - 1; i >= 0; i--)
            {
                UnitLocation mine = Mines[i];
                if (unburrowedMines.Contains(mine.Tag))
                {
                    Remove(i);
                    continue;
                }
                bool removed = false;
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                {
                    if (agent.Unit.DetectRange <= 1)
                        continue;

                    if (agent.DistanceSq(mine.Pos) <= agent.Unit.DetectRange * agent.Unit.DetectRange - 4)
                    {
                        Remove(i);
                        removed = true;
                        break;
                    }
                }
                if (removed)
                    continue;
                foreach (SC2APIProtocol.Effect effect in tyr.Observation.Observation.RawData.Effects)
                {
                    if (effect.EffectId != 6)
                        continue;
                    if (SC2Util.DistanceSq(effect.Pos[0], mine.Pos) <= 8 * 8)
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
            Mines[i] = Mines[Mines.Count - 1];
            Mines.RemoveAt(Mines.Count - 1);
        }

        public void Update(Tyr tyr)
        {
            Dictionary<ulong, UnitLocation> existingMines = new Dictionary<ulong, UnitLocation>();
            foreach (UnitLocation mine in Mines)
                existingMines.Add(mine.Tag, mine);

            HashSet<ulong> removeMines = new HashSet<ulong>();

            foreach (Unit enemy in tyr.Enemies())
            {
                if (enemy.UnitType == UnitTypes.WIDOW_MINE)
                {
                    if (BurrowFrame.ContainsKey(enemy.Tag))
                        BurrowFrame[enemy.Tag] = tyr.Frame;
                    else
                        BurrowFrame.Add(enemy.Tag, tyr.Frame);
                }
                if (existingMines.ContainsKey(enemy.Tag))
                {
                    if (enemy.UnitType == UnitTypes.WIDOW_MINE_BURROWED)
                    {
                        existingMines[enemy.Tag].Pos = enemy.Pos;
                        existingMines[enemy.Tag].LastSeenFrame = tyr.Frame;
                    }
                    else
                        removeMines.Add(enemy.Tag);
                    continue;
                }

                if (enemy.UnitType != UnitTypes.WIDOW_MINE_BURROWED)
                    continue;

                Mines.Add(new UnitLocation() { Tag = enemy.Tag, UnitType = enemy.UnitType, LastSeenFrame = tyr.Frame, Pos = enemy.Pos });
            }

            for (int i = Mines.Count - 1; i >= 0; i--)
                if (removeMines.Contains(Mines[i].Tag))
                    Remove(i);
        }
    }
}
