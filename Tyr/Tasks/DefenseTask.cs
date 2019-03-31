using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class DefenseTask : Task
    {
        public static DefenseTask GroundDefenseTask = new DefenseTask();
        public static DefenseTask AirDefenseTask = new DefenseTask() { Air = true };
        public int MaxDefenseRadius = 40;
        public int DrawDefenderRadius = 40;
        public int MainDefenseRadius = 30;
        public int ExpandDefenseRadius = 20;
        public bool Air = false;

        private bool Defending = false;

        public HashSet<uint> PreferEnemyTypes = new HashSet<uint>();
        public HashSet<uint> IgnoreEnemyTypes = new HashSet<uint>();

        private Unit Target = null;
        private int TargetCalculatedFrame = 0;

        public DefenseTask() : base(7)
        { }

        public static void Enable()
        {
            GroundDefenseTask.Stopped = false;
            Tyr.Bot.TaskManager.Add(GroundDefenseTask);
            AirDefenseTask.Stopped = false;
            Tyr.Bot.TaskManager.Add(AirDefenseTask);
        }

        public override bool DoWant(Agent agent)
        {
            if (!agent.IsCombatUnit)
                return false;
            if (!agent.CanAttackAir() && Air)
                return false;
            if (!agent.CanAttackGround() && !Air)
                return false;
            if (SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation)) <= DrawDefenderRadius * DrawDefenderRadius)
                return true;
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
                if (b.Owner == Tyr.Bot.PlayerId && agent.DistanceSq(b.BaseLocation.Pos) <= 15 * 15)
                    return true;
            return false;
        }

        public override bool IsNeeded()
        {
            Unit target = GetTarget();
            Defending = target != null;
            return Defending;
        }

        public Unit GetTarget()
        {
            if (TargetCalculatedFrame == Tyr.Bot.Frame)
                return Target;
            TargetCalculatedFrame = Tyr.Bot.Frame;

            Target = null;
            float dist = GetMaxDefenseRadiusSq();

            foreach (Unit unit in Tyr.Bot.Enemies())
                if (unit.Owner != Tyr.Bot.PlayerId
                    && !IgnoreEnemyTypes.Contains(unit.UnitType)
                    && unit.Owner != Tyr.Bot.NeutralPlayerId
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
                    if (!unit.IsFlying && Air && unit.UnitType != UnitTypes.COLLOSUS)
                        continue;
                    float newDist = SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation));
                    if (newDist >= GetMaxDefenseRadiusSq())
                        continue;

                    bool nearBase = newDist <= GetMainDefenseRadiusSq();
                    if (!nearBase)
                    {
                        foreach (Base b in Tyr.Bot.BaseManager.Bases)
                        {
                            if (b.Owner != Tyr.Bot.PlayerId)
                                continue;
                            float expandDist = SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos);
                            if (expandDist >= 18 * 18
                                && (unit.UnitType == UnitTypes.CREEP_TUMOR
                                || unit.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                                || unit.UnitType == UnitTypes.CREEP_TUMOR_QUEEN))
                                continue;
                            if (expandDist <= GetExpandDefenseRadiusSq())
                            {
                                nearBase = true;
                                break;
                            }
                        }
                    }
                    if (!nearBase)
                        continue;

                    if (Target != null && !PreferEnemyTypes.Contains(unit.UnitType) && PreferEnemyTypes.Contains(Target.UnitType))
                        continue;

                    if (newDist >= dist && !PreferEnemyTypes.Contains(unit.UnitType))
                        continue;

                    if (newDist >= dist && Target != null &&  PreferEnemyTypes.Contains(Target.UnitType))
                        continue;

                    if (newDist > GetMaxDefenseRadiusSq())
                        continue;

                    dist = newDist;
                    Target = unit;
                }
            return Target;
        }

        public override void OnFrame(Tyr tyr)
        {
            Unit target = GetTarget();
            if (target == null)
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
                tyr.MicroController.Attack(agent, SC2Util.To2D(target.Pos));
        }

        float GetMainDefenseRadiusSq()
        {
            if (Defending)
                return (MainDefenseRadius + 5) * (MainDefenseRadius + 5);
            else
                return MainDefenseRadius * MainDefenseRadius;
        }

        float GetMaxDefenseRadiusSq()
        {
            if (Defending)
                return (MaxDefenseRadius + 5) * (MaxDefenseRadius + 5);
            else
                return MaxDefenseRadius * MaxDefenseRadius;
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
