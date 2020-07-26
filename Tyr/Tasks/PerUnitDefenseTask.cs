using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class PerUnitDefenseTask : Task
    {
        public static PerUnitDefenseTask AirDefenseTask = new PerUnitDefenseTask() { Air = true };
        public static PerUnitDefenseTask GroundDefenseTask = new PerUnitDefenseTask() { Air = false };
        public int MaxDefenseRadius = 40;
        public int DrawDefenderRadius = 40;
        public int MainDefenseRadius = 30;
        public int ExpandDefenseRadius = 25;
        public bool Air = false;
        public HashSet<uint> AllowedDefenderTypes = new HashSet<uint>();

        Dictionary<ulong, Unit> Targetting = new Dictionary<ulong, Unit>();
        Dictionary<ulong, Unit> UnassignedAttackers = new Dictionary<ulong, Unit>();
        Dictionary<ulong, Unit> AssignedAttackers = new Dictionary<ulong, Unit>();
        private int AttackersUpdateFrame = 0;


        public HashSet<uint> IgnoreEnemyTypes = new HashSet<uint>();

        public PerUnitDefenseTask() : base(8)
        { }

        public static void Enable()
        {
            AirDefenseTask.Stopped = false;
            Bot.Main.TaskManager.Add(AirDefenseTask);
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
            return SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation)) <= DrawDefenderRadius * DrawDefenderRadius;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void Add(Agent agent, UnitDescriptor descriptor)
        {
            base.Add(agent, descriptor);
            Unit enemy = (Unit)descriptor.Marker;

            AssignedAttackers.Add(enemy.Tag, enemy);
            UnassignedAttackers.Remove(enemy.Tag);
            Targetting.Add(agent.Unit.Tag, enemy);
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            UpdateAttackers();
            List<UnitDescriptor> descriptors = new List<UnitDescriptor>();
            foreach (KeyValuePair<ulong, Unit> pair in UnassignedAttackers)
                descriptors.Add(new UnitDescriptor()
                {
                    UnitTypes = AllowedDefenderTypes.Count > 0 ? AllowedDefenderTypes : null,
                    Count = 1,
                    Pos = new Point2D() { X = pair.Value.Pos.X, Y = pair.Value.Pos.Y },
                    Marker = pair.Value
                });
            return descriptors;
        }

        public override void OnFrame(Bot bot)
        {
            UpdateAttackers();

            foreach (Agent agent in units)
            {
                bot.DrawLine(agent, Targetting[agent.Unit.Tag].Pos);
                bot.MicroController.Attack(agent, SC2Util.To2D(Targetting[agent.Unit.Tag].Pos));
            }
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

        private void UpdateAttackers()
        {
            if (AttackersUpdateFrame >= Bot.Main.Frame)
                return;
            AttackersUpdateFrame = Bot.Main.Frame;

            HashSet<ulong> enemiesInRange = new HashSet<ulong>();
            foreach (Unit unit in Bot.Main.Enemies())
                if (!IgnoreEnemyTypes.Contains(unit.UnitType)
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
                    float newDist = SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));
                    if (newDist >= MaxDefenseRadius * MaxDefenseRadius)
                        continue;

                    bool nearBase = newDist <= MainDefenseRadius * MainDefenseRadius;
                    if (!nearBase)
                    {
                        foreach (Base b in Bot.Main.BaseManager.Bases)
                            if (b.Owner == Bot.Main.PlayerId && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= ExpandDefenseRadius * ExpandDefenseRadius)
                            {
                                nearBase = true;
                                break;
                            }
                    }
                    if (nearBase)
                    {
                        enemiesInRange.Add(unit.Tag);
                        if (AssignedAttackers.ContainsKey(unit.Tag))
                            AssignedAttackers[unit.Tag] = unit;
                        else if (UnassignedAttackers.ContainsKey(unit.Tag))
                            UnassignedAttackers[unit.Tag] = unit;
                        else
                            UnassignedAttackers.Add(unit.Tag, unit);
                    }
                }
            HashSet<ulong> removeEnemies = new HashSet<ulong>();
            foreach (ulong enemyTag in UnassignedAttackers.Keys)
                if (!enemiesInRange.Contains(enemyTag))
                    removeEnemies.Add(enemyTag);
            foreach (ulong removeEnemy in removeEnemies)
                UnassignedAttackers.Remove(removeEnemy);
            removeEnemies = new HashSet<ulong>();

            foreach (ulong enemyTag in AssignedAttackers.Keys)
                if (!enemiesInRange.Contains(enemyTag))
                    removeEnemies.Add(enemyTag);
            foreach (ulong removeEnemy in removeEnemies)
                AssignedAttackers.Remove(removeEnemy);

            HashSet<ulong> removeAgents = new HashSet<ulong>();
            foreach (KeyValuePair<ulong, Unit> pair in Targetting)
                if (removeEnemies.Contains(pair.Value.Tag))
                    removeAgents.Add(pair.Key);

            foreach (ulong removeAgent in removeAgents)
                Targetting.Remove(removeAgent);
            HashSet<ulong> existingAgentTags = new HashSet<ulong>();
            for (int i = Units.Count - 1; i >= 0; i--)
            {
                if (removeAgents.Contains(Units[i].Unit.Tag))
                    ClearAt(i);
                else existingAgentTags.Add(Units[i].Unit.Tag);
            }
            HashSet<ulong> deadAgents = new HashSet<ulong>();
            foreach (ulong agentTag in Targetting.Keys)
                if (!existingAgentTags.Contains(agentTag))
                    deadAgents.Add(agentTag);
            foreach (ulong deadAgent in deadAgents)
            {
                if (Targetting.ContainsKey(deadAgent))
                {
                    Unit target = Targetting[deadAgent];
                    Targetting.Remove(deadAgent);
                    if (AssignedAttackers.ContainsKey(target.Tag))
                    {
                        AssignedAttackers.Remove(target.Tag);
                        UnassignedAttackers.Add(target.Tag, target);
                    }
                }
            }

            foreach (Agent agent in Units)
            {
                if (!Targetting.ContainsKey(agent.Unit.Tag))
                    continue;
                if (!AssignedAttackers.ContainsKey(Targetting[agent.Unit.Tag].Tag))
                    continue;
                Targetting[agent.Unit.Tag] = AssignedAttackers[Targetting[agent.Unit.Tag].Tag];
            }
            Bot.Main.DrawText("Defense unit count: " + Units.Count);
            Bot.Main.DrawText("Defense unassigned attackers count: " + UnassignedAttackers.Count);
            Bot.Main.DrawText("Defense assigned attackers count: " + AssignedAttackers.Count);
        }

        internal bool IsDefending()
        {
            UpdateAttackers();
            return UnassignedAttackers.Count + AssignedAttackers.Count > 0;
        }
    }
}
