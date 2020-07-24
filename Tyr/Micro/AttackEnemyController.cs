using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class AttackEnemyController : CustomController
    {
        private HashSet<uint> Attacker = new HashSet<uint>();
        private HashSet<uint> Targets = new HashSet<uint>();
        public float Range;
        public bool MoveCommand = false;

        public AttackEnemyController(uint from, uint to, float range, bool moveCommand)
        {
            Attacker.Add(from);
            Targets.Add(to);
            Range = range;
            MoveCommand = moveCommand;
        }
        public AttackEnemyController(uint from, HashSet<uint> to, float range, bool moveCommand)
        {
            Attacker.Add(from);
            Targets = to;
            Range = range;
            MoveCommand = moveCommand;
        }

        public AttackEnemyController(uint from, HashSet<uint> to, float range)
        {
            Attacker.Add(from);
            Targets = to;
            Range = range;
        }

        public AttackEnemyController(HashSet<uint> from, uint to, float range)
        {
            Attacker = from;
            Targets.Add(to);
            Range = range;
        }

        public AttackEnemyController(HashSet<uint> from, HashSet<uint> to, float range)
        {
            Attacker = from;
            Targets = to;
            Range = range;
        }

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (!Attacker.Contains(agent.Unit.UnitType))
                return false;
            
            float dist;

            Unit attack = null;
            dist = Range * Range;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!Targets.Contains(enemy.UnitType))
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    attack = enemy;
                    dist = newDist;
                }
            }
            if (attack != null && dist < Range * Range)
            {
                if (MoveCommand)
                    agent.Order(Abilities.MOVE, SC2Util.To2D(attack.Pos));
                else
                    agent.Order(Abilities.ATTACK, attack.Tag);
                return true;
            }

            return false;
        }
    }
}
