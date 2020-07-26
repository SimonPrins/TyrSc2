using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class DefendClosestBaseTask : Task
    {
        public static DefendClosestBaseTask Task = new DefendClosestBaseTask();
        public int ExpandDefenseRadius = 40;
        public int DrawDefenderRadius = 40;
        public int MinimumAttackers = 3;
        
        private bool Defending = false;
        
        public DefendClosestBaseTask() : base(4)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (!agent.IsCombatUnit)
                return false;
            if (SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation)) <= DrawDefenderRadius * DrawDefenderRadius)
                return true;
            foreach (Base b in Bot.Main.BaseManager.Bases)
                if (b.Owner == Bot.Main.PlayerId && agent.DistanceSq(b.BaseLocation.Pos) <= 15 * 15)
                    return true;
            return false;
        }

        public Base GetDefendedBase()
        {
            Dictionary<Base, int> attackerCount = new Dictionary<Base, int>();

            foreach (Unit unit in Bot.Main.Enemies())
                if (unit.UnitType != UnitTypes.ADEPT_PHASE_SHIFT
                    && unit.UnitType != UnitTypes.KD8_CHARGE
                    && unit.UnitType != UnitTypes.OVERLORD
                    && unit.UnitType != UnitTypes.OVERSEER
                    && unit.UnitType != UnitTypes.CHANGELING
                    && unit.UnitType != UnitTypes.CHANGELING_MARINE
                    && unit.UnitType != UnitTypes.CHANGELING_MARINE_SHIELD
                    && unit.UnitType != UnitTypes.CHANGELING_ZEALOT
                    && unit.UnitType != UnitTypes.CHANGELING_ZERGLING
                    && unit.UnitType != UnitTypes.CHANGELING_ZERGLING_WINGS)
                {
                    float dist = GetExpandDefenseRadiusSq();
                    Base closest = null;
                    foreach (Base b in Bot.Main.BaseManager.Bases)
                    {
                        if (b.Owner != Bot.Main.PlayerId)
                            continue;

                        float newDist = SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos);
                        if (newDist < dist)
                        {
                            dist = newDist;
                            closest = b;
                        }
                    }
                    if (closest == null)
                        continue;
                    if (!attackerCount.ContainsKey(closest))
                        attackerCount.Add(closest, 1);
                    else
                        attackerCount[closest]++;
                }

            Base defendedBase = null;
            int attackers = MinimumAttackers - 1;
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (!attackerCount.ContainsKey(b))
                    continue;
                if (attackerCount[b] > attackers)
                {
                    attackers = attackerCount[b];
                    defendedBase = b;
                }
            }
            return defendedBase;
        }

        public override bool IsNeeded()
        {
            return GetDefendedBase() != null;
        }

        public override void OnFrame(Bot bot)
        {
            Base defendedBase = GetDefendedBase();
            if (defendedBase == null)
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
                bot.MicroController.Attack(agent, defendedBase.BaseLocation.Pos);
        }
        
        float GetExpandDefenseRadiusSq()
        {
            if (Defending)
                return (ExpandDefenseRadius + 5) * (ExpandDefenseRadius + 5);
            else
                return ExpandDefenseRadius * ExpandDefenseRadius;
        }
    }
}
