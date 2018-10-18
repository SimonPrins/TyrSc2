using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class OracleHarassTask : Task
    {
        public int RequiredSize { get; set; } = 2;
        public int PulsarFrame;

        HashSet<uint> buffs = new HashSet<uint>();

        public OracleHarassTask() : base(5)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.ORACLE;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.UnitManager.Completed(UnitTypes.ORACLE)  >= RequiredSize;
        }

        public override void OnFrame(Tyr tyr)
        {
            Dictionary<ulong, Unit> targets = new Dictionary<ulong, Unit>();
            bool attacking = false;
            Point2D defendTarget = null;

            Unit enemyWorker = null;
            float workerDist = 400;

            foreach (Agent agent in units)
            {
                Unit target = null;
                float health = 10000;
                ulong tag = 0;
                foreach (Unit enemy in tyr.Observation.Observation.RawData.Units)
                {
                    if (enemy.Alliance != Alliance.Enemy)
                        continue;

                    float dist = SC2Util.DistanceSq(agent.Unit.Pos, enemy.Pos);
                    if (UnitTypes.WorkerTypes.Contains(enemy.UnitType) && dist < workerDist)
                    {
                        enemyWorker = enemy;
                        workerDist = dist;
                    }

                    if (dist >= 8 * 8)
                        continue;

                    if (!UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                        continue;

                    if (enemy.Health + enemy.Shield > health)
                        continue;

                    if (enemy.Health + enemy.Shield == health && tag > enemy.Tag)
                        continue;

                    health = enemy.Health + enemy.Shield;
                    target = enemy;
                    tag = enemy.Tag;
                    attacking = true;
                    defendTarget = SC2Util.To2D(agent.Unit.Pos);
                }
                
                targets.Add(agent.Unit.Tag, target);
            }
            foreach (Agent agent in units)
            {
                foreach (uint buff in agent.Unit.BuffIds)
                    if (!buffs.Contains(buff))
                    {
                        buffs.Add(buff);
                        System.Console.WriteLine("Buff: " + buff);
                    }
                
                Unit target = targets[agent.Unit.Tag];

                if ((attacking || enemyWorker != null) && (tyr.Frame - PulsarFrame >= 400 || tyr.Frame == PulsarFrame))
                {
                    PulsarFrame = tyr.Frame;
                    agent.Order(2375);
                }
                else if (target != null)
                    agent.Order(Abilities.ATTACK, target.Tag);
                else if (attacking)
                    agent.Order(Abilities.MOVE, defendTarget);
                else if (enemyWorker != null)
                    agent.Order(Abilities.ATTACK, enemyWorker.Tag);
                else
                    agent.Order(Abilities.MOVE, tyr.TargetManager.AttackTarget);
            }
        }
    }
}
