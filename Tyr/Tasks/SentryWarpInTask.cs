﻿using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    public class SentryWarpInTask : Task
    {
        public static SentryWarpInTask Task = new SentryWarpInTask();
        private Point2D WayPoint;
        private Point2D DropPos;
        private HashSet<ulong> PassedWayPoint = new HashSet<ulong>();
        private HashSet<ulong> Loaded = new HashSet<ulong>();
        private HashSet<ulong> DroppedUnits = new HashSet<ulong>();
        public SentryWarpInTask() : base(10)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            Agent warpPrism = null;
            foreach (Agent unit in Units)
                if (unit.Unit.UnitType == UnitTypes.WARP_PRISM || unit.Unit.UnitType == UnitTypes.WARP_PRISM_PHASING)
                {
                    warpPrism = unit;
                    break;
                }
            if (warpPrism == null)
                result.Add(new UnitDescriptor(UnitTypes.WARP_PRISM) { Pos = SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation), Count = 1, MaxDist = 50 });
            else if (5 - Units.Count - warpPrism.Unit.Passengers.Count > 0)
                result.Add(new UnitDescriptor(UnitTypes.SENTRY) { Pos = SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation), Count = 5 - Units.Count - warpPrism.Unit.Passengers.Count, MaxDist = 50 });

            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            DetermineDropTarget();

            Agent warpPrism = null;
            foreach (Agent unit in Units)
                if (unit.Unit.UnitType == UnitTypes.WARP_PRISM || unit.Unit.UnitType == UnitTypes.WARP_PRISM_PHASING)
                {
                    warpPrism = unit;
                    break;
                }
            if (warpPrism == null)
                Clear();

            if (DropPos == null || warpPrism == null)
                return;

            bot.DrawSphere(new Point() { X = DropPos.X, Y = DropPos.Y, Z = warpPrism.Unit.Pos.Z });
            bot.DrawText("SentryWarpInTask size: " + Units.Count);

            foreach (PassengerUnit passenger in warpPrism.Unit.Passengers)
                DroppedUnits.Add(passenger.Tag);
            foreach (Agent agent in units)
            {
                if (agent.Unit.Tag != warpPrism.Unit.Tag)
                {
                    if (!DroppedUnits.Contains(agent.Unit.Tag)
                        && agent.DistanceSq(warpPrism) >= 5 * 5)
                        agent.Order(Abilities.MOVE, warpPrism.Unit.Tag);
                    else if (DroppedUnits.Contains(agent.Unit.Tag))
                        Attack(agent, bot.TargetManager.AttackTarget);
                    continue;
                }
                if (!PassedWayPoint.Contains(agent.Unit.Tag)
                    && warpPrism.Unit.Passengers.Count < 4)
                {
                    foreach (Agent passenger in units)
                    {
                        if (passenger.Unit.IsFlying)
                            continue;
                        if (DroppedUnits.Contains(passenger.Unit.Tag))
                            continue;
                        warpPrism.Order(911, passenger.Unit.Tag);
                        break;
                    }
                    continue;
                }
                if (!PassedWayPoint.Contains(agent.Unit.Tag))
                {
                    agent.Order(Abilities.MOVE, WayPoint);
                    bot.DrawText("WaypointDistance: " + Math.Sqrt(agent.DistanceSq(WayPoint)));
                    bot.DrawSphere(new Point() { X = WayPoint.X, Y = WayPoint.Y, Z = agent.Unit.Pos.Z } );
                    if (agent.DistanceSq(WayPoint) <= 10 * 10)
                        PassedWayPoint.Add(agent.Unit.Tag);
                }
                else if (agent.DistanceSq(DropPos) <= 2)
                {
                    bot.DrawText("Dropping.");
                    //agent.Order(1528);
                    agent.Order(913, DropPos);
                    bot.DrawLine(agent, DropPos);
                }
                else
                {
                    bot.DrawText("Moving to drop.");
                    agent.Order(Abilities.MOVE, DropPos);
                    bot.DrawLine(agent, DropPos);
                }
            }
        }

        private void DetermineDropTarget()
        {
            if (DropPos != null)
                return;
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count == 0)
                return;

            Base enemyMain = null;
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (SC2Util.DistanceSq(b.BaseLocation.Pos, Bot.Main.TargetManager.PotentialEnemyStartLocations[0]) <= 2 * 2)
                {
                    enemyMain = b;
                    break;
                }
            }
            if (enemyMain == null)
                return;
            DropPos = new PotentialHelper(enemyMain.MineralLinePos, 4).From(Bot.Main.TargetManager.PotentialEnemyStartLocations[0]).Get();
            Point2D enemyThird = Bot.Main.MapAnalyzer.GetEnemyThird().Pos;
            float topDist = enemyThird.Y;
            float leftDist = enemyThird.X;
            float bottomDist = Bot.Main.GameInfo.StartRaw.MapSize.Y - enemyThird.Y;
            float rightDist = Bot.Main.GameInfo.StartRaw.MapSize.X - enemyThird.X;

            if (topDist < leftDist && topDist < bottomDist && topDist < rightDist)
                enemyThird = new Point2D() { X = enemyThird.X, Y = Bot.Main.GameInfo.StartRaw.PlayableArea.P0.Y + 5};
            else if (leftDist < bottomDist && leftDist < rightDist)
                enemyThird = new Point2D() { X = Bot.Main.GameInfo.StartRaw.PlayableArea.P0.X + 5, Y = enemyThird.Y };
            else if (bottomDist < rightDist)
                enemyThird = new Point2D() { X = enemyThird.X, Y = Bot.Main.GameInfo.StartRaw.PlayableArea.P1.Y - 5 };
            else
                enemyThird = new Point2D() { X = Bot.Main.GameInfo.StartRaw.PlayableArea.P1.X - 5, Y = enemyThird.Y };
            WayPoint = new PotentialHelper(enemyMain.BaseLocation.Pos, 50).To(enemyThird).Get();
            WayPoint.X = Math.Max(Bot.Main.GameInfo.StartRaw.PlayableArea.P0.X + 5, WayPoint.X);
            WayPoint.X = Math.Min(Bot.Main.GameInfo.StartRaw.PlayableArea.P1.X - 5, WayPoint.X);
            WayPoint.Y = Math.Max(Bot.Main.GameInfo.StartRaw.PlayableArea.P0.Y + 5, WayPoint.Y);
            WayPoint.Y = Math.Min(Bot.Main.GameInfo.StartRaw.PlayableArea.P1.Y - 5, WayPoint.Y);
            DebugUtil.WriteLine("Warp prism DropPos: " + DropPos);
            DebugUtil.WriteLine("Warp prism WayPoint: " + WayPoint);
        }
    }
}
