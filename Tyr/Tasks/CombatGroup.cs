using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class CombatGroup
    {
        public List<Agent> Units = new List<Agent>();

        public int State = Charge;

        private Agent MedivacRetreatTarget = null;
        private int MedivacRetreatTargetUpdateFrame = 0;

        public static int Retreat = 0;
        public static int Attack = 1;
        public static int Charge = 2;

        public void AttackAt(Point2D target, Task task)
        {
            List<Unit> closeEnemies = GetCloseEnemies();
            int totalResources = 0;
            foreach (Agent agent in Units)
                totalResources += (int)UnitTypes.LookUp[agent.Unit.UnitType].VespeneCost + (int)UnitTypes.LookUp[agent.Unit.UnitType].MineralCost;

            int enemyResources = 0;
            int enemyMeleeUnits = 0;
            foreach (Unit enemy in closeEnemies)
            {
                enemyResources += (int)UnitTypes.LookUp[enemy.UnitType].VespeneCost + (int)UnitTypes.LookUp[enemy.UnitType].MineralCost;
                if (!UnitTypes.RangedTypes.Contains(enemy.UnitType) && !UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    enemyMeleeUnits++;
            }

            Bot.Bot.DrawText("totalResources: " + totalResources + " enemyResources: " + enemyResources);

            if (State == Charge)
            {
                if (totalResources < enemyResources)
                    State = Retreat;
                else if (totalResources < enemyResources * 2)
                    State = Attack;
            } else if (State == Attack)
            {
                if (totalResources >= 3 * enemyResources)
                    State = Charge;
                else if (totalResources * 4 < enemyResources * 3)
                    State = Retreat;
            } else if (State == Retreat)
            {
                if (totalResources >= enemyResources * 2)
                    State = Charge;
                else if (totalResources > enemyResources)
                    State = Attack;
            }


            Bot.Bot.DrawText("CombatState: " + State);

            foreach (Agent agent in Units)
            {
                if (agent.Unit.UnitType == UnitTypes.MEDIVAC)
                {
                    UpdateMedivacRetreatTarget(Bot.Bot);
                    if (MedivacRetreatTarget != null)
                    {
                        task.Attack(agent, SC2Util.To2D(MedivacRetreatTarget.Unit.Pos));
                        continue;
                    }
                }

                if (State == Charge)
                {
                    if (enemyMeleeUnits >= 6 || agent.Unit.UnitType == UnitTypes.INFESTOR || agent.Unit.UnitType == UnitTypes.INFESTOR_BURROWED)
                        task.Attack(agent, target);

                    Point2D toward = target;
                    float distance = 12 * 12;
                    foreach (Unit enemy in closeEnemies)
                    {
                        if ((!agent.CanAttackGround() && !enemy.IsFlying) || (!agent.CanAttackAir() && enemy.IsFlying))
                            continue;

                        float dist = agent.DistanceSq(enemy);
                        if (dist < distance)
                        {
                            distance = dist;
                            toward = SC2Util.To2D(enemy.Pos);
                        }
                    }

                    if (agent.Unit.WeaponCooldown < 3)
                        task.Attack(agent, toward);
                    else if (distance <= 2 * 2)
                        agent.Order(Abilities.MOVE, agent.From(target, 3));
                    else
                        agent.Order(Abilities.MOVE, toward);
                } else if (State == Attack)
                    task.Attack(agent, target);
                else if (State == Retreat)
                {
                    bool close = false;
                    foreach (Unit enemy in closeEnemies)
                        if (agent.DistanceSq(enemy) <= 12 * 12)
                        {
                            close = true;
                            break;
                        }
                    if (!close)
                        task.Attack(agent, target);
                    if (agent.Unit.WeaponCooldown < 3)
                        task.Attack(agent, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
                    else
                        agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
                }
            }
        }

        private void UpdateMedivacRetreatTarget(Bot tyr)
        {
            if (MedivacRetreatTargetUpdateFrame == tyr.Frame)
                return;
            MedivacRetreatTargetUpdateFrame = tyr.Frame;

            float distance = 1000 * 1000;
            foreach (Agent agent in Units)
            {
                if (agent.Unit.IsFlying)
                    continue;

                float newDist = agent.DistanceSq(tyr.TargetManager.AttackTarget);
                if (newDist < distance)
                {
                    distance = newDist;
                    MedivacRetreatTarget = agent;
                }
            }
        }

        public List<Unit> GetCloseEnemies()
        {
            HashSet<ulong> alreadyInList = new HashSet<ulong>();
            List<Unit> enemies = new List<Unit>();
            foreach (Unit enemy in Bot.Bot.Enemies())
                foreach (Agent agent in Units)
                    if ((UnitTypes.CombatUnitTypes.Contains(enemy.UnitType) || UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                        && agent.DistanceSq(enemy) <= 10 * 10)
                    {
                        enemies.Add(enemy);
                        alreadyInList.Add(enemy.Tag);
                        break;
                    }

            for (int i = 0; i < enemies.Count; i++)
            {
                Unit closeEnemy = enemies[i];
                foreach (Unit enemy in Bot.Bot.Enemies())
                    if (!alreadyInList.Contains(enemy.Tag) && (UnitTypes.CombatUnitTypes.Contains(enemy.UnitType) || UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                        && SC2Util.DistanceSq(closeEnemy.Pos, enemy.Pos) <= 4 * 4)
                    {
                        enemies.Add(enemy);
                        alreadyInList.Add(enemy.Tag);
                    }
            }
            return enemies;
        }

    }
}
