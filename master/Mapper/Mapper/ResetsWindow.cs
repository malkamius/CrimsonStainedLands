using CrimsonStainedLands;
using CrimsonStainedLands.World;
using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CLSMapper
{
    public partial class ResetsWindow : Form
    {
        public AreaData Area;

        public List<ItemDisplay> Items = new List<ItemDisplay>();
        public List<NPCDisplay> NPCs = new List<NPCDisplay>();
        public List<RoomDisplay> Rooms = new List<RoomDisplay>();
        private bool save;

        public ResetsWindow(AreaData area)
        {
            InitializeComponent();
            this.Area = area;
        }

        private void ResetsWindow_Load(object sender, EventArgs e)
        {
            PopulateLists();
            PopulateResets();

        }

        private void PopulateLists()
        {
            ResetTypesCombo.Items.Clear();
            ResetTypesCombo.Items.AddRange((from f in Utility.GetEnumValues<ResetTypes>() select (object)f.ToString()).ToArray());
        }

        private void PopulateResets()
        {
            save = false;
            ResetsTreeView.Nodes.Clear();
            NPCTemplateData lastNPC = null;
            ItemTemplateData lastItem = null;
            foreach (var reset in Area.Resets)
            {
                TreeNode node;
                RoomData.Rooms.TryGetValue(reset.roomVnum, out var room);

                switch (reset.resetType)
                {
                    case ResetTypes.NPC:
                        {
                            if (Area.NPCTemplates.TryGetValue(reset.spawnVnum, out var npc))
                                ResetsTreeView.Nodes.Add(node = new TreeNode("NPC " + reset.spawnVnum + " " + npc.ShortDescription + " to " + reset.roomVnum + " " + (room != null ? room.Name : "unknown room")));
                            else
                                ResetsTreeView.Nodes.Add(node = new TreeNode("NPC " + reset.spawnVnum + " unknown to " + reset.roomVnum + " " + (room != null ? room.Name : "unknown room")));
                            lastNPC = npc;
                        }
                        node.Tag = reset;
                        break;
                    case ResetTypes.Item:
                        {
                            if (Area.ItemTemplates.TryGetValue(reset.spawnVnum, out var item))
                            {
                                ResetsTreeView.Nodes.Add(node = new TreeNode("ITEM " + reset.spawnVnum + " " + item.ShortDescription + " to " + reset.roomVnum + " " + (room != null ? room.Name : "unknown room")));
                                if (item.itemTypes.ISSET(ItemTypes.Container))
                                {
                                    lastItem = item;
                                }
                            }
                            else
                                ResetsTreeView.Nodes.Add(node = new TreeNode("ITEM " + reset.spawnVnum + " unknown to " + reset.roomVnum + " " + (room != null ? room.Name : "unknown room")));

                        }
                        node.Tag = reset;
                        break;
                    case ResetTypes.Equip:
                    case ResetTypes.Give:
                        {
                            if (Area.ItemTemplates.TryGetValue(reset.spawnVnum, out var item))
                            {
                                ResetsTreeView.Nodes.Add(node = new TreeNode(reset.resetType.ToString().ToUpper() + " " + reset.spawnVnum + " " + item.ShortDescription + " to " + (lastNPC != null ? lastNPC.ShortDescription : "unknown npc")));
                                if (item.itemTypes.ISSET(ItemTypes.Container))
                                {
                                    lastItem = item;
                                }
                            }
                            else
                                ResetsTreeView.Nodes.Add(node = new TreeNode(reset.resetType.ToString().ToUpper() + " " + reset.spawnVnum + " unknown to " + reset.roomVnum + " " + (lastNPC != null ? lastNPC.ShortDescription : "unknown npc")));

                        }
                        node.Tag = reset;
                        break;
                    case ResetTypes.Put:
                        {
                            if (Area.ItemTemplates.TryGetValue(reset.spawnVnum, out var item))
                            {
                                ResetsTreeView.Nodes.Add(node = new TreeNode("PUT " + reset.spawnVnum + " " + item.ShortDescription + " to " + (lastItem != null ? lastItem.ShortDescription : "unknown npc")));
                                if (item.itemTypes.ISSET(ItemTypes.Container))
                                {
                                    lastItem = item;
                                }
                            }
                            else
                                ResetsTreeView.Nodes.Add(node = new TreeNode("PUT " + reset.spawnVnum + " unknown to " + reset.roomVnum + " " + lastItem != null ? lastItem.ShortDescription : "unknown item"));
                        }
                        node.Tag = reset;
                        break;
                }
            }

            Rooms.Clear();
            NPCs.Clear();
            Items.Clear();

            Rooms.AddRange(from room in Area.Rooms.Values select new RoomDisplay(room));
            NPCs.AddRange(from npc in Area.NPCTemplates.Values select new NPCDisplay(npc));
            Items.AddRange(from item in Area.ItemTemplates.Values select new ItemDisplay(item));

            RoomCombo.DataSource = Rooms;
            RoomCombo.DisplayMember = "Display";
            save = true;
        }

        private void ResetTypesCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            SpawnCombo.DataSource = null;
            var type = ResetTypes.NPC;
            Utility.GetEnumValue(ResetTypesCombo.SelectedItem.ToString(), ref type);
            if (save && ResetsTreeView.SelectedNode != null && ResetsTreeView.SelectedNode.Tag is ResetData)
            {
                var reset = (ResetData)ResetsTreeView.SelectedNode.Tag;
                reset.resetType = type;
                Area.saved = false;
            }
            switch (type)
            {
                case ResetTypes.NPC:
                    SpawnCombo.DataSource = NPCs;
                    SpawnCombo.DisplayMember = "Display";
                    break;
                default:
                    SpawnCombo.DataSource = Items;
                    SpawnCombo.DisplayMember = "Display";
                    break;
            }
        }

        private void ResetsTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null && e.Node.Tag is ResetData)
            {
                save = false;
                var reset = (ResetData)e.Node.Tag;
                ResetTypesCombo.SelectedItem = reset.resetType.ToString();

                RoomCombo.SelectedItem = (from room in Rooms where room.Room.Vnum == reset.roomVnum select room).FirstOrDefault();
                if (reset.resetType == ResetTypes.NPC)
                {
                    SpawnCombo.SelectedItem = (from npc in NPCs where npc.NPC.Vnum == reset.spawnVnum select npc).FirstOrDefault();
                }
                else
                    SpawnCombo.SelectedItem = (from item in Items where item.Item.Vnum == reset.spawnVnum select item).FirstOrDefault();
                if (SpawnCombo.SelectedItem == null)
                    SpawnCombo.Text = reset.spawnVnum.ToString();
                MaxCountText.Text = reset.maxCount.ToString();
                CountText.Text = reset.count.ToString();
                save = true;
            }


        }

        private void RoomCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (save && ResetsTreeView.SelectedNode != null && ResetsTreeView.SelectedNode.Tag is ResetData)
            {
                var reset = (ResetData)ResetsTreeView.SelectedNode.Tag;

                reset.roomVnum = ((RoomDisplay)RoomCombo.SelectedItem).Room.Vnum;
                Area.saved = false;
            }
        }

        private void SpawnCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (save && ResetsTreeView.SelectedNode != null && ResetsTreeView.SelectedNode.Tag is ResetData)
            {
                var reset = (ResetData)ResetsTreeView.SelectedNode.Tag;

                reset.spawnVnum = reset.resetType == ResetTypes.NPC ? ((NPCDisplay)SpawnCombo.SelectedItem).NPC.Vnum : ((ItemDisplay)SpawnCombo.SelectedItem).Item.Vnum;
                Area.saved = false;
                PopulateResets();
                ResetsTreeView.SelectedNode = (from node in ResetsTreeView.Nodes.OfType<TreeNode>() where node.Tag == reset select node).FirstOrDefault();
            }
        }

        private void MoveResetUp_Click(object sender, EventArgs e)
        {
            if (ResetsTreeView.SelectedNode != null && ResetsTreeView.SelectedNode.Tag is ResetData)
            {
                var reset = (ResetData)ResetsTreeView.SelectedNode.Tag;
                var index = Area.Resets.IndexOf(reset);
                if (index > 0)
                {
                    Area.Resets.RemoveAt(index);
                    Area.Resets.Insert(index - 1, reset);
                    Area.saved = false;
                    PopulateResets();
                    ResetsTreeView.SelectedNode = (from node in ResetsTreeView.Nodes.OfType<TreeNode>() where node.Tag == reset select node).FirstOrDefault();
                }
            }
        }

        private void MoveResetDown_Click(object sender, EventArgs e)
        {
            if (ResetsTreeView.SelectedNode != null && ResetsTreeView.SelectedNode.Tag is ResetData)
            {
                var reset = (ResetData)ResetsTreeView.SelectedNode.Tag;
                var index = Area.Resets.IndexOf(reset);
                if (index < Area.Resets.Count - 1)
                {
                    Area.Resets.RemoveAt(index);
                    Area.Resets.Insert(index + 1, reset);
                    Area.saved = false;
                    PopulateResets();
                    ResetsTreeView.SelectedNode = (from node in ResetsTreeView.Nodes.OfType<TreeNode>() where node.Tag == reset select node).FirstOrDefault();
                }
            }
        }

        private void SpawnCombo_TextChanged(object sender, EventArgs e)
        {
            if (ResetsTreeView.SelectedNode != null && ResetsTreeView.SelectedNode.Tag is ResetData)
            {
                var reset = (ResetData)ResetsTreeView.SelectedNode.Tag;
                if (SpawnCombo.SelectedItem == null)
                {
                    if(int.TryParse(SpawnCombo.Text, out reset.spawnVnum))
                        Area.saved = false;
                }
            }
        }

        private void DeleteReset_Click(object sender, EventArgs e)
        {
            if (ResetsTreeView.SelectedNode != null && ResetsTreeView.SelectedNode.Tag is ResetData)
            {
                var reset = (ResetData)ResetsTreeView.SelectedNode.Tag;
                var index = Area.Resets.IndexOf(reset);
                if (index < Area.Resets.Count - 1)
                {
                    Area.Resets.RemoveAt(index);
                    Area.saved = false;
                    PopulateResets();
                    ResetsTreeView.SelectedNode = (from node in ResetsTreeView.Nodes.OfType<TreeNode>() where node.Tag == reset select node).FirstOrDefault();
                }
            }
        }

        private void NewResetButton_Click(object sender, EventArgs e)
        {
            var reset = new ResetData();
            Area.Resets.Add(reset) ;
            PopulateResets();
                
            
        }

        private void CountText_TextChanged(object sender, EventArgs e)
        {
            if (save && ResetsTreeView.SelectedNode != null && ResetsTreeView.SelectedNode.Tag is ResetData)
            {
                var reset = (ResetData)ResetsTreeView.SelectedNode.Tag;

                int.TryParse(CountText.Text, out reset.count);

                PopulateResets();
                ResetsTreeView.SelectedNode = (from node in ResetsTreeView.Nodes.OfType<TreeNode>() where node.Tag == reset select node).FirstOrDefault();
            }
        }

        private void MaxCountText_TextChanged(object sender, EventArgs e)
        {
            if (save && ResetsTreeView.SelectedNode != null && ResetsTreeView.SelectedNode.Tag is ResetData)
            {
                var reset = (ResetData)ResetsTreeView.SelectedNode.Tag;

                int.TryParse(MaxCountText.Text, out reset.maxCount);

                PopulateResets();
                ResetsTreeView.SelectedNode = (from node in ResetsTreeView.Nodes.OfType<TreeNode>() where node.Tag == reset select node).FirstOrDefault();
            }
        }
    }
}
