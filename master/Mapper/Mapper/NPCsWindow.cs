using CrimsonStainedLands.Extensions;
using CrimsonStainedLands;
using CrimsonStainedLands.World;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection.PortableExecutable;

namespace CLSMapper
{
    public partial class NPCsWindow : Form
    {
        public AreaData Area;
        bool save = false;
        private List<LearnedDisplay> Learned = new List<LearnedDisplay>();
        public NPCsWindow(AreaData area)
        {
            InitializeComponent();
            this.Area = area;
        }

        private void NPCsWindow_Load(object sender, EventArgs e)
        {
            PopulateFlags();
            PopulateRaces();
            PopulateNPCs();
        }

        private void PopulateNPCs()
        {
            NPCsTreeView.Nodes.AddRange((from item in Area.NPCTemplates.Values select new TreeNode(item.Vnum + " - " + item.ShortDescription) { Tag = item }).ToArray());
        }

        private void PopulateFlags()
        {
            FlagsListBox.Items.AddRange((from flag in Utility.GetEnumValues<ActFlags>() select (object)flag.ToString()).ToArray());
            WeaponDamageTypeComboBox.Items.AddRange((from WeaponDamageMessage in WeaponDamageMessage.WeaponDamageMessages select (object)WeaponDamageMessage.Keyword).ToArray());
            SpellComboBox.Items.AddRange((from skill in SkillSpell.Skills.Values select ((object)skill.name)).ToArray());
        }

        private void PopulateRaces()
        {
            raceCombo.Items.AddRange((from race in Race.Races select ((object)race.name)).ToArray());
        }

        private void ClearChecks(CheckedListBox list)
        {
            for (var i = 0; i < list.Items.Count; i++)
                list.SetItemChecked(i, false);
        }

        private void NPCsTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            save = false;
            if (e.Node != null && e.Node.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)e.Node.Tag;

                VnumText.Text = npc.Vnum.ToString();
                NameTextBox.Text = npc.Name;
                ShortTextBox.Text = npc.ShortDescription;
                LongTextBox.Text = npc.LongDescription;
                DescTextBox.Text = npc.Description.Replace("\n", Environment.NewLine);

                ClearChecks(FlagsListBox);
                foreach (var flag in npc.Flags.ToArray())
                {
                    var index = FlagsListBox.Items.IndexOf(flag.ToString());
                    FlagsListBox.SetItemChecked(index, true);
                }

                if (npc.HitPointDice == null)
                    npc.HitPointDice = new Dice(0, 0, 0);
                HPDiceSides.Text = npc.HitPointDice.DiceSides.ToString();
                HPDiceCount.Text = npc.HitPointDice.DiceCount.ToString();
                HPDiceBonus.Text = npc.HitPointDice.DiceBonus.ToString();

                if (npc.ManaPointDice == null)
                    npc.ManaPointDice = new Dice(0, 0, 0);
                ManaDiceSides.Text = npc.ManaPointDice.DiceSides.ToString();
                ManaDiceCount.Text = npc.ManaPointDice.DiceCount.ToString();
                ManaDiceBonus.Text = npc.ManaPointDice.DiceBonus.ToString();
                WeaponDamageTypeComboBox.SelectedItem = npc.WeaponDamageMessage != null ? npc.WeaponDamageMessage.Keyword.ToString() : (npc.Race != null && npc.Race.parts.Contains(PartFlags.Claws)?  WeaponDamageMessage.GetWeaponDamageMessage("claw") : WeaponDamageMessage.GetWeaponDamageMessage("punch"));
                if (npc.DamageDice == null)
                    npc.DamageDice = new Dice(0, 0, 0);
                DamageDiceSides.Text = npc.DamageDice.DiceSides.ToString();
                DamageDiceCount.Text = npc.DamageDice.DiceCount.ToString();
                DamageDiceBonus.Text = npc.DamageDice.DiceBonus.ToString();

                LevelTextBox.Text = npc.Level.ToString();

                ACBash.Text = npc.ArmorBash.ToString();
                ACSlash.Text = npc.ArmorSlash.ToString();
                ACPierce.Text = npc.ArmorPierce.ToString();
                ACMagic.Text = npc.ArmorExotic.ToString();

                ClearChecks(FlagsListBox);
                foreach (var flag in npc.Flags.ToArray())
                {
                    var index = FlagsListBox.Items.IndexOf(flag.ToString());
                    FlagsListBox.SetItemChecked(index, true);
                }
                raceCombo.SelectedIndex = raceCombo.Items.IndexOf("human");
                if (npc.Race != null)
                    raceCombo.SelectedIndex = raceCombo.Items.IndexOf(npc.Race.name);

                UpdateLearned();
            }
            save = true;
        }

        private void NameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                npc.Name = NameTextBox.Text;

                Area.saved = false;
            }
        }

        private void ShortTextBox_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                npc.ShortDescription = ShortTextBox.Text;

                Area.saved = false;
            }
        }

        private void LongTextBox_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                npc.LongDescription = LongTextBox.Text;

                Area.saved = false;
            }
        }

        private void DescTextBox_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                npc.Description = DescTextBox.Text;

                Area.saved = false;
            }
        }

        private void FlagsListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                npc.Flags.Clear();

                if (e.NewValue == CheckState.Checked)
                {
                    ActFlags itemtype = ActFlags.NPC;
                    if (Utility.GetEnumValue<ActFlags>(FlagsListBox.Items[e.Index].ToString(), ref itemtype))
                    {
                        npc.Flags.SETBIT(itemtype);
                    }
                }
                foreach (var checkeditem in FlagsListBox.CheckedItems)
                {
                    ActFlags itemtype = ActFlags.NPC;
                    if (Utility.GetEnumValue<ActFlags>(checkeditem.ToString(), ref itemtype))
                        npc.Flags.SETBIT(itemtype);

                }
                Area.saved = false;
            }
        }

        private void ACBash_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                int.TryParse(ACBash.Text, out npc.ArmorBash);
                Area.saved = false;
            }
        }

        private void ACSlash_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                int.TryParse(ACSlash.Text, out npc.ArmorSlash);
                Area.saved = false;
            }
        }

        private void ACPierce_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                int.TryParse(ACPierce.Text, out npc.ArmorPierce);
                Area.saved = false;
            }
        }

        private void ACMagic_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                int.TryParse(ACMagic.Text, out npc.ArmorExotic);
                Area.saved = false;
            }
        }

        private void LevelTextBox_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if(int.TryParse(LevelTextBox.Text, out var npclevel))
                {
                    npc.Level = npclevel;
                }
                Area.saved = false;
            }
        }

        private void AddItemButton_Click(object sender, EventArgs e)
        {
            var vnum = Area.NPCTemplates.Count > 0 ? Area.NPCTemplates.Max(r => r.Key) + 1 : Area.VNumStart;

            var npcTemplate = new NPCTemplateData();
            npcTemplate.Vnum = vnum;
            npcTemplate.Area = Area;
            Area.NPCTemplates.Add(vnum, npcTemplate);
            NPCsTreeView.Nodes.Clear();
            PopulateNPCs();
            NPCsTreeView.SelectedNode = (from node in NPCsTreeView.Nodes.OfType<TreeNode>() where node.Tag == npcTemplate select node).FirstOrDefault();
            Area.saved = false;
        }

        private void UpdateLearned()
        {
            if (NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var item = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                Learned.Clear();
                LearnedListBox.DataSource = null;
                Learned.AddRange(from learned in item.Learned select new LearnedDisplay(learned.Key, learned.Value.Percentage));
                LearnedListBox.DataSource = Learned;
                LearnedListBox.DisplayMember = "Display";
            }
        }
        private void AddLearnedButton_Click(object sender, EventArgs e)
        {
            if (NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData && SpellComboBox.SelectedIndex >= 0)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                SkillSpell skill = SkillSpell.SkillLookup(SpellComboBox.SelectedItem.ToString());
                int level;
                if (skill != null && int.TryParse(SpellLevelTextBox.Text, out level))
                {
                    npc.Learned[skill] = new LearnedSkillSpell() { Percentage = level, Level = 1, Skill = skill, SkillName = skill.name };
                }
                UpdateLearned();
                Area.saved = false;

            }
        }

        private void DeleteSpellButton_Click(object sender, EventArgs e)
        {
            var learned = (LearnedDisplay)LearnedListBox.SelectedItem;

            if (learned != null)
            {
                if (NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
                {
                    var item = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                    item.Learned.Remove(learned.spell);
                    UpdateLearned();
                    Area.saved = false;
                }
            }
        }

        private void VnumText_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                int vnum = 0;
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;

                if (int.TryParse(VnumText.Text, out vnum) && !NPCTemplateData.Templates.ContainsKey(vnum))
                {
                    NPCTemplateData.Templates[npc.Vnum] = null;
                    npc.Vnum = vnum;
                    npc.Area.saved = false;
                    NPCTemplateData.Templates[npc.Vnum] = npc;
                }
            }
        }

        private void ManaDiceSides_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if (npc.ManaPointDice == null)
                    npc.ManaPointDice = new Dice(0, 0, 0);
                int.TryParse(ManaDiceSides.Text, out npc.ManaPointDice.DiceSides);
                Area.saved = false;
            }
        }

        private void HPDiceSides_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if (npc.HitPointDice == null)
                    npc.HitPointDice = new Dice(0, 0, 0);
                int.TryParse(HPDiceSides.Text, out npc.HitPointDice.DiceSides);
                Area.saved = false;
            }
        }

        private void HPDiceCount_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if (npc.HitPointDice == null)
                    npc.HitPointDice = new Dice(0, 0, 0);
                int.TryParse(HPDiceCount.Text, out npc.HitPointDice.DiceCount);
                Area.saved = false;
            }
        }

        private void HPDiceBonus_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if (npc.HitPointDice == null)
                    npc.HitPointDice = new Dice(0, 0, 0);
                int.TryParse(HPDiceBonus.Text, out npc.HitPointDice.DiceBonus);
                Area.saved = false;
            }
        }

        private void ManaDiceCount_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if (npc.ManaPointDice == null)
                    npc.ManaPointDice = new Dice(0, 0, 0);
                int.TryParse(ManaDiceCount.Text, out npc.ManaPointDice.DiceCount);
                Area.saved = false;
            }
        }

        private void ManaDiceBonus_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if (npc.ManaPointDice == null)
                    npc.ManaPointDice = new Dice(0, 0, 0);
                int.TryParse(ManaDiceBonus.Text, out npc.ManaPointDice.DiceBonus);
                Area.saved = false;
            }
        }

        private void DamageDiceSides_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if (npc.DamageDice == null)
                    npc.DamageDice = new Dice(0, 0, 0);
                int.TryParse(DamageDiceSides.Text, out npc.DamageDice.DiceSides);
                Area.saved = false;
            }
        }

        private void DamageDiceCount_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if (npc.DamageDice == null)
                    npc.DamageDice = new Dice(0, 0, 0);
                int.TryParse(DamageDiceCount.Text, out npc.DamageDice.DiceCount);
                Area.saved = false;
            }
        }

        private void DamageDiceBonus_TextChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                if (npc.DamageDice == null)
                    npc.DamageDice = new Dice(0, 0, 0);
                int.TryParse(DamageDiceBonus.Text, out npc.DamageDice.DiceBonus);
                Area.saved = false;
            }
        }

        private void FlagsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void WeaponDamageTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                npc.WeaponDamageMessage = WeaponDamageMessage.GetWeaponDamageMessage(WeaponDamageTypeComboBox.SelectedItem.ToString());
                Area.saved = false;
            }
        }

        private void raceCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (save && NPCsTreeView.SelectedNode != null && NPCsTreeView.SelectedNode.Tag is NPCTemplateData)
            {
                var npc = (NPCTemplateData)NPCsTreeView.SelectedNode.Tag;
                npc.Race = Race.GetRace(raceCombo.SelectedItem.ToString());
                Area.saved = false;
            }
        }
    }
}
