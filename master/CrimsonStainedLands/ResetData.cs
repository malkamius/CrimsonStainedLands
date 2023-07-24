using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public enum ResetTypes
    {
        NPC,
        Equip,
        Give,
        Item,
        Put,
        EquipRandom
    }
    public class ResetData
    {
        public AreaData area;
        public ResetTypes resetType;
        public int spawnVnum;
        public string spawnVnums;
        public int roomVnum;
        public int count;
        public int maxCount;

        private static int _lastNPCVnum;
        private static int _lastItemVnum;

        private int lastNPCVnum;
        private int lastItemVnum;


        public ResetData()
        {

        }

        public ResetData(AreaData area, ResetTypes resetType, int spawnVnum, int roomVnum, int count, int maxCount, string spawnVnums = "")
        {
            this.area = area;
            this.resetType = resetType;
            if (this.resetType == ResetTypes.NPC || this.resetType == ResetTypes.Item)
            {
                this.roomVnum = roomVnum;
                this.count = count;
                this.maxCount = Math.Max(1, maxCount);
            }
            this.spawnVnum = spawnVnum;
            this.spawnVnums = spawnVnums;
            if (resetType == ResetTypes.NPC)
            {
                _lastNPCVnum = spawnVnum;

                if (NPCTemplateData.Templates.TryGetValue(spawnVnum, out var template))
                {
                    template.MaxCount++;
                }
            }
            else if (resetType == ResetTypes.Item)
                _lastItemVnum = spawnVnum;
            else if (resetType == ResetTypes.Equip || resetType == ResetTypes.Give || resetType == ResetTypes.EquipRandom)
                lastNPCVnum = _lastNPCVnum;
            else if (resetType == ResetTypes.Put)
                lastItemVnum = _lastItemVnum;

            area.resets.Add(this);
        }
        public ResetData(AreaData area, XElement reset)
        {
            this.area = area;
            Extensions.Utility.GetEnumValue<ResetTypes>(reset.GetElementValue("type"), ref resetType);

            if (resetType != ResetTypes.EquipRandom)
                spawnVnum = reset.GetElementValueInt("vnum");
            else
                spawnVnums = reset.GetElementValue("vnum");

            if (resetType == ResetTypes.NPC || resetType == ResetTypes.Item)
            {
                roomVnum = reset.GetElementValueInt("destination");
                count = reset.GetElementValueInt("count");
                maxCount = Math.Max(reset.GetElementValueInt("max"), 1);

                if (resetType == ResetTypes.NPC && NPCTemplateData.Templates.TryGetValue(spawnVnum, out var template))
                {
                    template.MaxCount++;
                }

            }


            if (resetType == ResetTypes.NPC)
                _lastNPCVnum = spawnVnum;
            else if (resetType == ResetTypes.Item)
                _lastItemVnum = spawnVnum;
            else if (resetType == ResetTypes.Equip || resetType == ResetTypes.Give || resetType == ResetTypes.EquipRandom)
                lastNPCVnum = _lastNPCVnum;
            else if (resetType == ResetTypes.Put)
                lastItemVnum = _lastItemVnum;


            area.resets.Add(this);
        }

        internal void execute(ref NPCData lastNPC, ref ItemData lastItem)
        {
            if (resetType == ResetTypes.NPC)
            {
                NPCTemplateData template = null;
                if (RoomData.Rooms.TryGetValue(roomVnum, out RoomData room) && NPCTemplateData.Templates.TryGetValue(spawnVnum, out template))
                {
                    //for(spawnNum = 0; spawnNum < count; spawnNum++)
                    //{
                    //int globalcount = 0;
                    int roomcount = 0;
                    //foreach (var NPC in room.characters.OfType<NPCData>())
                    //foreach (var NPC in NPCData.NPCs)
                    //    if (NPC.vnum == spawnVnum)
                    //        globalcount++;
                    foreach (var NPC in room.Characters.OfType<NPCData>())
                    {
                        if (NPC.vnum == spawnVnum)
                            roomcount++;
                    }



                    if ((template.Count < maxCount || template.Count < template.MaxCount) && roomcount < this.count)
                    {
                        if (maxCount < template.MaxCount)
                        {
                            game.log("NPC " + spawnVnum + " has a maxcount of " + maxCount + " but template shows " + template.MaxCount + " resets" + (area != null ? " in area " + area.name + "." : ""));
                            // This will affect save world
                            //maxCount = template.MaxCount;
                        }
                        var newNPC = new NPCData(template, room);
                        lastNPC = newNPC;
                    }
                    else
                        lastNPC = null;
                }
                else if (room == null)
                    game.log("Room " + roomVnum + " not found for reset of NPC " + spawnVnum + (area != null ? " in area " + area.name + "." : ""));
                else if (template == null)
                    game.log("NPC " + spawnVnum + " not found for reset to room " + roomVnum + (area != null ? " in area " + area.name + "." : ""));
            }
            else if (resetType == ResetTypes.Item)
            {
                if (RoomData.Rooms.TryGetValue(roomVnum, out RoomData room) && ItemTemplateData.Templates.TryGetValue(spawnVnum, out ItemTemplateData template))
                {
                    int count = 0;

                    foreach (var Item in room.items)
                        if (Item.Vnum == spawnVnum)
                        {
                            Item.extraFlags.Clear();
                            Item.extraFlags.AddRange(template.extraFlags);
                            if (Item.ItemType.ISSET(ItemTypes.Container))
                                lastItem = Item;
                            count++;
                        }

                    if (count < maxCount || (maxCount == 0 && count == 0))
                    {

                        var newItem = new ItemData(template, room);
                        if (newItem.ItemType.ISSET(ItemTypes.Container))
                            lastItem = newItem;
                    }
                    //else
                    //lastItem = null;
                }
                else
                    game.log("Room " + roomVnum + " or Item " + spawnVnum + " not found for reset");
            }
            else if (resetType == ResetTypes.Put)
            {
                if (lastItem != null && ItemTemplateData.Templates.TryGetValue(spawnVnum, out ItemTemplateData template))
                {
                    int count = 0;

                    foreach (var Item in lastItem.Contains)
                        if (Item.Vnum == spawnVnum)
                        {
                            if (Item.ItemType.ISSET(ItemTypes.Container))
                                lastItem = Item;
                            count++;
                        }

                    if (count < maxCount || (maxCount == 0 && count == 0))
                    {

                        var newItem = new ItemData(template, lastItem);
                        if (newItem.ItemType.ISSET(ItemTypes.Container))
                            lastItem = newItem;
                    }
                }
                else if (lastItem != null)
                    game.log("Item " + spawnVnum + " not found for put reset");
            }
            else if (resetType == ResetTypes.Give)
            {
                if (lastNPC != null && ItemTemplateData.Templates.TryGetValue(spawnVnum, out ItemTemplateData template))
                {
                    int count = 0;

                    foreach (var Item in lastNPC.Inventory)
                        if (Item.Vnum == spawnVnum)
                            count++;

                    if (count < maxCount || maxCount == 0)
                    {

                        var newItem = new ItemData(template, lastNPC);
                    }
                }
                else if (lastNPC != null)
                    game.log("Item " + spawnVnum + " not found for give reset");
            }
            else if (resetType == ResetTypes.Equip)
            {
                if (lastNPC != null && ItemTemplateData.Templates.TryGetValue(spawnVnum, out ItemTemplateData template))
                {
                    int count = 0;

                    foreach (var Item in lastNPC.Inventory.Concat(lastNPC.Equipment.Values))
                        if (Item.Vnum == spawnVnum)
                            count++;

                    if (count < maxCount || maxCount == 0)
                    {

                        var newItem = new ItemData(template, lastNPC, true);
                        lastItem = newItem;
                    }
                }
                else if (lastNPC != null)
                    game.log("Item " + spawnVnum + " not found for give reset");
            }
            else if (resetType == ResetTypes.EquipRandom)
            {
                var spawnVnumsArr = spawnVnums.Split(' ');
                ;
                if (int.TryParse(spawnVnumsArr.SelectRandom(), out var spawnVnum)
                    && lastNPC != null
                    && ItemTemplateData.Templates.TryGetValue(spawnVnum, out ItemTemplateData template))
                {
                    int count = 0;

                    foreach (var Item in lastNPC.Inventory.Concat(lastNPC.Equipment.Values))
                        if (spawnVnumsArr.Contains(Item.Vnum.ToString()))
                            count++;

                    if (count < maxCount || maxCount == 0)
                    {

                        var newItem = new ItemData(template, lastNPC, true);
                        lastItem = newItem;
                    }
                }
                else if (lastNPC != null)
                    game.log("Item " + spawnVnum + " not found for give reset");
            }

        } // end execute

        public XElement Element
        {
            get
            {
                XComment comment = null;
                if (resetType == ResetTypes.NPC)
                {
                    if (NPCTemplateData.Templates.TryGetValue(spawnVnum, out var npc) && RoomData.Rooms.TryGetValue(roomVnum, out var room))
                    {
                        comment = new XComment(string.Format("Spawn {0} to {1}", !npc.ShortDescription.ISEMPTY() ? npc.ShortDescription : npc.Name, room.Name));
                        lastNPCVnum = npc.Vnum;
                    }
                }
                else if (resetType == ResetTypes.Item)
                {
                    if (ItemTemplateData.Templates.TryGetValue(spawnVnum, out var item) && RoomData.Rooms.TryGetValue(roomVnum, out var room))
                    {
                        comment = new XComment(string.Format("Spawn {0} to {1}", !item.ShortDescription.ISEMPTY() ? item.ShortDescription : item.Name, room.Name));
                        if(item.itemTypes.ISSET(ItemTypes.Container))
                        lastItemVnum = item.Vnum;
                    }
                }
                else if (resetType == ResetTypes.Give || resetType == ResetTypes.Put || resetType == ResetTypes.Equip)
                {
                    if (resetType != ResetTypes.Put && ItemTemplateData.Templates.TryGetValue(spawnVnum, out var item) && NPCTemplateData.Templates.TryGetValue(lastNPCVnum, out var npc))
                    {
                        comment = new XComment(string.Format("Spawn {0} to {1}", (!item.ShortDescription.ISEMPTY() ? item.ShortDescription : item.Name), !npc.ShortDescription.ISEMPTY() ? npc.ShortDescription : npc.Name));
                    }
                    else if (ItemTemplateData.Templates.TryGetValue(spawnVnum, out item))
                    {
                        comment = new XComment(string.Format("Spawn {0} to previous {1}", (!item.ShortDescription.ISEMPTY() ? item.ShortDescription : item.Name), (resetType == ResetTypes.Put ? "item" : "npc")));
                    }
                }
                else if (resetType == ResetTypes.EquipRandom)
                {
                    var items = new List<string>();
                    foreach (var vnum in from vnum in spawnVnums.Split(' ') select int.Parse(vnum))
                    {
                        if (ItemTemplateData.Templates.TryGetValue(vnum, out var item))
                        {
                            items.Add((!item.ShortDescription.ISEMPTY() ? item.ShortDescription : item.Name));
                        }
                    }
                    comment = new XComment(string.Format("Spawn {0} to previous {1}", "[" + string.Join("|", items) + "]", "npc"));
                }

                return new XElement("Reset",
                    comment,
                    new XElement("Type", resetType.ToString()),
                    new XElement("Destination", roomVnum),
                    new XElement("Count", count),
                    new XElement("Max", maxCount),
                    resetType != ResetTypes.EquipRandom ?
                    new XElement("Vnum", spawnVnum) :
                    new XElement("Vnum", spawnVnums)

                    );
            }
        }
    }
}
