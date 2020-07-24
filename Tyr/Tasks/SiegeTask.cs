using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class SiegeTask : Task
    {
        public static SiegeTask Task = new SiegeTask();

        public int RequiredSize { get; set; } = 14;
        public int RetreatSize { get; set; } = 0;

        State CurrentState = State.Advance;

        private HashSet<ulong> UnsiegingTanks = new HashSet<ulong>();

        enum State
        {
            Advance,
            SiegeUp,
            LeapFrog,
            Contain,
            Kill
        }

        public SiegeTask() : base(5)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit;
        }

        public override bool IsNeeded()
        {
            int combatUnits = 0;
            foreach (uint combatType in UnitTypes.CombatUnitTypes)
                if (!UnitTypes.EquivalentTypes.ContainsKey(combatType))
                    combatUnits += Bot.Bot.UnitManager.Completed(combatType);
            if (combatUnits >= RequiredSize)
                return true;
            return false;
        }

        public override void OnFrame(Bot tyr)
        {
            if (units.Count <= RetreatSize)
            {
                CurrentState = State.Advance;
                Clear();
                return;
            }

            bool canAttackGround = false;
            foreach (Agent agent in Units)
                if (agent.CanAttackGround())
                    canAttackGround = true;

            if (!canAttackGround)
            {
                CurrentState = State.Advance;
                Clear();
                return;
            }

            tyr.DrawText("SiegeTask state: " + CurrentState);

            if (CurrentState == State.Advance)
                Advance(tyr);
            else if (CurrentState == State.SiegeUp)
                SiegeUp(tyr);
            else if (CurrentState == State.LeapFrog)
                LeapFrog(tyr);
            else if (CurrentState == State.Kill)
                Kill(tyr);
        }

        public void Advance(Bot tyr)
        {
            bool enemyClose = false;

            foreach (Agent agent in units)
            {
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (!UnitTypes.CanAttackGround(enemy.UnitType) && !UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                        continue;

                    if (!UnitTypes.RangedTypes.Contains(enemy.UnitType) && !UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                        continue;

                    if (enemy.IsFlying)
                        continue;

                    if (agent.DistanceSq(enemy) <= 15 * 15)
                    {
                        enemyClose = true;
                        break;
                    }
                }
                Attack(agent, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }

            if (enemyClose)
                CurrentState = State.SiegeUp;
        }

        public void SiegeUp(Bot tyr)
        {
            bool tanksAreSieged = true;
            List<Agent> tanks = GetTanks();

            foreach (Agent agent in units)
            {
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED)
                    continue;
                else if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK
                    && agent.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]) <= 50 * 50)
                {
                    bool closeEnemy = false;
                    foreach (Unit enemy in Bot.Bot.Enemies())
                        if (agent.DistanceSq(enemy) <= 14 * 14)
                        {
                            closeEnemy = true;
                            break;
                        }
                    if (closeEnemy)
                    {
                        tanksAreSieged = false;
                        agent.Order(Abilities.SIEGE);
                        continue;
                    }
                }
                else
                {
                    Agent closestTank = null;
                    float tankDistance = 18 * 18;
                    foreach (Agent tank in tanks)
                    {
                        float newDist = agent.DistanceSq(tank);
                        if (newDist < tankDistance)
                        {
                            tankDistance = newDist;
                            closestTank = tank;
                        }
                    }

                    if (tankDistance >= 4 * 4 && closestTank != null)
                    {
                        agent.Order(Abilities.MOVE, SC2Util.To2D(closestTank.Unit.Pos));
                        continue;
                    }
                }
                Attack(agent, tyr.TargetManager.PotentialEnemyStartLocations[0]);

            }

            if (tanksAreSieged)
                CurrentState = State.LeapFrog;
        }

        public void LeapFrog(Bot tyr)
        {
            List<Agent> tanks = GetTanks();

            foreach (Agent tank in tanks)
                if (tyr.MapAnalyzer.EnemyDistances[(int)tank.Unit.Pos.X, (int)tank.Unit.Pos.Y] <= 50
                    || tank.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]) <= 20 * 20)
                {
                    CurrentState = State.Kill;
                    break;
                }

            int tanksSieged = 0;
            int tanksUnsieged = 0;
            foreach (Agent agent in units)
            {
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK)
                    tanksUnsieged++;
                else if(agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED)
                {
                    if (UnsiegingTanks.Contains(agent.Unit.Tag))
                        tanksUnsieged++;
                    else
                        tanksSieged++;
                }
            }

            int allowedUnsiegedTanks = (tanksUnsieged + tanksSieged) / 2;
            if (allowedUnsiegedTanks < 2)
                allowedUnsiegedTanks = 2;

            List<Agent> potentialUnsiegers = GetPotentialUnsiegers();

            foreach (Agent agent in potentialUnsiegers)
            {
                if (allowedUnsiegedTanks - tanksUnsieged <= 0)
                    break;

                bool enemyClose = false;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (!UnitTypes.CanAttackGround(enemy.UnitType) && !UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                        continue;

                    if (!UnitTypes.RangedTypes.Contains(enemy.UnitType) && !UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                        continue;

                    if (enemy.IsFlying)
                        continue;

                    if (agent.DistanceSq(enemy) <= 13 * 13)
                    {
                        enemyClose = true;
                        break;
                    }
                }
                if (enemyClose)
                    tyr.DrawSphere(agent.Unit.Pos, 1, new Color() { R = 0, G = 255, B = 0 });
                
            }

            foreach (Agent agent in potentialUnsiegers)
            {
                if (allowedUnsiegedTanks - tanksUnsieged <= 0)
                    break;

                bool enemyClose = false;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (!UnitTypes.CanAttackGround(enemy.UnitType) && !UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                        continue;

                    if (!UnitTypes.RangedTypes.Contains(enemy.UnitType) && !UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                        continue;

                    if (enemy.IsFlying)
                        continue;

                    if (agent.DistanceSq(enemy) <= 13 * 13)
                    {
                        enemyClose = true;
                        break;
                    }
                }
                if (enemyClose)
                    continue;

                allowedUnsiegedTanks--;
                UnsiegingTanks.Add(agent.Unit.Tag);
            }

            float closestDistance = 1000000;

            foreach (Agent agent in Units)
            {
                if (UnsiegingTanks.Contains(agent.Unit.Tag))
                    tyr.DrawSphere(agent.Unit.Pos);
                if (agent.Unit.UnitType != UnitTypes.SIEGE_TANK_SIEGED
                    || UnsiegingTanks.Contains(agent.Unit.Tag))
                    continue;

                tyr.DrawLine(agent, tyr.TargetManager.PotentialEnemyStartLocations[0]);

                float distSq = agent.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]);
                if (distSq < closestDistance)
                    closestDistance = distSq;
            }

            float minimumDist = (float)Math.Sqrt(closestDistance) + 8;

            float farthestDistance = 0;
            foreach (Agent agent in Units)
            {
                if (agent.Unit.UnitType != UnitTypes.SIEGE_TANK_SIEGED
                    || UnsiegingTanks.Contains(agent.Unit.Tag))
                    continue;

                float distSq = agent.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]);
                if (distSq >= minimumDist * minimumDist)
                    continue;

                if (distSq > farthestDistance)
                    farthestDistance = distSq;
            }

            farthestDistance = (float)Math.Sqrt(farthestDistance);
            closestDistance = (float)Math.Sqrt(closestDistance);

            tyr.DrawText("Closest tank dist: " + closestDistance);
            tyr.DrawText("Farthest tank dist: " + farthestDistance);


            foreach (Agent agent in Units)
            {
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK)
                {
                    if (!UnsiegingTanks.Contains(agent.Unit.Tag))
                        agent.Order(Abilities.SIEGE);
                    else
                    {
                        float dist = agent.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]);
                        if (dist < (farthestDistance - 6) * (farthestDistance - 6) || dist < (closestDistance - 2) + (closestDistance - 2)
                            && SufficientlySpread(agent))
                        {
                            UnsiegingTanks.Remove(agent.Unit.Tag);
                            agent.Order(Abilities.UNSIEGE);
                        }
                        else
                            Attack(agent, tyr.TargetManager.PotentialEnemyStartLocations[0]);
                    }
                } else if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED)
                {
                    if (UnsiegingTanks.Contains(agent.Unit.Tag))
                    {
                        bool closeEnemy = false;
                        foreach (Unit enemy in Bot.Bot.Enemies())
                        {
                            if (!UnitTypes.CanAttackGround(enemy.UnitType) && !UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                                continue;

                            if (!UnitTypes.RangedTypes.Contains(enemy.UnitType) && !UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                                continue;

                            if (enemy.IsFlying)
                                continue;

                            if (agent.DistanceSq(enemy) <= 13 * 13)
                            {
                                closeEnemy = true;
                                break;
                            }
                        }
                        if (!closeEnemy)
                            agent.Order(Abilities.UNSIEGE);
                    }
                } else
                {
                    if (agent.Unit.UnitType == UnitTypes.HELLBAT
                        || agent.Unit.UnitType == UnitTypes.HELLION
                        || agent.Unit.UnitType == UnitTypes.MARINE)
                    {
                        Agent closestTank = null;
                        float distance = 18 * 18;
                        foreach (Agent tank in tanks)
                        {
                            float newDist = tank.DistanceSq(agent);
                            if (newDist < distance)
                            {
                                closestTank = tank;
                                distance = newDist;
                            }
                        }
                        if (distance >= 5 * 5 && closestTank != null)
                        {
                            agent.Order(Abilities.MOVE, SC2Util.To2D(closestTank.Unit.Pos));
                            continue;
                        }

                    }
                    Attack(agent, tyr.TargetManager.PotentialEnemyStartLocations[0]);
                }
            }
        }

        private bool SufficientlySpread(Agent agent)
        {
            foreach (Agent other in units)
            {
                if (other.Unit.UnitType != UnitTypes.SIEGE_TANK
                    && other.Unit.UnitType != UnitTypes.SIEGE_TANK_SIEGED)
                    continue;
                if (UnsiegingTanks.Contains(other.Unit.UnitType))
                    continue;
                if (other.Unit.Tag == agent.Unit.Tag)
                    continue;

                if (agent.DistanceSq(other) <= 2 * 2)
                    return false;
            }
            return true;
        }

        public void Kill(Bot tyr)
        {
            foreach (Agent agent in Units)
                Attack(agent, tyr.TargetManager.AttackTarget);
        }

        private List<Agent> GetTanks()
        {
            List<Agent> result = new List<Agent>();

            foreach (Agent agent in Units)
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED
                    || agent.Unit.UnitType == UnitTypes.SIEGE_TANK)
                    result.Add(agent);

            return result;
        }

        private List<Agent> GetPotentialUnsiegers()
        {
            List<Agent> result = new List<Agent>();

            foreach (Agent agent in Units)
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED
                    && !UnsiegingTanks.Contains(agent.Unit.Tag))
                    result.Add(agent);

            result.Sort((agent1, agent2) => {
                return Math.Sign(agent2.DistanceSq(Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]) - agent1.DistanceSq(Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]));
            });

            return result;
        }
    }
}
