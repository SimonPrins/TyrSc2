﻿using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Managers
{
    public class UnitManager : Manager
    {
        public Dictionary<ulong, Agent> Agents = new Dictionary<ulong, Agent>();

        // Counts the number of units of each type we own.
        public Dictionary<uint, int> Counts = new Dictionary<uint, int>();
        public Dictionary<uint, int> CompletedCounts = new Dictionary<uint, int>();
        public uint FoodExpected { get; internal set; }

        public Dictionary<ulong, Agent> DisappearedUnits = new Dictionary<ulong, Agent>();
        public HashSet<uint> ActiveOrders = new HashSet<uint>();

        public void OnFrame(Bot bot)
        {
            Counts = new Dictionary<uint, int>();
            CompletedCounts = new Dictionary<uint, int>();
            HashSet<ulong> existingUnits = new HashSet<ulong>();
            foreach (Base b in bot.BaseManager.Bases)
            {
                b.BuildingCounts = new Dictionary<uint, int>();
                b.BuildingsCompleted = new Dictionary<uint, int>();
            }
            ActiveOrders = new HashSet<uint>();

            int directCountNexus = 0;
            int abilityCountNexus = 0;
            FoodExpected = 0;
            // Update our unit set.
            foreach (Unit unit in bot.Observation.Observation.RawData.Units)
            {
                if (unit.Owner != bot.PlayerId)
                {
                    Agents.Remove(unit.Tag);
                    continue;
                }
                // Count how many of each unitType we have.
                CollectionUtil.Increment(Counts, unit.UnitType);
                if (unit.UnitType == UnitTypes.NEXUS)
                    directCountNexus++;
                if (UnitTypes.EquivalentTypes.ContainsKey(unit.UnitType))
                    foreach (uint t in UnitTypes.EquivalentTypes[unit.UnitType])
                        CollectionUtil.Increment(Counts, t);
                if (unit.BuildProgress >= 0.9999f)
                {
                    CollectionUtil.Increment(CompletedCounts, unit.UnitType);
                    if (UnitTypes.EquivalentTypes.ContainsKey(unit.UnitType))
                        foreach (uint t in UnitTypes.EquivalentTypes[unit.UnitType])
                            CollectionUtil.Increment(CompletedCounts, t);
                }

                if (unit.Orders != null && unit.Orders.Count > 0 && Abilities.Creates.ContainsKey(unit.Orders[0].AbilityId)
                    && unit.UnitType != UnitTypes.SCV && unit.UnitType != UnitTypes.PROBE)
                {
                    CollectionUtil.Increment(Counts, Abilities.Creates[unit.Orders[0].AbilityId]);
                    if (Abilities.Creates[unit.Orders[0].AbilityId] == UnitTypes.NEXUS)
                        abilityCountNexus++;
                    if (unit.Orders.Count >= 2 && unit.Orders[1].Progress > 0 && Abilities.Creates.ContainsKey(unit.Orders[1].AbilityId))
                        CollectionUtil.Increment(Counts, Abilities.Creates[unit.Orders[1].AbilityId]);
                }

                if (unit.BuildProgress < 1 && (unit.UnitType == UnitTypes.PYLON || unit.UnitType == UnitTypes.SUPPLY_DEPOT))
                    FoodExpected += 8;
                if (unit.Orders != null && unit.Orders.Count > 0 && unit.Orders[0].AbilityId == 1344)
                    FoodExpected += 8;
                if (unit.Orders != null && unit.Orders.Count > 0 && unit.Orders[0].AbilityId == 1216)
                    CollectionUtil.Increment(Counts, UnitTypes.LAIR);


                existingUnits.Add(unit.Tag);

                if (unit.Passengers != null)
                {
                    foreach (PassengerUnit passenger in unit.Passengers)
                    {
                        CollectionUtil.Increment(Counts, passenger.UnitType);
                        CollectionUtil.Increment(CompletedCounts, passenger.UnitType);
                    }
                }

                if (Agents.ContainsKey(unit.Tag))
                {
                    Agent agent = Agents[unit.Tag];
                    agent.PreviousUnit = agent.Unit;
                    agent.Unit = unit;

                    agent.Command = null;
                    if (agent.Base != null)
                    {
                        CollectionUtil.Increment(agent.Base.BuildingCounts, unit.UnitType);
                        if (UnitTypes.EquivalentTypes.ContainsKey(unit.UnitType))
                            foreach (uint t in UnitTypes.EquivalentTypes[unit.UnitType])
                                CollectionUtil.Increment(agent.Base.BuildingCounts, t);
                        if (unit.BuildProgress >= 0.9999f)
                        {
                            CollectionUtil.Increment(agent.Base.BuildingsCompleted, unit.UnitType);
                            if (UnitTypes.EquivalentTypes.ContainsKey(unit.UnitType))
                                foreach (uint t in UnitTypes.EquivalentTypes[unit.UnitType])
                                    CollectionUtil.Increment(agent.Base.BuildingsCompleted, t);
                        }
                    }
                }
                else
                {
                    if (DisappearedUnits.ContainsKey(unit.Tag))
                    {
                        Agents.Add(unit.Tag, DisappearedUnits[unit.Tag]);
                        DisappearedUnits[unit.Tag].Unit = unit;
                    }
                    else
                    {
                        Agent agent = new Agent(unit);
                        Agents.Add(unit.Tag, agent);
                        bot.TaskManager.NewAgent(agent);
                    }
                }

                if (unit.Orders != null && unit.Orders.Count > 0)
                    ActiveOrders.Add(unit.Orders[0].AbilityId);
            }

            int buildRequestNexusCounts = 0;
            foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
            {
                // Count how many of each unitType we intend to build.
                CollectionUtil.Increment(Counts, request.Type);
                if (request.Type == UnitTypes.NEXUS)
                    buildRequestNexusCounts++;
                if (request.Type == UnitTypes.PYLON)
                    FoodExpected += 8;
                if (request.Type == UnitTypes.SUPPLY_DEPOT)
                    FoodExpected += 8;
                if (request.Base != null)
                    CollectionUtil.Increment(request.Base.BuildingCounts, request.Type);

                if (request.worker.Unit.Orders == null
                    || request.worker.Unit.Orders.Count == 0
                    || request.worker.Unit.Orders[0].AbilityId != BuildingType.LookUp[request.Type].Ability)
                {
                    bot.ReservedMinerals += BuildingType.LookUp[request.Type].Minerals;
                    bot.ReservedGas += BuildingType.LookUp[request.Type].Gas;
                    string workerAbility = "";
                    if (request.worker.Unit.Orders != null
                        && request.worker.Unit.Orders.Count > 0)
                        workerAbility = " Ability: " + request.worker.Unit.Orders[0].AbilityId;
                    bot.DrawText("Reserving: " + BuildingType.LookUp[request.Type].Name + workerAbility);
                }
            }

            foreach (BuildRequest request in ConstructionTask.Task.UnassignedRequests)
            {
                // Count how many of each unitType we intend to build.
                CollectionUtil.Increment(Counts, request.Type);
                if (request.Type == UnitTypes.NEXUS)
                    buildRequestNexusCounts++;
                FoodExpected += 8;
                if (request.Base != null)
                    CollectionUtil.Increment(request.Base.BuildingCounts, request.Type);

                bot.ReservedMinerals += BuildingType.LookUp[request.Type].Minerals;
                bot.ReservedGas += BuildingType.LookUp[request.Type].Gas;
                bot.DrawText("Reserving: " + BuildingType.LookUp[request.Type].Name);
            }

            // Remove dead units.
            if (bot.Observation != null
                && bot.Observation.Observation != null
                && bot.Observation.Observation.RawData != null
                && bot.Observation.Observation.RawData.Event != null
                && bot.Observation.Observation.RawData.Event.DeadUnits != null)
                foreach (ulong deadUnit in bot.Observation.Observation.RawData.Event.DeadUnits)
                    Agents.Remove(deadUnit);

            Bot.Main.DrawText("Direct nexus count: " + directCountNexus);
            Bot.Main.DrawText("Ability nexus count: " + abilityCountNexus);
            Bot.Main.DrawText("Build request nexus count: " + buildRequestNexusCounts);
        }

        public int Count(uint type)
        {
            if (Counts.ContainsKey(type))
                return Counts[type];
            else
                return 0;
        }

        public int Completed(uint type)
        {
            if (CompletedCounts.ContainsKey(type))
                return CompletedCounts[type];
            else
                return 0;
        }

        public int Count(HashSet<uint> types)
        {
            int total = 0;
            foreach (uint type in types)
                total += Count(type);
            return total;
        }

        public int Completed(HashSet<uint> types)
        {
            int total = 0;
            foreach (uint type in types)
                total += Completed(type);
            return total;
        }

        public void AddActions(List<Action> actions)
        {
            foreach (KeyValuePair<ulong, Agent> pair in Agents)
            {
                if (Bot.Main.ArchonMode && pair.Value.Unit.IsSelected)
                    continue;
                if (pair.Value.Command != null)
                {
                    Action action = new Action();
                    action.ActionRaw = new ActionRaw();
                    action.ActionRaw.UnitCommand = pair.Value.Command;
                    actions.Add(action);
                }
            }
        }

        public void BuildingConstructing(BuildRequest request)
        {
            CollectionUtil.Increment(Counts, request.Type);
            if (request.Base != null)
                CollectionUtil.Increment(request.Base.BuildingCounts, request.Type);
        }

        public void UnitTraining(uint unitType)
        {
            if (unitType == UnitTypes.ZERGLING)
                CollectionUtil.Add(Counts, unitType, 2);
            else
                CollectionUtil.Increment(Counts, unitType);
        }
    }
}
