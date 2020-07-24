using System.Collections.Generic;

namespace Tyr.Agents
{
    public class TrainingType
    {
        public uint UnitType;
        public HashSet<uint> ProducingUnits;
        public uint Ability;
        public uint WarpInAbility;
        public int Minerals;
        public int Gas;
        public int Food;
        public bool RequiresTechLab;
        public bool IsAddOn;

        public static Dictionary<uint, TrainingType> LookUp = GetLookUp();

        public static Dictionary<uint, TrainingType> GetLookUp()
        {
            Dictionary<uint, TrainingType> result = new Dictionary<uint, TrainingType>();

            Add(result, new TrainingType() { UnitType = UnitTypes.MOTHERSHIP, ProducingUnits = Set(UnitTypes.NEXUS), Minerals = 400, Gas = 400, Ability = 110 });
            Add(result, new TrainingType() { UnitType = UnitTypes.BARRACKS_TECH_LAB, ProducingUnits = Set(UnitTypes.BARRACKS), Minerals = 50, Gas = 25, Ability = 421, IsAddOn = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.BARRACKS_REACTOR, ProducingUnits = Set(UnitTypes.BARRACKS), Minerals = 50, Gas = 50, Ability = 422, IsAddOn = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.FACTORY_TECH_LAB, ProducingUnits = Set(UnitTypes.FACTORY), Minerals = 50, Gas = 25, Ability = 454, IsAddOn = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.FACTORY_REACTOR, ProducingUnits = Set(UnitTypes.FACTORY), Minerals = 50, Gas = 50, Ability = 455, IsAddOn = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.STARPORT_TECH_LAB, ProducingUnits = Set(UnitTypes.STARPORT), Minerals = 50, Gas = 25, Ability = 487, IsAddOn = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.STARPORT_REACTOR, ProducingUnits = Set(UnitTypes.STARPORT), Minerals = 50, Gas = 50, Ability = 488, IsAddOn = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.SCV, ProducingUnits = new HashSet<uint>() { UnitTypes.COMMAND_CENTER, UnitTypes.ORBITAL_COMMAND, UnitTypes.PLANETARY_FORTRESS }, Minerals = 50, Ability = 524, Food = 1 });
            Add(result, new TrainingType() { UnitType = UnitTypes.MARINE, ProducingUnits = Set(UnitTypes.BARRACKS), Minerals = 50, Ability = 560, Food = 1 });
            Add(result, new TrainingType() { UnitType = UnitTypes.REAPER, ProducingUnits = Set(UnitTypes.BARRACKS), Minerals = 50, Gas = 50, Ability = 561, Food = 1 });
            Add(result, new TrainingType() { UnitType = UnitTypes.SIEGE_TANK, ProducingUnits = Set(UnitTypes.FACTORY), Minerals = 150, Gas = 125, Ability = 591, Food = 3, RequiresTechLab = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.HELLION, ProducingUnits = Set(UnitTypes.FACTORY), Minerals = 100, Ability = 595, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.HELLBAT, ProducingUnits = Set(UnitTypes.FACTORY), Minerals = 100, Ability = 596, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.CYCLONE, ProducingUnits = Set(UnitTypes.FACTORY), Minerals = 150, Gas = 100, Ability = 597, Food = 3, RequiresTechLab = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.WIDOW_MINE, ProducingUnits = Set(UnitTypes.FACTORY), Minerals = 75, Gas = 25, Ability = 614, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.BANSHEE, ProducingUnits = Set(UnitTypes.STARPORT), Minerals = 150, Gas = 100, Ability = 621, Food = 3, RequiresTechLab = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.RAVEN, ProducingUnits = Set(UnitTypes.STARPORT), Minerals = 100, Gas = 200, Ability = 622, Food = 2, RequiresTechLab = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.BATTLECRUISER, ProducingUnits = Set(UnitTypes.STARPORT), Minerals = 400, Gas = 300, Ability = 623, Food = 6, RequiresTechLab = true });
            Add(result, new TrainingType() { UnitType = UnitTypes.VIKING_FIGHTER, ProducingUnits = Set(UnitTypes.STARPORT), Minerals = 150, Gas = 75, Ability = 624, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.LIBERATOR, ProducingUnits = Set(UnitTypes.STARPORT), Minerals = 150, Gas = 150, Ability = 626, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.ZEALOT, ProducingUnits = Set(UnitTypes.GATEWAY, UnitTypes.WARP_GATE), Minerals = 100, Ability = 916, WarpInAbility = 1413, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.STALKER, ProducingUnits = Set(UnitTypes.GATEWAY, UnitTypes.WARP_GATE), Minerals = 125, Gas = 50, Ability = 917, WarpInAbility = 1414, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.HIGH_TEMPLAR, ProducingUnits = Set(UnitTypes.GATEWAY, UnitTypes.WARP_GATE), Minerals = 50, Gas = 150, Ability = 919, WarpInAbility = 1416, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.DARK_TEMPLAR, ProducingUnits = Set(UnitTypes.GATEWAY, UnitTypes.WARP_GATE), Minerals = 125, Gas = 125, Ability = 920, WarpInAbility = 1417, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.SENTRY, ProducingUnits = Set(UnitTypes.GATEWAY, UnitTypes.WARP_GATE), Minerals = 50, Gas = 100, Ability = 921, WarpInAbility = 1418, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.ADEPT, ProducingUnits = Set(UnitTypes.GATEWAY, UnitTypes.WARP_GATE), Minerals = 100, Gas = 25, Ability = 922, WarpInAbility = 1419, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.PHOENIX, ProducingUnits = Set(UnitTypes.STARGATE), Minerals = 150, Gas = 100, Ability = 946, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.CARRIER, ProducingUnits = Set(UnitTypes.STARGATE), Minerals = 350, Gas = 250, Ability = 948, Food = 6 });
            Add(result, new TrainingType() { UnitType = UnitTypes.VOID_RAY, ProducingUnits = Set(UnitTypes.STARGATE), Minerals = 250, Gas = 150, Ability = 950, Food = 4 });
            Add(result, new TrainingType() { UnitType = UnitTypes.ORACLE, ProducingUnits = Set(UnitTypes.STARGATE), Minerals = 150, Gas = 150, Ability = 954, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.TEMPEST, ProducingUnits = Set(UnitTypes.STARGATE), Minerals = 250, Gas = 175, Ability = 955, Food = 5 });
            Add(result, new TrainingType() { UnitType = UnitTypes.WARP_PRISM, ProducingUnits = Set(UnitTypes.ROBOTICS_FACILITY), Minerals = 250, Ability = 976, Food = 2 });
            Add(result, new TrainingType() { UnitType = UnitTypes.OBSERVER, ProducingUnits = Set(UnitTypes.ROBOTICS_FACILITY), Minerals = 25, Gas = 75, Ability = 977, Food = 1 });
            Add(result, new TrainingType() { UnitType = UnitTypes.COLOSUS, ProducingUnits = Set(UnitTypes.ROBOTICS_FACILITY), Minerals = 300, Gas = 200, Ability = 978, Food = 6 });
            Add(result, new TrainingType() { UnitType = UnitTypes.IMMORTAL, ProducingUnits = Set(UnitTypes.ROBOTICS_FACILITY), Minerals = 275, Gas = 100, Ability = 979, Food = 4 });
            Add(result, new TrainingType() { UnitType = UnitTypes.DISRUPTOR, ProducingUnits = Set(UnitTypes.ROBOTICS_FACILITY), Minerals = 150, Gas = 150, Ability = 994, Food = 3 });
            Add(result, new TrainingType() { UnitType = UnitTypes.PROBE, ProducingUnits = Set(UnitTypes.NEXUS), Minerals = 50, Ability = 1006, Food = 1 });
            Add(result, new TrainingType() { UnitType = UnitTypes.LAIR, ProducingUnits = Set(UnitTypes.HATCHERY), Minerals = 150, Gas = 100, Ability = 1216 });
            Add(result, new TrainingType() { UnitType = UnitTypes.HIVE, ProducingUnits = Set(UnitTypes.LAIR), Minerals = 200, Gas = 150, Ability = 1218 });
            Add(result, new TrainingType() { UnitType = UnitTypes.GREATER_SPIRE, ProducingUnits = Set(UnitTypes.SPIRE), Minerals = 100, Gas = 150, Ability = 1220 });
            Add(result, new TrainingType() { UnitType = UnitTypes.ORBITAL_COMMAND, ProducingUnits = Set(UnitTypes.COMMAND_CENTER), Minerals = 150, Ability = 1516 });
            Add(result, new TrainingType() { UnitType = UnitTypes.QUEEN, ProducingUnits = new HashSet<uint>() { UnitTypes.HATCHERY, UnitTypes.LAIR, UnitTypes.HIVE }, Minerals = 150, Ability = 1632 });

            return result;
        }

        private static HashSet<T> Set<T>(params T[] elements)
        {
            HashSet<T> set = new HashSet<T>();
            foreach (T element in elements)
                set.Add(element);
            return set;
        }

        private static void Add(Dictionary<uint, TrainingType> result, TrainingType trainingType)
        {
            result.Add(trainingType.UnitType, trainingType);
        }
    }
}
