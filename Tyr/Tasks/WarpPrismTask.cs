using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Builds;
using Tyr.Util;

namespace Tyr.Tasks
{
    class WarpPrismTask : Task
    {
        public static WarpPrismTask Task = new WarpPrismTask();
        public List<WarpPrismObjective> Objectives = new List<WarpPrismObjective>();
        public HashSet<uint> IgnoreAllyUnitTypes = new HashSet<uint>() { UnitTypes.OBSERVER, UnitTypes.WARP_PRISM, UnitTypes.WARP_PRISM_PHASING};

        public bool Cancelled = false;

        private bool WarpInInProgress = false;
        private HashSet<ulong> WarpInTags = new HashSet<ulong>();

        public HashSet<uint> SaveUnitTypes = new HashSet<uint>() { UnitTypes.IMMORTAL, UnitTypes.ARCHON };
        public HashSet<uint> ArmyUnitTypes = new HashSet<uint>() { UnitTypes.IMMORTAL, UnitTypes.ARCHON };

        public int WarpInStartFrane = -200;

        public WarpPrismTask() : base(10)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return (agent.Unit.UnitType == UnitTypes.WARP_PRISM && Units.Count == 0);
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (Units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.WARP_PRISM } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            UpdateWarpingInUnits();

            Point2D armyLocation = GetArmyFrontPosition();
            Point2D closestEnemyToArmy = armyLocation == null ? null : GetClosestEnemy(armyLocation, false);
            if (closestEnemyToArmy != null && SC2Util.DistanceSq(closestEnemyToArmy, armyLocation) <= 6 * 6)
                armyLocation = new PotentialHelper(armyLocation, 4).From(closestEnemyToArmy).Get();

            if (armyLocation != null)
            {
                if (Units.Count >= 1)
                    tyr.DrawSphere(new Point() { X = armyLocation.X, Y = armyLocation.Y, Z = Units[0].Unit.Pos.Z });
                DeterminePickupTargets(armyLocation);
            }

            foreach (Agent agent in Units)
            {
                Point2D closestEnemy = GetClosestEnemy(SC2Util.To2D(agent.Unit.Pos), false);
                Point2D closestAirEnemy = GetClosestEnemy(SC2Util.To2D(agent.Unit.Pos), true);
                float enemyDist = closestEnemy == null ? 1000000 : agent.DistanceSq(closestEnemy);
                float airEnemyDist = closestAirEnemy == null ? 1000000 : agent.DistanceSq(closestAirEnemy);

                if (agent.Unit.UnitType == UnitTypes.WARP_PRISM
                    && agent.Unit.Passengers.Count < (airEnemyDist < 5 * 5 ? 1 : 2)
                    && agent.Unit.Health + agent.Unit.Shield >= 60)
                {
                    bool pickingUp = false;
                    foreach (WarpPrismObjective objective in Objectives)
                    {
                        if (objective is PickUpObjective)
                        {
                            ulong tag = ((PickUpObjective)objective).UnitTag;
                            pickingUp = true;
                            agent.Order(911, tag);
                            break;
                        }
                    }
                    if (pickingUp)
                        continue;
                }

                if (enemyDist < 6 * 6 
                    && agent.Unit.UnitType == UnitTypes.WARP_PRISM_PHASING)
                {
                    if (!WarpInInProgress)
                        agent.Order(Abilities.WarpPrismTransportMode);
                    continue;
                }

                if (enemyDist >= 5 * 5 && agent.Unit.Passengers.Count > 0)
                {
                    agent.Order(913, SC2Util.To2D(agent.Unit.Pos));
                    continue;
                }
                if (enemyDist <= 8 * 8 && agent.Unit.UnitType == UnitTypes.WARP_PRISM)
                {
                    Point2D fleePos = new PotentialHelper(agent.Unit.Pos, 6).From(closestEnemy).Get();
                    agent.Order(Abilities.MOVE, fleePos);
                    continue;
                }
                if (armyLocation == null)
                {
                    agent.Order(Abilities.MOVE, tyr.BaseManager.NaturalDefensePos);
                    continue;
                }

                if ((agent.DistanceSq(armyLocation) >= 10 * 10 || Bot.Main.Frame - WarpInStartFrane >= 22.4 * 20)
                    && agent.Unit.UnitType == UnitTypes.WARP_PRISM_PHASING
                    && !WarpInInProgress
                    && (!WarpInObjectiveSet() || Bot.Main.Frame - WarpInStartFrane >= 22.4 * 20))
                {
                    agent.Order(Abilities.WarpPrismTransportMode);
                    continue;
                }
                /*
                if (agent.Unit.UnitType == UnitTypes.WARP_PRISM
                    && agent.DistanceSq(armyLocation) >= 10 * 10)
                {
                    agent.Order(Abilities.MOVE, armyLocation);
                    continue;
                }
                */
                if (agent.Unit.UnitType == UnitTypes.WARP_PRISM && WarpInObjectiveSet())
                {
                    WarpInStartFrane = tyr.Frame;
                    agent.Order(Abilities.WarpPrismPhasingMode);
                    continue;
                }
                if (agent.Unit.UnitType == UnitTypes.WARP_PRISM && agent.DistanceSq(armyLocation) >= 4 * 4)
                {
                    agent.Order(Abilities.MOVE, armyLocation);
                    continue;
                }
                if (agent.Unit.UnitType == UnitTypes.WARP_PRISM_PHASING)
                {
                    WarpInObjective warpInObjective = null;
                    foreach (WarpPrismObjective objective in Objectives)
                        if (objective is WarpInObjective)
                        {
                            warpInObjective = (WarpInObjective)objective;
                            break;
                        }
                    if (warpInObjective != null)
                    {
                        Point2D warpInLocation = WarpInPlacer.FindPlacement(SC2Util.To2D(agent.Unit.Pos), warpInObjective.UnitType);
                        WarpIn(warpInLocation, warpInObjective.UnitType);
                    }
                }
            }
        }

        private void DeterminePickupTargets(Point2D armyLocation)
        {
            int count = 0;
            for (int i = Objectives.Count - 1; i >= 0; i--)
            {
                WarpPrismObjective objective = Objectives[i];
                if (objective is PickUpObjective)
                {
                    ulong tag = ((PickUpObjective)objective).UnitTag;
                    if (!Bot.Main.UnitManager.Agents.ContainsKey(tag))
                    {
                        Objectives.RemoveAt(i);
                        continue;
                    }
                    Agent agent = Bot.Main.UnitManager.Agents[tag];
                    if (!IsInDanger(agent))
                    {
                        Objectives.RemoveAt(i);
                        continue;
                    }
                    bool alreadyPickedUp = false;
                    foreach (Agent prism in Units)
                    {
                        foreach (PassengerUnit passenger in prism.Unit.Passengers)
                            if (passenger.Tag == tag)
                                alreadyPickedUp = true;
                        Bot.Main.DrawLine(prism, agent.Unit.Pos);
                    }
                    if (alreadyPickedUp)
                    {
                        Objectives.RemoveAt(i);
                        continue;
                    }
                    count++;
                }
            }
            foreach (Agent agent in Bot.Main.Units())
            {
                if (count >= 2)
                    break;
                if (!SaveUnitTypes.Contains(agent.Unit.UnitType))
                    continue;
                if (agent.Unit.IsFlying)
                    continue;
                if (agent.DistanceSq(armyLocation) >= 12 * 12)
                    continue;
                if (!IsInDanger(agent))
                    continue;
                Objectives.Add(new PickUpObjective() { UnitTag = agent.Unit.Tag });
                foreach (Agent prism in Units)
                    Bot.Main.DrawLine(prism, agent.Unit.Pos);
                count++;
            }
        }

        private bool IsInDanger(Agent agent)
        {
            int closeEnemyValue = 0;
            float range = UnitTypes.LookUp[agent.Unit.UnitType].Weapons[0].Range;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (agent.DistanceSq(enemy) >= range * range)
                    continue;

                closeEnemyValue+= enemy.UnitType == UnitTypes.ZERGLING ? 1 : 2;
            }
            return closeEnemyValue >= 8;
        }

        private void UpdateWarpingInUnits()
        {
            WarpInInProgress = false;
            foreach (Agent agent in Bot.Main.Units())
            {
                if (agent.Unit.BuildProgress >= 0.99)
                    continue;
                if (agent.Unit.UnitType != UnitTypes.ZEALOT
                    && agent.Unit.UnitType != UnitTypes.SENTRY
                    && agent.Unit.UnitType != UnitTypes.STALKER
                    && agent.Unit.UnitType != UnitTypes.ADEPT
                    && agent.Unit.UnitType != UnitTypes.HIGH_TEMPLAR
                    && agent.Unit.UnitType != UnitTypes.DARK_TEMPLAR)
                    continue;

                bool nearWarpPrism = false;
                foreach (Agent warpPrism in Units)
                {
                    if (agent.DistanceSq(warpPrism) <= 6 * 6)
                    {
                        nearWarpPrism = true;
                        break;
                    }
                }
                if (nearWarpPrism)
                    WarpInInProgress = true;
                if (!WarpInTags.Contains(agent.Unit.Tag))
                {
                    WarpPrismObjective removeObjective = null;
                    foreach (WarpPrismObjective objective in Objectives)
                        if (objective is WarpInObjective
                            && ((WarpInObjective)objective).UnitType == agent.Unit.UnitType)
                        {
                            ((WarpInObjective)objective).Number--;
                            if (((WarpInObjective)objective).Number <= 0)
                                removeObjective = objective;
                            break;
                        }
                    if (removeObjective != null)
                        Objectives.Remove(removeObjective);
                    WarpInTags.Add(agent.Unit.Tag);
                }
            }
        }

        public void WarpIn(Point2D warpInLocation, uint unitType)
        {
            TrainingType trainType = TrainingType.LookUp[unitType];
            if (Build.FoodLeft() < trainType.Food)
                return;
            foreach (Agent agent in ProductionTask.Task.Units)
            {
                if (agent.Unit.BuildProgress < 0.99)
                    continue;

                if (!trainType.ProducingUnits.Contains(agent.Unit.UnitType))
                    continue;

                if (agent.Unit.Orders != null && agent.Unit.Orders.Count >= 2)
                    continue;

                if (agent.CurrentAbility() != 0)
                    continue;

                if (agent.Command != null)
                    continue;

                if (Bot.Main.Frame - agent.LastOrderFrame < 5)
                    continue;

                if (agent.Unit.UnitType == UnitTypes.GATEWAY && UpgradeType.LookUp[UpgradeType.WarpGate].Done())
                    continue;

                Bot.Main.ReservedGas += trainType.Gas;
                Bot.Main.ReservedMinerals += trainType.Minerals;

                if (Bot.Main.Build.Minerals() < 0)
                    return;
                if (Bot.Main.Build.Gas() < 0)
                    return;

                if (agent.Unit.UnitType == UnitTypes.WARP_GATE)
                    agent.Order((int)trainType.WarpInAbility, warpInLocation);
            }
        }

        public bool WarpInObjectiveSet()
        {
            foreach (WarpPrismObjective objective in Objectives)
                if (objective is WarpInObjective)
                    return true;
            return false;
        }

        public void AddWarpInObjective(uint unitType)
        {
            AddWarpInObjective(unitType, 1);
        }

        public void AddWarpInObjective(uint unitType, int number)
        {
            Objectives.Add(new WarpInObjective() { UnitType = unitType, Number = number });
        }

        public bool IsWarpingIn()
        {
            foreach (Agent agent in Units)
                if (agent.Unit.UnitType == UnitTypes.WARP_PRISM_PHASING)
                    return true;
            return false;
        }

        public Point2D WarpInLocation()
        {
            foreach (Agent agent in Units)
                if (agent.Unit.UnitType == UnitTypes.WARP_PRISM_PHASING)
                    return SC2Util.To2D(agent.Unit.Pos);
            return null;
        }

        public Point2D GetArmyFrontPosition()
        {
            Point2D target = Bot.Main.TargetManager.AttackTarget;

            Agent closest = null;
            float dist = 1000000;
            foreach (Agent agent in Bot.Main.Units())
            {
                if (IgnoreAllyUnitTypes.Contains(agent.Unit.UnitType))
                    continue;

                if (!ArmyUnitTypes.Contains(agent.Unit.UnitType))
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
            if (closest == null)
                return null;
            Agent furthest = closest;
            dist = 0;

            foreach (Agent agent in Bot.Main.Units())
            {
                if (IgnoreAllyUnitTypes.Contains(agent.Unit.UnitType))
                    continue;

                if (!ArmyUnitTypes.Contains(agent.Unit.UnitType))
                    continue;

                if (agent.Unit.IsFlying)
                    continue;
                if (agent.DistanceSq(closest) >= 16 * 16)
                    continue;

                float enemyDist = 1000000;
                foreach (Unit enemy in Bot.Main.Enemies())
                    if (UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                        enemyDist = Math.Min(enemyDist, agent.DistanceSq(enemy));
                if (enemyDist < dist)
                    continue;
                if (enemyDist > 999999)
                    continue;
                furthest = agent;
                dist = enemyDist;
            }
            return SC2Util.To2D(furthest.Unit.Pos);
        }

        public Point2D GetClosestEnemy(Point2D pos, bool targetsAir)
        {
            Unit closestEnemy = null;
            float dist = 1000000;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                    continue;

                if (targetsAir
                    && !UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                    continue;

                float newDist = SC2Util.DistanceSq(enemy.Pos, pos);
                if (newDist < dist)
                {
                    closestEnemy = enemy;
                    dist = newDist;
                }
            }
            if (closestEnemy == null)
                return null;
            return SC2Util.To2D(closestEnemy.Pos);
        }
    }

    public interface WarpPrismObjective
    {

    }

    public class WarpInObjective : WarpPrismObjective
    {
        public uint UnitType;
        public int Number = 1;
    }

    public class PickUpObjective : WarpPrismObjective
    {
        public ulong UnitTag;
    }
}
