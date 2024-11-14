namespace CLSMapper
{
    partial class NPCsWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.NPCsTreeView = new System.Windows.Forms.TreeView();
            this.CloneItemButton = new System.Windows.Forms.Button();
            this.AddItemButton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.FlagsListBox = new System.Windows.Forms.CheckedListBox();
            this.LongTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.ShortTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.DescTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ACMagic = new System.Windows.Forms.TextBox();
            this.ACPierce = new System.Windows.Forms.TextBox();
            this.ACSlash = new System.Windows.Forms.TextBox();
            this.ACBash = new System.Windows.Forms.TextBox();
            this.label23 = new System.Windows.Forms.Label();
            this.LevelTextBox = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.HPDiceBonus = new System.Windows.Forms.TextBox();
            this.HPDiceCount = new System.Windows.Forms.TextBox();
            this.HPDiceSides = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.WeaponDamageTypeComboBox = new System.Windows.Forms.ComboBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.ManaDiceBonus = new System.Windows.Forms.TextBox();
            this.ManaDiceCount = new System.Windows.Forms.TextBox();
            this.ManaDiceSides = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.SpellLevelTextBox = new System.Windows.Forms.TextBox();
            this.SpellComboBox = new System.Windows.Forms.ComboBox();
            this.DeleteSpellButton = new System.Windows.Forms.Button();
            this.AddSpellButton = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.LearnedListBox = new System.Windows.Forms.ListBox();
            this.VnumText = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.DamageDiceBonus = new System.Windows.Forms.TextBox();
            this.DamageDiceCount = new System.Windows.Forms.TextBox();
            this.DamageDiceSides = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.raceCombo = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // NPCsTreeView
            // 
            this.NPCsTreeView.HideSelection = false;
            this.NPCsTreeView.Location = new System.Drawing.Point(12, 12);
            this.NPCsTreeView.Name = "NPCsTreeView";
            this.NPCsTreeView.Size = new System.Drawing.Size(177, 346);
            this.NPCsTreeView.TabIndex = 0;
            this.NPCsTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.NPCsTreeView_AfterSelect);
            // 
            // CloneItemButton
            // 
            this.CloneItemButton.Enabled = false;
            this.CloneItemButton.Location = new System.Drawing.Point(12, 393);
            this.CloneItemButton.Name = "CloneItemButton";
            this.CloneItemButton.Size = new System.Drawing.Size(177, 23);
            this.CloneItemButton.TabIndex = 2;
            this.CloneItemButton.Text = "Clone";
            this.CloneItemButton.UseVisualStyleBackColor = true;
            // 
            // AddItemButton
            // 
            this.AddItemButton.Location = new System.Drawing.Point(12, 364);
            this.AddItemButton.Name = "AddItemButton";
            this.AddItemButton.Size = new System.Drawing.Size(177, 23);
            this.AddItemButton.TabIndex = 1;
            this.AddItemButton.Text = "Add";
            this.AddItemButton.UseVisualStyleBackColor = true;
            this.AddItemButton.Click += new System.EventHandler(this.AddItemButton_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(195, 280);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(34, 15);
            this.label5.TabIndex = 44;
            this.label5.Text = "Flags";
            // 
            // FlagsListBox
            // 
            this.FlagsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FlagsListBox.FormattingEnabled = true;
            this.FlagsListBox.Location = new System.Drawing.Point(277, 280);
            this.FlagsListBox.Name = "FlagsListBox";
            this.FlagsListBox.Size = new System.Drawing.Size(322, 130);
            this.FlagsListBox.TabIndex = 8;
            this.FlagsListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.FlagsListBox_ItemCheck);
            this.FlagsListBox.SelectedIndexChanged += new System.EventHandler(this.FlagsListBox_SelectedIndexChanged);
            // 
            // LongTextBox
            // 
            this.LongTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LongTextBox.Location = new System.Drawing.Point(277, 99);
            this.LongTextBox.Name = "LongTextBox";
            this.LongTextBox.Size = new System.Drawing.Size(322, 23);
            this.LongTextBox.TabIndex = 6;
            this.LongTextBox.TextChanged += new System.EventHandler(this.LongTextBox_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(195, 102);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 15);
            this.label4.TabIndex = 41;
            this.label4.Text = "Long";
            // 
            // ShortTextBox
            // 
            this.ShortTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ShortTextBox.Location = new System.Drawing.Point(277, 70);
            this.ShortTextBox.Name = "ShortTextBox";
            this.ShortTextBox.Size = new System.Drawing.Size(322, 23);
            this.ShortTextBox.TabIndex = 5;
            this.ShortTextBox.TextChanged += new System.EventHandler(this.ShortTextBox_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(195, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 15);
            this.label3.TabIndex = 38;
            this.label3.Text = "Short";
            // 
            // DescTextBox
            // 
            this.DescTextBox.AcceptsReturn = true;
            this.DescTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DescTextBox.Location = new System.Drawing.Point(277, 127);
            this.DescTextBox.Multiline = true;
            this.DescTextBox.Name = "DescTextBox";
            this.DescTextBox.Size = new System.Drawing.Size(322, 147);
            this.DescTextBox.TabIndex = 7;
            this.DescTextBox.WordWrap = false;
            this.DescTextBox.TextChanged += new System.EventHandler(this.DescTextBox_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(195, 127);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 15);
            this.label2.TabIndex = 36;
            this.label2.Text = "Desc";
            // 
            // NameTextBox
            // 
            this.NameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NameTextBox.Location = new System.Drawing.Point(277, 40);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(322, 23);
            this.NameTextBox.TabIndex = 4;
            this.NameTextBox.TextChanged += new System.EventHandler(this.NameTextBox_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(195, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 15);
            this.label1.TabIndex = 34;
            this.label1.Text = "Name";
            // 
            // ACMagic
            // 
            this.ACMagic.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ACMagic.Location = new System.Drawing.Point(549, 416);
            this.ACMagic.Name = "ACMagic";
            this.ACMagic.Size = new System.Drawing.Size(42, 23);
            this.ACMagic.TabIndex = 12;
            this.ACMagic.TextChanged += new System.EventHandler(this.ACMagic_TextChanged);
            // 
            // ACPierce
            // 
            this.ACPierce.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ACPierce.Location = new System.Drawing.Point(501, 415);
            this.ACPierce.Name = "ACPierce";
            this.ACPierce.Size = new System.Drawing.Size(42, 23);
            this.ACPierce.TabIndex = 11;
            this.ACPierce.TextChanged += new System.EventHandler(this.ACPierce_TextChanged);
            // 
            // ACSlash
            // 
            this.ACSlash.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ACSlash.Location = new System.Drawing.Point(453, 416);
            this.ACSlash.Name = "ACSlash";
            this.ACSlash.Size = new System.Drawing.Size(42, 23);
            this.ACSlash.TabIndex = 10;
            this.ACSlash.TextChanged += new System.EventHandler(this.ACSlash_TextChanged);
            // 
            // ACBash
            // 
            this.ACBash.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ACBash.Location = new System.Drawing.Point(405, 417);
            this.ACBash.Name = "ACBash";
            this.ACBash.Size = new System.Drawing.Size(42, 23);
            this.ACBash.TabIndex = 9;
            this.ACBash.TextChanged += new System.EventHandler(this.ACBash_TextChanged);
            // 
            // label23
            // 
            this.label23.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(241, 420);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(158, 15);
            this.label23.TabIndex = 68;
            this.label23.Text = "AC Bash/Slash/Pierce/Magic";
            // 
            // LevelTextBox
            // 
            this.LevelTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LevelTextBox.Location = new System.Drawing.Point(687, 12);
            this.LevelTextBox.Name = "LevelTextBox";
            this.LevelTextBox.Size = new System.Drawing.Size(42, 23);
            this.LevelTextBox.TabIndex = 13;
            this.LevelTextBox.TextChanged += new System.EventHandler(this.LevelTextBox_TextChanged);
            // 
            // label21
            // 
            this.label21.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(605, 15);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(34, 15);
            this.label21.TabIndex = 66;
            this.label21.Text = "Level";
            // 
            // label19
            // 
            this.label19.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(706, 43);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(14, 15);
            this.label19.TabIndex = 65;
            this.label19.Text = "d";
            // 
            // label18
            // 
            this.label18.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(773, 43);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(15, 15);
            this.label18.TabIndex = 64;
            this.label18.Text = "+";
            // 
            // HPDiceBonus
            // 
            this.HPDiceBonus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HPDiceBonus.Location = new System.Drawing.Point(794, 40);
            this.HPDiceBonus.Name = "HPDiceBonus";
            this.HPDiceBonus.Size = new System.Drawing.Size(42, 23);
            this.HPDiceBonus.TabIndex = 16;
            this.HPDiceBonus.TextChanged += new System.EventHandler(this.HPDiceBonus_TextChanged);
            // 
            // HPDiceCount
            // 
            this.HPDiceCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HPDiceCount.Location = new System.Drawing.Point(725, 40);
            this.HPDiceCount.Name = "HPDiceCount";
            this.HPDiceCount.Size = new System.Drawing.Size(42, 23);
            this.HPDiceCount.TabIndex = 15;
            this.HPDiceCount.TextChanged += new System.EventHandler(this.HPDiceCount_TextChanged);
            // 
            // HPDiceSides
            // 
            this.HPDiceSides.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HPDiceSides.Location = new System.Drawing.Point(660, 40);
            this.HPDiceSides.Name = "HPDiceSides";
            this.HPDiceSides.Size = new System.Drawing.Size(42, 23);
            this.HPDiceSides.TabIndex = 14;
            this.HPDiceSides.TextChanged += new System.EventHandler(this.HPDiceSides_TextChanged);
            // 
            // label17
            // 
            this.label17.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(605, 44);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(49, 15);
            this.label17.TabIndex = 60;
            this.label17.Text = "HP Dice";
            // 
            // WeaponDamageTypeComboBox
            // 
            this.WeaponDamageTypeComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.WeaponDamageTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.WeaponDamageTypeComboBox.FormattingEnabled = true;
            this.WeaponDamageTypeComboBox.Location = new System.Drawing.Point(605, 387);
            this.WeaponDamageTypeComboBox.Name = "WeaponDamageTypeComboBox";
            this.WeaponDamageTypeComboBox.Size = new System.Drawing.Size(212, 23);
            this.WeaponDamageTypeComboBox.TabIndex = 28;
            this.WeaponDamageTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.WeaponDamageTypeComboBox_SelectedIndexChanged);
            // 
            // label16
            // 
            this.label16.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(605, 368);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(125, 15);
            this.label16.TabIndex = 73;
            this.label16.Text = "Weapon Damage Type";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(706, 72);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(14, 15);
            this.label6.TabIndex = 79;
            this.label6.Text = "d";
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(773, 72);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(15, 15);
            this.label7.TabIndex = 78;
            this.label7.Text = "+";
            // 
            // ManaDiceBonus
            // 
            this.ManaDiceBonus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ManaDiceBonus.Location = new System.Drawing.Point(794, 69);
            this.ManaDiceBonus.Name = "ManaDiceBonus";
            this.ManaDiceBonus.Size = new System.Drawing.Size(42, 23);
            this.ManaDiceBonus.TabIndex = 19;
            this.ManaDiceBonus.TextChanged += new System.EventHandler(this.ManaDiceBonus_TextChanged);
            // 
            // ManaDiceCount
            // 
            this.ManaDiceCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ManaDiceCount.Location = new System.Drawing.Point(725, 69);
            this.ManaDiceCount.Name = "ManaDiceCount";
            this.ManaDiceCount.Size = new System.Drawing.Size(42, 23);
            this.ManaDiceCount.TabIndex = 18;
            this.ManaDiceCount.TextChanged += new System.EventHandler(this.ManaDiceCount_TextChanged);
            // 
            // ManaDiceSides
            // 
            this.ManaDiceSides.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ManaDiceSides.Location = new System.Drawing.Point(660, 69);
            this.ManaDiceSides.Name = "ManaDiceSides";
            this.ManaDiceSides.Size = new System.Drawing.Size(42, 23);
            this.ManaDiceSides.TabIndex = 17;
            this.ManaDiceSides.TextChanged += new System.EventHandler(this.ManaDiceSides_TextChanged);
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(605, 73);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(37, 15);
            this.label8.TabIndex = 74;
            this.label8.Text = "Mana";
            // 
            // SpellLevelTextBox
            // 
            this.SpellLevelTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SpellLevelTextBox.Location = new System.Drawing.Point(787, 230);
            this.SpellLevelTextBox.Name = "SpellLevelTextBox";
            this.SpellLevelTextBox.Size = new System.Drawing.Size(83, 23);
            this.SpellLevelTextBox.TabIndex = 25;
            // 
            // SpellComboBox
            // 
            this.SpellComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SpellComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SpellComboBox.FormattingEnabled = true;
            this.SpellComboBox.Location = new System.Drawing.Point(604, 231);
            this.SpellComboBox.Name = "SpellComboBox";
            this.SpellComboBox.Size = new System.Drawing.Size(176, 23);
            this.SpellComboBox.TabIndex = 24;
            // 
            // DeleteSpellButton
            // 
            this.DeleteSpellButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DeleteSpellButton.Location = new System.Drawing.Point(706, 259);
            this.DeleteSpellButton.Name = "DeleteSpellButton";
            this.DeleteSpellButton.Size = new System.Drawing.Size(75, 23);
            this.DeleteSpellButton.TabIndex = 26;
            this.DeleteSpellButton.Text = "Delete";
            this.DeleteSpellButton.UseVisualStyleBackColor = true;
            this.DeleteSpellButton.Click += new System.EventHandler(this.DeleteSpellButton_Click);
            // 
            // AddSpellButton
            // 
            this.AddSpellButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddSpellButton.Location = new System.Drawing.Point(787, 259);
            this.AddSpellButton.Name = "AddSpellButton";
            this.AddSpellButton.Size = new System.Drawing.Size(75, 23);
            this.AddSpellButton.TabIndex = 27;
            this.AddSpellButton.Text = "Add";
            this.AddSpellButton.UseVisualStyleBackColor = true;
            this.AddSpellButton.Click += new System.EventHandler(this.AddLearnedButton_Click);
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(605, 130);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(49, 15);
            this.label13.TabIndex = 81;
            this.label13.Text = "Learned";
            // 
            // LearnedListBox
            // 
            this.LearnedListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LearnedListBox.FormattingEnabled = true;
            this.LearnedListBox.ItemHeight = 15;
            this.LearnedListBox.Location = new System.Drawing.Point(605, 148);
            this.LearnedListBox.Name = "LearnedListBox";
            this.LearnedListBox.Size = new System.Drawing.Size(265, 79);
            this.LearnedListBox.TabIndex = 23;
            // 
            // VnumText
            // 
            this.VnumText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.VnumText.Location = new System.Drawing.Point(277, 12);
            this.VnumText.Name = "VnumText";
            this.VnumText.Size = new System.Drawing.Size(322, 23);
            this.VnumText.TabIndex = 3;
            this.VnumText.TextChanged += new System.EventHandler(this.VnumText_TextChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(195, 15);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(39, 15);
            this.label9.TabIndex = 86;
            this.label9.Text = "Vnum";
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(706, 101);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(14, 15);
            this.label10.TabIndex = 93;
            this.label10.Text = "d";
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(773, 101);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(15, 15);
            this.label11.TabIndex = 92;
            this.label11.Text = "+";
            // 
            // DamageDiceBonus
            // 
            this.DamageDiceBonus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DamageDiceBonus.Location = new System.Drawing.Point(794, 98);
            this.DamageDiceBonus.Name = "DamageDiceBonus";
            this.DamageDiceBonus.Size = new System.Drawing.Size(42, 23);
            this.DamageDiceBonus.TabIndex = 22;
            this.DamageDiceBonus.TextChanged += new System.EventHandler(this.DamageDiceBonus_TextChanged);
            // 
            // DamageDiceCount
            // 
            this.DamageDiceCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DamageDiceCount.Location = new System.Drawing.Point(725, 98);
            this.DamageDiceCount.Name = "DamageDiceCount";
            this.DamageDiceCount.Size = new System.Drawing.Size(42, 23);
            this.DamageDiceCount.TabIndex = 21;
            this.DamageDiceCount.TextChanged += new System.EventHandler(this.DamageDiceCount_TextChanged);
            // 
            // DamageDiceSides
            // 
            this.DamageDiceSides.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DamageDiceSides.Location = new System.Drawing.Point(660, 98);
            this.DamageDiceSides.Name = "DamageDiceSides";
            this.DamageDiceSides.Size = new System.Drawing.Size(42, 23);
            this.DamageDiceSides.TabIndex = 20;
            this.DamageDiceSides.TextChanged += new System.EventHandler(this.DamageDiceSides_TextChanged);
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(605, 102);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(51, 15);
            this.label12.TabIndex = 88;
            this.label12.Text = "Damage";
            // 
            // label14
            // 
            this.label14.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(605, 323);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(32, 15);
            this.label14.TabIndex = 95;
            this.label14.Text = "Race";
            // 
            // raceCombo
            // 
            this.raceCombo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.raceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.raceCombo.FormattingEnabled = true;
            this.raceCombo.Location = new System.Drawing.Point(605, 342);
            this.raceCombo.Name = "raceCombo";
            this.raceCombo.Size = new System.Drawing.Size(212, 23);
            this.raceCombo.TabIndex = 94;
            this.raceCombo.SelectedIndexChanged += new System.EventHandler(this.raceCombo_SelectedIndexChanged);
            // 
            // NPCsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(881, 450);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.raceCombo);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.DamageDiceBonus);
            this.Controls.Add(this.DamageDiceCount);
            this.Controls.Add(this.DamageDiceSides);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.VnumText);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.SpellLevelTextBox);
            this.Controls.Add(this.SpellComboBox);
            this.Controls.Add(this.DeleteSpellButton);
            this.Controls.Add(this.AddSpellButton);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.LearnedListBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.ManaDiceBonus);
            this.Controls.Add(this.ManaDiceCount);
            this.Controls.Add(this.ManaDiceSides);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.ACMagic);
            this.Controls.Add(this.ACPierce);
            this.Controls.Add(this.ACSlash);
            this.Controls.Add(this.ACBash);
            this.Controls.Add(this.label23);
            this.Controls.Add(this.LevelTextBox);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.HPDiceBonus);
            this.Controls.Add(this.HPDiceCount);
            this.Controls.Add(this.HPDiceSides);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.WeaponDamageTypeComboBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.FlagsListBox);
            this.Controls.Add(this.LongTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.ShortTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.DescTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.NameTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CloneItemButton);
            this.Controls.Add(this.AddItemButton);
            this.Controls.Add(this.NPCsTreeView);
            this.Name = "NPCsWindow";
            this.Text = "NPCs";
            this.Load += new System.EventHandler(this.NPCsWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TreeView NPCsTreeView;
        private Button CloneItemButton;
        private Button AddItemButton;
        private Label label5;
        private CheckedListBox FlagsListBox;
        private TextBox LongTextBox;
        private Label label4;
        private TextBox ShortTextBox;
        private Label label3;
        private TextBox DescTextBox;
        private Label label2;
        private TextBox NameTextBox;
        private Label label1;
        private TextBox ACMagic;
        private TextBox ACPierce;
        private TextBox ACSlash;
        private TextBox ACBash;
        private Label label23;
        private TextBox LevelTextBox;
        private Label label21;
        private Label label19;
        private Label label18;
        private TextBox HPDiceBonus;
        private TextBox HPDiceCount;
        private TextBox HPDiceSides;
        private Label label17;
        private ComboBox WeaponDamageTypeComboBox;
        private Label label16;
        private Label label6;
        private Label label7;
        private TextBox ManaDiceBonus;
        private TextBox ManaDiceCount;
        private TextBox ManaDiceSides;
        private Label label8;
        private TextBox SpellLevelTextBox;
        private ComboBox SpellComboBox;
        private Button DeleteSpellButton;
        private Button AddSpellButton;
        private Label label13;
        private ListBox LearnedListBox;
        private TextBox VnumText;
        private Label label9;
        private Label label10;
        private Label label11;
        private TextBox DamageDiceBonus;
        private TextBox DamageDiceCount;
        private TextBox DamageDiceSides;
        private Label label12;
        private Label label14;
        private ComboBox raceCombo;
    }
}