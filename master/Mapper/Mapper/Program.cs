using static CrimsonStainedLands.Command;
using System.Reflection;
using CrimsonStainedLands;
using CrimsonStainedLands.World;

namespace CLSMapper
{
    public static class Program
    {
        public delegate void SpellFun(Magic.CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType targetIsType);

        public class Globals
        {
            public Character character { get; set; }
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //var metadata = MetadataReference.CreateFromFile(typeof(Character).Assembly.Location);
            //var script = CSharpScript.Create<bool>("new System.Func<CrimsonStainedLands.Character, bool>(ch => ch.HitPoints == ch.MaxHitPoints)(character)", options: ScriptOptions.Default.WithReferences(metadata), typeof(Globals));
            //var result = script.RunAsync(new Globals { character = new CrimsonStainedLands.Character() }).Result.ReturnValue;

            //var result = CSharpScript.EvaluateAsync<bool>("new System.Func<CrimsonStainedLands.Character, bool>(ch => ch.HitPoints < ch.MaxHitPoints)(character)", 
            //    options: ScriptOptions.Default.WithReferences(metadata),
            //    new Globals { character = new CrimsonStainedLands.Character() }, typeof(Globals)).Result;
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainWindow());
        }
    }
}