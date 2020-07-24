using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class LiberatorController : CustomController
    {
        public Dictionary<ulong, int> LastEnemyFrame = new Dictionary<ulong, int>();
        public Dictionary<ulong, Point2D> SiegeTarget = new Dictionary<ulong, Point2D>();
        public int KeepLiberatorSiegedTime = 5;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.LIBERATOR
                && agent.Unit.UnitType != UnitTypes.LIBERATOR_AG)
                return false;

            bool closeEnemy = false;
            if (!LastEnemyFrame.ContainsKey(agent.Unit.Tag))
                LastEnemyFrame.Add(agent.Unit.Tag, 0);
            else if (Bot.Main.Frame - LastEnemyFrame[agent.Unit.Tag] <= 22.4 * KeepLiberatorSiegedTime)
                closeEnemy = true;
            

            Unit airDefense = GetAirDefense(agent);
            if (airDefense != null)
            {
                if (agent.Unit.UnitType == UnitTypes.LIBERATOR)
                    agent.Order(Abilities.MOVE, agent.From(airDefense, 4));
                else 
                    agent.Order(2560);
                return true;
            }

            bool alreadySieging = SiegeTarget.ContainsKey(agent.Unit.Tag);
            if (agent.Unit.UnitType == UnitTypes.LIBERATOR)
            {
                if (alreadySieging && EnemyInSiegeRange(agent))
                {
                    LastEnemyFrame[agent.Unit.Tag] = Bot.Main.Frame;
                    agent.Order(2558, SiegeTarget[agent.Unit.Tag]);
                    return true;
                }

                Point2D siegeTarget = GetSiegeTarget(agent);
                if (siegeTarget != null)
                {
                    LastEnemyFrame[agent.Unit.Tag] = Bot.Main.Frame;
                    SiegeTarget[agent.Unit.Tag] = siegeTarget;
                    agent.Order(2558, SiegeTarget[agent.Unit.Tag]);
                }

                return false;
            } else
            {
                if (EnemyInSiegeRange(agent))
                {
                    LastEnemyFrame[agent.Unit.Tag] = Bot.Main.Frame;
                }
                else if (GetUnderAttack(agent) != null || !closeEnemy)
                {
                    SiegeTarget.Remove(agent.Unit.Tag);
                    agent.Order(2560);
                }
                return true;
            }
        }

        private Unit GetAirDefense(Agent agent)
        {
            float distance = 8 * 8;
            Unit result = null;
            foreach (Unit airDefense in Bot.Main.Enemies())
            {
                if (airDefense.UnitType != UnitTypes.BUNKER
                    && airDefense.UnitType != UnitTypes.MISSILE_TURRET
                    && airDefense.UnitType != UnitTypes.PHOTON_CANNON
                    && airDefense.UnitType != UnitTypes.SPORE_CRAWLER)
                    continue;

                float newDist = agent.DistanceSq(airDefense);
                if (newDist < distance)
                {
                    distance = newDist;
                    result = airDefense;
                }
            }
            return result;
        }

        private Point2D GetSiegeTarget(Agent agent)
        {
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.IsFlying)
                    continue;

                if (enemy.UnitType == UnitTypes.CREEP_TUMOR
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_QUEEN)
                    continue;

                if (enemy.UnitType == UnitTypes.ADEPT_PHASE_SHIFT
                    || enemy.UnitType == UnitTypes.KD8_CHARGE)
                    continue;

                if (enemy.UnitType == UnitTypes.BROODLING)
                    continue;

                if (UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                    continue;

                if (agent.DistanceSq(enemy) > 8 * 8)
                    continue;

                PotentialHelper potential = new PotentialHelper(enemy.Pos);
                potential.Magnitude = 3;
                potential.To(agent.Unit);
                Point2D siegeTarget = potential.Get();
                
                if (!LiberationZoneTooClose(agent, siegeTarget))
                    return siegeTarget;
            }
            return null;
        }

        private bool LiberationZoneTooClose(Agent agent, Point2D siegeTarget)
        {
            foreach (Agent liberator in Bot.Main.UnitManager.Agents.Values)
            {
                if (liberator.Unit.UnitType != UnitTypes.LIBERATOR
                    && liberator.Unit.UnitType != UnitTypes.LIBERATOR_AG)
                    continue;

                if (!SiegeTarget.ContainsKey(liberator.Unit.Tag))
                    continue;
                if (agent.Unit.Tag == liberator.Unit.Tag)
                    continue;

                Point2D liberationZone = SiegeTarget[liberator.Unit.Tag];

                if (SC2Util.DistanceSq(siegeTarget, liberationZone) <= 4 * 4)
                    return true;
            }
            return false;
        }

        private bool EnemyInSiegeRange(Agent agent)
        {
            if (!SiegeTarget.ContainsKey(agent.Unit.Tag))
                return false;

            Point2D siegeTarget = SiegeTarget[agent.Unit.Tag];
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.IsFlying)
                    continue;

                if (enemy.UnitType == UnitTypes.CREEP_TUMOR
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_QUEEN)
                    continue;

                if (enemy.UnitType == UnitTypes.ADEPT_PHASE_SHIFT
                    || enemy.UnitType == UnitTypes.KD8_CHARGE)
                    continue;

                if (enemy.UnitType == UnitTypes.BROODLING)
                    continue;

                if (UnitTypes.BuildingTypes.Contains(enemy.UnitType))
                    continue;

                if (SC2Util.DistanceSq(siegeTarget, enemy.Pos) <= 5 * 5)
                    return true;
            }
            return false;

        }

        private Unit GetUnderAttack(Agent agent)
        {
            float distance = 8 * 8;
            Unit result = null;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < distance)
                {
                    distance = newDist;
                    result = enemy;
                }
            }
            return result;
        }
    }
}
