using System.Reflection;
using System.Xml.Linq;
using Mysqlx.Expr;

namespace CrimsonStainedLands
{
    public class Module
    {
        public string Name { get; set; } = "Unknown";
        public string Description { get; set; } = "None";
        public string Path { get; }

        public Assembly ModuleAssembly { get; set; } = null;

        public Module(string path, Assembly assembly)
        {
            Path = path;
            ModuleAssembly = assembly;
        }

        public static List<Module> Modules { get; } = new List<Module>();

        public static event Action OnDataLoadedEvent;

        public static event Action PulseBeforeEvent;
        public static event Action PulseAfterEvent;
        
        public static void PulseBefore()
        {
            PulseBeforeEvent?.Invoke();
        }

        public static void PulseAfter()
        {
            PulseAfterEvent?.Invoke();
        }

        public static void DataLoaded()
        {
            OnDataLoadedEvent?.Invoke();
        }

        public static class Combat
        {
            public static event Func<CrimsonStainedLands.Character, CrimsonStainedLands.Character, bool?> IsSafeCheckEvent;

            /// <summary>
            /// Return null if typical isSafe rules should apply. Return true if the defender is safe from the attacker. Return false if the defender is not safe from the attacker.
            /// </summary>
            /// <param name="attacker"></param>
            /// <param name="defender"></param>
            /// <returns></returns>
            public static bool? IsSafe(CrimsonStainedLands.Character attacker, CrimsonStainedLands.Character defender)
            {
                if (IsSafeCheckEvent != null)
                {
                    foreach (Func<CrimsonStainedLands.Character, CrimsonStainedLands.Character, bool?> handler in IsSafeCheckEvent.GetInvocationList())
                    {
                        var result = handler(attacker, defender);
                        if (result.HasValue)
                        {
                            return result.Value;
                        }
                    }
                }
                return null;
            }
        }

        public static class Character
        {
            public delegate void EnterRoomHandler(CrimsonStainedLands.Character character, CrimsonStainedLands.World.RoomData oldRoom, CrimsonStainedLands.World.RoomData newRoom);

            /// <summary>
            /// Important to note that this is not called on new characters right now...
            /// </summary>
            public static event Action<CrimsonStainedLands.Character, XElement> LoadingEvent;
            public static event Action<CrimsonStainedLands.Character, XElement> SerializingEvent;
            public static event EnterRoomHandler OnEnterRoomEvent;
            
            public static void OnLoading(CrimsonStainedLands.Character character, XElement rootElement)
            {
                LoadingEvent?.Invoke(character, rootElement);
            }

            public static void OnSerializing(CrimsonStainedLands.Character character, XElement rootElement)
            {
                SerializingEvent?.Invoke(character, rootElement);
            }

            public static void OnEnterRoom(CrimsonStainedLands.Character character, CrimsonStainedLands.World.RoomData oldRoom, CrimsonStainedLands.World.RoomData newRoom)
            {
                OnEnterRoomEvent?.Invoke(character, oldRoom, newRoom);
            }
        }

        public static void LoadModules()
        {
            if (System.IO.File.Exists("modules.xml"))
            {
                XElement root = XElement.Load("modules.xml");
                foreach (XElement moduleElement in root.Elements("module"))
                {
                    string path = moduleElement.Element("path")?.Value;
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(path);

                        var moduleTypes = assembly.GetTypes().Where(t => typeof(Module).IsAssignableFrom(t) && t != typeof(Module));
                        foreach (var moduleType in moduleTypes)
                        {
                            var ctor = moduleType.GetConstructor(new[] { typeof(string), typeof(Assembly) });
                            if (ctor != null)
                            {
                                var moduleInstance = ctor.Invoke(new object[] { path, assembly }) as Module;
                                if (moduleInstance != null)
                                {
                                    Modules.Add(moduleInstance);
                                    Game.log($"Loaded module: {moduleInstance.Name} from {path}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Game.bug($"Failed to load module(s) from {path}: {ex}");
                        System.Environment.Exit(1);
                    }
                }
            }
        }
    }
}