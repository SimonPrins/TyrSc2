using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds;
using Tyr.Util;

namespace Tyr.Tasks
{
    class MassSentriesTask : Task
    {
        public static MassSentriesTask Task = new MassSentriesTask();

        public int RequiredSize = 20;
        public int RetreatSize = 6;
        public int StretchGoal = 0;

        public int LastAttackingFrame = -100;
        public int LastStretchFrame = -100;

        public bool AttackSent = false;

        private ForceFieldUtil ForceFieldUtil = new ForceFieldUtil();

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Bot.TaskManager.Add(Task);
        }

        public MassSentriesTask() : base(5)
        {
            this.JoinCombatSimulation = true;
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.SENTRY;
        }

        public override bool IsNeeded()
        {
            if (Stopped)
                return false;

            int sentryCount = Bot.Bot.UnitManager.Completed(UnitTypes.SENTRY);
            Bot.Bot.DrawText("Needed Sentries: " + sentryCount + "/" + Math.Max(StretchGoal + 5, RequiredSize));
            if (LastAttackingFrame >= Bot.Bot.Frame - 1)
            {
                if (sentryCount >= RequiredSize)
                {
                    AttackSent = true;
                    return true;
                }
                return false;
            }
            if (sentryCount >= Math.Max(StretchGoal + 5, RequiredSize) || Build.FoodUsed() > 194)
            {
                AttackSent = true;
                return true;
            }
            return false;
        }

        public override void OnFrame(Bot tyr)
        {
            if (units.Count <= RetreatSize && Units.Count > 0)
            {
                Clear();
                return;
            }

            tyr.DrawText("Army size: " + Units.Count);

            if (Units.Count > 0)
            {
                if (LastAttackingFrame >= Bot.Bot.Frame - 1 && tyr.Frame - LastStretchFrame >= 22.3 * 60)
                {
                    LastStretchFrame = tyr.Frame;
                    StretchGoal = Math.Max(Math.Min(Units.Count, StretchGoal + 5), StretchGoal);
                }
                LastAttackingFrame = tyr.Frame;
            }
            ForceFieldUtil.DetermineForceFieldPlacement(Units);


            List<Unit> threatenedForceFields = new List<Unit>();
            foreach (Unit unit in tyr.Observation.Observation.RawData.Units)
            {
                if (unit.UnitType != UnitTypes.FORCE_FIELD)
                    continue;
                if (SC2Util.DistanceSq(unit.Pos, tyr.MapAnalyzer.StartLocation) <= 30 * 30)
                    continue;
                bool closeMeleeEnemy = false;
                bool closeRangedEnemy = false;
                foreach (Unit enemy in Bot.Bot.Enemies())
                {
                    if (UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                        continue;
                    if (enemy.IsFlying)
                        continue;
                    if (enemy.UnitType == UnitTypes.LARVA
                        || enemy.UnitType == UnitTypes.EGG)
                        continue;
                    float dist = SC2Util.DistanceSq(unit.Pos, enemy.Pos);
                    if (UnitTypes.WorkerTypes.Contains(enemy.UnitType)
                        || enemy.UnitType == UnitTypes.ZEALOT
                        || enemy.UnitType == UnitTypes.ZERGLING
                        || enemy.UnitType == UnitTypes.BROODLING)
                    {
                        if (dist <= 4 * 4)
                            closeMeleeEnemy = true;
                    }
                    else
                    {
                        if (dist <= 6 * 6)
                            closeRangedEnemy = true;
                    }
                }
                if (closeRangedEnemy && !closeMeleeEnemy)
                    threatenedForceFields.Add(unit);
            }

            foreach (Agent agent in units)
            {
                if (ForceFieldUtil.Place(agent))
                    continue;

                float dist = 12 * 12;
                Unit closeForceField = null;
                foreach (Unit forceField in threatenedForceFields)
                {
                    float newDist = agent.DistanceSq(forceField);
                    if (newDist >= dist)
                        continue;
                    dist = newDist;
                    closeForceField = forceField;
                }

                foreach (Unit enemy in Bot.Bot.Enemies())
                {
                    if (closeForceField == null)
                        break;
                    if (agent.DistanceSq(enemy) <= dist)
                        closeForceField = null;
                }

                if (closeForceField != null)
                {
                    agent.Flee(closeForceField.Pos);
                    continue;
                }


                Attack(agent, tyr.TargetManager.AttackTarget);
            }
        }
    }
}
