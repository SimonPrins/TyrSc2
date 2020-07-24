using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using Tyr.Agents;
using Tyr.MapAnalysis;
using Tyr.Util;

namespace Tyr.Tasks
{
    class WarpPrismElevatorTask : Task
    {
        public static WarpPrismElevatorTask Task = new WarpPrismElevatorTask();
        private Agent WarpPrism = null;
        private Point2D EnemyThird = null;
        private Point2D LoadArea = null;
        public Point2D StagingArea = null;
        private HashSet<ulong> DroppedUnits = new HashSet<ulong>();
        private bool WarpPrismInPlace = false;

        public bool Cancelled = false;

        public ulong PickupUnitTag;

        public WarpPrismElevatorTask() : base(5)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit || (agent.Unit.UnitType == UnitTypes.WARP_PRISM && WarpPrism == null);
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (Cancelled)
                return result;
            if (WarpPrism == null)
                result.Add(new UnitDescriptor() { Pos = Bot.Bot.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.WARP_PRISM } });
            result.Add(new UnitDescriptor() { Pos = Bot.Bot.TargetManager.AttackTarget, UnitTypes = UnitTypes.CombatUnitTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return EnemyThird != null;
        }

        public override void OnFrame(Bot tyr)
        {
            if (WarpPrism != null)
            {
                bool warpPrismAlive = false;
                foreach (Agent agent in Units)
                {
                    if (agent.Unit.Tag == WarpPrism.Unit.Tag)
                    {
                        warpPrismAlive = true;
                        break;
                    }
                }
                if (!warpPrismAlive)
                    WarpPrism = null;
            }

            if (Cancelled)
                for (int i = Units.Count - 1; i >= 0; i--)
                {
                    if (!DroppedUnits.Contains(Units[i].Unit.Tag)
                        && Units[i].Unit.UnitType != UnitTypes.WARP_PRISM)
                        ClearAt(i);
                }

            if (EnemyThird == null && tyr.TargetManager.PotentialEnemyStartLocations.Count == 1)
            {
                Point2D enemyNatural = tyr.MapAnalyzer.GetEnemyNatural().Pos;
                Point2D enemyMain = tyr.TargetManager.PotentialEnemyStartLocations[0];
                Point2D enemyRamp = tyr.MapAnalyzer.GetEnemyRamp();
                float dist = 1000000;
                foreach (BaseLocation loc in tyr.MapAnalyzer.BaseLocations)
                {
                    if (SC2Util.DistanceSq(loc.Pos, enemyNatural) <= 2 * 2)
                        continue;
                    float mainDist = SC2Util.DistanceSq(loc.Pos, enemyMain);
                    if (mainDist <= 2 * 2)
                        continue;
                    if (mainDist > 50 * 50)
                        continue;
                    //float newDist = SC2Util.DistanceSq(loc.Pos, enemyRamp);
                    if (mainDist > dist)
                        continue;
                    dist = mainDist;
                    EnemyThird = loc.Pos;
                }
                PotentialHelper potential;
                dist = 25 * 25;
                for (int x = 0; x < tyr.MapAnalyzer.EnemyDistances.GetLength(0); x++)
                    for (int y = 0; y < tyr.MapAnalyzer.EnemyDistances.GetLength(1); y++)
                    {
                        if (tyr.MapAnalyzer.EnemyDistances[x, y] > 30)
                            continue;

                        Point2D point = new Point2D() { X = x, Y = y };
                        float newDist = SC2Util.DistanceSq(point, EnemyThird);
                        if (newDist > dist)
                            continue;
                        dist = newDist;
                        
                        StagingArea = new PotentialHelper(point, 0.5f)
                            .To(tyr.TargetManager.PotentialEnemyStartLocations[0])
                            .Get();
                    }
                
                potential = new PotentialHelper(StagingArea, 6.5f);
                potential.From(tyr.TargetManager.PotentialEnemyStartLocations[0]);
                LoadArea = potential.Get();

                if (tyr.Map == MapEnum.Simulacrum)
                {
                    if (tyr.MapAnalyzer.StartLocation.X < 100)
                    {
                        StagingArea = new Point2D() { X = 136.5f, Y = 51.2f};
                        LoadArea = new Point2D() { X = 134.5f, Y = 55.2f };
                    } else
                    {
                        StagingArea = new Point2D() { X = 79, Y = 133f };
                        LoadArea = new Point2D() { X = 82f, Y = 130.5f };
                    }
                }
            }

            if (StagingArea != null)
                tyr.DrawSphere(new Point() { X = StagingArea.X, Y = StagingArea.Y, Z = tyr.MapAnalyzer.StartLocation.Z });

            if (units.Count == 0)
                return;

            if (WarpPrism == null)
            {
                foreach (Agent agent in units)
                    if (agent.Unit.UnitType == UnitTypes.WARP_PRISM)
                    {
                        WarpPrism = agent;
                        break;
                    }
            }
            PickupUnitTag = 0;
            tyr.DrawText("Pickup unit: " + PickupUnitTag);
            OrderWarpPrism();
            
            foreach (Agent agent in units)
            {
                if (WarpPrism != null && agent.Unit.Tag == WarpPrism.Unit.Tag)
                    continue;
                if (agent.Unit.IsFlying)
                    DroppedUnits.Add(agent.Unit.Tag);
                if (DroppedUnits.Contains(agent.Unit.Tag))
                    Attack(agent, tyr.TargetManager.AttackTarget);
                else if (PickupUnitTag == agent.Unit.Tag)
                {
                    if (PickupUnitTag != 0)
                        tyr.DrawLine(agent.Unit.Pos, WarpPrism.Unit.Pos);
                    agent.Order(Abilities.MOVE, WarpPrism.Unit.Tag);
                }
                else if (WarpPrismInPlace)
                    Attack(agent, LoadArea);
                else
                    Attack(agent, EnemyThird);
            }
        }

        private void OrderWarpPrism()
        {
            Unit closeEnemy = null;
            float dist = 8 * 8;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.PHOTON_CANNON)
                    continue;
                float newDist = SC2Util.DistanceSq(enemy.Pos, StagingArea);
                if (newDist > dist)
                    continue;
                dist = newDist;
                closeEnemy = enemy;
            }

            Point2D stagingAreaFinal;
            if (closeEnemy != null)
            {
                PotentialHelper potential = new PotentialHelper(StagingArea, 2);
                potential.From(closeEnemy.Pos, 2);
                potential.To(Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]);
                stagingAreaFinal = potential.Get();
            }
            else
            {
                stagingAreaFinal = StagingArea;
            }


            WarpPrismInPlace = false;
            if (WarpPrism == null)
                return;
            float distance = WarpPrism.DistanceSq(stagingAreaFinal);
            if (distance < 20 * 20)
                WarpPrismInPlace = true;
            if (distance >= 9 * 9)
            {
                WarpPrism.Order(Abilities.MOVE, stagingAreaFinal);
                return;
            }
            if (WarpPrism.Unit.Passengers != null && WarpPrism.Unit.Passengers.Count > 0)
            {
                WarpPrism.Order(913, stagingAreaFinal);
                foreach (PassengerUnit passenger in WarpPrism.Unit.Passengers)
                    DroppedUnits.Add(passenger.Tag);
                return;
            }
            foreach (Agent agent in Units)
            {
                if (agent.Unit.IsFlying)
                    continue;
                if (DroppedUnits.Contains(agent.Unit.Tag))
                    continue;
                if (agent.DistanceSq(WarpPrism) > 7 * 7)
                    continue;
                WarpPrism.Order(911, agent.Unit.Tag);
                PickupUnitTag = agent.Unit.Tag;
                return;
            }
            if (WarpPrism.DistanceSq(stagingAreaFinal) <= 0.5 * 0.5
                    && WarpPrism.Unit.UnitType == UnitTypes.WARP_PRISM)
            {
                WarpPrism.Order(Abilities.WarpPrismPhasingMode);
                return;
            }
            WarpPrism.Order(Abilities.MOVE, stagingAreaFinal);
        }
    }
}
