using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class DistributedDefenseTask : Task
    {
        public static DistributedDefenseTask AirDefenseTask = new DistributedDefenseTask() { Air = true };
        public int MaxDefenseRadius = 40;
        public int DrawDefenderRadius = 40;
        public int MainDefenseRadius = 30;
        public int ExpandDefenseRadius = 20;
        public bool Air = false;
        public HashSet<uint> AllowedDefenderTypes = new HashSet<uint>();

        Dictionary<ulong, Unit> Targetting = new Dictionary<ulong, Unit>();

        public HashSet<uint> IgnoreEnemyTypes = new HashSet<uint>();

        public DistributedDefenseTask() : base(7)
        { }

        public static void Enable()
        {
            AirDefenseTask.Stopped = false;
            Tyr.Bot.TaskManager.Add(AirDefenseTask);
        }

        public override bool DoWant(Agent agent)
        {
            if (AllowedDefenderTypes.Count != 0 && !AllowedDefenderTypes.Contains(agent.Unit.UnitType))
                return false;
            if (!agent.IsCombatUnit)
                return false;
            if (!agent.CanAttackAir() && Air)
                return false;
            if (!agent.CanAttackGround() && !Air)
                return false;
            return SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation)) <= DrawDefenderRadius * DrawDefenderRadius;
        }

        public override bool IsNeeded()
        {
            foreach (Unit unit in Tyr.Bot.Observation.Observation.RawData.Units)
                if (unit.Owner != Tyr.Bot.PlayerId
                    && !IgnoreEnemyTypes.Contains(unit.UnitType)
                    && unit.Owner != Tyr.Bot.NeutralPlayerId
                    && unit.UnitType != UnitTypes.ADEPT_PHASE_SHIFT
                    && unit.UnitType != UnitTypes.OVERLORD
                    && unit.UnitType != UnitTypes.OVERSEER
                    && unit.UnitType != UnitTypes.CHANGELING
                    && unit.UnitType != UnitTypes.CHANGELING_MARINE
                    && unit.UnitType != UnitTypes.CHANGELING_MARINE_SHIELD
                    && unit.UnitType != UnitTypes.CHANGELING_ZEALOT
                    && unit.UnitType != UnitTypes.CHANGELING_ZERGLING
                    && unit.UnitType != UnitTypes.CHANGELING_ZERGLING_WINGS)
                {
                    if (unit.IsFlying && !Air)
                        continue;
                    if (!unit.IsFlying && Air)
                        continue;
                    if (SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation)) <= MainDefenseRadius * MainDefenseRadius)
                        return true;
                    if (SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation)) >= MaxDefenseRadius * MaxDefenseRadius)
                        continue;
                    foreach (Base b in Tyr.Bot.BaseManager.Bases)
                        if (b.Owner == Tyr.Bot.PlayerId && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= ExpandDefenseRadius * ExpandDefenseRadius)
                            return true;
                }
            return false;
        }

        public override void OnFrame(Tyr tyr)
        {
            Dictionary<ulong, int> assignedDefenders = new Dictionary<ulong, int>();
            Dictionary<ulong, Unit> attackers = new Dictionary<ulong, Unit>();

            foreach (Unit unit in Tyr.Bot.Observation.Observation.RawData.Units)
                if (unit.Owner != Tyr.Bot.PlayerId
                    && !IgnoreEnemyTypes.Contains(unit.UnitType)
                    && unit.Owner != Tyr.Bot.NeutralPlayerId
                    && unit.UnitType != UnitTypes.ADEPT_PHASE_SHIFT
                    && unit.UnitType != UnitTypes.OVERLORD
                    && unit.UnitType != UnitTypes.OVERSEER
                    && unit.UnitType != UnitTypes.CHANGELING
                    && unit.UnitType != UnitTypes.CHANGELING_MARINE
                    && unit.UnitType != UnitTypes.CHANGELING_MARINE_SHIELD
                    && unit.UnitType != UnitTypes.CHANGELING_ZEALOT
                    && unit.UnitType != UnitTypes.CHANGELING_ZERGLING
                    && unit.UnitType != UnitTypes.CHANGELING_ZERGLING_WINGS)
                {
                    if (unit.IsFlying && !Air)
                        continue;
                    if (!unit.IsFlying && Air)
                        continue;
                    float newDist = SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Tyr.Bot. MapAnalyzer.StartLocation));
                    if (newDist >= MaxDefenseRadius * MaxDefenseRadius)
                        continue;

                    bool nearBase = newDist <= MainDefenseRadius * MainDefenseRadius;
                    if (!nearBase)
                    {
                        foreach (Base b in Tyr.Bot.BaseManager.Bases)
                            if (b.Owner == Tyr.Bot.PlayerId && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= ExpandDefenseRadius * ExpandDefenseRadius)
                            {
                                nearBase = true;
                                break;
                            }
                    }
                    if (nearBase)
                        attackers.Add(unit.Tag, unit);
                }

            if (attackers.Count == 0)
            {
                Clear();
                return;
            }

            foreach (Agent agent in units)
                if (Targetting.ContainsKey(agent.Unit.Tag))
                {
                    Unit target = Targetting[agent.Unit.Tag];
                    if (!attackers.ContainsKey(target.Tag))
                        Targetting.Remove(target.Tag);
                    else
                        AddDefender(assignedDefenders, target.Tag);
                }

            int maxDefenders = 1;
            ulong[] attackersArray = new ulong[attackers.Count];
            attackers.Keys.CopyTo(attackersArray, 0);
            int attackersPos = 0;
            foreach (Agent agent in units)
            {
                if (Targetting.ContainsKey(agent.Unit.Tag))
                    continue;

                bool assigned = false;
                while (!assigned)
                {
                    ulong target = attackersArray[attackersPos];
                    if (GetDefenders(assignedDefenders, target) < maxDefenders)
                    {
                        AddDefender(assignedDefenders, target);
                        Targetting.Add(agent.Unit.Tag, attackers[target]);
                        assigned = true;
                    }

                    attackersPos++;
                    if (attackersPos >= attackersArray.Length)
                    {
                        attackersPos = 0;
                        maxDefenders++;
                    }
                }
            }

            foreach (Agent agent in units)
                tyr.MicroController.Attack(agent, SC2Util.To2D(Targetting[agent.Unit.Tag].Pos));
        }

        private int GetDefenders(Dictionary<ulong, int> assignedDefenders, ulong enemyTag)
        {
            if (assignedDefenders.ContainsKey(enemyTag))
                return assignedDefenders[enemyTag];
            return 0;
        }

        private void AddDefender(Dictionary<ulong, int> assignedDefenders, ulong enemyTag)
        {
            if (!assignedDefenders.ContainsKey(enemyTag))
                assignedDefenders.Add(enemyTag, 1);
            else
                assignedDefenders[enemyTag]++;
        }
    }
}
