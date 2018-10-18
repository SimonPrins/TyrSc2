using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Util;

namespace Tyr.Agents
{
    public class BuildingType
    {
        public uint Type { get; private set; }
        public int Ability { get; private set; }
        public Point2D Size { get; private set; }
        public string Name { get; private set; }
        public int Minerals { get; private set; }
        public int Gas { get; private set; }

        public static Dictionary<uint, BuildingType> LookUp = createLookUp();
        private static Dictionary<uint, BuildingType> createLookUp()
        {
            Dictionary<uint, BuildingType> lookUp = new Dictionary<uint, BuildingType>();

            lookUp.Add(59, new BuildingType() { Type = 59, Ability = 880, Size = SC2Util.Point(5, 5), Name = "Nexus", Minerals = 400 });
            lookUp.Add(60, new BuildingType() { Type = 60, Ability = 881, Size = SC2Util.Point(2, 2), Name = "Pylon", Minerals = 100 });
            lookUp.Add(61, new BuildingType() { Type = 61, Ability = 882, Size = SC2Util.Point(3, 3), Name = "Assimilator", Minerals = 75 });
            lookUp.Add(62, new BuildingType() { Type = 62, Ability = 883, Size = SC2Util.Point(3, 3), Name = "Gateway", Minerals = 150 });
            lookUp.Add(63, new BuildingType() { Type = 63, Ability = 884, Size = SC2Util.Point(3, 3), Name = "Forge", Minerals = 150 });
            lookUp.Add(64, new BuildingType() { Type = 64, Ability = 885, Size = SC2Util.Point(3, 3), Name = "FleetBeacon", Minerals = 300, Gas = 200 });
            lookUp.Add(65, new BuildingType() { Type = 65, Ability = 886, Size = SC2Util.Point(3, 3), Name = "TwilightCounsel", Minerals = 150, Gas = 100 });
            lookUp.Add(66, new BuildingType() { Type = 66, Ability = 887, Size = SC2Util.Point(2, 2), Name = "PhotonCannon", Minerals = 150 });
            lookUp.Add(67, new BuildingType() { Type = 67, Ability = 889, Size = SC2Util.Point(3, 3), Name = "Stargate", Minerals = 150, Gas = 150 });
            lookUp.Add(68, new BuildingType() { Type = 68, Ability = 890, Size = SC2Util.Point(3, 3), Name = "TemplarArchives", Minerals = 150, Gas = 200 });
            lookUp.Add(69, new BuildingType() { Type = 69, Ability = 891, Size = SC2Util.Point(2, 2), Name = "DarkShrine", Minerals = 150, Gas = 150 });
            lookUp.Add(70, new BuildingType() { Type = 70, Ability = 892, Size = SC2Util.Point(3, 3), Name = "RoboticsBay", Minerals = 200, Gas = 200 });
            lookUp.Add(71, new BuildingType() { Type = 71, Ability = 893, Size = SC2Util.Point(3, 3), Name = "RoboticsFacility", Minerals = 200, Gas = 150 });
            lookUp.Add(72, new BuildingType() { Type = 72, Ability = 894, Size = SC2Util.Point(3, 3), Name = "CyberneticsCore", Minerals = 150 });
            lookUp.Add(86, new BuildingType() { Type = 86, Ability = 1152, Size = SC2Util.Point(5, 5), Name = "Hatchery", Minerals = 300 });
            lookUp.Add(87, new BuildingType() { Type = 87, Ability = 1694, Size = SC2Util.Point(1, 1), Name = "CreepTumor"});
            lookUp.Add(88, new BuildingType() { Type = 88, Ability = 1154, Size = SC2Util.Point(2, 2), Name = "Extractor", Minerals = 25 });
            lookUp.Add(89, new BuildingType() { Type = 89, Ability = 1155, Size = SC2Util.Point(3, 3), Name = "SpawningPool", Minerals = 200 });
            lookUp.Add(90, new BuildingType() { Type = 90, Ability = 1156, Size = SC2Util.Point(3, 3), Name = "EvolutionChamber", Minerals = 75 });
            lookUp.Add(91, new BuildingType() { Type = 91, Ability = 1157, Size = SC2Util.Point(3, 3), Name = "HydraliskDen", Minerals = 100, Gas = 100 });
            lookUp.Add(92, new BuildingType() { Type = 92, Ability = 1158, Size = SC2Util.Point(3, 3), Name = "Spire", Minerals = 200, Gas = 200 });
            lookUp.Add(94, new BuildingType() { Type = 94, Ability = 1160, Size = SC2Util.Point(3, 3), Name = "InfestationPit", Minerals = 100, Gas = 100 });
            lookUp.Add(97, new BuildingType() { Type = 97, Ability = 1165, Size = SC2Util.Point(3, 3), Name = "RoachWarren", Minerals = 150 });
            lookUp.Add(98, new BuildingType() { Type = 98, Ability = 1166, Size = SC2Util.Point(2, 2), Name = "SpineCrawler", Minerals = 100 });
            lookUp.Add(504, new BuildingType() { Type = 504, Ability = 1163, Size = SC2Util.Point(3, 3), Name = "LurkerDen", Minerals = 100, Gas = 150 });
            lookUp.Add(1910, new BuildingType() { Type = 1910, Ability = 895, Size = SC2Util.Point(2, 2), Name = "ShieldBattery", Minerals = 100 });
            lookUp.Add(639, new BuildingType() { Type = 639, Size = SC2Util.Point(6, 6), Name = "DestructibleRocks" });

            return lookUp;
        }
    }
}
