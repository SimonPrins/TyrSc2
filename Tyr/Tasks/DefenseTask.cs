using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class DefenseTask : Task
    {
        public static DefenseTask Task = new DefenseTask();
        public int MaxDefenseRadius = 40;
        public int MainDefenseRadius = 30;
        public int ExpandDefenseRadius = 20;

        public HashSet<uint> IgnoreEnemyTypes = new HashSet<uint>();

        public DefenseTask() : base(7)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit && SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(Tyr.Bot. MapAnalyzer.StartLocation)) <= MaxDefenseRadius * MaxDefenseRadius;
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
            Unit target = null;
            float dist = MaxDefenseRadius * MaxDefenseRadius;

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
                    if (nearBase && newDist < dist)
                    {
                        dist = newDist;
                        target = unit;
                    }
                }
            if (target == null)
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
                tyr.MicroController.Attack(agent, SC2Util.To2D(target.Pos));
        }
    }
}
