using CrimsonStainedLands.Extensions;
using CrimsonStainedLands.World;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CrimsonStainedLands.Programs;

namespace CrimsonStainedLands
{
    public class Programs
    {
        public static List<Program<ItemData>> ItemPrograms = new List<Program<ItemData>>();
        public static List<Program<NPCData>> NPCPrograms = new List<Program<NPCData>>();
        public static List<Program<AffectData>> AffectPrograms = new List<Program<AffectData>>();
        public static List<Program<RoomData>> RoomPrograms = new List<Program<RoomData>>();

        public enum ProgramTypes
        {
            None = 0,
            Say = 1,
            Open,
            Close,
            RoundCombat,
            OneHitMiss,
            OneHitHit,
            OneHitAny,
            EnterRoom,
            ExitRoom,
            PlayerDeath,
            SenderDeath,
            Use,
            Invoke,
            Give,
            Receive,
            Wear,
            AffectTick,
            AffectEnd,
            BeforeUnlock,
            BeforeRelock,
            Pulse,
            PulseViolence
        }

        static Programs()
        {
            ItemPrograms.Add(new SayProgramEverfullSkin());
            ItemPrograms.Add(new OpenProgramPouchOfNourishment());
            ItemPrograms.Add(new OneHitHitCureSerious());
            ItemPrograms.Add(new OneHitHitBlackHammer());
            ItemPrograms.Add(new OneHitHitLightningEmbossedRing());
            ItemPrograms.Add(new OneHitHitRangerWeapon());
            ItemPrograms.Add(new UseNewbieLeverProg());
            ItemPrograms.Add(new WearNewbieGearProg());


            NPCPrograms.Add(new QuestSayHelloProgram());
            NPCPrograms.Add(new AstoriaGuardBeforeUnlock());

            //NPCPrograms.Add(new ForemanRespondProgram());
            //NPCPrograms.Add(new DocksBountyQuestProgram());

            AffectPrograms.Add(new AffectDuelStartingProgram());
            AffectPrograms.Add(new AffectDuelStartProgram());
            AffectPrograms.Add(new AffectDuelTickProgram());

            AffectPrograms.Add(new AffectFirstAidTick());
            AffectPrograms.Add(new AffectFirstAidEnd());
        }

        public static bool AffectProgramLookup(string name, out Program<AffectData> program)
        {
            program = null;
            if (name.ISEMPTY()) return false;
            foreach (var programlookup in AffectPrograms)
            {
                if (programlookup.Name.StringCmp(name))
                {
                    program = programlookup;
                    return true;
                }
            }
            Game.log("AffectProgram {0} not found.", name);
            return false;
        }

        public static bool ItemProgramLookup(string name, out Program<ItemData> program)
        {
            program = null;
            if (name.ISEMPTY()) return false;
            foreach (var programlookup in ItemPrograms)
            {
                if (programlookup.Name.StringCmp(name))
                {
                    program = programlookup;
                    return true;
                }
            }
            Game.log("ItemProgram {0} not found.", name);
            return false;
        }

        public static bool NPCProgramLookup(string name, out Program<NPCData> program)
        {
            program = null;
            if (name.ISEMPTY()) return false;
            foreach (var programlookup in NPCPrograms)
            {
                if (programlookup.Name.StringCmp(name))
                {
                    program = programlookup;
                    return true;
                }
            }

            return false;
        }

        public static bool RoomProgramLookup(string name, out Program<RoomData> program)
        {
            program = null;
            if (name.ISEMPTY()) return false;
            foreach (var programlookup in RoomPrograms)
            {
                if (programlookup.Name.StringCmp(name))
                {
                    program = programlookup;
                    return true;
                }
            }
            Game.log("RoomProgram {0} not found.", name);
            return false;
        }

        public static bool ExecutePrograms(ProgramTypes type, Character player, ItemData item, string arguments)
        {
            bool success = false;
            foreach (var prog in item.Programs)
            {
                if (prog.Types.ISSET(type))
                {
                    success = prog.Execute(player, item, null, item, null, type, "");
                    if (success) break;
                }
            }

            foreach (var program in item.LuaPrograms)
            {
                if (program.ProgramTypes.ISSET(type))
                {
                    success = program.Execute(player, null, null, item, null, null, type, arguments);
                    if (success) break;
                }
            }
            return success;
        }

        public static bool ExecutePrograms(ProgramTypes type, Character player, Character npc, ItemData item, RoomData room, string arguments)
        {
            bool success = false;
            if (item != null)
            {
                foreach (var prog in item.Programs)
                {
                    if (prog.Types.ISSET(type))
                    {
                        success = prog.Execute(player, item, null, item, null, type, "");
                        if (success) break;
                    }
                }

                foreach (var program in item.LuaPrograms)
                {
                    if (program.ProgramTypes.ISSET(type))
                    {
                        success = program.Execute(player, null, room, item, null, null, type, arguments);
                        if (success) break;
                    }
                }
            }
            if (player is NPCData)
            {
                foreach (var program in ((NPCData)player).Programs)
                {
                    if (program.Types.ISSET(type))
                    {
                        success = program.Execute(player, (NPCData)npc, npc, item, null, type, arguments);
                    }
                    if (success) break;
                }
                if (!success)
                    foreach (var program in ((NPCData)player).LuaPrograms)
                    {
                        if (program.ProgramTypes.ISSET(type))
                        {
                            success = program.Execute(player, npc, null, item, null, null, type, arguments);
                        }
                        if (success) break;
                    }
            }
            else if (npc is NPCData)
            {
                foreach (var program in ((NPCData)npc).Programs)
                {
                    if (program.Types.ISSET(type))
                    {
                        success = program.Execute(player, (NPCData)npc, npc, item, null, type, arguments);
                    }
                    if (success) break;
                }
                if (!success)
                    foreach (var program in ((NPCData)npc).LuaPrograms)
                    {
                        if (program.ProgramTypes.ISSET(type))
                        {
                            success = program.Execute(player, npc, null, item, null, null, type, arguments);
                        }
                        if (success) break;
                    }
            }

            return success;
        }

        public static bool ExecutePrograms(ProgramTypes type, Character player, Character other, RoomData room, string arguments)
        {
            var npc = other as NPCData;
            bool success = false;
            if (type == ProgramTypes.EnterRoom || type == ProgramTypes.ExitRoom)
            {
                {
                    var programs = from program in room.Programs
                                   where program.Types.ISSET(type)
                                   select program;

                    foreach (var program in programs)
                    {
                        if (program.Execute(player, room, null, null, null, type, ""))
                        { success = true; break; }
                    }
                    if (success) return success;

                    var luaPrograms = from program in room.LuaPrograms
                                      where program.ProgramTypes.ISSET(type)
                                      select program;
                    foreach (var program in luaPrograms)
                    {
                        if (program.Execute(player, null, room, null, null, null, type, ""))
                        { success = true; break; }
                    }
                    if (success) return success;
                }

                foreach (var triggernpc in room.Characters.OfType<NPCData>().ToArray())
                {
                    var programs = from program in triggernpc.Programs
                                   where program.Types.ISSET(type)
                                   select program;
                    foreach (var program in programs)
                    {
                        if ((success = program.Execute(player, triggernpc, null, null, null, type, "")))
                            break;
                    }

                    if (success) return success;

                    var luaPrograms = from program in triggernpc.LuaPrograms
                                      where program.ProgramTypes.ISSET(type)
                                      select program;
                    foreach (var program in luaPrograms)
                    {
                        try
                        {
                            if ((success = program.Execute(player, triggernpc, room, null, null, null, type, "")))
                                break;
                        }
                        catch (Exception ex)
                        {
                            Game.log(ex.Message);
                        }
                    }

                    if (success) return success;
                }

                if (player is NPCData)
                {
                    var programs = from program in ((NPCData)player).Programs
                                   where program.Types.ISSET(type)
                                   select program;
                    foreach (var program in programs)
                    {
                        if ((success = program.Execute(player, ((NPCData)player), null, null, null, type, "")))
                            break;
                    }

                    if (success) return success;

                    var luaPrograms = from program in ((NPCData)player).LuaPrograms
                                      where program.ProgramTypes.ISSET(type)
                                      select program;
                    foreach (var program in luaPrograms)
                    {
                        try
                        {
                            if ((success = program.Execute(player, ((NPCData)player), room, null, null, null, type, "")))
                                break;
                        }
                        catch(Exception ex)
                        {
                            Game.log(ex.Message);
                        }
                    }

                    if (success) return success;
                }

                if (player.Form == null)
                {

                    foreach (var item in room.items.Concat(player.Inventory).Concat(player.Equipment.Values).ToArray())
                    {
                        var programs = from program in item.Programs
                                       where program.Types.ISSET(type)
                                       select program;

                        foreach (var program in programs)
                        {
                            if ((success = program.Execute(player, item, null, item, null, type, "")))
                                break;
                        }
                        if (success) break;

                        if (npc != null)
                        {
                            var luaPrograms = from program in npc.LuaPrograms
                                              where program.ProgramTypes.ISSET(type)
                                              select program;
                            foreach (var program in luaPrograms)
                            {
                                if ((success = program.Execute(player, null, room, item, null, null, type, "")))
                                    break;
                            }
                            if (success) break;
                        }
                    }
                    return success;
                }
            } // end enter / exit room
            else if (type == ProgramTypes.Say)
            {

                {
                    var programs = from program in room.Programs
                                   where program.Types.ISSET(type)
                                   select program;

                    foreach (var program in programs)
                    {
                        if (program.Execute(player, room, null, null, null, type, arguments))
                        { success = true; break; }
                    }
                    if (success) return success;

                    var luaPrograms = from program in room.LuaPrograms
                                      where program.ProgramTypes.ISSET(type)
                                      select program;
                    foreach (var program in luaPrograms)
                    {
                        if (program.Execute(player, null, room, null, null, null, type, arguments))
                        { success = true; break; }
                    }
                    if (success) return success;
                }

                foreach (var triggernpc in room.Characters.OfType<NPCData>().ToArray())
                {
                    var programs = from program in triggernpc.Programs
                                   where program.Types.ISSET(type)
                                   select program;
                    foreach (var program in programs)
                    {
                        if ((success = program.Execute(player, triggernpc, null, null, null, type, arguments)))
                            break;
                    }

                    if (success) return success;

                    var luaPrograms = from program in triggernpc.LuaPrograms
                                      where program.ProgramTypes.ISSET(type)
                                      select program;
                    foreach (var program in luaPrograms)
                    {
                        if ((success = program.Execute(player, triggernpc, room, null, null, null, type, arguments)))
                            break;
                    }

                    if (success) return success;
                }

                if (player.Form == null)
                {

                    foreach (var item in room.items.Concat(player.Inventory).Concat(player.Equipment.Values).ToArray())
                    {
                        var programs = from program in item.Programs
                                       where program.Types.ISSET(type)
                                       select program;

                        foreach (var program in programs)
                        {
                            if ((success = program.Execute(player, item, null, item, null, type, arguments)))
                                break;
                        }
                        if (success) break;

                        if (npc != null)
                        {
                            var luaPrograms = from program in npc.LuaPrograms
                                              where program.ProgramTypes.ISSET(type)
                                              select program;
                            foreach (var program in luaPrograms)
                            {
                                if ((success = program.Execute(player, null, room, item, null, null, type, arguments)))
                                    break;
                            }
                            if (success) break;
                        }
                    }
                    return success;
                }
            }
            else if (type == ProgramTypes.RoundCombat || type == ProgramTypes.OneHitMiss ||
                type == ProgramTypes.OneHitHit || type == ProgramTypes.OneHitAny)
            {
                if (player.Form == null)
                {
                    foreach (var item in player.Equipment.Values)
                    {
                        if (item == null)
                            continue;

                        // Find programs associated with round combat for the item
                        var programs = from program in item.Programs where program.Types.ISSET(type) select program;

                        foreach (var program in programs)
                        {
                            if ((success = program.Execute(player, item, npc, item, null, type, "")))
                                break;
                        }
                        if (success)
                            break;

                        var luaprograms = from program in item.LuaPrograms where program.ProgramTypes.ISSET(type) select program;

                        foreach (var program in luaprograms)
                        {
                            if ((success = program.Execute(player, npc, null, item, null, null, type, "")))
                                break;
                        }
                        if (success)
                            break;
                    }
                }

                if (npc != null && npc is NPCData)
                {
                    var luaPrograms = from program in npc.LuaPrograms
                                      where program.ProgramTypes.ISSET(type)
                                      select program;
                    foreach (var program in luaPrograms)
                    {
                        if (success = program.Execute(player, npc, room, null, null, null, type, ""))
                            break;
                    }
                }

                if (player != null && player is NPCData)
                {
                    var luaPrograms = from program in ((NPCData)player).LuaPrograms
                                      where program.ProgramTypes.ISSET(type)
                                      select program;
                    foreach (var program in luaPrograms)
                    {
                        if (success = program.Execute(player, npc, room, null, null, null, type, ""))
                            break;
                    }
                }
            }
            return success;
        }

        public interface Program<T>
        {
            string Name { get; }
            string Description { get; }
            List<ProgramTypes> Types { get; }
            bool Execute(Character player, T sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments);

        }

        public class SayProgramEverfullSkin : Program<ItemData>
        {
            public string Name => "EverfullSkin";
            public string Description => "A skin that can be filled with water or alcohol with a word";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.Say };

            public bool Execute(Character player, ItemData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.Say && sender.ItemType.ISSET(ItemTypes.DrinkContainer) && !arguments.ISEMPTY())
                {
                    if (arguments.StringCmp("ensharra"))
                    {
                        sender.Nutrition = 40;
                        sender.Charges = 40;
                        sender.Liquid = "water";
                        player.Act("A glow envelops $p briefly before fading away.", null, sender, null, ActType.ToRoom);
                        player.Act("A glow envelops $p briefly before fading away.", null, sender, null, ActType.ToChar);
                        return true;
                    }
                    else if (arguments.StringCmp("dorbae"))
                    {
                        sender.Nutrition = 40;
                        sender.Charges = 40;
                        sender.Liquid = "firebreather";
                        player.Act("A glow envelops $p briefly before fading away.", null, sender, null, ActType.ToRoom);
                        player.Act("A glow envelops $p briefly before fading away.", null, sender, null, ActType.ToChar);
                    }
                }
                return false;
            } // end execute
        } // end everfullskin program

        public class OpenProgramPouchOfNourishment : Program<ItemData>
        {
            public string Name => "PouchNourishment";
            public string Description => "A pouch that produces food when opened";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.Open };

            public bool Execute(Character player, ItemData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.Open && sender.ItemType.ISSET(ItemTypes.Container))
                {


                    if (sender.Contains.Count == 0)
                    {
                        var vnums = new int[] { 13, 14, 15, 16, 17, 18, 19, 20 };// all items with "food" extra flag
                        var vnum = vnums[Utility.Random(0, vnums.Length - 1)];
                        //ItemData item;
                        ItemTemplateData itemtemplate;
                        if (ItemTemplateData.Templates.TryGetValue(vnum, out itemtemplate))
                        {
                            var newitem = new ItemData(itemtemplate);
                            sender.Contains.Insert(0, newitem);
                            newitem.Container = sender;
                            player.Act("The markings on $p glow briefly.\n\r", null, sender, null, ActType.ToChar);
                            player.Act("The markings on $p glow briefly.\n\r", null, sender, null, ActType.ToRoom);
                            return true;
                        }
                    }
                }
                return false;
            } // end execute
        } // end pouch nourishment program

        public class OneHitHitCureSerious : Program<ItemData>
        {
            public string Name => "OneHitHitCureSerious";
            public string Description => "Heal a little bit when hitting an enemy";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.OneHitHit };

            public bool Execute(Character player, ItemData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.OneHitHit && Utility.Random(0, 10) <= 2)
                {
                    player.Act("\\W$p glows white.\\x\n\r", null, sender, null, ActType.ToChar);
                    player.Act("\\W$p glows white.\\x\n\r", null, sender, null, ActType.ToRoom);
                    Magic.ItemCastSpell(Magic.CastType.Cast, SkillSpell.SkillLookup("cure serious"), sender.Level, player, player, sender, null);
                    return true;
                }
                return false;
            } // end execute
        } // end on hit cure serious program

        public class OneHitHitBlackHammer : Program<ItemData>
        {
            public string Name => "OneHitHitBlackHammer";
            public string Description => "Cast lightning bolt or deafen";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.OneHitHit };

            public bool Execute(Character player, ItemData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.OneHitHit && Utility.Random(0, 10) <= 2)
                {
                    player.Act("\\W$p hums loudly briefly.\\x\n\r", null, sender, null, ActType.ToChar);
                    player.Act("\\W$p hums loudly briefly.\\x\n\r", null, sender, null, ActType.ToRoom);
                    Magic.ItemCastSpell(Magic.CastType.Cast, SkillSpell.SkillLookup("lightning bolt"), sender.Level, player, victim, sender, null);
                    return true;
                }
                return false;
            } // end execute
        } // end OneHitHitBlackHammer

        public class OneHitHitLightningEmbossedRing : Program<ItemData>
        {
            public string Name => "OneHitHitLightningEmbossedRing";
            public string Description => "Cast lightning bolt";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.OneHitHit };

            public bool Execute(Character player, ItemData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.OneHitHit && Utility.Random(0, 10) <= 2)
                {
                    player.Act("\\W$p hums loudly briefly.\\x\n\r", null, sender, null, ActType.ToChar);
                    player.Act("\\W$p hums loudly briefly.\\x\n\r", null, sender, null, ActType.ToRoom);
                    Magic.ItemCastSpell(Magic.CastType.Cast, SkillSpell.SkillLookup("lightning bolt"), sender.Level, player, victim, sender, null);
                    return true;
                }
                return false;
            } // end execute
        } // end OneHitHitLightningEmbossedRing

        public class OneHitHitRangerWeapon : Program<ItemData>
        {
            public string Name => "OneHitHitRangerWeapon";
            public string Description => "Heal a little bit when hitting an enemy";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.OneHitHit };

            public bool Execute(Character player, ItemData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.OneHitHit && Utility.Random(0, 10) <= 2)
                {
                    player.Act("\\W$p glows white.\\x\n\r", null, sender, null, ActType.ToChar);
                    player.Act("\\W$p glows white.\\x\n\r", null, sender, null, ActType.ToRoom);
                    var heal = Utility.dice(2, item.Level / 2, item.Level / 2);
                    player.HitPoints = Math.Min(player.HitPoints + heal, player.MaxHitPoints);

                    return true;
                }
                return false;
            } // end execute
        } // end on
        public class QuestSayHelloProgram : Program<NPCData>
        {
            public string Name => "SayHelloProg";
            public string Description => "Quest for player to say hello";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.EnterRoom, ProgramTypes.Say };

            public bool Execute(Character player, NPCData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.EnterRoom)
                {
                    var quest = Quest.GetQuest(30000);
                    if (QuestProgressData.IsQuestAvailable(player, quest))
                    {
                        DoActCommunication.DoSay(sender, "Alas, another hero come to give their life in the Labyrinth...");
                        QuestProgressData.StartQuest(player, sender.ShortDescription, quest);
                        return true;
                    }

                }
                else if (type == ProgramTypes.Say)
                {
                    var quest = Quest.GetQuest(30000);
                    if (QuestProgressData.IsQuestInProgress(player, quest) && (arguments.StringCmp("Hello") || arguments.StringCmp("Hi")))
                    {
                        QuestProgressData.CompleteQuest(player, quest);
                        return true;
                    }
                    else if (QuestProgressData.IsQuestInProgress(player, quest))
                    {
                        QuestProgressData.FailQuest(player, quest);
                    }
                    else if (QuestProgressData.IsQuestFailed(player, quest) && (new string[] { "i'm sorry", "im sorry", "sorry" }.Any(s => s.StringCmp(arguments))))
                    {
                        DoActCommunication.DoSay(sender, "Ugh, alright, why don't you say hello then.");
                        QuestProgressData.ResetQuest(player, quest);
                    }

                }
                return false;
            } // end execute
        } // end QuestSayHelloProgram

        public class AstoriaGuardBeforeUnlock : Program<NPCData>
        {
            public string Name => "AstoriaGuardBeforeUnlock";
            public string Description => "Guard must be dead or sleeping to unlock chest.";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.BeforeUnlock, ProgramTypes.BeforeRelock };

            public bool Execute(Character player, NPCData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (sender.IsAwake && sender.CanSee(player))
                {
                    int count = 0;
                    var chest = sender.GetItemRoom("chest", ref count);
                    if (chest != null && chest.Vnum == 19095)
                    {
                        sender.Act("$n steps between $N and $p.", player, chest);
                        sender.Act("$n steps between you and $p.", player, chest, type: ActType.ToVictim);
                        return true;
                    }
                }
                return false;
            } // end execute
        } // end AstoriaGuardBeforeUnlock

        public class UseNewbieLeverProg : Program<ItemData>
        {
            public string Name => "UseNewbieLeverProg";
            public string Description => "Use the lever in the newbie arena";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.Use };

            public bool Execute(Character player, ItemData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.Use)
                {
                    var quest = Quest.GetQuest(3700);
                    QuestProgressData progress;
                    if (quest != null && ((progress = Quest.GetQuestProgress(player, 3700)) == null || progress.Status != Quest.QuestStatus.Complete))
                    {
                        ItemTemplateData.Templates.TryGetValue(3731, out var template);
                        if (template != null)
                        {
                            var cape = new ItemData(template, player);
                            player.Act("You pull the lever and let go. It retracts to its original position.\n\r");
                            player.Act("$n pulls the lever and lets go. It retracts to its original position.\n\r", type: ActType.ToRoom);
                            player.Act("$p falls out of a chute to the west into your hands.\n\r", item: cape, type: ActType.ToChar);
                            player.Act("$p falls out of a chute to the west into $n's hands.\n\r", item: cape, type: ActType.ToRoom);
                        }


                        QuestProgressData.CompleteQuest(player, quest);
                    }
                    else
                        player.send("You pull the lever, but nothing seems to happen.\n\r");
                    return true;
                }
                return false;
            } // end execute
        } // end use newbie lever

        public class WearNewbieGearProg : Program<ItemData>
        {
            public string Name => "WearNewbieGear";
            public string Description => "Wear a piece of standard issue gear";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.Wear };

            public bool Execute(Character player, ItemData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.Wear && item != null && item.Template != null)
                {
                    var quest = Quest.GetQuest(item.Vnum);
                    QuestProgressData progress;
                    if (quest != null && ((progress = Quest.GetQuestProgress(player, quest.Vnum)) == null || progress.Status != Quest.QuestStatus.Complete))
                    {
                        QuestProgressData.CompleteQuest(player, quest);
                    }
                    return true;
                }
                return false;
            } // end execute
        } // end wear newbie gear

        public class ForemanRespondProgram : Program<NPCData>
        {
            public string Name => "ForemanRespondProgram";
            public string Description => "Quest for player to say yes or no to the foreman";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.EnterRoom, ProgramTypes.Say };

            public bool Execute(Character player, NPCData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.EnterRoom)
                {
                    var quest = Quest.GetQuest(60000);

                    if (QuestProgressData.IsQuestAvailable(player, quest))
                    {
                        DoActCommunication.DoSay(sender, "Hey there, are you up for a job?");
                        QuestProgressData.StartQuest(player, sender.ShortDescription, quest);
                        return true;
                    }


                }
                else if (type == ProgramTypes.Say)
                {
                    var quest = Quest.GetQuest(60000);
                    if (QuestProgressData.IsQuestInProgress(player, quest) && (
                        "yes".StringPrefix(arguments) || "yeah".StringPrefix(arguments) ||
                        "okay".StringPrefix(arguments)))
                    {
                        QuestProgressData.CompleteQuest(player, quest);
                        QuestProgressData.StartQuest(player, sender.Name, Quest.GetQuest(60001));

                        return true;
                    }
                    else if (QuestProgressData.IsQuestInProgress(player, quest) &&
                        ("nope".StringPrefix(arguments) || "nah".StringPrefix(arguments)))
                    {
                        QuestProgressData.FailQuest(player, quest);
                    }
                }
                return false;
            } // end execute
        } // end ForemanRespondProgram

        public class DocksBountyQuestProgram : Program<NPCData>
        {
            public string Name => "DocksBountyQuestProgram";
            public string Description => "Quest for player to kill critters for the foreman";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.EnterRoom, ProgramTypes.SenderDeath };

            public bool Execute(Character player, NPCData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                if (type == ProgramTypes.EnterRoom)
                {
                    var quest = Quest.GetQuest(60001);
                    if (sender.vnum == 60000 && QuestProgressData.IsQuestInProgress(player, quest) && sender.CanSee(player))
                    {

                        var progress = Quest.GetQuestProgress(player, quest);
                        var bountyKills = progress.ExtraState.GetAttributeValueInt("BountyKills");
                        var paidBountyKills = progress.ExtraState.GetAttributeValueInt("PaidBountyKills");

                        if (paidBountyKills < bountyKills)
                        {
                            var topay = bountyKills - paidBountyKills;
                            DoActCommunication.DoSay(sender, string.Format("I see you've slain {0} critters. Here's your coin.", topay));
                            sender.Silver += topay;
                            DoActItem.DoGive(sender, string.Format("{0} silver {1}", topay, player.Name));
                            progress.ExtraState.SetAttributeValue("PaidBountyKills", bountyKills);
                        }

                        if (bountyKills == 50)
                        {
                            QuestProgressData.CompleteQuest(player, quest);
                        }

                        return true;
                    }
                }
                else if (type == ProgramTypes.SenderDeath)
                {
                    var quest = Quest.GetQuest(60001);

                    // Dying NPC is rat, centipede or spider
                    if (QuestProgressData.IsQuestInProgress(player, quest) && new int[] { 60001, 60002, 60003 }.Contains(sender.vnum))
                    {
                        var progress = Quest.GetQuestProgress(player, quest);
                        var bountyKills = progress.ExtraState.GetAttributeValueInt("BountyKills");

                        if (bountyKills < 50)
                            progress.ExtraState.SetAttributeValue("BountyKills", bountyKills + 1);

                        return true;
                    }

                }
                return false;
            } // end execute
        } // end ForemanRespondProgram

        public class AffectDuelStartingProgram : Program<AffectData>
        {
            public string Name => "DuelStartingProgram";
            public string Description => "Count down a duel";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.AffectTick };

            public bool Execute(Character player, AffectData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                var opponent = Character.Characters.Where(c => !c.IsNPC && c.Name == sender.ownerName).FirstOrDefault();

                if (opponent == null)
                {
                    player.send("Your opponent doesn't seem to be around any more.\n\r");
                    player.StripAffect(AffectFlags.DuelStarting);
                }

                player.send("Your duel starts in the span of \\r{0}\\x rounds of combat.\n\r", sender.duration + 1);
                return true;
            } // end execute
        } // end AffectDuelStartingProgram

        public class AffectDuelStartProgram : Program<AffectData>
        {
            public string Name => "DuelStartProgram";
            public string Description => "Initiate a duel";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.AffectEnd };

            public bool Execute(Character player, AffectData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                var opponent = Character.Characters.Where(c => !c.IsNPC && c.Name == sender.ownerName).FirstOrDefault();

                if (opponent == null)
                {
                    player.send("Your opponent doesn't seem to be around any more.\n\r");
                }
                else
                {
                    var newaffect = new AffectData();
                    newaffect.flags.SETBIT(AffectFlags.DuelInProgress);
                    newaffect.ownerName = opponent.Name;
                    newaffect.duration = -1;
                    newaffect.frequency = Frequency.Violence;
                    newaffect.displayName = "Dueling " + newaffect.ownerName;
                    newaffect.endMessage = "Your duel has ended.";
                    newaffect.beginMessage = "Your duel has started.";
                    newaffect.hidden = false;
                    newaffect.tickProgram = "DuelTickProgram";
                    newaffect.RemoveAndSaveFlags.SETBIT(AffectData.StripAndSaveFlags.DoNotSave);
                    player.AffectToChar(newaffect);
                }
                return true;
            } // end execute
        } // end AffectDuelStartProgram

        public class AffectDuelTickProgram : Program<AffectData>
        {
            public string Name => "DuelTickProgram";
            public string Description => "Continue a duel";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.AffectTick };

            public bool Execute(Character player, AffectData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                var opponent = Character.Characters.Where(c => !c.IsNPC && c.Name == sender.ownerName).FirstOrDefault();

                if (opponent == null)
                {
                    player.send("Your opponent doesn't seem to be around any more.\n\r");

                    player.AffectFromChar(sender, AffectRemoveReason.Other);

                }

                return true;
            } // end execute
        }

        public class AffectFirstAidTick : Program<AffectData>
        {
            public string Name => "AffectFirstAidTick";
            public string Description => "Continue a duel";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.AffectTick };

            public bool Execute(Character player, AffectData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                Character applicator;
                Character recipient;

                if (sender.flags.ISSET(AffectFlags.ApplyingFirstAid))
                {
                    recipient = sender.GetOwner();
                    applicator = player;
                }
                else
                {
                    recipient = player;
                    applicator = sender.GetOwner();
                }

                if (applicator == null || recipient == null)
                {
                    return true;
                }

                if (applicator.Room == recipient.Room && sender.flags.ISSET(AffectFlags.ApplyingFirstAid))
                {

                    applicator.Act("$n continues applying bandages to $N's wounds.", recipient, type: ActType.ToRoomNotVictim);
                    if (applicator != recipient)
                    {
                        applicator.Act("$n continues applying bandages to your wounds.", recipient, type: ActType.ToVictim);
                        applicator.Act("You continue applying bandages to $N's wounds.", recipient, type: ActType.ToChar);
                    }
                    else
                        applicator.Act("You continue applying bandages to your wounds.", recipient, type: ActType.ToChar);
                }

                return true;
            } // end execute
        }

        public class AffectFirstAidEnd : Program<AffectData>
        {
            public string Name => "AffectFirstAidEnd";
            public string Description => "First aid ended";

            public List<ProgramTypes> Types => new List<ProgramTypes> { ProgramTypes.AffectEnd };

            public bool Execute(Character player, AffectData sender, Character victim, ItemData item, SkillSpell skill, ProgramTypes type, string arguments)
            {
                Character applicator;
                Character recipient;

                if (sender.flags.ISSET(AffectFlags.ApplyingFirstAid))
                {
                    recipient = sender.GetOwner();
                    applicator = player;
                }
                else
                {
                    recipient = player;
                    applicator = sender.GetOwner();
                }

                if (applicator == null || recipient == null)
                {
                    return true;
                }

                if (sender.RemovedReason == AffectRemoveReason.Moved ||
                    sender.RemovedReason == AffectRemoveReason.ChangedPosition ||
                    sender.RemovedReason == AffectRemoveReason.Combat ||
                    sender.RemovedReason == AffectRemoveReason.Died)
                {
                    applicator.Act("$n's attempt to apply bandages to $N's wounds is disrupted.", recipient, type: ActType.ToRoomNotVictim);
                    if (applicator != recipient)
                    {
                        applicator.Act("$n's attempt to apply bandages to your wounds is disrupted.", recipient, type: ActType.ToVictim);
                        applicator.Act("Your attempt to apply bandages to $N's wounds is disrupted.", recipient, type: ActType.ToChar);
                    }
                    else
                        applicator.Act("Your attempt to apply bandages to your wounds is disrupted.", recipient, type: ActType.ToChar);
                    applicator.StripAffect(AffectFlags.ApplyingFirstAid, true);
                    recipient.StripAffect(AffectFlags.FirstAidBeingApplied, true);
                }
                else if (player == applicator && sender.duration == 0 && sender.flags.ISSET(AffectFlags.ApplyingFirstAid))
                {
                    Character.EndFirstAid(applicator, recipient);
                }

                return true;
            } // end execute
        }
    } // end programs
} // end crimsonstainedlands
