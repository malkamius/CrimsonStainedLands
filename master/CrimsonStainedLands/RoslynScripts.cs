using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    /// <summary>
    /// Execute scripts from files, too slow for use
    /// </summary>
    public static class RoslynScripts
    {
        public class CharacterGlobal
        {
            public Character character { get; set; }
        }

        private static Dictionary<string, Script> scripts = new Dictionary<string, Script>();

        static PortableExecutableReference metadata = MetadataReference.CreateFromFile(typeof(Character).Assembly.Location);
        public static bool ExecuteCharacterBoolScript(Character ch, string script)
        {
            if (SkillSpell.UseRoslyn == false) return true;

            var start = DateTime.Now;
            try
            {
                if (!scripts.TryGetValue(script, out var compiled))
                {
                    compiled = CSharpScript.Create<bool>(string.Format("new System.Func<CrimsonStainedLands.Character, bool>({0})(character)", script), options: ScriptOptions.Default.WithReferences(metadata), typeof(CharacterGlobal));
                    scripts[script] = compiled;
                }
                //var script = CSharpScript.Create<bool>("new System.Func<CrimsonStainedLands.Character, bool>(ch => ch.HitPoints == ch.MaxHitPoints)(character)", options: ScriptOptions.Default.WithReferences(metadata), typeof(Globals));

                using (var result = compiled.RunAsync(new CharacterGlobal { character = ch }))
                {
                    return (bool)result.Result.ReturnValue;
                }
            }
            finally
            {
                //game.log("Roslyn took {0}.", DateTime.Now - start);
            }
            //using (var asynccall = CSharpScript.EvaluateAsync<bool>(String.Format("new System.Func<CrimsonStainedLands.Character, bool>({0})(character)", script), options: ScriptOptions.Default.WithReferences(metadata), new CharacterGlobal { character = new CrimsonStainedLands.Character() }, typeof(CharacterGlobal)))
            //{
            //    game.log("ExecuteCharacterBoolScript - {0}", asynccall.Result);
            //    return asynccall.Result;
            //}
        }

        public static void PrepareCharacterBoolScript(string script)
        {
            if(!scripts.TryGetValue(script, out var compiled))
            {
                compiled = CSharpScript.Create<bool>(string.Format("new System.Func<CrimsonStainedLands.Character, bool>({0})(character)", script), options: ScriptOptions.Default.WithReferences(metadata), typeof(CharacterGlobal));
                compiled.Compile();
                scripts[script] = compiled;
            }
            
        }
    }
}
