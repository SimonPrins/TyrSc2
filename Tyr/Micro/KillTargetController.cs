using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class KillTargetController : CustomController
    {
        public TargetFilter Filter;
        public bool NoEnemiesAround;
        public float MaxEnemyRange = 12;
        public float MaxDist = 15;
        public HashSet<uint> ExcludedAttackerTypes = new HashSet<uint>();
        public bool FocusDamaged = false;
        public bool MoveForwardWhenInRange = true;
        public bool Debug = false;
        int LastDebugFrame = 0;

        public delegate bool TargetFilter(Unit enemy);

        public KillTargetController(uint targetType) : this(targetType, false)
        { }

        public KillTargetController(uint targetType, bool noEnemiesAround): this((enemy) => enemy.UnitType == targetType, noEnemiesAround)
        { }
        public KillTargetController(TargetFilter filter) : this(filter, false)
        { }

        public KillTargetController(TargetFilter filter, bool noEnemiesAround)
        {
            Filter = filter;
            NoEnemiesAround = noEnemiesAround;
        }

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (ExcludedAttackerTypes.Contains(agent.Unit.UnitType))
                return false;

            bool debugThisFrame = Debug && LastDebugFrame != Bot.Main.Frame;
            LastDebugFrame = Bot.Main.Frame;
            if (debugThisFrame)
                Bot.Main.DrawText("Try attack target.");
            Unit killTarget = null;

            float dist = MaxDist * MaxDist;
            float hp = 1000000;
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (!Filter(unit))
                {
                    if (NoEnemiesAround)
                    {
                        if (!UnitTypes.WorkerTypes.Contains(unit.UnitType)
                            && UnitTypes.CombatUnitTypes.Contains(unit.UnitType)
                            && agent.DistanceSq(unit) <= MaxEnemyRange * MaxEnemyRange)
                            return false;
                    }
                    continue;
                }
                if (debugThisFrame)
                    Bot.Main.DrawText("Found potential attack target.");

                if (unit.UnitType != UnitTypes.COLOSUS && !agent.CanAttackGround() && !unit.IsFlying)
                    continue;
                if (!agent.CanAttackAir() && unit.IsFlying)
                    continue;

                float newDist = agent.DistanceSq(unit);
                float newHP = unit.Health + unit.Shield;
                if (FocusDamaged)
                {
                    if (newHP > hp)
                        continue;

                    if (newHP == hp && newDist > dist)
                        continue;
                } else if (newDist > dist)
                    continue;
                killTarget = unit;
                dist = newDist;
                hp = newHP;
            }

            if (killTarget == null)
            {
                if (debugThisFrame)
                    Bot.Main.DrawText("No attack target found.");
                return false;
            }

            if (Debug)
                Bot.Main.DrawLine(agent, killTarget.Pos);
            if (agent.Unit.WeaponCooldown > 0)
            {
                if (!MoveForwardWhenInRange)
                    return false;
                agent.Order(Abilities.MOVE, SC2Util.To2D(killTarget.Pos));
            }
            else {
                agent.Order(Abilities.ATTACK, killTarget.Tag);
            }

            return true;
        }

        public KillTargetController ExcludeAttackerType(uint attackerType)
        {
            ExcludedAttackerTypes.Add(attackerType);
            return this;
        }
    }
}
