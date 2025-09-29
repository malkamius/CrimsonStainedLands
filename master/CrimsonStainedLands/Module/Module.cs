using System.Reflection;
using System.Xml.Linq;

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

        // public virtual void Initialize()
        // {
        //     // Base implementation does nothing
        // }

        public static event Action OnPulse;
        public static event Action OnCombat;

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
                                if(moduleInstance != null)
                                {
                                    Modules.Add(moduleInstance);
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