using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SC2API_CSharp;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Builds;
using Tyr.Builds.Protoss;
using Tyr.buildSelection;
using Tyr.BuildSelection;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Plugins;
using Tyr.Util;

namespace Tyr
{
    public class Bot : SC2API_CSharp.Bot
    {
        public GameConnection GameConnection;
        public ResponseData Data;
        public int NeutralPlayerId { get; } = 16;
        public Race MyRace;
        public Race EnemyRace;

        public ResponseGameInfo GameInfo;

        public ResponseObservation Observation;

        public uint PlayerId;
        public string OpponentID;

        List<Action> actions = new List<Action>();

        public BuildingPlacer buildingPlacer;
        public int Frame { get; private set; }
        public MapAnalyzer MapAnalyzer { get; internal set; } = new MapAnalyzer();
        public EnemyStrategyAnalyzer EnemyStrategyAnalyzer = new EnemyStrategyAnalyzer();

        public List<Plugin> Plugins = new List<Plugin>();

        public int ReservedMinerals;
        public int ReservedGas;

        public MicroController MicroController = new MicroController();

        public bool Surrendered = false;
        public int SurrenderedFrame = 0;

        // Managers
        public List<Manager> Managers = new List<Manager>();
        public UnitManager UnitManager = new UnitManager();
        public EnemyManager EnemyManager = new EnemyManager();
        public TargetManager TargetManager = new TargetManager();
        public TaskManager TaskManager = new TaskManager();
        public EffectManager EffectManager = new EffectManager();
        public EnemyMineManager EnemyMineManager = new EnemyMineManager();
        public EnemyTankManager EnemyTankManager = new EnemyTankManager();
        public EnemyCycloneManager EnemyCycloneManager = new EnemyCycloneManager();
        public EnemyBansheesManager EnemyBansheesManager = new EnemyBansheesManager();

        public BaseManager BaseManager = new BaseManager();
        public NexusAbilityManager NexusAbilityManager = new NexusAbilityManager();
        public OrbitalAbilityManager OrbitalAbilityManager = new OrbitalAbilityManager();

        public MapEnum Map;

        public static Bot Main { get; internal set; }

        public Build Build;

        public bool Monday { get; set; }
        private bool Day9Sent = false;
        private List<Unit> EnemyBases = new List<Unit>();

        private bool loggedError = false;

        private long totalExecutionTime;
        private long maxExecutionTime;

        public Build FixedBuild;

        private Request DrawRequest;

        public static bool Debug = false;

        private int TextLine = 0;

        private long PrevTime = -1;

        List<long> Times = new List<long>();

        public BuildsProvider BuildsProvider = new ProbotsBuildsProvider();
        public BuildSelector BuildSelector = new RotateSelector();

        public int VersionNumber = 1;

        public string GameVersion;
        public bool AllowChat = true;
        public bool AllowGG = true;
        public bool ProbotsChatMessages = false;

        public bool OldMapData;
        private int SendTempestText = -1;
        public bool ArchonMode = false;

        public Bot()
        {
            buildingPlacer = new BuildingPlacer(this);
            Main = this;
        }

        public IEnumerable<Action> onFrame(ResponseObservation observation)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            actions = new List<Action>();
            DrawRequest = null;
            TextLine = 0;
            


            long time1 = 0;
            long time2 = 0;
            long time3 = 0;
            try
            {
                if (observation.Observation == null)
                    return actions;

                if (ProbotsChatMessages)
                {

                    if (Observation.Chat != null && SendTempestText == -1)
                    {
                        foreach (ChatReceived chat in Observation.Chat)
                        {
                            if (chat.PlayerId == PlayerId)
                                continue;
                            if (chat.Message.ToLower().Contains("op strat detected"))
                                SendTempestText = Frame + 67;
                        }
                    }
                    if (Frame == (int)(22.4 * 30))
                    {
                        if (EnemyRace == Race.Terran
                            && !StrategyAnalysis.Cyclone.Get().DetectedPreviously
                            && !StrategyAnalysis.Banshee.Get().DetectedPreviously
                            && !StrategyAnalysis.Marauder.Get().DetectedPreviously)
                        {
                            Chat("Tempests are not OP. They are perfectly balanced, as all things should be.");
                        }
                        else
                        {
                            List<string> messages = new List<string>() {
                        "Fun isn't something one considers when balancing the universe.",
                        "I am inevitable.",
                        "The hardeset choices require the strongest wills."};
                            int message = new System.Random().Next(messages.Count);
                            Chat(messages[message]);
                        }
                    }
                }
                else if (Frame == 1)
                    Chat(Monday ? "Happy monday! :D" : "Good luck, have fun! :D");


                Observation = observation;

                ReservedMinerals = 0;
                ReservedGas = 0;

                foreach (Plugin plugin in Plugins)
                    plugin.OnFrame();

                EnemyStrategyAnalyzer.OnFrame(this);

                time1 = stopWatch.ElapsedMilliseconds;

                foreach (Manager manager in Managers)
                    manager.OnFrame(this);

                time2 = stopWatch.ElapsedMilliseconds;

                Build.OnFrameBase(this);

                time3 = stopWatch.ElapsedMilliseconds;

                UnitManager.AddActions(actions);

                if (!Surrendered)
                {
                    Surrendered = CheckSurrender();
                    if (Surrendered)
                    {
                        Chat("gg");
                        SurrenderedFrame = Frame;
                        FileUtil.Register("result " + EnemyRace + " " + Build.Name() + " Defeat");
                    }
                }

                if (Surrendered && Frame - SurrenderedFrame >= 118)
                    GameConnection.RequestLeaveGame().Wait();

                TrySenDay9();

                DrawText("Minerals: " + Minerals());
                DrawText("Gas: " + Gas());
            }
            catch (System.Exception e)
            {
                if (!loggedError)
                {
                    FileUtil.Log("Error occured: " + e.ToString());
                    loggedError = true;
                }
                DebugUtil.WriteLine("Exception in OnFrame: " + e.ToString());
            }

            Frame++;

            stopWatch.Stop();
            totalExecutionTime += stopWatch.ElapsedMilliseconds;
            maxExecutionTime = System.Math.Max(maxExecutionTime, stopWatch.ElapsedMilliseconds);
            
            DrawText("Average ms per frame: " + totalExecutionTime / Frame + " Max ms per frame: " + maxExecutionTime);
            DrawText("Managers time: " + (time2 - time1) + " Build order time: " + (time3 - time2));

            if (DrawRequest != null)
                GameConnection.SendRequest(DrawRequest).Wait();

            return actions;
        }

        public void DrawText(string text)
        {
            DrawScreen(text, 12, 0.05f, 0.1f + 0.02f * TextLine);
            TextLine++;
        }

        public void DrawScreen(string text, uint size, float x, float y)
        {
            if (Debug)
            {
                InitializeDebugCommand();
                DrawRequest.Debug.Debug[0].Draw.Text.Add(new DebugText() { Text = text, Size = size, VirtualPos = new Point() { X = x, Y = y } });
            }
        }

        public void DrawLine(Agent p1, Point p2)
        {
            DrawLine(p1.Unit.Pos, p2);
        }

        public void DrawLine(Agent p1, Point2D p2)
        {
            Point pos3 = new Point { X = p2.X, Y = p2.Y, Z = MapAnalyzer.MapHeight((int)p2.X, (int)p2.Y) };
            DrawLine(p1.Unit.Pos, pos3);
        }

        public void DrawLine(Point p1, Point p2)
        {
            if (Debug)
            {
                InitializeDebugCommand();
                DrawRequest.Debug.Debug[0].Draw.Lines.Add(new DebugLine() { Color = new Color() { R = 255, G = 0, B = 0 }, Line = new Line() { P0 = p1, P1 = p2 } });
            }
        }

        public void DrawSphere(Point2D pos)
        {
            if (Debug)
            {
                Point pos3 = new Point { X = pos.X, Y = pos.Y, Z = MapAnalyzer.MapHeight((int)pos.X, (int)pos.Y) };
                DrawSphere(pos3);
            }
        }

        public void DrawSphere(Point pos)
        {
            if (Debug)
            {
                InitializeDebugCommand();
                DrawRequest.Debug.Debug[0].Draw.Spheres.Add(new DebugSphere() { Color = new Color() { R = 255, G = 0, B = 0 }, R = 2, P = pos });
            }
        }

        public void DrawSphere(Point pos, float radius, Color color)
        {
            if (Debug)
            {
                InitializeDebugCommand();
                DrawRequest.Debug.Debug[0].Draw.Spheres.Add(new DebugSphere() { Color = color, R = radius, P = pos });
            }
        }

        private void InitializeDebugCommand()
        {
            if (DrawRequest == null)
            {
                DrawRequest = new Request();

                DrawRequest.Debug = new RequestDebug();
                DebugCommand debugCommand = new DebugCommand();
                debugCommand.Draw = new DebugDraw();
                DrawRequest.Debug.Debug.Add(debugCommand);
            }
        }

        private void TrySenDay9()
        {
            if (!Monday || Day9Sent)
                return;


            foreach (Unit unit in EnemyBases)
            {
                bool removed = false;
                if (Observation.Observation.RawData.Event != null
                    && Observation.Observation.RawData.Event.DeadUnits != null)
                    foreach (ulong tag in Observation.Observation.RawData.Event.DeadUnits)
                        if (unit.Tag == tag)
                            removed = true;

                if (!removed)
                    continue;

                Chat("Day[9] made me do it!");
                Day9Sent = true;
            }

            EnemyBases = new List<Unit>();
            foreach (Unit unit in Observation.Observation.RawData.Units)
                if (unit.Alliance == Alliance.Enemy && UnitTypes.ResourceCenters.Contains(unit.UnitType))
                    EnemyBases.Add(unit);
        }

        private bool CheckSurrender()
        {
            if (!AllowGG)
                return false;
            int buildings = 0;
            int health = 0;
            int shield = 0;
            foreach (Agent agent in UnitManager.Agents.Values)
            {
                if (agent.IsBuilding)
                {
                    buildings++;
                    health = (int)System.Math.Max(health, agent.Unit.Health);
                    shield = (int)System.Math.Max(shield, agent.Unit.Shield);
                }
            }
            if (buildings <= 1
                && shield == 0
                && health <= 150)
                return true;

            int bases = UnitManager.Count(UnitTypes.NEXUS) + UnitManager.Count(UnitTypes.COMMAND_CENTER) + UnitManager.Count(UnitTypes.COMMAND_CENTER_FLYING) + UnitManager.Count(UnitTypes.ORBITAL_COMMAND) + UnitManager.Count(UnitTypes.ORBITAL_COMMAND_FLYING) + UnitManager.Count(UnitTypes.PLANETARY_FORTRESS) + UnitManager.Count(UnitTypes.HATCHERY) + UnitManager.Count(UnitTypes.LAIR) + UnitManager.Count(UnitTypes.HIVE);
            int workers = UnitManager.Count(UnitTypes.SCV) + UnitManager.Count(UnitTypes.PROBE) + UnitManager.Count(UnitTypes.DRONE);
            int minerals = (int)Observation.Observation.PlayerCommon.Minerals - ReservedMinerals;
            if (bases > 0 && workers > 0)
                return false;

            if (workers > 0 && minerals >= 400)
                return false;

            if (bases > 0 && minerals >= 50)
                return false;

            foreach (Agent agent in UnitManager.Agents.Values)
                if (agent.IsCombatUnit)
                    return false;

            return true;
        }

        public void Chat(string message)
        {
            Action action = new Action();
            action.ActionChat = new ActionChat() { Message = message };
            if (AllowChat)
                actions.Add(action);
        }

        private void printActions()
        {
            if (Observation.Actions.Count > 0 && Observation.Actions[0].ActionRaw.UnitCommand != null && Observation.Actions[0].ActionRaw.UnitCommand.UnitTags.Count > 0)
            {
                try
                {
                    DebugUtil.WriteLine("New action performed:" + Observation.Actions[0].ActionRaw.UnitCommand);
                    DebugUtil.WriteLine("Ability ID: " + Observation.Actions[0].ActionRaw.UnitCommand.AbilityId);
                    if (Observation.Actions[0].ActionRaw.UnitCommand.TargetWorldSpacePos != null)
                    {
                        Point2D pos = Observation.Actions[0].ActionRaw.UnitCommand.TargetWorldSpacePos;
                        DebugUtil.WriteLine("Position: " + pos);
                    }
                    else if (Observation.Actions[0].ActionRaw.UnitCommand.TargetUnitTag > 0)
                    {
                        DebugUtil.WriteLine("TargetUnit: " + Observation.Actions[0].ActionRaw.UnitCommand.TargetUnitTag);
                        foreach (Unit unit in Observation.Observation.RawData.Units)
                            if (unit.Tag == Observation.Actions[0].ActionRaw.UnitCommand.TargetUnitTag)
                            {
                                DebugUtil.WriteLine("TargetUnitType: " + unit.UnitType);
                                break;
                            }
                    }
                    foreach (Unit unit in Observation.Observation.RawData.Units)
                        if (unit.Tag == Observation.Actions[0].ActionRaw.UnitCommand.UnitTags[0])
                            DebugUtil.WriteLine("By unit of type: " + unit.UnitType);
                    DebugUtil.WriteLine();
                }
                catch (System.Exception) { }
            }
        }

        public void OnInitialize()
        {
            foreach (System.Type pluginType in typeof(Plugin).Assembly.GetTypes().Where(type => typeof(Plugin).IsAssignableFrom(type)))
            {
                if (pluginType.IsAbstract)
                    continue;
                Plugin plugin = (Plugin)pluginType.GetConstructor(new System.Type[0]).Invoke(new object[0]);
                Plugins.Add(plugin);
            }
            foreach (Plugin plugin in Plugins)
                plugin.OnInitialize();
        }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentID)
        {
            ArchonMode = Settings.ArchonMode();
            
            Observation = observation;
            GameInfo = gameInfo;


            foreach (MapEnum map in System.Enum.GetValues(typeof(MapEnum)))
            {
                if (MapNameMatches(map))
                {
                    System.Console.WriteLine("Setting map name to: " + map.ToString());
                    GameInfo.MapName = map.ToString();
                    Map = map;
                    break;
                }
            }

            PlayerId = playerId;
            Data = data;

            if (!Settings.MapAllowed(GameInfo.MapName))
            {
                DebugUtil.WriteLine("Tyr does not support the map " + GameInfo.MapName + ". If Tyr should accept this map as well, you can add the line 'map " + GameInfo.MapName + "' to the settings.txt file.");
                FileUtil.Log("Tyr does not support the map " + GameInfo.MapName + ". If Tyr should accept this map as well, you can add the line 'map " + GameInfo.MapName + "' to the settings.txt file.");
                System.Console.ReadLine();
                throw new System.Exception("Tyr does not support the map " + GameInfo.MapName + ". If Tyr should accept this map as well, you can add the line 'map " + GameInfo.MapName + "' to the settings.txt file.");
            }

            GameVersion = pingResponse.GameVersion;
            OldMapData = SC2Util.IsVersionBefore("4.9.3");
            DebugUtil.WriteLine("Game version: " + pingResponse.GameVersion);
            UnitTypes.LoadData(data);

            OpponentID = opponentID;

            MyRace = GameInfo.PlayerInfo[(int)Observation.Observation.PlayerCommon.PlayerId - 1].RaceActual;
            EnemyRace = GameInfo.PlayerInfo[2 - (int)Observation.Observation.PlayerCommon.PlayerId].RaceActual;
            DebugUtil.WriteLine("MyRace: " + MyRace);
            DebugUtil.WriteLine("EnemyRace: " + EnemyRace);
            DebugUtil.WriteLine("Game started on map: " + GameInfo.MapName);

            FileUtil.Log("Game started on map: " + GameInfo.MapName);
            FileUtil.Log("Enemy race: " + EnemyRace);

            MapAnalyzer.Analyze(this);
            TargetManager.OnStart(this);
            BaseManager.OnStart(this);

            foreach (Plugin plugin in Plugins)
                plugin.OnStart();

            Build = DetermineBuild();
            Build.InitializeTasks();
            Build.OnStart(this);

            FileUtil.Register("started " + EnemyRace + " " + Build.Name());

            Managers.Add(UnitManager);
            Managers.Add(EnemyManager);
            Managers.Add(BaseManager);
            Managers.Add(TargetManager);
            Managers.Add(TaskManager);
            Managers.Add(EffectManager);
            Managers.Add(EnemyMineManager);
            Managers.Add(EnemyTankManager);
            Managers.Add(EnemyCycloneManager);
            Managers.Add(EnemyBansheesManager);

            if (GameInfo.PlayerInfo[(int)Observation.Observation.PlayerCommon.PlayerId - 1].RaceActual == Race.Protoss)
                Managers.Add(NexusAbilityManager);

            if (GameInfo.PlayerInfo[(int)Observation.Observation.PlayerCommon.PlayerId - 1].RaceActual == Race.Terran)
                Managers.Add(OrbitalAbilityManager);
        }

        private bool MapNameMatches(MapEnum map)
        {
            string enumName = map.ToString();
            List<string> mapWords = new List<string>();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < enumName.Length; i++)
            {
                if (enumName[i] >= 'A' && enumName[i] <= 'Z')
                {
                    if (sb.Length > 0)
                        mapWords.Add(sb.ToString());
                    sb = new System.Text.StringBuilder();
                    sb.Append((char)(enumName[i] - 'A' + 'a'));
                }
                else
                    sb.Append(enumName[i]);
            }
            if (sb.Length > 0)
                mapWords.Add(sb.ToString());

            string lowercaseMapName = GameInfo.MapName.ToLower();

            foreach (string mapWord in mapWords)
                if (!lowercaseMapName.Contains(mapWord))
                    return false;
            return true;
        }

        private Build DetermineBuild()
        {
            if (FixedBuild != null)
            {
                DebugUtil.WriteLine("Picking fixed build: " + FixedBuild.Name());
                return FixedBuild;
            }


            string[] lines = FileUtil.ReadResultsFile();
            EnemyStrategyAnalyzer.Load(lines);

            List<Build> options = BuildsProvider.GetBuilds(this, lines);
            return BuildSelector.Select(options, lines);
        }

        public List<Unit> Enemies()
        {
            return EnemyManager.GetEnemies();
        }

        public List<Unit> CloakedEnemies()
        {
            return EnemyManager.GetCloakedEnemies();
        }

        public Dictionary<ulong, Agent>.ValueCollection Units()
        {
            return UnitManager.Agents.Values;
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
            FileUtil.Register("Result: " + result);
            if (Frame > 0)
                FileUtil.Register("Average ms per frame: " + totalExecutionTime / Frame + " Max ms per frame: " + maxExecutionTime);
        }


        public int Minerals()
        {
            return (int)Observation.Observation.PlayerCommon.Minerals - ReservedMinerals;
        }

        public int Gas()
        {
            return (int)Observation.Observation.PlayerCommon.Vespene - ReservedGas;
        }

        private void Time()
        {
            long currentTime = System.DateTime.Now.Ticks;
            if (PrevTime != -1 && Frame <= 224)
                Times.Add(currentTime - PrevTime);
            PrevTime = currentTime;
            if (Frame == 224 + 22)
                foreach (long time in Times)
                    FileUtil.Debug(time + "");
        }
    }
}
