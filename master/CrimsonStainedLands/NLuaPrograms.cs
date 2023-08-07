using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static CrimsonStainedLands.Programs;

namespace CrimsonStainedLands
{
    public class NLuaPrograms
    {
        private static string NLuaProgramsPath = @"data\areas";

        public static Dictionary<string, NLuaProgram> Programs = new Dictionary<string, NLuaProgram>();

        internal static NLua.Lua Lua = new NLua.Lua();

        public static void LoadPrograms(AreaData area)
        {
            string AreaProgramsPath = System.IO.Path.GetDirectoryName(area.fileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(area.fileName) + "_Programs.xml";
            if (System.IO.File.Exists(AreaProgramsPath))
            {
                var elements = XElement.Load(AreaProgramsPath);
                foreach (var element in elements.Elements("LuaProgram"))
                {
                    var newProgram = new NLuaProgram()
                    {
                        Name = element.GetAttributeValue("Name"),
                        Description = element.GetAttributeValue("Description"),
                        Path = AreaProgramsPath,
                        Area = area
                    };
                    Utility.GetEnumValues(element.GetAttributeValue("ProgramTypes"), ref newProgram.ProgramTypes);

                    if (!newProgram.Name.ISEMPTY())
                    {
                        Programs[area.name + "_" + newProgram.Name] = newProgram;
                    }
                }
            }
        }

        public static bool ProgramLookup(string name, out NLuaProgram program)
        {
            program = null;
            if (name.ISEMPTY()) return false;
            foreach (var programlookup in Programs.Values)
            {
                if (programlookup.Name.StringCmp(name))
                {
                    program = programlookup;
                    return true;
                }
            }
            game.log("LuaProgram {0} not found.", name);
            return false;
        }

        public class NLuaProgram
        {

            public AreaData Area { get; set; }
            /// <summary>
            /// The name of the program referenced by area files
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// A description of the program
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// The path containing the LUA code
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// Code being modified, not yet saved to xml
            /// </summary>
            public string CodeInProgress { get; set; }

            /// <summary>
            /// Types of triggers for this program
            /// </summary>
            public List<Programs.ProgramTypes> ProgramTypes = new List<Programs.ProgramTypes>();

            public string GetCodeFromFile()
            {
                if (System.IO.File.Exists(Path))
                {
                    var elements = XElement.Load(Path);
                    var element = elements.Elements().FirstOrDefault(e => e.Name == "LuaProgram" && e.GetAttributeValue("Name").StringCmp(Name));

                    if (element != null)
                    {
                        return element.Value;
                    }
                }
                return null;
            }

            static bool Executing = false;
            public bool Execute(Character player, Character npc, RoomData room, ItemData item, SkillSpell skill, AffectData affect, Programs.ProgramTypes type, string arguments)
            {
                if (Executing) return false;
                try
                {
                    Executing = true;
                    var code = GetCodeFromFile();

                    if (!code.ISEMPTY())
                    {
                        Lua["Player"] = player;
                        Lua["NPC"] = npc;
                        Lua["Room"] = room;
                        Lua["Item"] = item;
                        Lua["Skill"] = skill;
                        Lua["Affect"] = affect;
                        Lua["ProgramType"] = type.ToString();
                        Lua["Arguments"] = arguments;
                        /// an instance of a wrapper class to call quest functions from lua
                        Lua["QuestHelper"] = new QuestHelperClass();

                        Lua["WorldHelper"] = new WorldHelperClass();
                        Lua["StringHelper"] = new StringHelperClass();


                        try
                        {
                            var results = Lua.DoString(code);

                            if (results.Length > 0 && results[0] is bool)
                                return (bool)results[0];
                        }
                        catch (Exception ex)
                        {
                            game.log("Error executing lua script: {0}", ex.ToString());
                        }
                    }

                    return false;
                }
                finally
                {
                    Executing = false;
                }
            }
        } // end NLuaProgram

        public class QuestHelperClass
        {
            public bool IsQuestComplete(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    return QuestProgressData.IsQuestComplete(ch, quest);
                return false;
            }

            public bool IsQuestAvailable(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    return QuestProgressData.IsQuestAvailable(ch, quest);
                return false;
            }

            public bool IsQuestInProgress(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    return QuestProgressData.IsQuestInProgress(ch, quest);
                return false;
            }

            public bool IsQuestFailed(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    return QuestProgressData.IsQuestFailed(ch, quest);
                return false;
            }

            public bool HasQuestPrerequisites(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    return QuestProgressData.HasQuestPrerequisites(ch, quest);
                return false;
            }

            public void StartQuest(Character ch, string giver, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    QuestProgressData.StartQuest(ch, giver, quest);
            }

            public void DropQuest(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    QuestProgressData.DropQuest(ch, quest);
            }

            public void FailQuest(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    QuestProgressData.FailQuest(ch, quest);
            }

            public void CompleteQuest(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    QuestProgressData.CompleteQuest(ch, quest);
            }

            public void DisableQuest(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    QuestProgressData.DisableQuest(ch, quest);
            }

            public void ResetQuest(Character ch, int vnum)
            {
                var quest = Quest.GetQuest(vnum);

                if (quest != null && ch != null && ch is Player)
                    QuestProgressData.ResetQuest(ch, quest);
            }

            public string GetQuestInformation(Character ch, int vnum, string property)
            {
                var progress = Quest.GetQuestProgress(ch, vnum);
                if (progress != null)
                    return progress.ExtraState.GetAttributeValue(property);
                else return "";
            }

            public void SetQuestInformation(Character ch, int vnum, string property, string value)
            {
                var progress = Quest.GetQuestProgress(ch, vnum);
                if (progress != null)
                    progress.ExtraState.SetAttributeValue(property, value);
            }

            public void SetQuestInformation(Character ch, int vnum, string property, int value)
            {
                var progress = Quest.GetQuestProgress(ch, vnum);
                if (progress != null)
                    progress.ExtraState.SetAttributeValue(property, value);
            }

            public int GetQuestInformationInt(Character ch, int vnum, string property)
            {
                var progress = Quest.GetQuestProgress(ch, vnum);
                if (progress != null)
                    return progress.ExtraState.GetAttributeValueInt(property);
                else return 0;
            }
        }

        public class WorldHelperClass
        {
            public void DoSay(Character ch, string arguments)
            {
                DoActCommunication.DoSay(ch, arguments);
            }

            public void DoGive(Character ch, string arguments)
            {
                Character.DoGive(ch, arguments);
            }

            public void Log(string text)
            {
                game.log(text);
            }
        }

        public class StringHelperClass
        {
            public bool StringPrefix(string full, string partial)
            {
                return full.StringPrefix(partial);
            }
        }
    } // end NLuaPrograms
} // end namespace
