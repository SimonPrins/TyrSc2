using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Micro;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class WorkerRushTask : Task
    {
        public static WorkerRushTask Task = new WorkerRushTask();

        public int TakeWorkers = 12;
        protected HashSet<ulong> regenerating = new HashSet<ulong>();
        PriorityTargetting RangedTargetting = new PriorityTargetting();
        PriorityTargetting CloseTargetting = new PriorityTargetting();
        protected bool Close = false;
        public bool MoveCommandWhenSafe = false;
        private MoveWhenSafeController MoveWhenSafeController = new MoveWhenSafeController();

        public WorkerRushTask() : base(9)
        {
            RangedTargetting.DefaultPriorities.MaxRange = 10;
            RangedTargetting.DefaultPriorities[UnitTypes.LARVA] = -1;
            RangedTargetting.DefaultPriorities[UnitTypes.EGG] = -1;
            RangedTargetting.DefaultPriorities[UnitTypes.OVERLORD] = -1;
            RangedTargetting.DefaultPriorities[UnitTypes.HATCHERY] = -1;
            RangedTargetting.DefaultPriorities[UnitTypes.SPAWNING_POOL] = -1;

            foreach (uint t in UnitTypes.BuildingTypes)
                RangedTargetting.DefaultPriorities[t] = -1;

            CloseTargetting.DefaultPriorities.MaxRange = 2;
            CloseTargetting.DefaultPriorities[UnitTypes.LARVA] = -1;
            CloseTargetting.DefaultPriorities[UnitTypes.EGG] = -1;
            CloseTargetting.DefaultPriorities[UnitTypes.OVERLORD] = -1;
            CloseTargetting.DefaultPriorities[UnitTypes.HATCHERY] = -1;
            CloseTargetting.DefaultPriorities[UnitTypes.SPAWNING_POOL] = -1;

            foreach (uint t in UnitTypes.BuildingTypes)
                CloseTargetting.DefaultPriorities[t] = -1;
        }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && (TakeWorkers > 0 || 
                SC2Util.DistanceSq(agent.Unit.Pos, Bot.Main.MapAnalyzer.StartLocation) >= 20 * 20 && SC2Util.DistanceSq(agent.Unit.Pos, Bot.Main.MapAnalyzer.StartLocation) <= 41 * 41);
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Count = TakeWorkers, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void Add(Agent agent)
        {
            base.Add(agent);
            TakeWorkers--;
        }

        public override void OnFrame(Bot bot)
        {
            ulong mineral = 0;
            if (bot.BaseManager.Main.BaseLocation.MineralFields.Count > 0)
                mineral = bot.BaseManager.Main.BaseLocation.MineralFields[0].Tag;

            if (!Close)
            {
                foreach (Agent agent in units)
                {
                    agent.Order(Abilities.MOVE, bot.TargetManager.AttackTarget);
                    if (agent.DistanceSq(bot.TargetManager.AttackTarget) <= 15 * 15)
                        Close = true;
                    int closeEnemies = 0;
                    foreach (Unit enemy in bot.Enemies())
                    {
                        if (agent.DistanceSq(enemy) <= 10 * 10)
                            closeEnemies++;
                    }
                    if (closeEnemies > 3)
                        Close = true;
                }
                return;
            }

            foreach (Agent agent in units)
            {
                if (!regenerating.Contains(agent.Unit.Tag) && agent.Unit.Shield <= 3 && agent.Unit.UnitType == UnitTypes.PROBE)
                    regenerating.Add(agent.Unit.Tag);
                else if (regenerating.Contains(agent.Unit.Tag) && agent.Unit.Shield == agent.Unit.ShieldMax)
                    regenerating.Remove(agent.Unit.Tag);

                if (regenerating.Contains(agent.Unit.Tag))
                {
                    bool flee = false;
                    foreach (Unit enemy in bot.Observation.Observation.RawData.Units)
                    {
                        if (enemy.Alliance != Alliance.Enemy)
                            continue;
                        if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType) && !UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                            continue;

                        if (SC2Util.DistanceSq(agent.Unit.Pos, enemy.Pos) <= 3 * 3)
                        {
                            flee = true;
                            break;
                        }
                    }

                    if (flee)
                    {
                        if (mineral == 0)
                            agent.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
                        else
                            agent.Order(Abilities.MOVE, mineral);
                    }
                    else
                        agent.Order(Abilities.ATTACK, bot.TargetManager.AttackTarget);
                }
                else
                {
                    Unit broodling = GetBroodling(agent);
                    if (broodling != null || agent.Unit.WeaponCooldown > 6)
                    {
                        if (mineral == 0)
                            agent.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
                        else
                            agent.Order(Abilities.MOVE, mineral);
                        continue;
                    }

                    Unit closeTarget = CloseTargetting.GetTarget(agent);
                    if (closeTarget != null
                        && !UnitTypes.BuildingTypes.Contains(closeTarget.UnitType))
                    {
                        //agent.Order(Abilities.ATTACK, closeTarget.Tag);
                        agent.Order(Abilities.ATTACK, bot.TargetManager.AttackTarget);
                        continue;
                    }

                    Unit rangeTarget = RangedTargetting.GetTarget(agent);
                    if (rangeTarget != null
                        && !UnitTypes.BuildingTypes.Contains(rangeTarget.UnitType))
                    {
                        agent.Order(Abilities.ATTACK, rangeTarget.Tag);
                        continue;
                    }
                    if (MoveCommandWhenSafe
                       && MoveWhenSafeController.DetermineAction(agent, bot.TargetManager.AttackTarget))
                        continue;

                    agent.Order(Abilities.ATTACK, bot.TargetManager.AttackTarget);
                }
            }
        }

        private Unit GetBroodling(Agent agent)
        {
            Unit broodling = null;
            float dist = 6 * 6;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BROODLING)
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    dist = newDist;
                    broodling = enemy;
                }
            }
            return broodling;
        }
    }
}
