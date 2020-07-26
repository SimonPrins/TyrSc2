using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class ArmyOracleTask : Task
    {
        public static ArmyOracleTask Task = new ArmyOracleTask();

        public HashSet<uint> IgnoreAllyUnitTypes = new HashSet<uint>();

        public ulong Revelator;
        public int RevelatorFrame = 0;

        public ArmyOracleTask() : base(10)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.ORACLE;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.ORACLE } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            if (units.Count == 0)
                return;


            Point2D target = bot.TargetManager.AttackTarget;

            Agent closest = null;
            float dist = 1000000;
            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.ORACLE)
                    continue;

                if (IgnoreAllyUnitTypes.Contains(agent.Unit.UnitType))
                    continue;

                if (!UnitTypes.CombatUnitTypes.Contains(agent.Unit.UnitType))
                    continue;

                if (agent.Unit.IsFlying)
                    continue;

                float newDist = agent.DistanceSq(target);
                if (newDist < dist)
                {
                    closest = agent;
                    dist = newDist;
                }
            }

            int bases = 0;
            foreach (Base b in bot.BaseManager.Bases)
                if (b.ResourceCenter != null)
                    bases++;

            Point2D defenseLocation;
            if (bases >= 2)
                defenseLocation = bot.BaseManager.NaturalDefensePos;
            else defenseLocation = bot.BaseManager.MainDefensePos;

            foreach (Agent oracle in Units)
            {
                if (Revelation(oracle))
                    continue;

                Unit fleeEnemy = null;
                dist = 10 * 10;
                foreach (Unit enemy in bot.Enemies())
                {
                    if (!UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                        continue;
                    float newDist = units[0].DistanceSq(enemy);
                    if (enemy.UnitType == UnitTypes.WIDOW_MINE_BURROWED
                        && dist >= 6 * 6)
                        continue;
                    if (newDist < dist)
                    {
                        fleeEnemy = enemy;
                        dist = newDist;
                    }
                }

                if (fleeEnemy != null)
                {
                    oracle.Order(Abilities.MOVE, oracle.From(fleeEnemy, 4));
                    continue;
                }


                if (bot.Frame % 5 == 0)
                    continue;

                if (closest == null)
                    oracle.Order(Abilities.MOVE, defenseLocation);
                else
                    oracle.Order(Abilities.MOVE, SC2Util.To2D(closest.Unit.Pos));
            }

        }

        private bool Revelation(Agent oracle)
        {
            if (Bot.Main.Frame - RevelatorFrame <= 10)
                return oracle.Unit.Tag == Revelator;
            
            if (Bot.Main.Frame % 5 != 0)
                return false;
            if (oracle.Unit.Energy < 50)
                return false;
            Unit followEnemy = null;
            float dist = 15 * 15;
            foreach (Unit enemy in Bot.Main.CloakedEnemies())
            {
                if (enemy.Cloak != CloakState.Cloaked)
                    continue;
                float newDist = units[0].DistanceSq(enemy);

                if (newDist < dist)
                {
                    followEnemy = enemy;
                    dist = newDist;
                }
            }
            if (followEnemy != null)
            {
                Point2D target;
                if (dist >= 6 * 6)
                    target = new PotentialHelper(followEnemy.Pos, 5).To(oracle.Unit.Pos).Get();
                else target = new Point2D() { X = (followEnemy.Pos.X + oracle.Unit.Pos.X) / 2f, Y = (followEnemy.Pos.Y + oracle.Unit.Pos.Y) / 2f };


                oracle.Order(2146, target);
                Revelator = oracle.Unit.Tag;
                RevelatorFrame = Bot.Main.Frame;
                return true;
            }
            return false;
        }
    }
}
