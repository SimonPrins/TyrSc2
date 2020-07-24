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
        public static DistributedDefenseTask GroundDefenseTask = new DistributedDefenseTask() { Air = false };
        public int MaxDefenseRadius = 40;
        public int DrawDefenderRadius = 40;
        public int MainDefenseRadius = 30;
        public int ExpandDefenseRadius = 20;
        public bool Air = false;
        public HashSet<uint> AllowedDefenderTypes = new HashSet<uint>();

        Dictionary<ulong, Unit> Targetting = new Dictionary<ulong, Unit>();

        public HashSet<uint> IgnoreEnemyTypes = new HashSet<uint>();

        public int MaxDefendersPerEnemy = 1000;

        public DistributedDefenseTask() : base(7)
        { }

        public static void Enable()
        {
            AirDefenseTask.Stopped = false;
            Bot.Bot.TaskManager.Add(AirDefenseTask);
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
            return SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation)) <= DrawDefenderRadius * DrawDefenderRadius;
        }

        public override bool IsNeeded()
        {
            foreach (Unit unit in Bot.Bot.Observation.Observation.RawData.Units)
                if (unit.Owner != Bot.Bot.PlayerId
                    && !IgnoreEnemyTypes.Contains(unit.UnitType)
                    && unit.Owner != Bot.Bot.NeutralPlayerId
                    && unit.UnitType != UnitTypes.ADEPT_PHASE_SHIFT
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
                    if (unit.IsFlying && !Air)
                        continue;
                    if (!unit.IsFlying && Air)
                        continue;
                    if (SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation)) <= MainDefenseRadius * MainDefenseRadius)
                        return true;
                    if (SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation)) >= MaxDefenseRadius * MaxDefenseRadius)
                        continue;
                    foreach (Base b in Bot.Bot.BaseManager.Bases)
                        if (b.Owner == Bot.Bot.PlayerId && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= ExpandDefenseRadius * ExpandDefenseRadius)
                            return true;
                }
            Clear();
            return false;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            if (MaxDefendersPerEnemy == 1000)
                return base.GetDescriptors();

            List<UnitDescriptor> descriptors = new List<UnitDescriptor>();
            Dictionary<ulong, Unit> attackers = GetAttackers();

            int requiredDefenders = attackers.Count - Units.Count;
            if (requiredDefenders > 0)
                descriptors.Add(new UnitDescriptor() { UnitTypes = AllowedDefenderTypes.Count > 0 ? AllowedDefenderTypes : null, Count = requiredDefenders , Pos = Bot.Bot.BaseManager.Main.BaseLocation.Pos });
            return descriptors;
        }

        public override void OnFrame(Bot tyr)
        {
            Dictionary<ulong, int> assignedDefenders = new Dictionary<ulong, int>();
            Dictionary<ulong, Unit> attackers = GetAttackers();
            
            if (attackers.Count == 0)
            {
                Clear();
                return;
            }

            List<ulong> removeTargets = new List<ulong>();
            foreach (KeyValuePair<ulong, Unit> pair in Targetting)
            {
                if (attackers.ContainsKey(pair.Key))
                    Targetting[pair.Key] = attackers[pair.Key];
                else
                    removeTargets.Add(pair.Key);
            }
            foreach (ulong removeTarget in removeTargets)
                Targetting.Remove(removeTarget);

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
            HashSet<ulong> removeAgents = new HashSet<ulong>();
            foreach (Agent agent in units)
            {
                if (maxDefenders > MaxDefendersPerEnemy)
                {
                    removeAgents.Add(agent.Unit.Tag);
                    continue;
                }
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
                        if (maxDefenders > MaxDefendersPerEnemy)
                            break;
                    }
                }
            }
            for (int i = Units.Count - 1; i >= 0; i--)
                if (removeAgents.Contains(Units[i].Unit.Tag))
                    ClearAt(i);

            foreach (Agent agent in units)
            {
                tyr.DrawLine(agent, Targetting[agent.Unit.Tag].Pos);
                tyr.MicroController.Attack(agent, SC2Util.To2D(Targetting[agent.Unit.Tag].Pos));
            }
        }

        public Dictionary<ulong, Unit> GetAttackers()
        {
            Dictionary<ulong, Unit> attackers = new Dictionary<ulong, Unit>();
            foreach (Unit unit in Bot.Bot.Observation.Observation.RawData.Units)
                if (unit.Owner != Bot.Bot.PlayerId
                    && !IgnoreEnemyTypes.Contains(unit.UnitType)
                    && unit.Owner != Bot.Bot.NeutralPlayerId
                    && unit.UnitType != UnitTypes.ADEPT_PHASE_SHIFT
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
                    if (unit.IsFlying && !Air)
                        continue;
                    if (!unit.IsFlying && Air)
                        continue;
                    float newDist = SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
                    if (newDist >= MaxDefenseRadius * MaxDefenseRadius)
                        continue;

                    bool nearBase = newDist <= MainDefenseRadius * MainDefenseRadius;
                    if (!nearBase)
                    {
                        foreach (Base b in Bot.Bot.BaseManager.Bases)
                            if (b.Owner == Bot.Bot.PlayerId && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= ExpandDefenseRadius * ExpandDefenseRadius)
                            {
                                nearBase = true;
                                break;
                            }
                    }
                    if (nearBase)
                        attackers.Add(unit.Tag, unit);
                }
            return attackers;
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
