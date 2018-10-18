using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class CannonRushTask : Task
    {
        private int LastBuiltFrame = 0;
        public CannonRushTask() : base(8)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && Units.Count < 2;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.Frame >= 600;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Tyr.Bot.TargetManager.AttackTarget, Count = 2, UnitTypes = new HashSet<uint>() { UnitTypes.PROBE } });
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            ulong mineral = 0;
            if (tyr.BaseManager.Main.BaseLocation.MineralFields.Count > 0)
                mineral = tyr.BaseManager.Main.BaseLocation.MineralFields[0].Tag;

            foreach (Agent agent in units)
            {
                if (agent.Unit.Orders.Count > 0 &&
                    (agent.Unit.Orders[0].AbilityId == BuildingType.LookUp[UnitTypes.PYLON].Ability || agent.Unit.Orders[0].AbilityId == BuildingType.LookUp[UnitTypes.PHOTON_CANNON].Ability))
                    continue;
                bool flee = false;
                Unit closestEnemy = null;
                float enemyDist = 9 * 9;
                foreach (Unit enemy in tyr.Observation.Observation.RawData.Units)
                {
                    if (enemy.Alliance != Alliance.Enemy)
                        continue;
                    if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType) && !UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                        continue;

                    float newDist = SC2Util.DistanceSq(agent.Unit.Pos, enemy.Pos);
                    if (newDist <= enemyDist)
                    {
                        flee = true;
                        closestEnemy = enemy;
                        enemyDist = newDist;
                    }
                }

                if (SC2Util.DistanceSq(agent.Unit.Pos, tyr.TargetManager.AttackTarget) >= 15 * 15)
                    flee = false;

                bool buildThings = flee || SC2Util.DistanceSq(agent.Unit.Pos, tyr.TargetManager.AttackTarget) <= 3 * 3;

                if (buildThings)
                {
                    int pylonsNeeded = 3 + tyr.UnitManager.Count(UnitTypes.PHOTON_CANNON) / 2;

                    if (tyr.Observation.Observation.PlayerCommon.Minerals >= 100 && tyr.UnitManager.Count(UnitTypes.FORGE) > 0 && tyr.Frame - LastBuiltFrame >= 5
                        && tyr.UnitManager.Count(UnitTypes.PYLON) < pylonsNeeded)
                    {
                        Point2D buildLocation = tyr.buildingPlacer.FindPlacement(SC2Util.To2D(agent.Unit.Pos), SC2Util.Point(2, 2), UnitTypes.PYLON);
                        agent.Order(BuildingType.LookUp[UnitTypes.PYLON].Ability, buildLocation);
                        LastBuiltFrame = tyr.Frame;
                        continue;
                    }
                    else if (tyr.Observation.Observation.PlayerCommon.Minerals >= 150 && tyr.UnitManager.Completed(UnitTypes.FORGE) > 0 && tyr.Frame - LastBuiltFrame >= 5)
                    {
                        Point aroundPos = null;
                        foreach (Agent pylon in tyr.UnitManager.Agents.Values)
                            if (pylon.Unit.UnitType == UnitTypes.PYLON && SC2Util.DistanceSq(pylon.Unit.Pos, agent.Unit.Pos) <= 20 * 20
                                && pylon.Unit.BuildProgress >= 1)
                                aroundPos = pylon.Unit.Pos;

                        if (aroundPos != null)
                        {
                            Point2D buildLocation = tyr.buildingPlacer.FindPlacement(SC2Util.To2D(aroundPos), SC2Util.Point(2, 2), UnitTypes.PHOTON_CANNON);
                            agent.Order(BuildingType.LookUp[UnitTypes.PHOTON_CANNON].Ability, buildLocation);
                            LastBuiltFrame = tyr.Frame;
                            continue;
                        }
                    }
                }
                Wander(agent, flee, closestEnemy);
            }
        }

        public void Wander(Agent agent, bool flee, Unit closestEnemy)
        {
            if (flee)
            {
                Point2D fleeDir = SC2Util.Point(0, 0);
                if (closestEnemy != null)
                {
                    fleeDir = SC2Util.Point(agent.Unit.Pos.X - closestEnemy.Pos.X, agent.Unit.Pos.Y - closestEnemy.Pos.Y);
                    fleeDir = SC2Util.Normalize(fleeDir);
                    fleeDir = SC2Util.Point(fleeDir.X * 2, fleeDir.Y * 2);
                }
                Point2D targetDir = SC2Util.Point(Tyr.Bot.TargetManager.AttackTarget.X - agent.Unit.Pos.X, Tyr.Bot.TargetManager.AttackTarget.Y - agent.Unit.Pos.Y);
                targetDir = SC2Util.Normalize(targetDir);

                // Considered harmful.
                Point2D goTo = SC2Util.Point(agent.Unit.Pos.X + fleeDir.X * 2 + targetDir.X * 2, agent.Unit.Pos.Y + fleeDir.Y * 2 + targetDir.Y * 2);

                agent.Order(Abilities.MOVE, goTo);
            }
            else
                agent.Order(Abilities.MOVE, Tyr.Bot.TargetManager.AttackTarget);
        }
    }
}
