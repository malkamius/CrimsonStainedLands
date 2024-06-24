using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Collections.ObjectModel;

namespace CrimsonStainedLands
{
    public class AreaData
    {
        public static ObservableCollection<AreaData> Areas = new ObservableCollection<AreaData>();
        public string Name;
        public bool saved = true;
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// level summary
        /// </summary>
        public string info { get; set; } = string.Empty;

        public int VNumStart { get; set; }
        public int VNumEnd { get; set; }
        public string Builders { get; set; }
        public int Security { get; set; }
        public string Credits { get; set; }
        public short Age { get; set; }
        public int OverRoomVnum { get; set; }
        int LastPeopleCount = 0;
        public Dictionary<int, NPCTemplateData> NPCTemplates { get; set; }  = new Dictionary<int, NPCTemplateData>();
        public Dictionary<int, ItemTemplateData> ItemTemplates { get; set; } = new Dictionary<int, ItemTemplateData>();
        public List<HelpData> Helps { get; set; } = new List<HelpData>();

        public Dictionary<int, RoomData> Rooms { get; set; } = new Dictionary<int, RoomData>();
        public List<ResetData> Resets { get; set; } = new List<ResetData>();

        public List<Character> People = new List<Character>();
        public int Timer;
        public Dictionary<int, Quest> Quests { get; set; } = new Dictionary<int, Quest>();
        
        public static void LoadAreas(bool headersOnly = false)
        {
            DateTime loadstart = DateTime.Now;
            var tasks = new List<Task>();
            /// Now load area programs before area npcs and rooms, things referencing programs
            Directory.GetFiles(Settings.AreasPath, "*.xml").
                Concat(Directory.GetFiles(Settings.AreasPath, "*.json")).
                Where(path => 
                !path.EndsWith("_programs.xml", StringComparison.InvariantCultureIgnoreCase) && 
                !path.EndsWith("_programs.json", StringComparison.InvariantCultureIgnoreCase)
                ).AsParallel().ForAll(file =>
            {
                if (file.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    AreaData area = new AreaData();
                    if (headersOnly)
                        area.LoadHeader(file);
                    else
                        area.Load(file);
                } 
                else
                {
                    string jsonString = File.ReadAllText(file);
                    AreaData area = JsonSerializer.Deserialize<AreaData>(jsonString);
                    Areas.Add(area);
                }
            });

            Game.log("Loaded areas in {0}", DateTime.Now - loadstart);

            if (!headersOnly)
                FixExits();

            Game.log("Loaded " + Areas.Count + " areas.");

            //foreach (var file in Directory.GetFiles("data\\updates", "*.xml"))
            //{
            //    var itemupdates = XElement.Load(file);

            //    foreach (var item in itemupdates.Elements("Item"))
            //    {
            //        var vnum = item.GetAttributeValueInt("Vnum");
            //        var silver = item.GetAttributeValueInt("Silver");
            //        if (vnum != 0 && ItemTemplateData.Templates.TryGetValue(vnum, out var template) && silver != 0)
            //        {
            //            template.Gold = silver / 1000;
            //            template.Silver = silver % 1000;
            //        }
            //    }
            //}

            //foreach (var file in Directory.GetFiles("data\\updates", "*.xml"))
            //{
            //    var itemupdates = XElement.Load(file);

            //    foreach(var item in itemupdates.Elements("Item"))
            //    {
            //        var vnum = item.GetAttributeValueInt("Vnum");

            //        if (vnum != 0 && ItemTemplateData.Templates.TryGetValue(vnum, out var template))
            //        {
            //            if (item.HasElement("Spells"))
            //            {
            //                var spellsElement = item.GetElement("Spells");
            //                foreach (var spellElement in spellsElement.Elements("Spell"))
            //                {
            //                    template.spells.Add(new ItemSpellData(spellElement));
            //                    template.Area.saved = false;
            //                }
            //            }
            //        }
            //    }
            //}

            //foreach (var file in Directory.GetFiles("data\\updates", "*.xml"))
            //{
            //    var updates = XElement.Load(file);

            //    foreach(var npcelement in updates.Elements("NPC"))
            //    {
            //        var vnum = npcelement.GetAttributeValueInt("vnum");
            //        var alignment = Alignment.Neutral;
            //        if(utility.GetEnumValue(npcelement.GetAttributeValue("alignment", "neutral"), ref alignment) && NPCTemplateData.Templates.TryGetValue(vnum, out var npc) )
            //        {
            //            npc.Alignment = alignment;
            //            npc.Area.saved = false;
            //        }
            //    }
            //}

        }

        private static void FixExits()
        {
            foreach (var area in Areas)
            {
                foreach (var room in area.Rooms.Values)
                {
                    for(var index = 0; index < room.exits.Length; index++)
                    {
                        if (room.exits[index] != null)
                        {
                            var exit = room.exits[index];
                            RoomData.Rooms.TryGetValue(exit.destinationVnum, out exit.destination);
                            exit.source = room;
                            if(room.OriginalExits[index] != null && room.OriginalExits[index].destinationVnum == exit.destinationVnum)
                            {
                                var oexit = room.OriginalExits[index];
                                oexit.source = room;
                                oexit.destination = exit.destination;
                            }
                            
                        }
                    }
                    //foreach (var exit in room.exits)
                    //{
                    //    if (exit != null)
                    //    {
                    //        RoomData.Rooms.TryGetValue(exit.destinationVnum, out exit.destination);
                    //        exit.source = room;
                    //    }
                    //}
                }
            }
        }

        public static void ResetAreas()
        {
            foreach (var area in Areas)
            {
                area.ResetArea();
            }
        }

        private void RandomizeExits()
        {
            foreach(var room in Rooms.Values)
            {
                if(room.flags.ISSET(RoomFlags.RandomExits))
                {
                    var exits = room.exits.ToArray();

                    for(int i = 0; i < exits.Length; i++)
                    {
                        var exit = room.exits[i];
                        if (exit != null && !exit.flags.ISSET(ExitFlags.NoRandomize))
                        {
                            var otherexit = room.exits.Where(e => e != null && e != exit && !e.flags.ISSET(ExitFlags.NoRandomize)).SelectRandom();

                            if (otherexit != null)
                            {
                                var otheri = Array.IndexOf(room.exits, otherexit);
                                room.exits[i] = otherexit;
                                room.exits[otheri] = exit;

                                Game.log("Switched {0} with {1} in {2}.", Enum.GetName(typeof(Direction), i), Enum.GetName(typeof(Direction), otheri), room.Vnum);
                            }
                        }
                    }
                }
            }
        }

        public AreaData(string file, bool headerOnly = false)
        {
            if (!headerOnly)
                Load(file);
            else
                LoadHeader(file);
        }

        public AreaData()
        {

        }

        public void LoadHeader(string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                FileName = file;

                XElement root = XElement.Load(file, LoadOptions.PreserveWhitespace);

                if (root.HasElement("AreaData"))
                {

                    var areaData = root.Elements("AreaData").FirstOrDefault();

                    Name = areaData.GetElementValue("Name");

                    this.VNumStart = areaData.GetElementValueInt("vnumStart");
                    this.VNumEnd = areaData.GetElementValueInt("vnumEnd");

                    info = areaData.GetElementValue("Info");
                    Builders = areaData.GetElementValue("Builders");
                    Security = areaData.GetElementValueInt("Security");
                    Credits = areaData.GetElementValue("Credits");
                    OverRoomVnum = areaData.GetElementValueInt("OverRoomVnum");
                    if (Name.ISEMPTY())
                        Game.log("Area " + FileName + "has no name");
                    if (this.VNumStart == 0)
                        Game.log("Area " + Name + " has no start vnum");
                    if (this.VNumEnd == 0)
                        Game.log("Area " + Name + " has no end vnum");

                }
            }
            AreaData.Areas.Add(this);
        }

        /// <summary>
        /// Load/reload an area file
        /// </summary>
        /// <param name="file"></param>
        public void Load(string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                FileName = file;
                  
                XElement root = XElement.Load(file, LoadOptions.PreserveWhitespace);
                
                if (root.HasElement("AreaData"))
                {

                    var areaData = root.Elements("AreaData").FirstOrDefault();

                    Name = areaData.GetElementValue("Name");

                    this.VNumStart = areaData.GetElementValueInt("vnumStart");
                    this.VNumEnd = areaData.GetElementValueInt("vnumEnd");

                    info = areaData.GetElementValue("Info");
                    Builders = areaData.GetElementValue("Builders");
                    Security = areaData.GetElementValueInt("Security");
                    Credits = areaData.GetElementValue("Credits");
                    OverRoomVnum = areaData.GetElementValueInt("OverRoomVnum");
                    if (Name.ISEMPTY())
                        Game.log("Area " + FileName + "has no name");
                    if (this.VNumStart == 0)
                        Game.log("Area " + Name + " has no start vnum");
                    if (this.VNumEnd == 0)
                        Game.log("Area " + Name + " has no end vnum");

                }

                // Clear rooms for a reload
                foreach (var room in Rooms.ToArray())
                    RoomData.Rooms.TryRemove(room.Key, out _);
                Rooms.Clear();

                LoadRooms(root);

                // Clear and reload item templates
                foreach (var itemtemplate in ItemTemplates)
                    ItemTemplateData.Templates.TryRemove(itemtemplate.Key, out _);
                ItemTemplates.Clear();
                LoadItemTemplates(root);

                foreach (var npctemplate in NPCTemplates)
                    NPCTemplateData.Templates.TryRemove(npctemplate.Key, out _);
                NPCTemplates.Clear();
                LoadNPCTemplates(root);

                foreach (var help in Helps)
                    HelpData.Helps.Remove(help);
                Helps.Clear();
                LoadHelps(root);


                if(!AreaData.Areas.Contains(this))
                    AreaData.Areas.Add(this);

                Resets.Clear();
                
                LoadResets(root);

                LoadQuests(root);
                
                // Prepare area for reset
                Timer = 0;

                //// Reset everything
                //resetArea();
            }
        }

        public void LoadQuests(XElement root)
        {
            var quests = root.GetElement("Quests");

            if (quests != null)
            {
                Quest.LoadQuests(this, quests);
            }
        }

        /// <summary>
        /// Load help entries from an area element
        /// </summary>
        /// <param name="root"></param>
        public void LoadHelps(XElement root)
        {
            var items = root.GetElement("Helps");

            if (items != null)
            {
                foreach (var item in items.Elements())
                {
                    try
                    {
                        var newHelp = new HelpData(this, item);

                    }
                    catch (Exception ex)
                    {
                        Game.log(ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Load rooms from an area element
        /// </summary>
        /// <param name="root"></param>
        public void LoadRooms(XElement root)
        {
            var rooms = root.Elements("Rooms");

            if (rooms != null)
                foreach (var room in rooms.Elements())
                {
                    try
                    {
                        var newRoom = new RoomData(this, room);

                    }
                    catch (Exception ex)
                    {
                        Game.log(ex.ToString());
                    }


                }
        }

        /// <summary>
        /// Load NPC Templates from an area element
        /// </summary>
        /// <param name="root"></param>
        public void LoadNPCTemplates(XElement root)
        {
            var npcs = root.Elements("NPCs");

            if (npcs != null)
                foreach (var npc in npcs.Elements())
                {
                    try
                    {
                        var newNPC = new NPCTemplateData(this, npc);

                    }
                    catch (Exception ex)
                    {
                        Game.log(ex.ToString());
                    }
                }
        }

        /// <summary>
        /// Load item templates from an area element
        /// </summary>
        /// <param name="root"></param>
        public void LoadItemTemplates(XElement root)
        {
            var items = root.GetElement("Items");

            if (items != null)
            {
                foreach (var item in items.Elements())
                {
                    try
                    {
                        var newItemTemplate = new ItemTemplateData(this, item);

                    }
                    catch (Exception ex)
                    {
                        Game.log(ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Load resets from an area element
        /// </summary>
        /// <param name="root"></param>
        public void LoadResets(XElement root)
        {
            var resets = root.GetElement("Resets");
            if (resets != null || (root.Name == "Resets" && (resets = root) != null))
                foreach (var reset in resets.Elements())
                {
                    try
                    {
                        var newReset = new ResetData(this, reset);
                    }
                    catch (Exception ex)
                    {
                        Game.log(ex.ToString());
                    }

                }
        }

        /// <summary>
        /// Decrement reset timer. Execute resets for an area if timer has expired. 
        /// Reset door flags in an area.
        /// </summary>
        public void ResetArea(bool force = false)
        {
            if (force) Timer = 1;

            if (Timer > 0)
                Timer--;
            else
                Timer = 0;

            if (LastPeopleCount > 0 && this.People.Count == 0)
            {
                Timer = 3;// game.PULSE_AREA;
            }
            else if (People.Count == 0 && Timer > 3) //game.PULSE_AREA)
            {
                Timer = 3;
            }
            else if (LastPeopleCount == 0 && People.Count > 0)
            {
                Timer = 15; // game.PULSE_AREA * 5;
            }
            LastPeopleCount = this.People.Count;
            // RESET Every PULSE if empty, otherwise every 3 PULSEs
            if (Timer <= 0)// || (this.people.Count == 0 && timer <= (game.PULSE_AREA * 4)))
            {
                RandomizeExits();
                if (this.People.Count > 0)
                    Timer = 15;//  game.PULSE_AREA * 5;
                else
                    Timer = 3;// game.PULSE_AREA;
                //game.log("RESET AREA :: " + name);
                NPCData lastNPC = null;
                ItemData lastItem = null;

                foreach (var reset in Resets)
                {
                    reset.execute(ref lastNPC, ref lastItem);
                }

                foreach (var room in Rooms.Values)
                {
                    foreach (var exit in room.exits)
                    {
                        if (exit != null)
                        {
                            exit.flags.Clear();
                            exit.flags.AddRange(exit.originalFlags);
                        }
                    }

                    foreach (var item in room.items)
                    {
                        if (!item.wearFlags.ISSET(WearFlags.Take) && item.Template != null && item.Template.extraFlags.ISSET(ExtraFlags.Closed) && !item.extraFlags.ISSET(ExtraFlags.Closed))
                        {
                            item.extraFlags.SETBIT(ExtraFlags.Closed);
                        }
                    }
                }

            }
        } // end reset area

        /// <summary>
        /// work in progress for olc
        /// </summary>
        [JsonIgnore]
        public XElement Element
        {
            get
            {
                return new XElement("AreaData",
                    new XElement("Name", Name),
                    new XElement("Info", info),
                    new XElement("VnumStart", VNumStart),
                    new XElement("VnumEnd", VNumEnd),
                    new XElement("Builders", Builders),
                    new XElement("Security", Security),
                    new XElement("Credits", Credits),
                    new XElement("OverRoomVnum", OverRoomVnum)
                    );
            }
        } // end Element

        public void Save()
        {
            if (!FileName.ISEMPTY())
            {
                var element = new XElement("Area");
                element.Add(Element);
                element.Add(new XElement("Rooms", from room in Rooms select room.Value.Element));
                element.Add(new XElement("NPCs", from npc in NPCTemplates select npc.Value.NPCTemplateElement));
                element.Add(new XElement("Items", from item in ItemTemplates select item.Value.Element));
                element.Add(new XElement("Resets", from reset in Resets select reset.Element));
                if (Helps.Count > 0)
                {
                    element.Add(new XElement("Helps", from help in Helps select help.Element));
                }
                if (Quests.Count > 0)
                {
                    element.Add(new XElement("Quests", from quest in Quests.Values select quest.Element));
                }
                element.Save(FileName);
                saved = true;
            }
        }

        /// <summary>
        /// JSON doesn't support strings with new lines unescaped, big reason not to continue working on this...
        /// Can't very easily use a text editor to change room descriptions etc
        /// </summary>
        public void SaveToJson()
        {
            var jsonpath = Settings.AreasPath + "_json";
            System.IO.Directory.CreateDirectory(jsonpath);
            if (!string.IsNullOrEmpty(FileName))
            {
                var jsonname = System.IO.Path.GetFileNameWithoutExtension(FileName);
                var fullfilepath = System.IO.Path.Join(jsonpath, jsonname + ".json");
                
                string jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve });
                File.WriteAllText(fullfilepath, jsonString);
                saved = true;
            }
        }

        public static void DoASaveWorlds(Character ch, string arguments)
        {
            int areaCount = 0;
            foreach (var area in Areas)
            {
                if (!area.saved || arguments.StringCmp("world"))
                {
                    area.Save();
                    areaCount++;
                }
            }
            if (ch != null)
                ch.send("World Saved - {0} areas affected.\n\r", areaCount);
        }
    }
}