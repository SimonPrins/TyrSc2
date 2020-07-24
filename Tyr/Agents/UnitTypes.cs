using SC2APIProtocol;
using System.Collections.Generic;

namespace Tyr.Agents
{
    class UnitTypes
    {
        public static Dictionary<uint, UnitTypeData> LookUp = new Dictionary<uint, UnitTypeData>();
        public static uint COLOSUS = 4;
        public static uint TECH_LAB = 5;
        public static uint REACTOR = 6;
        public static uint INFESTOR_TERRAN = 7;
        public static uint BANELING_COCOON = 8;
        public static uint BANELING = 9;
        public static uint MOTHERSHIP = 10;
        public static uint POINT_DEFENSE_DRONE = 11;
        public static uint CHANGELING = 12;
        public static uint CHANGELING_ZEALOT = 13;
        public static uint CHANGELING_MARINE_SHIELD = 14;
        public static uint CHANGELING_MARINE = 15;
        public static uint CHANGELING_ZERGLING_WINGS = 16;
        public static uint CHANGELING_ZERGLING = 17;
        public static uint COMMAND_CENTER = 18;
        public static uint SUPPLY_DEPOT = 19;
        public static uint REFINERY = 20;
        public static uint BARRACKS = 21;
        public static uint ENGINEERING_BAY = 22;
        public static uint MISSILE_TURRET = 23;
        public static uint BUNKER = 24;
        public static uint SENSOR_TOWER = 25;
        public static uint GHOST_ACADEMY = 26;
        public static uint FACTORY = 27;
        public static uint STARPORT = 28;
        public static uint ARMORY = 29;
        public static uint FUSION_CORE = 30;
        public static uint AUTO_TURRET = 31;
        public static uint SIEGE_TANK_SIEGED = 32;
        public static uint SIEGE_TANK = 33;
        public static uint VIKING_ASSUALT = 34;
        public static uint VIKING_FIGHTER = 35;
        public static uint COMMAND_CENTER_FLYING = 36;
        public static uint BARRACKS_TECH_LAB = 37;
        public static uint BARRACKS_REACTOR = 38;
        public static uint FACTORY_TECH_LAB = 39;
        public static uint FACTORY_REACTOR = 40;
        public static uint STARPORT_TECH_LAB = 41;
        public static uint STARPORT_REACTOR = 42;
        public static uint FACTORY_FLYING = 43;
        public static uint STARPORT_FLYING = 44;
        public static uint SCV = 45;
        public static uint BARRACKS_FLYING = 46;
        public static uint SUPPLY_DEPOT_LOWERED = 47;
        public static uint MARINE = 48;
        public static uint REAPER = 49;
        public static uint GHOST = 50;
        public static uint MARAUDER = 51;
        public static uint THOR = 52;
        public static uint HELLION = 53;
        public static uint MEDIVAC = 54;
        public static uint BANSHEE = 55;
        public static uint RAVEN = 56;
        public static uint BATTLECRUISER = 57;
        public static uint NUKE = 58;
        public static uint NEXUS = 59;
        public static uint PYLON = 60;
        public static uint ASSIMILATOR = 61;
        public static uint GATEWAY = 62;
        public static uint FORGE = 63;
        public static uint FLEET_BEACON = 64;
        public static uint TWILIGHT_COUNSEL = 65;
        public static uint PHOTON_CANNON = 66;
        public static uint STARGATE = 67;
        public static uint TEMPLAR_ARCHIVE = 68;
        public static uint DARK_SHRINE = 69;
        public static uint ROBOTICS_BAY = 70;
        public static uint ROBOTICS_FACILITY = 71;
        public static uint CYBERNETICS_CORE = 72;
        public static uint ZEALOT = 73;
        public static uint STALKER = 74;
        public static uint HIGH_TEMPLAR = 75;
        public static uint DARK_TEMPLAR = 76;
        public static uint SENTRY = 77;
        public static uint PHOENIX = 78;
        public static uint CARRIER = 79;
        public static uint VOID_RAY = 80;
        public static uint WARP_PRISM = 81;
        public static uint OBSERVER = 82;
        public static uint IMMORTAL = 83;
        public static uint PROBE = 84;
        public static uint INTERCEPTOR = 85;
        public static uint HATCHERY = 86;
        public static uint CREEP_TUMOR = 87;
        public static uint EXTRACTOR = 88;
        public static uint SPAWNING_POOL = 89;
        public static uint EVOLUTION_CHAMBER = 90;
        public static uint HYDRALISK_DEN = 91;
        public static uint SPIRE = 92;
        public static uint ULTRALISK_CAVERN = 93;
        public static uint INFESTATION_PIT = 94;
        public static uint NYDUS_NETWORK = 95;
        public static uint BANELING_NEST = 96;
        public static uint ROACH_WARREN = 97;
        public static uint SPINE_CRAWLER = 98;
        public static uint SPORE_CRAWLER = 99;
        public static uint LAIR = 100;
        public static uint HIVE = 101;
        public static uint GREATER_SPIRE = 102;
        public static uint EGG = 103;
        public static uint DRONE = 104;
        public static uint ZERGLING = 105;
        public static uint OVERLORD = 106;
        public static uint HYDRALISK = 107;
        public static uint MUTALISK = 108;
        public static uint ULTRALISK = 109;
        public static uint ROACH = 110;
        public static uint INFESTOR = 111;
        public static uint CORRUPTOR = 112;
        public static uint BROOD_LORD_COCOON = 113;
        public static uint BROOD_LORD = 114;
        public static uint BANELING_BURROWED = 115;
        public static uint DRONE_BURROWED = 116;
        public static uint HYDRALISK_BURROWED = 117;
        public static uint ROACH_BURROWED = 118;
        public static uint ZERGLING_BURROWED = 119;
        public static uint INFESTOR_TERRAN_BURROWED = 120;
        public static uint QUEEN_BURROWED = 125;
        public static uint QUEEN = 126;
        public static uint INFESTOR_BURROWED = 127;
        public static uint OVERLORD_COCOON = 128;
        public static uint OVERSEER = 129;
        public static uint PLANETARY_FORTRESS = 130;
        public static uint ULTRALISK_BURROWED = 131;
        public static uint ORBITAL_COMMAND = 132;
        public static uint WARP_GATE = 133;
        public static uint ORBITAL_COMMAND_FLYING = 134;
        public static uint FORCE_FIELD = 135;
        public static uint WARP_PRISM_PHASING = 136;
        public static uint CREEP_TUMOR_BURROWED = 137;
        public static uint CREEP_TUMOR_QUEEN = 138;
        public static uint SPINE_CRAWLER_UPROOTED = 139;
        public static uint SPORE_CRAWLER_UPROOTED = 140;
        public static uint ARCHON = 141;
        public static uint NYDUS_CANAL = 142;
        public static uint BROODLING_ESCORT = 143;
        public static uint RICH_MINERAL_FIELD = 146;
        public static uint RICH_MINERAL_FIELD_750 = 147;
        public static uint URSADON = 148;
        public static uint XEL_NAGA_TOWER = 149;
        public static uint INFESTED_TERRANS_EGG = 150;
        public static uint LARVA = 151;
        public static uint BROODLING = 289;
        public static uint ADEPT = 311;
        public static uint MINERAL_FIELD = 341;
        public static uint VESPENE_GEYSER = 342;
        public static uint SPACE_PLATFORM_GEYSER = 343;
        public static uint RICH_VESPENE_GEYSER = 344;
        public static uint DESTRUCTIBLE_DEBRIS_ULBR = 376;
        public static uint DESTRUCTIBLE_DEBRIS_BLUR = 377;
        public static uint MINERAL_FIELD_750 = 483;
        public static uint HELLBAT = 484;
        public static uint SWARM_HOST = 494;
        public static uint ORACLE = 495;
        public static uint TEMPEST = 496;
        public static uint WIDOW_MINE = 498;
        public static uint VIPER = 499;
        public static uint WIDOW_MINE_BURROWED = 500;
        public static uint LURKER = 502;
        public static uint LURKER_BURROWED = 503;
        public static uint PROTOSS_VESPENE_GEYSER = 608;
        public static uint DESTRUCTIBLE_ROCKS6X6 = 639;
        public static uint LAB_MINERAL_FIELD = 665;
        public static uint LAB_MINERAL_FIELD_750 = 666;
        public static uint RAVAGER = 688;
        public static uint LIBERATOR = 689;
        public static uint RAVAGER_BURROWED = 690;
        public static uint THOR_SINGLE_TARGET = 691;
        public static uint CYCLONE = 692;
        public static uint LOCUST_FLYING = 693;
        public static uint DISRUPTOR = 694;
        public static uint DISRUPTOR_PHASED = 733;
        public static uint LIBERATOR_AG = 734;
        public static uint PURIFIER_RICH_MINERAL_FIELD = 796;
        public static uint PURIFIER_RICH_MINERAL_FIELD_750 = 797;
        public static uint ADEPT_PHASE_SHIFT = 801;
        public static uint KD8_CHARGE = 830;
        public static uint PURIFIER_VESPENE_GEYSER = 880;
        public static uint SHAKURAS_VESPENE_GEYSER = 881;
        public static uint PURIFIER_MINERAL_FIELD = 884;
        public static uint PURIFIER_MINERAL_FIELD_750 = 885;
        public static uint BATTLE_STATION_MINERAL_FIELD = 886;
        public static uint BATTLE_STATION_MINERAL_FIELD_750 = 887;
        public static uint LURKER_DEN = 504;
        public static uint SHIELD_BATTERY = 1910;
        public static uint REFINERY_RICH = 1943;
        public static uint ASSIMILATOR_RICH = 1980;
        public static uint EXTRACTOR_RICH = 1981;
        public static uint MINERAL_FIELD_450 = 1982;
        public static uint MINERAL_FIELD_OPAQUE = 1983;
        public static uint MINERAL_FIELD_OPAQUE_900 = 1984;

        public static void LoadData(ResponseData data)
        {
            if (Bot.Bot.GameVersion.StartsWith("4.10."))
            {
                ASSIMILATOR_RICH = 1956;
                EXTRACTOR_RICH = 1957;
            }
            foreach (UnitTypeData unitType in data.Units)
            {
                LookUp.Add(unitType.UnitId, unitType);
                if (unitType.AbilityId != 0)
                    Abilities.Creates.Add(unitType.AbilityId, unitType.UnitId);
            }
        }

        public static HashSet<uint> BuildingTypes = new HashSet<uint>
            {
                ARMORY,
                ASSIMILATOR,
                ASSIMILATOR_RICH,
                BANELING_NEST,
                BARRACKS,
                BARRACKS_FLYING,
                BARRACKS_REACTOR,
                BARRACKS_TECH_LAB,
                BUNKER,
                COMMAND_CENTER,
                COMMAND_CENTER_FLYING,
                CYBERNETICS_CORE,
                DARK_SHRINE,
                ENGINEERING_BAY,
                EVOLUTION_CHAMBER,
                EXTRACTOR,
                EXTRACTOR_RICH,
                FACTORY,
                FACTORY_FLYING,
                FACTORY_REACTOR,
                FACTORY_TECH_LAB,
                FLEET_BEACON,
                FORGE,
                FUSION_CORE,
                GATEWAY,
                GHOST_ACADEMY,
                GREATER_SPIRE,
                HATCHERY,
                HIVE,
                HYDRALISK_DEN,
                INFESTATION_PIT,
                LAIR,
                MISSILE_TURRET,
                NEXUS,
                NYDUS_NETWORK,
                ORBITAL_COMMAND,
                ORBITAL_COMMAND_FLYING,
                PHOTON_CANNON,
                PLANETARY_FORTRESS,
                PYLON,
                REACTOR,
                REFINERY,
                REFINERY_RICH,
                ROACH_WARREN,
                ROBOTICS_BAY,
                ROBOTICS_FACILITY,
                SENSOR_TOWER,
                SPAWNING_POOL,
                SPINE_CRAWLER,
                SPINE_CRAWLER_UPROOTED,
                SPIRE,
                SPORE_CRAWLER,
                SPORE_CRAWLER_UPROOTED,
                STARPORT,
                STARGATE,
                STARPORT_FLYING,
                STARPORT_REACTOR,
                STARPORT_TECH_LAB,
                SUPPLY_DEPOT,
                SUPPLY_DEPOT_LOWERED,
                TECH_LAB,
                TEMPLAR_ARCHIVE,
                TWILIGHT_COUNSEL,
                ULTRALISK_CAVERN,
                WARP_GATE,
                SHIELD_BATTERY,
                LURKER_DEN
            };
        public static HashSet<uint> ProductionStructures = new HashSet<uint>
            {
                ARMORY,
                BANELING_NEST,
                BARRACKS,
                BARRACKS_TECH_LAB,
                COMMAND_CENTER,
                CYBERNETICS_CORE,
                ENGINEERING_BAY,
                EVOLUTION_CHAMBER,
                FACTORY,
                FACTORY_TECH_LAB,
                FLEET_BEACON,
                FORGE,
                FUSION_CORE,
                GATEWAY,
                GHOST_ACADEMY,
                GREATER_SPIRE,
                HATCHERY,
                HIVE,
                HYDRALISK_DEN,
                INFESTATION_PIT,
                LAIR,
                NEXUS,
                NYDUS_NETWORK,
                ORBITAL_COMMAND,
                PLANETARY_FORTRESS,
                ROACH_WARREN,
                ROBOTICS_BAY,
                ROBOTICS_FACILITY,
                SPAWNING_POOL,
                SPIRE,
                STARPORT,
                STARGATE,
                STARPORT_TECH_LAB,
                TECH_LAB,
                TEMPLAR_ARCHIVE,
                TWILIGHT_COUNSEL,
                ULTRALISK_CAVERN,
                WARP_GATE,
                LURKER_DEN
            };
        public static HashSet<uint> CombatUnitTypes = new HashSet<uint>
            {
                ARCHON,
                AUTO_TURRET,
                BANELING,
                BANELING_BURROWED,
                BANELING_COCOON,
                BANSHEE,
                BATTLECRUISER,
                BROOD_LORD,
                BROOD_LORD_COCOON,
                CARRIER,
                COLOSUS,
                CORRUPTOR,
                DARK_TEMPLAR,
                GHOST,
                HELLION,
                HIGH_TEMPLAR,
                HYDRALISK,
                HYDRALISK_BURROWED,
                IMMORTAL,
                INFESTOR,
                INFESTED_TERRANS_EGG,
                INFESTOR_BURROWED,
                INFESTOR_TERRAN,
                INFESTOR_TERRAN_BURROWED,
                MARAUDER,
                MARINE,
                MEDIVAC,
                MOTHERSHIP,
                MUTALISK,
                PHOENIX,
                QUEEN,
                QUEEN_BURROWED,
                RAVEN,
                REAPER,
                ROACH,
                ROACH_BURROWED,
                SENTRY,
                SIEGE_TANK,
                SIEGE_TANK_SIEGED,
                STALKER,
                THOR,
                THOR_SINGLE_TARGET,
                ULTRALISK,
                URSADON,
                VIKING_ASSUALT,
                VIKING_FIGHTER,
                VOID_RAY,
                ZEALOT,
                ZERGLING,
                ZERGLING_BURROWED,
                ORACLE,
                TEMPEST,
                ADEPT,
                RAVAGER,
                RAVAGER_BURROWED,
                LURKER,
                LURKER_BURROWED,
                HELLBAT,
                LIBERATOR,
                LIBERATOR_AG,
                CYCLONE,
                WIDOW_MINE,
                WIDOW_MINE_BURROWED,
                SWARM_HOST,
                DISRUPTOR
            };
        public static HashSet<uint> AirAttackTypes = new HashSet<uint>
            {
                ARCHON,
                AUTO_TURRET,
                BATTLECRUISER,
                CARRIER,
                CORRUPTOR,
                GHOST,
                HIGH_TEMPLAR,
                HYDRALISK,
                INFESTOR,
                INFESTOR_TERRAN,
                MARINE,
                MOTHERSHIP,
                MUTALISK,
                PHOENIX,
                QUEEN,
                SENTRY,
                STALKER,
                THOR,
                THOR_SINGLE_TARGET,
                VIKING_FIGHTER,
                VOID_RAY,
                PHOTON_CANNON,
                MISSILE_TURRET,
                SPORE_CRAWLER,
                BUNKER,
                LIBERATOR,
                TEMPEST,
                WIDOW_MINE,
                WIDOW_MINE_BURROWED
            };
        public static HashSet<uint> RangedTypes = new HashSet<uint>
        {
                ARCHON,
                AUTO_TURRET,
                BANSHEE,
                BATTLECRUISER,
                BROOD_LORD,
                CARRIER,
                COLOSUS,
                CORRUPTOR,
                GHOST,
                HELLION,
                HIGH_TEMPLAR,
                IMMORTAL,
                INFESTOR_TERRAN,
                MARAUDER,
                MARINE,
                MEDIVAC,
                MOTHERSHIP,
                MUTALISK,
                PHOENIX,
                QUEEN,
                REAPER,
                ROACH,
                SENTRY,
                SIEGE_TANK,
                SIEGE_TANK_SIEGED,
                STALKER,
                THOR,
                THOR_SINGLE_TARGET,
                VIKING_ASSUALT,
                VIKING_FIGHTER,
                VOID_RAY,
                ORACLE,
                ADEPT,
                RAVAGER,
                LURKER_BURROWED,
                HELLBAT,
                LIBERATOR,
                LIBERATOR_AG,
                HYDRALISK,
                TEMPEST,
                CYCLONE,
                WIDOW_MINE,
                WIDOW_MINE_BURROWED,
                DISRUPTOR
        };

        public static HashSet<uint> ResourceCenters = new HashSet<uint>
            {
                COMMAND_CENTER,
                COMMAND_CENTER_FLYING,
                HATCHERY,
                LAIR,
                HIVE,
                NEXUS,
                ORBITAL_COMMAND,
                ORBITAL_COMMAND_FLYING,
                PLANETARY_FORTRESS
        };
        public static HashSet<uint> MineralFields = new HashSet<uint>
            {
                RICH_MINERAL_FIELD,
                RICH_MINERAL_FIELD_750,
                MINERAL_FIELD,
                MINERAL_FIELD_750,
                LAB_MINERAL_FIELD,
                LAB_MINERAL_FIELD_750,
                PURIFIER_RICH_MINERAL_FIELD,
                PURIFIER_RICH_MINERAL_FIELD_750,
                PURIFIER_MINERAL_FIELD,
                PURIFIER_MINERAL_FIELD_750,
                BATTLE_STATION_MINERAL_FIELD,
                BATTLE_STATION_MINERAL_FIELD_750
        };
        public static HashSet<uint> GasGeysers = new HashSet<uint>
            {
                VESPENE_GEYSER,
                SPACE_PLATFORM_GEYSER,
                RICH_VESPENE_GEYSER,
                PROTOSS_VESPENE_GEYSER,
                PURIFIER_VESPENE_GEYSER,
                SHAKURAS_VESPENE_GEYSER,
                EXTRACTOR,
                EXTRACTOR_RICH,
                ASSIMILATOR,
                ASSIMILATOR_RICH,
                REFINERY,
                REFINERY_RICH

        };
        public static HashSet<uint> WorkerTypes = new HashSet<uint>
            {
                SCV,
                PROBE,
                DRONE
        };
        public static HashSet<uint> ChangelingTypes = new HashSet<uint>
            {
                CHANGELING,
                CHANGELING_MARINE,
                CHANGELING_MARINE_SHIELD,
                CHANGELING_ZEALOT,
                CHANGELING_ZERGLING,
                CHANGELING_ZERGLING_WINGS
        };
        public static HashSet<uint> DefensiveBuildingsTypes = new HashSet<uint>
            {
                MISSILE_TURRET,
                BUNKER,
                PLANETARY_FORTRESS,
                PHOTON_CANNON,
                SHIELD_BATTERY,
                SPORE_CRAWLER,
                SPORE_CRAWLER_UPROOTED,
                SPINE_CRAWLER,
                SPINE_CRAWLER_UPROOTED
        };

        public static Dictionary<uint, List<uint>> EquivalentTypes = new Dictionary<uint, List<uint>>() {
            { LURKER_BURROWED, new List<uint>() { LURKER } },
            { GREATER_SPIRE, new List<uint>() { SPIRE }},
            { HIVE, new List<uint>() { LAIR, HATCHERY}},
            { LAIR, new List<uint>() { HATCHERY }},
            { SUPPLY_DEPOT_LOWERED, new List<uint>() { SUPPLY_DEPOT }},
            { ORBITAL_COMMAND, new List<uint>() { COMMAND_CENTER }},
            { PLANETARY_FORTRESS, new List<uint>() { COMMAND_CENTER }},
            { LIBERATOR_AG, new List<uint>() { LIBERATOR }},
            { SIEGE_TANK_SIEGED, new List<uint>() { SIEGE_TANK }},
            { WIDOW_MINE_BURROWED, new List<uint>() { WIDOW_MINE }},
            { THOR_SINGLE_TARGET, new List<uint>() { THOR }},
            { WARP_GATE, new List<uint>() { GATEWAY }},
            { WARP_PRISM_PHASING, new List<uint>() { WARP_PRISM }}
        };

        public static bool CanAttackGround(uint type)
        {
            if (type == LIBERATOR 
                || type == CARRIER
                || type == WIDOW_MINE
                || type == WIDOW_MINE_BURROWED
                || type == SWARM_HOST
                || type == SPINE_CRAWLER
                || type == BATTLECRUISER
                || type == INFESTOR
                || type == DISRUPTOR
                || type == ORACLE
                || type == PHOENIX
                || type == SENTRY
                || type == VOID_RAY)
                return true;
            foreach (Weapon weapon in LookUp[type].Weapons)
                if (weapon.Type == Weapon.Types.TargetType.Any
                    || (weapon.Type == Weapon.Types.TargetType.Ground))
                    return true;
            return false;
        }

        public static bool CanAttackAir(uint type)
        {
            if (type == CARRIER
                || type == WIDOW_MINE
                || type == WIDOW_MINE_BURROWED
                || type == CYCLONE
                || type == INFESTOR
                || type == BATTLECRUISER
                || type == VOID_RAY)
                return true;
            foreach (Weapon weapon in LookUp[type].Weapons)
                if (weapon.Type == Weapon.Types.TargetType.Any
                    || (weapon.Type == Weapon.Types.TargetType.Air))
                    return true;
            return false;
        }
    }
}
