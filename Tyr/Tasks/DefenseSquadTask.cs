﻿using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    public class DefenseSquadTask : Task
    {
        public static List<DefenseSquadTask> Tasks = new List<DefenseSquadTask>();
        public Base Base;
        public Point2D OverrideDefenseLocation;
        public Point2D OverrideIdleLocation;
        private Point2D IdleLocation;
        public int MaxDefenders = 3;
        public uint Type;
        public bool AlwaysNeeded = false;
        public bool DraftFromFarAway = false;
        public float DefendRange = 20;
        public bool RetreatMoveCommand = false;


        public DefenseSquadTask(Base b) : base(6)
        {
            Base = b;
            Type = 0;
        }

        public DefenseSquadTask(Base b, uint type) : base(6)
        {
            Base = b;
            Type = type;
        }

        public static void Enable(bool excludeMainAndNatural, uint type)
        {
            if (Tasks.Count == 0)
                foreach (Base b in Bot.Main.BaseManager.Bases)
                {
                    DefenseSquadTask task = new DefenseSquadTask(b, type);
                    Tasks.Add(task);
                    Bot.Main.TaskManager.Add(task);
                }

            Enable(Tasks, excludeMainAndNatural, excludeMainAndNatural);
        }

        public static void Enable(List<DefenseSquadTask> tasks, bool excludeMain, bool excludeNatural)
        {
            foreach (DefenseSquadTask task in tasks)
            {
                if (task.Base == Bot.Main.BaseManager.Main && excludeMain)
                    task.Stopped = true;
                else if (task.Base == Bot.Main.BaseManager.Natural && excludeNatural)
                    task.Stopped = true;
                else
                    task.Stopped = false;
            }
        }

        public static List<DefenseSquadTask>  GetDefenseTasks(uint type)
        {
            List<DefenseSquadTask> tasks = new List<DefenseSquadTask>();
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                DefenseSquadTask task = new DefenseSquadTask(b, type);
                Bot.Main.TaskManager.Add(task);
                tasks.Add(task);
            }
            return tasks;
        }

        public override bool DoWant(Agent agent)
        {
            return (agent.Unit.UnitType == Type || Type == 0)
                && (UnitTypes.CombatUnitTypes.Contains(agent.Unit.UnitType) || Type != 0)
                && units.Count < MaxDefenders
                && (agent.DistanceSq(Bot.Main.MapAnalyzer.StartLocation) <= 55 * 55 || DraftFromFarAway);
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            HashSet<uint> unitTypes = null;
            if (Type != 0)
                unitTypes = new HashSet<uint>() { Type };
            result.Add(new UnitDescriptor() { Pos = Base.BaseLocation.Pos, Count = MaxDefenders - units.Count, UnitTypes = unitTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return Base.Owner == Bot.Main.PlayerId || AlwaysNeeded;
        }

        public override void OnFrame(Bot bot)
        {
            if (Stopped || (Base.Owner != Bot.Main.PlayerId && !AlwaysNeeded))
            {
                Clear();
                return;
            }
            while (units.Count > MaxDefenders)
                ClearLast();

            if (OverrideIdleLocation != null)
                IdleLocation = OverrideIdleLocation;
            else if (IdleLocation == null)
                IdleLocation = bot.MapAnalyzer.Walk(Base.BaseLocation.Pos, bot.MapAnalyzer.EnemyDistances, 8);

            float distance = DefendRange * DefendRange;
            Unit target = null;
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (unit.UnitType == UnitTypes.ADEPT_PHASE_SHIFT
                    || unit.UnitType == UnitTypes.KD8_CHARGE)
                    continue;

                if (unit.UnitType == UnitTypes.CHANGELING
                    || unit.UnitType == UnitTypes.CHANGELING_MARINE
                    || unit.UnitType == UnitTypes.CHANGELING_MARINE_SHIELD
                    || unit.UnitType == UnitTypes.CHANGELING_ZEALOT
                    || unit.UnitType == UnitTypes.CHANGELING_ZERGLING
                    || unit.UnitType == UnitTypes.CHANGELING_ZERGLING_WINGS)
                    continue;
                
                float newDist = SC2Util.DistanceSq(unit.Pos, OverrideDefenseLocation == null ? Base.BaseLocation.Pos : OverrideDefenseLocation);
                
                if (newDist > distance)
                    continue;

                distance = newDist;
                target = unit;
            }

            if (target == null)
            {
                foreach (Agent agent in units)
                {
                    if (agent.DistanceSq(IdleLocation) > 3 * 3)
                    {
                        if (RetreatMoveCommand)
                            agent.Order(Abilities.MOVE, IdleLocation);
                        else
                            bot.MicroController.Attack(agent, IdleLocation);
                    }
                    else if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK)
                        agent.Order(Abilities.SIEGE);
                }
            }
            else
            {
                foreach (Agent agent in units)
                {
                    if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED
                        && agent.DistanceSq(IdleLocation) <= 3 * 3)
                        continue;
                    bot.MicroController.Attack(agent, SC2Util.To2D(target.Pos));
                }
            }
        }
    }
}
