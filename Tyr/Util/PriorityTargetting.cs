using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Util
{
    public class PriorityTargetting
    {
        public Dictionary<uint, PrioritySettings> TypePriorities = new Dictionary<uint, PrioritySettings>();
        public PrioritySettings DefaultPriorities = new PrioritySettings();

        public Dictionary<ulong, int> DamageDealt = new Dictionary<ulong, int>();
        public int FrameUpdated = 0;

        public Unit GetTarget(Agent agent)
        {
            uint unitType = agent.Unit.UnitType;
            float maxRangeSq = 9;
            if (TypePriorities.ContainsKey(unitType)
                && TypePriorities[unitType].MaxRange > 0)
                maxRangeSq = TypePriorities[unitType].MaxRange * TypePriorities[unitType].MaxRange;
            if (DefaultPriorities.MaxRange > 0)
                maxRangeSq = DefaultPriorities.MaxRange * DefaultPriorities.MaxRange;

            Unit target = null;
            int priority = -2;
            float health = 0;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (SC2Util.DistanceSq(enemy.Pos, agent.Unit.Pos) > maxRangeSq)
                    continue;

                float enemyHealth = enemy.Health + enemy.Shield - GetDamageDealt(enemy.Tag);
                if (enemyHealth < 0)
                    continue;

                int newPriority = 0;
                if (TypePriorities.ContainsKey(unitType) && TypePriorities[unitType][enemy.UnitType] != int.MinValue)
                    newPriority = TypePriorities[unitType][enemy.UnitType];
                else if (DefaultPriorities[enemy.UnitType] != int.MinValue)
                    newPriority = DefaultPriorities[enemy.UnitType];

                if (newPriority < priority)
                    continue;

                if (newPriority == priority && enemyHealth >= health)
                    continue;

                target = enemy;
                priority = newPriority;
                health = enemyHealth;
            }

            if (priority >= 0)
            {
                AddDamageDealt(target.Tag, agent.GetDamage(target));
                return target;
            }
            else
                return null;
        }

        public int GetDamageDealt(ulong tag)
        {
            if (FrameUpdated < Bot.Main.Frame || !DamageDealt.ContainsKey(tag))
                return 0;
            else return DamageDealt[tag];
        }

        public void AddDamageDealt(ulong tag, int damage)
        {
            if (FrameUpdated < Bot.Main.Frame)
                DamageDealt = new Dictionary<ulong, int>();

            if (!DamageDealt.ContainsKey(tag))
                DamageDealt.Add(tag, damage);
            else
                DamageDealt[tag] += damage;
        }

        public PrioritySettings this[uint t]
        {
            get
            {
                if (!TypePriorities.ContainsKey(t))
                    TypePriorities.Add(t, new PrioritySettings());
                return TypePriorities[t];
            }
            set
            {
                if (!TypePriorities.ContainsKey(t))
                    TypePriorities.Add(t, value);
                else
                    TypePriorities[t] = value;
            }
        }
    }
}
