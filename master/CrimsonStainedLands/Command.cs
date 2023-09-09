using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    public delegate void CommandAction(Character ch, string arguments);

    public class Command
    {
        
        [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true) 
        ]
        public class CommandAttribute : Attribute 
        { 
            public Command Command { get; set; }
            public string SkillName { get; set; } = "";
            public CommandAttribute(string Name, string Info, Positions MinimumPosition, int MinimumLevel = 0, string skillspellname = "", bool NPCCommand = false) 
            {
                Command = new Command() { Name = Name, Info = Info, MinimumPosition = MinimumPosition, MinimumLevel = MinimumLevel, NPCCommand = NPCCommand };
                SkillName = skillspellname;
                //CrimsonStainedLands.Command.Commands.Add(Command);
            }

            public static void AddAttributeCommands()
            {
                var methods = Assembly.GetAssembly(typeof(CommandAttribute)).GetTypes()
                      .SelectMany(t => t.GetMethods())
                      .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                      .ToArray();

                foreach(var method in methods)
                {
                    foreach(var commandAttribute in method.GetCustomAttributes(typeof(CommandAttribute), false).OfType<CommandAttribute>())
                    {
                        commandAttribute.Command.Action = (CommandAction) method.CreateDelegate(typeof(CommandAction));
                        commandAttribute.Command.Skill = SkillSpell.SkillLookup(commandAttribute.SkillName);
                        Commands.Add(commandAttribute.Command);
                    }
                }
            }
        }


        public static List<Command> Commands = new List<Command>();

        public string Name;
        public CommandAction Action;
        public string Info;
        public Positions MinimumPosition;
        public int MinimumLevel = 0;
        public SkillSpell Skill = null;
        public bool NPCCommand = true;

        static Command()
        {
            Commands.Add(new Command { Name = "north", Action = CharacterDoFunctions.DoNorth, Info = "Walk north.", MinimumPosition = Positions.Standing, NPCCommand = false });
            Commands.Add(new Command { Name = "east", Action = CharacterDoFunctions.DoEast, Info = "Walk east.", MinimumPosition = Positions.Standing, NPCCommand = false });
            Commands.Add(new Command { Name = "south", Action = CharacterDoFunctions.DoSouth, Info = "Walk south.", MinimumPosition = Positions.Standing, NPCCommand = false });
            Commands.Add(new Command { Name = "west", Action = CharacterDoFunctions.DoWest, Info = "Walk west.", MinimumPosition = Positions.Standing, NPCCommand = false });
            Commands.Add(new Command { Name = "up", Action = CharacterDoFunctions.DoUp, Info = "Walk up.", MinimumPosition = Positions.Standing, NPCCommand = false });
            Commands.Add(new Command { Name = "down", Action = CharacterDoFunctions.DoDown, Info = "Walk down.", MinimumPosition = Positions.Standing, NPCCommand = false });
            Commands.Add(new Command { Name = "say", Action = DoActCommunication.DoSay, Info = "Say something to others in your room.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "sayto", Action = DoActCommunication.DoSayTo, Info = "Say something to the person in your room.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "save", Action = CharacterDoFunctions.DoSave, Info = "Save character information.", MinimumPosition = Positions.Dead, NPCCommand = false });
            Commands.Add(new Command { Name = "sing", Action = Songs.DoSing, Info = "Sing a song.", MinimumPosition = Positions.Resting, NPCCommand = false });
            
            Commands.Add(new Command { Name = "reply", Action = DoActCommunication.DoReply, Info = "Reply to a tell.", MinimumPosition = Positions.Resting });

            Commands.Add(new Command { Name = "whisper", Action = DoActCommunication.DoWhisper, Info = "Whisper something to others in your room.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "whisperto", Action = DoActCommunication.DoWhisperTo, Info = "Whisper something to the person in your room.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "tell", Action = DoActCommunication.DoTell, Info = "Tell something to a person.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "yell", Action = DoActCommunication.DoYell, Info = "Yell loudly in the area.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "pray", Action = DoActCommunication.DoPray, Info = "Pray something to the gods.", MinimumPosition = Positions.Dead, NPCCommand = false });
            Commands.Add(new Command { Name = "newbie", Action = DoActCommunication.DoNewbie, Info = "Ask something on the newbie channel.", MinimumPosition = Positions.Dead, NPCCommand = false });

            Commands.Add(new Command { Name = "quit", Action = CharacterDoFunctions.DoQuit, Info = "Exit the game.", MinimumPosition = Positions.Dead, NPCCommand = false });
            Commands.Add(new Command { Name = "where", Action = CharacterDoFunctions.DoWhere, Info = "Display list of nearby players.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "who", Action = CharacterDoFunctions.DoWho, Info = "Display list of players online.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "look", Action = DoActInfo.DoLook, Info = "Look around in the room you are in.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "unlock", Action = CharacterDoFunctions.DoUnlock, Info = "Unlock a door using a key.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "picklock", Action = CharacterDoFunctions.DoPickLock, Info = "Unlock a door using a thiefpick.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "infiltrate", Action = CharacterDoFunctions.DoInfiltrate, Info = "Unlock unpickable locks without a thiefpick.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "relock", Action = CharacterDoFunctions.DoRelock, Info = "Relock a door or chest using a thiefpick.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "follow", Action = CharacterDoFunctions.DoFollow, Info = "Follow someone.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "order", Action = CharacterDoFunctions.DoOrder, Info = "Order a pet to do something.", MinimumPosition = Positions.Resting, NPCCommand = false });

            Commands.Add(new Command { Name = "get", Action = DoActItem.DoGet, Info = "Pick up an item.", MinimumPosition = Positions.Resting });


            Commands.Add(new Command { Name = "group", Action = CharacterDoFunctions.DoGroup, Info = "Group with a follower.", MinimumPosition = Positions.Sleeping, NPCCommand = false });
            Commands.Add(new Command { Name = "gtell", Action = DoActCommunication.DoGTell, Info = "Tell the group something.", MinimumPosition = Positions.Sleeping, NPCCommand = false });

            Commands.Add(new Command { Name = "hide", Action = CharacterDoFunctions.DoHide, Info = "Hide in the shadows.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "sneak", Action = CharacterDoFunctions.DoSneak, Info = "Sneak around without stepping out of the shadows.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "gentlewalk", Action = CharacterDoFunctions.DoGentleWalk, Info = "Atempt to avoid combat with aggressive npc's when walking into their room.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "visible", Action = CharacterDoFunctions.DoVisible, Info = "Make yourself seen.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "detecthidden", Action = CharacterDoFunctions.DoDetectHidden, Info = "Become more aware of the shadows.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "arcanevision", Action = CharacterDoFunctions.DoArcaneVision, Info = "Become more aware magical items.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "enliven", Action = ShapeshiftForm.DoEnliven, Info = "Activate a spell while in form.", MinimumPosition = Positions.Standing });

            Commands.Add(new Command { Name = "cast", Action = Magic.DoCast, Info = "Cast a spell.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "commune", Action = Magic.DoCommune, Info = "Commune with the gods to receive their power.", MinimumPosition = Positions.Fighting });

            Commands.Add(new Command { Name = "kill", Action = Combat.DoKill, Info = "Kill something.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "murder", Action = Combat.DoKill, Info = "Kill something.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "flee", Action = Combat.DoFlee, Info = "Run from combat.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "disengage", Action = Combat.DoDisengage, Info = "Disengage from combat and fade into the shadows.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "envenomweapon", Action = Combat.DoEnvenomWeapon, Info = "Apply poison to weapon, giving a chance per hit to poison enemy.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "greaseitem", Action = CharacterDoFunctions.DoGreaseItem, Info = "Apply grease to an item, so it may be removed or dropped without uncurse.", MinimumPosition = Positions.Standing });

            Commands.Add(new Command { Name = "berserk", Action = Combat.DoBerserk, Info = "Enrage to do more damage.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "warcry", Action = Combat.DoWarCry, Info = "You become inspired from a loud war cry.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "kick", Action = Combat.DoKick, Info = "Kick someone you are fighting.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "dirtkick", Action = Combat.DoDirtKick, Info = "Kick dirt into the eyes of someone.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "trip", Action = Combat.DoTrip, Info = "Trip someone you are fighting.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "weapontrip", Action = Combat.DoWeaponTrip, Info = "Trip someone you are fighting with your weapon.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "bash", Action = Combat.DoBash, Info = "Bash someone you are fighting to the ground.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "shieldbash", Action = Combat.DoShieldBash, Info = "Bash someone with your shield to the ground.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "disarm", Action = Combat.DoDisarm, Info = "Attempt to disarm weapon from your victim.", MinimumPosition = Positions.Fighting });
            
            Commands.Add(new Command { Name = "kidneyshot", Action = Combat.DoKidneyShot, Info = "Stab someone in the kidneys, weakening them.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "blindfold", Action = Combat.DoBlindFold, Info = "Blindfold someone who is sapped or sleeping.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "gag", Action = Combat.DoGag, Info = "Stuff a gag into someones mouth if they are sapped or sleeping, preventing them from casting.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "bindhands", Action = Combat.DoBindHands, Info = "Bind someones hands if they are sapped or sleeping, preventing them from holding anything.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "bindlegs", Action = Combat.DoBindLegs, Info = "Bind someones legs if they are sapped or sleeping, preventing them from moving.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "stand", Action = CharacterDoFunctions.DoStand, Info = "Stand up from a resting, sitting or sleeping position.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "rest", Action = CharacterDoFunctions.DoRest, Info = "Begin resting. Regeneration should be better than that of standing.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "sit", Action = CharacterDoFunctions.DoSit, Info = "Sit down.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "sleep", Action = CharacterDoFunctions.DoSleep, Info = "Go to sleep. Regeneration should be better than that of resting.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "score", Action = DoActInfo.DoScore, Info = "Display character details.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "scan", Action = DoActInfo.DoScan, Info = "Scan the exits for monsters.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "wake", Action = CharacterDoFunctions.DoWake, Info = "Wake and stand up.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "sleepingdisarm", Action = Combat.DoSleepingDisarm, Info = "Disarm weapon from your victim while they sleep.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "sacrifice", Action = DoActItem.DoSacrifice, Info = "Sac items in room.", MinimumPosition = Positions.Dead });
                       
            Commands.Add(new Command { Name = "open", Action = CharacterDoFunctions.DoOpen, Info = "Open a door or container.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "close", Action = CharacterDoFunctions.DoClose, Info = "Close a door or container.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "lock", Action = CharacterDoFunctions.DoLock, Info = "Lock a door or container.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "steal", Action = CharacterDoFunctions.DoSteal, Info = "Steal something from someone's inventory.", MinimumPosition = Positions.Fighting});

            Commands.Add(new Command { Name = "eat", Action = DoActItem.DoEat, Info = "Eat some food.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "drink", Action = DoActItem.DoDrink, Info = "Quench your thirst.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "fill", Action = DoActItem.DoFill, Info = "Fill a container with liquid.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "practice", Action = CharacterDoFunctions.DoPractice, Info = "Show skills or practice at a guildmaster.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "train", Action = CharacterDoFunctions.DoTrain, Info = "Train a stat at a trainer.", MinimumPosition = Positions.Standing });
                        
            Commands.Add(new Command { Name = "recall", Action = CharacterDoFunctions.DoRecall, Info = "Transport you to the temple in Midgaard.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "nofollow", Action = CharacterDoFunctions.DoNofollow, Info = "Allow or disallow followers.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "notes", Action = CharacterDoFunctions.DoNotes, Info = "View or write notes.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "outfit", Action = CharacterDoFunctions.DoOutfit, Info = "Receive basic armor from the gods.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "fly", Action = CharacterDoFunctions.DoFly, Info = "Fly into the air above the lands.", MinimumPosition = Positions.Standing });

            Commands.Add(new Command { Name = "inventory", Action = DoActItem.DoInventory, Info = "Show items being carried", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "equipment", Action = DoActItem.DoEquipment, Info = "Show items being worn.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "drop", Action = DoActItem.DoDrop, Info = "Drop an item on the ground.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "wear", Action = DoActItem.DoWear, Info = "Wear an item you are carrying.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "wield", Action = DoActItem.DoWield, Info = "Wield a weapon you are carrying.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "remove", Action = DoActItem.DoRemove, Info = "Remove an item you are wearing.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "give", Action = DoActItem.DoGive, Info = "Give away an item you are holding.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "put", Action = DoActItem.DoPut, Info = "Put an item inside of a container.", MinimumPosition = Positions.Resting, NPCCommand = false });
            Commands.Add(new Command { Name = "quaf", Action = DoActItem.DoQuaf, Info = "Quaf a magical potion to receive its benefits.", MinimumPosition = Positions.Standing, NPCCommand = false });
            Commands.Add(new Command { Name = "recite", Action = DoActItem.DoRecite, Info = "Recite a magical scroll on yourself or someone else.", MinimumPosition = Positions.Standing, NPCCommand = false });
            Commands.Add(new Command { Name = "zap", Action = DoActItem.DoZap, Info = "Zap yourself, someone else or something with a magical wand.", MinimumPosition = Positions.Fighting, NPCCommand = false });
            Commands.Add(new Command { Name = "area", Action = CharacterDoFunctions.DoArea, Info = "Show the name of the area you are in.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "areas", Action = CharacterDoFunctions.DoAreas, Info = "List all areas with credits.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "list", Action = DoActItem.DoList, Info = "List what a shopkeeper has to sell.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "buy", Action = DoActItem.DoBuy, Info = "Buy something a vendor has to offer.", MinimumPosition = Positions.Resting , NPCCommand = false });
            Commands.Add(new Command { Name = "sell", Action = DoActItem.DoSell, Info = "Sell something to a shopkeeper.", MinimumPosition = Positions.Resting , NPCCommand = false });
            Commands.Add(new Command { Name = "value", Action = DoActItem.DoValue, Info = "Ask a shopkeeper how much something is worth to them.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "repair", Action = DoActItem.DoRepair, Info = "Repair an item at a shopkeeper.", MinimumPosition = Positions.Resting });

            Commands.Add(new Command { Name = "brandish", Action = DoActItem.DoBrandish, Info = "Brandish a staff and use its magic.", MinimumPosition = Positions.Standing, NPCCommand = false });

            Commands.Add(new Command { Name = "gain", Action = CharacterDoFunctions.DoGain, Info = "Convert practices into trains or revert trains into practices.", MinimumPosition = Positions.Resting });

            Commands.Add(new Command { Name = "rescue", Action = Combat.DoRescue, Info = "Rescue someone from others fighting them.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "intercept", Action = Combat.DoIntercept, Info = "Intercept someone from hitting someone else.", MinimumPosition = Positions.Fighting});
            
            Commands.Add(new Command { Name = "heal", Action = Magic.DoHeal, Info = "Heal at a healer or cleric.", MinimumPosition = Positions.Resting});

            Commands.Add(new Command { Name = "autoloot", Action = DoActInfo.DoAutoloot, Info = "Automatically loot the corpses of slain enemies.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "autosac", Action = DoActInfo.DoAutosac, Info = "Automatically sacrifice the corpses of slain enemies.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "autogold", Action = DoActInfo.DoAutogold, Info = "Automatically loot gold of slain enemies.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "autosplit", Action = DoActInfo.DoAutosplit, Info = "Automatically split coins gathered.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "autoassist", Action = DoActInfo.DoAutoassist, Info = "Automatically assist your group in combat.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "brief", Action = DoActInfo.DoBrief, Info = "Hide room descriptions.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "affects", Action = DoActInfo.DoAffects, Info = "List current affects.", MinimumPosition = Positions.Dead });
            
            Commands.Add(new Command { Name = "worth", Action = DoActInfo.DoWorth, Info = "Display character worth.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "color", Action = DoActInfo.DoColor, Info = "Toggle colors.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "description", Action = DoActInfo.DoDescription, Info = "Describe your character for others to see.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "consider", Action = DoActInfo.DoConsider, Info = "Consider how hard it would be to kill the target.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "skills", Action = DoActInfo.DoSkills, Info = "List skills learnable and learned.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "spells", Action = DoActInfo.DoSpells, Info = "List spells learnable and learned.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "supplications", Action = DoActInfo.DoSupplications, Info = "List supplications learnable and learned.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "songs", Action = DoActInfo.DoSongs, Info = "List songs learnable and learned.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "socials", Action = DoActInfo.DoSocials, Info = "List socials", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "commands", Action = DoActInfo.DoCommands, Info = "List commands.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "compare", Action = DoActItem.DoCompare, Info = "Compare two items.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "wimpy", Action = DoActInfo.DoWimpy, Info = "Set an amount of hitpoints to automatically attempt to flee.", MinimumPosition = Positions.Dead });

            Commands.Add(new Command { Name = "emote", Action = DoActInfo.DoEmote, Info = "Act something out for the room to see.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "help", Action = DoActInfo.DoHelp, Info = "Get information about a command or topic.", MinimumPosition = Positions.Resting });
            
            Commands.Add(new Command { Name = "knife", Action = Combat.DoKnife, Info = "Knife an enemy with a dagger.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "stab", Action = Combat.DoStab, Info = "Stab an enemy with a dagger.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "backstab", Action = Combat.DoBackstab, Info = "Stab an enemy in the back with a dagger.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "bs", Action = Combat.DoBackstab, Info = "Stab an enemy in the back with a dagger.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "dualbackstab", Action = Combat.DoDualBackstab, Info = "Stab an enemy in the back with both daggers.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "dbs", Action = Combat.DoDualBackstab, Info = "Stab an enemy in the back with both daggers.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "bindwounds", Action = Combat.DoBindWounds, Info = "Bind your wounds up.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "circlestab", Action = Combat.DoCircleStab, Info = "Circle behind and stab an enemy with a dagger.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "flurry", Action = Combat.DoFlurry, Info = "Unleash a flurry of attacks with your swords.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "drum", Action = Combat.DoDrum, Info = "Drum on someone with your maces.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "deposit", Action = CharacterDoFunctions.DoDeposit, Info = "Deposit some coins into the bank.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "withdraw", Action = CharacterDoFunctions.DoWithdraw, Info = "Withdraw some coins from the bank.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "balance", Action = CharacterDoFunctions.DoBalance, Info = "Check your bank balance.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "specialize", Action = CharacterDoFunctions.DoSpecialize, Info = "Specialize in a weapon.", MinimumPosition = Positions.Dead });

            Commands.Add(new Command { Name = "lore", Action = CharacterDoFunctions.DoLore, Info = "Inspect the quality of an item.", MinimumPosition = Positions.Standing });
            

            Commands.Add(new Command { Name = "bonus", Action = BonusInfo.DoBonus, Info = "Give the bonus information.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "time", Action = DoActInfo.DoTime, Info = "Give the date in .", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "weather", Action = DoActInfo.DoWeather, Info = "Report the current weather situation.", MinimumPosition = Positions.Resting});
            Commands.Add(new Command { Name = "prompt", Action = DoActInfo.DoPrompt, Info = "Set or view your prompt.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "creep", Action = CharacterDoFunctions.DoCreep, Info = "Maintain camouflage and Creep in a direction.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "crawl", Action = CharacterDoFunctions.DoCrawl, Info = "Crawl in a direction.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "drag", Action = CharacterDoFunctions.DoDrag, Info = "Drag someone in a direction.", MinimumPosition = Positions.Standing });

            Commands.Add(new Command { Name = "flyto", Action = CharacterDoFunctions.DoFlyto, Info = "Fly to someone in the same area.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "quests", Action = DoActInfo.DoQuests, Info = "Display quests.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "use", Action = DoActItem.DoUse, Info = "Use an item.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "invoke", Action = DoActItem.DoInvoke, Info = "Invoke something.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "shapeshift", Action = ShapeshiftForm.DoShapeshift, Info = "Take the form of something else.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "revert", Action = ShapeshiftForm.DoRevert, Info = "Revert back to your normal form.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "feint", Action = Combat.DoFeint, Info = "Distract a foe from their next attack.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "crusaderstrike", Action = Combat.DoCrusaderStrike, Info = "Strike a foe with the fervor of a holy paladin.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "angelswing", Action = Combat.DoAngelsWing, Info = "Strike a foe with fervor after a special shield move.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "berserkersstrike", Action = Combat.DoBerserkersStrike, Info = "Strike a foe with the method of a berserker.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "risingkick", Action = Combat.DoRisingKick, Info = "Kick all enemies fighting you.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "bindwounds", Action = Combat.DoBindWounds, Info = "Bind your wounds.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "acutevision", Action = CharacterDoFunctions.DoAcuteVision, Info = "See into the cover of the wilderness.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "findwater", Action = CharacterDoFunctions.DoFindWater, Info = "Create a spring of water from the ground.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "barkskin", Action = Combat.DoBarkskin, Info = "Makes your skin hard as bark, lowering your armor class.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "herbs", Action = Combat.DoHerbs, Info = "Find healing herbs in the wilderness.", MinimumPosition = Positions.Standing});
            Commands.Add(new Command { Name = "firstaid", Action = CharacterDoFunctions.DoFirstAid, Info = "Bandage yourself or others while reducing duration and damge from bleeds or poisons.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "serpentstrike", Action = Combat.DoSerpentStrike, Info = "Serpent strike a foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "lash", Action = Combat.DoLash, Info = "Lash a foe with whip or flail, lagging them.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "pugil", Action = Combat.DoPugil, Info = "Pugil someone with a staff.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "roundhouse", Action = Combat.DoRoundhouse, Info = "Roundhouse your foes.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "camp", Action = CharacterDoFunctions.DoCamp, Info = "Set up camp for improved regen while sleeping.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "fashionstaff", Action = Combat.DoFashionStaff, Info = "Fashion a ranger staff from a nearby tree limb.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "fashionspear", Action = Combat.DoFashionSpear, Info = "Fashion a ranger spear from a nearby tree limb.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "palmsmash", Action = Combat.DoPalmSmash, Info = "Smash somene with your bare hands.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "handflurry", Action = Combat.DoHandFlurry, Info = "Attempt a series of palm strikes with both hands.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "maceflurry", Action = Combat.DoMaceFlurry, Info = "Attempt a series of quick strikes with your mace.", MinimumPosition = Positions.Fighting });
            

            Commands.Add(new Command { Name = "wheelkick", Action = Combat.DoWheelKick, Info = "Perform a wheel kick on your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "sweepkick", Action = Combat.DoSweepKick, Info = "Perform a sweeping kick on your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "sidekick", Action = Combat.DoSideKick, Info = "Perform a side kick on your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "scissorskick", Action = Combat.DoScissorsKick, Info = "Perform a scissors kick on your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "crescentkick", Action = Combat.DoCrescentKick, Info = "Perform a crescent kick on your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "axekick", Action = Combat.DoAxeKick, Info = "Perform an axe kick on your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "mountainstormkick", Action = Combat.DoMountainStormKick, Info = "Perform a mountain storm kick on your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "doublespinkick", Action = Combat.DoDoubleSpinKick, Info = "Perform a double spin kick on your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "risingphoenixkick", Action = Combat.DoRisingPhoenixKick, Info = "Perform a rising phoenix kick on your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "caltraps", Action = Combat.DoCaltraps, Info = "Throw caltrops at the feet of your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "heightenedawareness", Action = CharacterDoFunctions.DoHeightenedAwareness, Info = "Become more aware of the invisible.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "strangle", Action = Combat.DoStrangle, Info = "Attempt to strangle someone putting them to sleep.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "sap", Action = Combat.DoSap, Info = "Attempt to sap someone to sleep.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "throw", Action = Combat.DoThrow, Info = "Attempt to throw someone to the ground.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "kotegaeshi", Action = Combat.DoKotegaeshi, Info = "Attempt to break someone's wrists.", MinimumPosition = Positions.Fighting});
            Commands.Add(new Command { Name = "kansetsuwaza", Action = Combat.DoKansetsuwaza, Info = "Attempt to elbow lock your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "vanish", Action = Combat.DoVanish, Info = "Attempt to vanish, appearing somewhere else in the area.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "endure", Action = Combat.DoEndure, Info = "Improves save vs spell and armor class.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "owaza", Action = Combat.DoOwaza, Info = "Attempts a series of special attacks", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "earclap", Action = Combat.DoEarClap, Info = "Deafen someone with a powerful clap to their ears.", MinimumPosition = Positions.Fighting });

            Commands.Add(new Command { Name = "pierce", Action = Combat.DoPierce, Info = "Pierce your enemy with a spear.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "thrust", Action = Combat.DoThrust, Info = "Attempt to knock your enemy back with a powerful thrust of a spear or polearm.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "headsmash", Action = Combat.DoHeadSmash, Info = "Attempt to smash your enemy's head after a successful feint with a staff or polearm.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "slice", Action = Combat.DoSlice, Info = "Attempt to slice open your enemy with a polearm.", MinimumPosition = Positions.Fighting });
            
            Commands.Add(new Command { Name = "whirl", Action = Combat.WarriorSpecializationSkills.DoWhirl, Info = "Attempt to whirl an axe at your enemy.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "backhand", Action = Combat.WarriorSpecializationSkills.DoBackhand, Info = "Attempt to backhand your enemy.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "sting", Action = Combat.WarriorSpecializationSkills.DoSting, Info = "Attempt to sting your enemy with a whip.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "bludgeon", Action = Combat.WarriorSpecializationSkills.DoBludgeon, Info = "Attempt to bludgeon your enemy with a flail.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "legsweep", Action = Combat.WarriorSpecializationSkills.DoLegsweep, Info = "Attempt to sweep the legs out from under your enemy.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "boneshatter", Action = Combat.WarriorSpecializationSkills.DoBoneshatter, Info = "Attempt to shatter your enemies bones.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "crossdownparry", Action = Combat.WarriorSpecializationSkills.DoCrossDownParry, Info = "Attempt to cross down parry someone with swords.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "pummel", Action = Combat.WarriorSpecializationSkills.DoPummel, Info = "Attempt to unleash a series of punches on someone.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "vitalarea", Action = Combat.WarriorSpecializationSkills.DoVitalArea, Info = "Attempt to strike an opponents vital areas reducing their strength and dexteriy.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "doublethrust", Action = Combat.WarriorSpecializationSkills.DoDoubleThrust, Info = "Attempt to strike an enemy with both of your swords.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "jab", Action = Combat.WarriorSpecializationSkills.DoJab, Info = "Attempt to jab an opponent with your sword.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "chop", Action = Combat.WarriorSpecializationSkills.DoChop, Info = "Attempt to chop an opponent with your polearm.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "disembowel", Action = Combat.WarriorSpecializationSkills.DoDisembowel, Info = "Attempt to disembowel an opponent with your axe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "crescentstrike", Action = Combat.WarriorSpecializationSkills.DoCrescentStrike, Info = "Strike an oponent with a crescent arc of your spear.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "disembowel", Action = Combat.WarriorSpecializationSkills.DoOverhead, Info = "Swing your axe overhead at a foe.", MinimumPosition = Positions.Fighting });
            
            Commands.Add(new Command { Name = "pincer", Action = Combat.WarriorSpecializationSkills.DoPincer, Info = "Attempt to pincer an opponent between your axes.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "underhandstab", Action = Combat.WarriorSpecializationSkills.DoUnderhandStab, Info = "Attempt underhand stab with your dagger.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "leveragekick", Action = Combat.WarriorSpecializationSkills.DoLeverageKick, Info = "Attempt to kick a foe, using your staff or spear for leverage.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "cranial", Action = Combat.WarriorSpecializationSkills.DoCranial, Info = "Attempt to strike a foe on the head with a mace.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "entrapweapon", Action = Combat.WarriorSpecializationSkills.DoEntrapWeapon, Info = "Attempt to entrap a foe's weapon, disarming them.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "stripweapon", Action = Combat.WarriorSpecializationSkills.DoStripWeapon, Info = "Attempt to strip a foes weapon, disarming them.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "hookweapon", Action = Combat.WarriorSpecializationSkills.DoHookWeapon, Info = "Attempt to hook a foes weapon with your axe, disarming them.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "weaponbreaker", Action = Combat.WarriorSpecializationSkills.DoWeaponBreaker, Info = "Attempt to break your foes weapon with your axe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "dent", Action = Combat.WarriorSpecializationSkills.DoDent, Info = "Attempt to break a foes armor with your mace.", MinimumPosition = Positions.Fighting });

            Commands.Add(new Command { Name = "blindnessdust", Action = Combat.DoBlindnessDust, Info = "Throw blindness dust: room affect.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "pepperdust", Action = Combat.DoPepperDust, Info = "Throw pepper dust, stinging and blinding: room affect.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "blisteragent", Action = Combat.DoBlisterAgent, Info = "Throw blister agent, causing bleeding, and reducing str and agi: room affect.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "stenchcloud", Action = Combat.DoStenchCloud, Info = "Generate a stench cloud, causing damage and revealing most people: room affect.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "poisondagger", Action = Combat.DoPoisonDagger, Info = "Attempt to create a poisoned dagger.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "poisondust", Action = Combat.DoPoisonDust, Info = "Throw poisonous dust: room affect.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "butcher", Action = CharacterDoFunctions.DoButcher, Info = "Butchers a corpse into an edible steak.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "owlkinship", Action = Combat.DoOwlKinship, Info = "Summon an owl to assist you in combat.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "wolfkinship", Action = Combat.DoWolfKinship, Info = "Summon a wolf to assist you in combat.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "serpentkinship", Action = Combat.DoSerpentKinship, Info = "Summon a serpent to assist you in combat.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "bearkinship", Action = Combat.DoBearKinship, Info = "Summon a bear to assist you in combat.", MinimumPosition = Positions.Fighting });

            Commands.Add(new Command { Name = "forms", Action = DoActInfo.DoForms, Info = "Display the forms you are familiar with.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "shapefocus", Action = ShapeshiftForm.DoShapeFocus, Info = "Set your major and minor shapeshift specializations.", MinimumPosition = Positions.Dead });

            Commands.Add(new Command { Name = "bite", Action = Combat.DoBite, Info = "Attempt to bite an opponent.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "puncturingbite", Action = Combat.DoPuncturingBite, Info = "Puncture an opponents skin, a causes bleeding, and blood feeding heals, fills your hunger and thirst.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "peck", Action = Combat.DoPeck, Info = "Attempt to peck an opponent.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "waspsting", Action = Combat.DoWaspSting, Info = "Attempt to sting an opponent.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "claw", Action = Combat.DoClaw, Info = "Attempt to claw an opponent.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "furor", Action = Combat.DoFuror, Info = "Unleash a series of vicious attacks at an opponent.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "trample", Action = Combat.DoTrample, Info = "Charge and trample an opponent.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "forage", Action = CharacterDoFunctions.DoForage, Info = "Forage around for food.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "jump", Action = Combat.DoJump, Info = "Jump at someone damaging them.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "sprint", Action = CharacterDoFunctions.DoSprint, Info = "Sprint in a direction.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "strike", Action = Combat.DoStrike, Info = "Coil and strike someone.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "pinch", Action = Combat.DoPinch, Info = "Pinch an enemy with your powerful pincers.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "mulekick", Action = Combat.DoAssassinMuleKick, Info = "Unleash a powerful kick on your second enemy.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "hoofstrike", Action = Combat.DoHoofStrike, Info = "Unleash a double kick with your hooves on your second enemy.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "antlerswipe", Action = Combat.DoAntlerSwipe, Info = "Swing your antlers inflicting a devasting blow.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "tailswipe", Action = Combat.DoTailSwipe, Info = "Swing your tail swiping an enemy off of their feet.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "rip", Action = Combat.DoRip, Info = "Rip your enemy up.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "talonstrike", Action = Combat.DoTalonStrike, Info = "Strike your enemy with powerful talons, causing bleeding and poison.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "swipe", Action = Combat.DoSwipe, Info = "Swipe at your enemy.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "devour", Action = Combat.DoDevour, Info = "Devour your enemy.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "camouflage", Action = CharacterDoFunctions.DoCamouflage, Info = "Camouflage in the wilderness.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "burrow", Action = CharacterDoFunctions.DoBurrow, Info = "Create a burrow to hide within.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "playdead", Action = CharacterDoFunctions.DoPlayDead, Info = "Attempt to fool your enemies into thinking you are dead.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "impale", Action = Combat.DoImpale, Info = "Impale your enemy, causing them to bleed.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "flank", Action = Combat.DoFlank, Info = "Flank your enemy, injuring them.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "ambush", Action = Combat.DoAmbush, Info = "Ambush your enemy.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "charge", Action = Combat.DoCharge, Info = "Charge your enemy.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "lancecharge", Action = Combat.DoLanceCharge, Info = "Charge your enemy.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "sabrecharge", Action = Combat.DoSabreCharge, Info = "Charge your enemy with your sword.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "crushingcharge", Action = Combat.DoCrushingCharge, Info = "Charge your enemy with your mace.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "headbutt", Action = Combat.DoHeadbutt, Info = "Headbutt your enemy, injuring them and you.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "lickself", Action = Combat.DoLickSelf, Info = "Lick your wounds.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "retract", Action = Combat.DoRetract, Info = "Tuck in for protection.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "tuskjab", Action = Combat.DoTuskJab, Info = "Jab someone with your tusks.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "hoofstomp", Action = Combat.DoHoofStomp, Info = "Stomp someone with your hooves.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "dive", Action = Combat.DoDive, Info = "Dive on someone from a ledge or tree.", MinimumPosition = Positions.Standing });
            Commands.Add(new Command { Name = "pounceattack", Action = Combat.DoPounceAttack, Info = "Pounce on your prey.", MinimumPosition = Positions.Fighting});
            Commands.Add(new Command { Name = "howl", Action = Combat.DoHowl, Info = "Unleash a deafening howl.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "quillspray", Action = Combat.DoQuillSpray, Info = "Spray quills everywhere.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "chestpound", Action = Combat.DoChestPound, Info = "Pound your chest, becoming enraged.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "laugh", Action = Combat.DoLaugh, Info = "Let loose a maniacal laugh.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "noxiousspray", Action = Combat.DoNoxiousSpray, Info = "Spray noxious chemicals everywhere.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "venomspit", Action = Combat.DoVenomSpit, Info = "Spit poisonous venom.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "shootblood", Action = Combat.DoShootBlood, Info = "Shoot blood at your enemy's eyes.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "spit", Action = Combat.DoSpit, Info = "Spit saliva and partially digested food at your enemy's eyes", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "zigzag", Action = Combat.DoZigzag, Info = "Perform a zigzagging motion to distract your enemies next attack.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "flight", Action = Combat.DoFlight, Info = "Use your wings to fly for a short time.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "glandspray", Action = Combat.DoGlandSpray, Info = "Spray someone with your stink glands.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "venomstrike", Action = Combat.DoVenomStrike, Info = "Strike someone with subduing venom.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "venomoussting", Action = Combat.DoVenomousSting, Info = "Sting someone with poison.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "secretefilament", Action = Combat.DoSecreteFilament, Info = "Spray your enemy with a secreted filament.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "acidexcrete", Action = Combat.DoAcidExcrete, Info = "Direct a stream of acetic acid at your foe.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "snarl", Action = Combat.DoSnarl, Info = "Lowers targets hitRoll by half for a short time.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "autotomy", Action = Combat.DoAutotomy, Info = "Detatch tail, rest or run while you can.", MinimumPosition = Positions.Fighting });

            Commands.Add(new Command { Name = "request", Action = DoActItem.DoRequest, Info = "Request an item from an NPC with a good alignment.", MinimumPosition = Positions.Resting });

            Commands.Add(new Command { Name = "toggle", Action = DoActInfo.DoToggle, Info = "List or Toggle an act flag on yourself.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "bug", Action = Game.DoBug, Info = "Report a bug or typo.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "map", Action = DoActMapper.DoMap, Info = "Display an ascii map of your current surroundings.", MinimumPosition = Positions.Dead });

            Commands.Add(new Command { Name = "duelchallenge", Action = Dueling.DoIssueDuelChallenge, Info = "Issue a duel challenge to another player.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "duelaccept", Action = Dueling.DoDuelAccept, Info = "Accept a duel challenge from another player.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "duelcancel", Action = Dueling.DoDuelDecline, Info = "Decline a duel challenge from another player.", MinimumPosition = Positions.Resting });
            Commands.Add(new Command { Name = "afk", Action = DoActInfo.DoAFK, Info = "Toggle your AFK flag.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "exits", Action = DoActInfo.DoExits, Info = "List current obvious exits.", MinimumPosition = Positions.Resting});
            Commands.Add(new Command { Name = "replay", Action = DoActCommunication.DoReplay, Info = "Play back the last 20 communications.", MinimumPosition = Positions.Dead });

            // IMM COMMANDS
            Commands.Add(new Command { Name = "immortal", Action = DoActWizard.DoImmortal, Info = "Chat with other immortals", MinimumPosition = Positions.Dead, MinimumLevel = 52 });
            Commands.Add(new Command { Name = "holylight", Action = DoActWizard.DoHolyLight, Info = "View immortal stuff", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "wizinvis", Action = DoActWizard.DoWizInvis, Info = "Make yourself invisible.", MinimumPosition = Positions.Dead });

            Commands.Add(new Command { Name = "goto", Action = DoActWizard.DoGoto, Info = "Teleport to the specified room.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "restore", Action = DoActWizard.DoRestore, Info = "Restore the hitpoints, mana and movement of a CharacterDoFunctions.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "reset", Action = DoActWizard.DoResetArea, Info = "Reset an area.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "shutdown", Action = DoActWizard.DoShutdown, Info = "Shutdown the server", MinimumPosition = Positions.Dead, MinimumLevel = 60 });


            Commands.Add(new Command { Name = "load", Action = DoActWizard.DoLoad, Info = "Load an object or mob into the room.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "stat", Action = DoActWizard.DoStat, Info = "List details of a room, mobile or object.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "set", Action = DoActWizard.DoSet, Info = "Set instance details of a mobile or object.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "string", Action = DoActWizard.DoString, Info = "Set instance strings of a mobile or object.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "slay", Action = DoActWizard.DoSlay, Info = "Instantly kill something.", MinimumPosition = Positions.Dead, MinimumLevel = 1 });
            Commands.Add(new Command { Name = "enumerate", Action = DoActWizard.DoEnumerate, Info = "List all areas, rooms, npcs or items.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "transfer", Action = DoActWizard.DoTransfer, Info = "Transfer a character to a location.", MinimumPosition = Positions.Dead });

            Commands.Add(new Command { Name = "startquest", Action = DoActWizard.DoStartQuest, Info = "Set progress of a quest to inprogress.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "completequest", Action = DoActWizard.DoCompleteQuest, Info = "Set progress of a quest to complete.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "failquest", Action = DoActWizard.DoFailQuest, Info = "Set progress of a quest to failed.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "disablequest", Action = DoActWizard.DoDisableQuest, Info = "Set progress of a quest to disabled.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "resetquest", Action = DoActWizard.DoResetQuest, Info = "Set progress of a quest to inprogress.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "dropquest", Action = DoActWizard.DoDropQuest, Info = "Set progress of a quest to none.", MinimumPosition = Positions.Dead });

            Commands.Add(new Command { Name = "reboot", Action = DoActWizard.DoReboot, Info = "Restart the server.", MinimumPosition = Positions.Dead, MinimumLevel = 60 });
            Commands.Add(new Command { Name = "wiznet", Action = WizardNet.DoWiznet, Info = "Enable wiznet to monitor events.", MinimumPosition = Positions.Dead, MinimumLevel = 52 });

            // OLC
            Commands.Add(new Command { Name = "asaveworld", Action = AreaData.DoASaveWorlds, Info = "Save unsaved areas.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "saveworld", Action = AreaData.DoASaveWorlds, Info = "Save unsaved areas.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "worldsave", Action = AreaData.DoASaveWorlds, Info = "Save unsaved areas.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "builders", Action = OLC.DoBuilder, Info = "Assign builders to an area.", MinimumPosition = Positions.Dead, MinimumLevel = 60 });
            Commands.Add(new Command { Name = "dig", Action = OLC.DoDig, Info = "Create a new exit or room.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "purge", Action = DoActWizard.DoPurge, Info = "Purge NPCs and items in a room.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "edit", Action = OLC.DoEdit, Info = "Edit a room, item or npc", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "aedit", Action = OLC.DoAEdit, Info = "Edit an area", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "redit", Action = OLC.DoREdit, Info = "Edit a room", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "medit", Action = OLC.DoMEdit, Info = "Edit an npc", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "oedit", Action = OLC.DoOEdit, Info = "Edit an item", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "hedit", Action = OLC.DoHEdit, Info = "Edit an item", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "create", Action = OLC.DoCreate, Info = "Create an area, item or npc", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "renumber", Action = OLC.DoRenumber, Info = "Renumber area, rooms, items and NPCs in the given range of vnums", MinimumPosition = Positions.Dead, MinimumLevel = 60 });
            Commands.Add(new Command { Name = "nextvnum", Action = OLC.DoNextVnum, Info = "Show the next vnum for rooms, npcs and items.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "freevnumranges", Action = OLC.DoFreeVnumRanges, Info = "Show the ranges of vnums not being used by areas.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "areaconnections", Action = DoActWizard.DoAreaConnections, Info = "List areas this area links to.", MinimumPosition = Positions.Dead });
            // END OLC
            Commands.Add(new Command { Name = "advance", Action = DoActWizard.DoAdvance, Info = "Set a player's level.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "flags", Action = DoActWizard.DoFlags, Info = "Set an instance of an item, mobile or player's flags.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "title", Action = DoActWizard.DoTitle, Info = "Set a player's title.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "force", Action = DoActWizard.DoForce, Info = "Force someone to execute a command.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "switch", Action = DoActWizard.DoSwitch, Info = "Switch into an NPC.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "return", Action = DoActWizard.DoReturn, Info = "Return from a switch command.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "snoop", Action = DoActWizard.DoSnoop, Info = "Watch a players input and output.", MinimumPosition = Positions.Dead });

            Commands.Add(new Command { Name = "extitle", Action = DoActWizard.DoExtendedTitle, Info = "Set a player's extended title.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "extendedtitle", Action = DoActWizard.DoExtendedTitle, Info = "Set a player's extended title.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "stripaffects", Action = DoActWizard.DoStripAffects, Info = "Strip your affects.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "forcetick", Action = DoActWizard.DoForceTick, Info = "Force a tick update to happen.", MinimumPosition = Positions.Dead });
            Commands.Add(new Command { Name = "setplayerpassword", Action = DoActWizard.DoSetPlayerPassword, Info = "Set another player's password.", MinimumPosition = Positions.Dead, MinimumLevel = 60 });

            Commands.Add(new Command { Name = "connections", Action = Game.DoConnections, Info = "See connections to the mud.", MinimumPosition = Positions.Dead, MinimumLevel = 59 });
            Commands.Add(new Command { Name = "banbyname", Action = Game.DoBanByName, Info = "Ban a player name, connected or not.", MinimumPosition = Positions.Dead, MinimumLevel = 59 });
            Commands.Add(new Command { Name = "banbyaddress", Action = Game.DoBanByAddress, Info = "Ban a player by specifying their name if they are online or their ipaddress.", MinimumPosition = Positions.Dead, MinimumLevel = 59 });

            Commands.Add(new Command { Name = "gecho", Action = CharacterDoFunctions.DoGlobalEcho, Info = "Echo a message to the world.", MinimumPosition = Positions.Dead, MinimumLevel = 54 });
            Commands.Add(new Command { Name = "globalecho", Action = CharacterDoFunctions.DoGlobalEcho, Info = "Echo a message to the world.", MinimumPosition = Positions.Dead, MinimumLevel = 54 });
            Commands.Add(new Command { Name = "aecho", Action = CharacterDoFunctions.DoAreaEcho, Info = "Echo a message to the area.", MinimumPosition = Positions.Dead, MinimumLevel = 54 });
            Commands.Add(new Command { Name = "areaecho", Action = CharacterDoFunctions.DoAreaEcho, Info = "Echo a message to the area.", MinimumPosition = Positions.Dead, MinimumLevel = 54 });
            Commands.Add(new Command { Name = "echo", Action = CharacterDoFunctions.DoEcho, Info = "Echo a message to the room.", MinimumPosition = Positions.Dead, MinimumLevel = 54 });



            // END IMM COMMANDS

            Commands.Add(new Command { Name = "suicide", Action = CharacterDoFunctions.DoSuicide, Info = "End your life.", MinimumPosition = Positions.Fighting });
            Commands.Add(new Command { Name = "password", Action = CharacterDoFunctions.DoPassword, Info = "Change your password.", MinimumPosition = Positions.Sleeping });
            Commands.Add(new Command { Name = "delete", Action = CharacterDoFunctions.DoDelete, Info = "Delete your character.", MinimumPosition = Positions.Sleeping });

        }

        internal static void LinkCommandSkills()
        {
           foreach(var command in Commands)
            {
                foreach(var skill in SkillSpell.Skills.Values)
                {
                    if(skill.name.Replace(" ", "").StringCmp(command.Name) && skill.SkillTypes.ISSET(SkillSpellTypes.Skill) && skill.spellFun == null)
                    {
                        command.Skill = skill;
                        break;
                    }
                }
            }
        }
    }
}
