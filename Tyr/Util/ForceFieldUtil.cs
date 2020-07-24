using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Util
{
    public class ForceFieldUtil
    {
        public int NumberOfForceFields = 6;
        private int ForceFieldPlacementFrame = -100;
        private Dictionary<ulong, Point2D> ForceFieldPlacementAssignments = new Dictionary<ulong, Point2D>();
        public void DetermineForceFieldPlacement(List<Agent> units)
        {
            if (Bot.Bot.Frame - ForceFieldPlacementFrame < 250)
                return;

            List<Unit> sentries = new List<Unit>();
            foreach (Agent agent in units)
                if (agent.Unit.UnitType == UnitTypes.SENTRY)
                    sentries.Add(agent.Unit);

            List<List<Unit>> sentryGroups = GroupUnits(sentries);
            List<Unit> mainSentryGroup = new List<Unit>();
            foreach (List<Unit> sentryGroup in sentryGroups)
                if (sentryGroup.Count > mainSentryGroup.Count)
                    mainSentryGroup = sentryGroup;

            Bot.Bot.DrawText("MainSentryGroup size: " + mainSentryGroup.Count);

            if (mainSentryGroup.Count < 8)
                return;
            List<Unit> closeEnemies = new List<Unit>();
            HashSet<ulong> enemyTags = new HashSet<ulong>();
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (!ConsiderEnemy(enemy))
                    continue;
                foreach (Unit sentry in mainSentryGroup)
                    if (SC2Util.DistanceSq(sentry.Pos, enemy.Pos) <= 10 * 10)
                    {
                        closeEnemies.Add(enemy);
                        enemyTags.Add(enemy.Tag);
                        break;
                    }
            }

            for (int i = 0; i < closeEnemies.Count; i++)
            {
                Unit closeEnemy = closeEnemies[i];
                foreach (Unit enemy in Bot.Bot.Enemies())
                {
                    if (!ConsiderEnemy(enemy))
                        continue;
                    if (enemyTags.Contains(enemy.Tag))
                        continue;
                    if (SC2Util.DistanceSq(closeEnemy.Pos, enemy.Pos) <= 4 * 4)
                    {
                        closeEnemies.Add(enemy);
                        enemyTags.Add(enemy.Tag);
                    }
                }
            }

            List<List<Unit>> enemyGroups = GroupUnits(closeEnemies);
            List<Unit> mainEnemyGroup = new List<Unit>();
            foreach (List<Unit> enemyGroup in enemyGroups)
                if (enemyGroup.Count > mainEnemyGroup.Count)
                    mainEnemyGroup = enemyGroup;

            Bot.Bot.DrawText("mainEnemyGroup size: " + mainEnemyGroup.Count);
            if (mainEnemyGroup.Count < 6)
                return;

            int zerglingCount = 0;
            int zealotCount = 0;
            foreach (Unit enemy in mainEnemyGroup)
            {
                if (enemy.UnitType == UnitTypes.ZEALOT)
                    zealotCount++;
                if (enemy.UnitType == UnitTypes.ZERGLING)
                    zerglingCount++;
            }

            bool closeForceFields = zealotCount >= 4 || zerglingCount >= 6;

            Point2D sentryCenter = CenterOfMass(mainSentryGroup);
            Point2D enemyCenter = CenterOfMass(mainEnemyGroup);

            Point2D towardEnemy = new Point2D() { X = enemyCenter.X - sentryCenter.X, Y = enemyCenter.Y - sentryCenter.Y };
            float length = (float)Math.Sqrt(towardEnemy.X * towardEnemy.X + towardEnemy.Y * towardEnemy.Y);
            towardEnemy.X /= length;
            towardEnemy.Y /= length;
            Point2D rightAngle = new Point2D() { X = towardEnemy.Y, Y = -towardEnemy.X };

            Dictionary<ulong, Point2D> newForceFieldLocations = new Dictionary<ulong, Point2D>();

            for (int i = 0; i < NumberOfForceFields; i++)
            {
                Point2D forceFieldLocation;
                if (closeForceFields && (i == 0 || i == NumberOfForceFields - 1))
                {
                    float iMiddle = i == 0 ? 0.5f : (i - 0.5f);
                    float x = sentryCenter.X + 1f * towardEnemy.X + (-1.5f * (NumberOfForceFields - 1) + 3 * iMiddle) * rightAngle.X;
                    float y = sentryCenter.Y + 1f * towardEnemy.Y + (-1.5f * (NumberOfForceFields - 1) + 3 * iMiddle) * rightAngle.Y;
                    forceFieldLocation = new Point2D() { X = x, Y = y };
                }
                else if (closeForceFields)
                {
                    float x = sentryCenter.X + 4 * towardEnemy.X + (-1.5f * (NumberOfForceFields - 1) + 3 * i) * rightAngle.X;
                    float y = sentryCenter.Y + 4 * towardEnemy.Y + (-1.5f * (NumberOfForceFields - 1) + 3 * i) * rightAngle.Y;
                    forceFieldLocation = new Point2D() { X = x, Y = y };
                }
                else
                    forceFieldLocation = new Point2D() { X = enemyCenter.X + (-1.5f * (NumberOfForceFields - 1) + 3 * i) * rightAngle.X, Y = enemyCenter.Y + (-1.5f * (NumberOfForceFields - 1) + 3 * i) * rightAngle.Y };
                float dist = 14 * 14;
                Agent pickedSentry = null;
                foreach (Agent agent in units)
                {
                    if (newForceFieldLocations.ContainsKey(agent.Unit.Tag))
                        continue;
                    if (agent.Unit.UnitType != UnitTypes.SENTRY)
                        continue;
                    if (agent.Unit.Energy < 50)
                        continue;
                    if (Bot.Bot.Frame - agent.LastOrderFrame < 3 && agent.LastAbility != Abilities.MOVE && agent.LastAbility != Abilities.ATTACK)
                        continue;
                    float newDist = agent.DistanceSq(forceFieldLocation);
                    if (newDist >= dist)
                        continue;
                    dist = newDist;
                    pickedSentry = agent;
                }
                if (pickedSentry == null)
                {
                    Bot.Bot.DrawText("Failed to find Sentry for forceField placement.");
                    return;
                }
                newForceFieldLocations.Add(pickedSentry.Unit.Tag, forceFieldLocation);
            }
            ForceFieldPlacementFrame = Bot.Bot.Frame;
            ForceFieldPlacementAssignments = newForceFieldLocations;
        }

        private bool ConsiderEnemy(Unit enemy)
        {
            if (enemy.IsFlying)
                return false;
            if (UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                return false;
            if (enemy.UnitType == UnitTypes.LARVA
                || enemy.UnitType == UnitTypes.EGG
                || UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                return false;
            return true;
        }

        public bool Place(Agent agent)
        {
            if (ForceFieldPlacementAssignments.ContainsKey(agent.Unit.Tag))
            {
                foreach (Unit unit in Bot.Bot.Observation.Observation.RawData.Units)
                {
                    if (unit.UnitType != 135)
                        continue;
                    if (SC2Util.DistanceSq(unit.Pos, ForceFieldPlacementAssignments[agent.Unit.Tag]) < 2)
                    {
                        ForceFieldPlacementAssignments.Remove(agent.Unit.Tag);
                        return false;
                    }
                }
                agent.Order(1526, ForceFieldPlacementAssignments[agent.Unit.Tag]);
                return true;
            }
            return false;
        }

        private Point2D CenterOfMass(List<Unit> units)
        {
            Point2D result = new Point2D();
            if (units.Count == 0)
                return result;
            foreach (Unit unit in units)
            {
                result.X += unit.Pos.X;
                result.Y += unit.Pos.Y;
            }
            result.X /= units.Count;
            result.Y /= units.Count;
            return result;
        }

        private List<List<Unit>> GroupUnits(List<Unit> units)
        {
            List<List<Unit>> groups = new List<List<Unit>>();

            while (units.Count > 0)
            {
                List<Unit> simulationGroup = new List<Unit>();

                simulationGroup.Add(units[units.Count - 1]);
                units.RemoveAt(units.Count - 1);

                for (int j = 0; j < simulationGroup.Count; j++)
                {
                    Unit current = simulationGroup[j];
                    for (int i = units.Count - 1; i >= 0; i--)
                    {
                        Unit compare = units[i];
                        if (SC2Util.DistanceSq(current.Pos, compare.Pos) > 4 * 4)
                            continue;
                        simulationGroup.Add(compare);
                        CollectionUtil.RemoveAt(units, i);
                    }
                }
                groups.Add(simulationGroup);
            }
            return groups;
        }
    }
}
