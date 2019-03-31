using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;

namespace Tyr.Tasks
{
    public class DistributeOverBasesTask : Task
    {
        public uint UnitType;
        private Dictionary<ulong, Base> AssignedBases = new Dictionary<ulong, Base>();
        public bool ExcludeMain = true;

        public DistributeOverBasesTask(uint unitType) : base(1)
        {
            UnitType = unitType;
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitType;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (Stopped)
            {
                Clear();
                return;
            }

            HashSet<Base> bases = new HashSet<Base>();
            foreach (Base b in tyr.BaseManager.Bases)
                if (b.Owner == tyr.PlayerId && (b != tyr.BaseManager.Main || !ExcludeMain))
                    bases.Add(b);
            if (bases.Count == 0)
            {
                Clear();
                return;
            }

            Dictionary<Base, int> defenderCounts = new Dictionary<Base, int>();
            
            foreach (Agent agent in units)
            {
                if (AssignedBases.ContainsKey(agent.Unit.Tag))
                {
                    if (bases.Contains(AssignedBases[agent.Unit.Tag]))
                        AddDefender(defenderCounts, AssignedBases[agent.Unit.Tag]);
                    else
                        AssignedBases.Remove(agent.Unit.Tag);
                }
            }

            tyr.DrawText("Distribute bases count: " + bases.Count);

            int maxDefenders;
            if (bases.Count > 0)
                maxDefenders = units.Count / bases.Count;
            else maxDefenders = units.Count;

            foreach (Agent agent in units)
            {
                if (!AssignedBases.ContainsKey(agent.Unit.Tag))
                    continue;

                Base assignedBase = AssignedBases[agent.Unit.Tag];
                if (defenderCounts[assignedBase] <= maxDefenders + 1)
                    continue;

                defenderCounts[assignedBase]--;
                AssignedBases.Remove(agent.Unit.Tag);
            }

            int basePos = 0;
            Base[] baseArray = new Base[bases.Count];
            bases.CopyTo(baseArray);
            foreach (Agent agent in units)
            {
                if (AssignedBases.ContainsKey(agent.Unit.Tag))
                    continue;

                while (Count(defenderCounts, baseArray[basePos]) >= maxDefenders)
                {
                    basePos++;
                    if (basePos >= baseArray.Length)
                    {
                        basePos = 0;
                        maxDefenders++;
                    }
                }

                AssignedBases.Add(agent.Unit.Tag, baseArray[basePos]);
                AddDefender(defenderCounts, baseArray[basePos]);
            }

            foreach (Agent agent in units)
            {
                Base target = AssignedBases[agent.Unit.Tag];
                if (agent.DistanceSq(target.BaseLocation.Pos) >= 6 * 6
                    && (agent.Unit.Orders == null || agent.Unit.Orders.Count == 0 || tyr.Frame % 10 == 0))
                    tyr.MicroController.Attack(agent, target.BaseLocation.Pos);
            }

        }

        public void Enable()
        {
            Enable(this);
            Stopped = false;
        }

        private int Count(Dictionary<Base, int> defenderCounts, Base b)
        {
            if (defenderCounts.ContainsKey(b))
                return defenderCounts[b];
            return 0;
        }

        private void AddDefender(Dictionary<Base, int> assignedDefenders, Base b)
        {
            if (!assignedDefenders.ContainsKey(b))
                assignedDefenders.Add(b, 1);
            else
                assignedDefenders[b]++;
        }
    }
}
