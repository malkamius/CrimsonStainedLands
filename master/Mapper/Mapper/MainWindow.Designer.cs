using Mapper;

namespace CLSMapper
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            mapPanel = new NoMouseWheelPanel();
            pictureBox1 = new NoMouseWheelPictureBox();
            selectorTreeView = new TreeView();
            createSquareButton = new Button();
            panel1 = new ZoomPanel();
            filterTextBox = new TextBox();
            panel2 = new Panel();
            label12 = new Label();
            VnumText = new TextBox();
            label6 = new Label();
            digDownButton = new Button();
            digUpButton = new Button();
            digWestButton = new Button();
            digEastButton = new Button();
            digSouthButton = new Button();
            digNorthButton = new Button();
            copyNameAndDescCheckBox = new CheckBox();
            lockedCheckBox = new CheckBox();
            closedCheckBox = new CheckBox();
            windowCheckBox = new CheckBox();
            doorCheckBox = new CheckBox();
            label5 = new Label();
            exitDirectionComboBox = new ComboBox();
            label7 = new Label();
            sectorComboBox = new ComboBox();
            exitDestinationTextBox = new TextBox();
            label4 = new Label();
            exitDescriptionTextBox = new TextBox();
            label3 = new Label();
            roomDescTextBox = new TextBox();
            label2 = new Label();
            roomNameTextBox = new TextBox();
            label1 = new Label();
            saveMapImageButton = new Button();
            saveWorldButton = new Button();
            ToolTip = new ToolTip(components);
            drawWholeWorldCheckBox = new CheckBox();
            panel3 = new Panel();
            label11 = new Label();
            OverroomVnumText = new TextBox();
            label10 = new Label();
            AreaCreditsText = new TextBox();
            label9 = new Label();
            AreaNameText = new TextBox();
            label8 = new Label();
            mapPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            SuspendLayout();
            // 
            // mapPanel
            // 
            mapPanel.AutoSize = true;
            mapPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            mapPanel.BorderStyle = BorderStyle.FixedSingle;
            mapPanel.Controls.Add(pictureBox1);
            mapPanel.Location = new Point(3, 3);
            mapPanel.Name = "mapPanel";
            mapPanel.Size = new Size(46, 52);
            mapPanel.TabIndex = 0;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(-1, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(42, 47);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            pictureBox1.MouseDown += pictureBox1_MouseDown;
            pictureBox1.MouseMove += pictureBox1_MouseMove;
            pictureBox1.MouseUp += pictureBox1_MouseUp;
            // 
            // selectorTreeView
            // 
            selectorTreeView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            selectorTreeView.HideSelection = false;
            selectorTreeView.Location = new Point(12, 66);
            selectorTreeView.Name = "selectorTreeView";
            selectorTreeView.Size = new Size(195, 513);
            selectorTreeView.TabIndex = 1;
            selectorTreeView.AfterSelect += selectorTreeView_AfterSelect;
            selectorTreeView.Click += selectorTreeView_Click;
            // 
            // createSquareButton
            // 
            createSquareButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            createSquareButton.Enabled = false;
            createSquareButton.Location = new Point(12, 643);
            createSquareButton.Name = "createSquareButton";
            createSquareButton.Size = new Size(195, 23);
            createSquareButton.TabIndex = 2;
            createSquareButton.Text = "Create Square";
            createSquareButton.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.AutoScroll = true;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(mapPanel);
            panel1.Location = new Point(213, 12);
            panel1.Name = "panel1";
            panel1.Size = new Size(335, 654);
            panel1.TabIndex = 3;
            panel1.Paint += panel1_Paint;
            // 
            // filterTextBox
            // 
            filterTextBox.Location = new Point(12, 37);
            filterTextBox.Name = "filterTextBox";
            filterTextBox.Size = new Size(195, 23);
            filterTextBox.TabIndex = 1;
            filterTextBox.TextChanged += filterTextBox_TextChanged;
            // 
            // panel2
            // 
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            panel2.Controls.Add(label12);
            panel2.Controls.Add(VnumText);
            panel2.Controls.Add(label6);
            panel2.Controls.Add(digDownButton);
            panel2.Controls.Add(digUpButton);
            panel2.Controls.Add(digWestButton);
            panel2.Controls.Add(digEastButton);
            panel2.Controls.Add(digSouthButton);
            panel2.Controls.Add(digNorthButton);
            panel2.Controls.Add(copyNameAndDescCheckBox);
            panel2.Controls.Add(lockedCheckBox);
            panel2.Controls.Add(closedCheckBox);
            panel2.Controls.Add(windowCheckBox);
            panel2.Controls.Add(doorCheckBox);
            panel2.Controls.Add(label5);
            panel2.Controls.Add(exitDirectionComboBox);
            panel2.Controls.Add(label7);
            panel2.Controls.Add(sectorComboBox);
            panel2.Controls.Add(exitDestinationTextBox);
            panel2.Controls.Add(label4);
            panel2.Controls.Add(exitDescriptionTextBox);
            panel2.Controls.Add(label3);
            panel2.Controls.Add(roomDescTextBox);
            panel2.Controls.Add(label2);
            panel2.Controls.Add(roomNameTextBox);
            panel2.Controls.Add(label1);
            panel2.Location = new Point(554, 149);
            panel2.Name = "panel2";
            panel2.Size = new Size(336, 517);
            panel2.TabIndex = 4;
            // 
            // label12
            // 
            label12.Location = new Point(0, 0);
            label12.Name = "label12";
            label12.Size = new Size(336, 15);
            label12.TabIndex = 36;
            label12.Text = "Room Details";
            label12.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // VnumText
            // 
            VnumText.Location = new Point(60, 18);
            VnumText.Name = "VnumText";
            VnumText.Size = new Size(269, 23);
            VnumText.TabIndex = 28;
            VnumText.TextChanged += VnumText_TextChanged;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(4, 21);
            label6.Name = "label6";
            label6.Size = new Size(39, 15);
            label6.TabIndex = 27;
            label6.Text = "Vnum";
            // 
            // digDownButton
            // 
            digDownButton.Location = new Point(254, 482);
            digDownButton.Name = "digDownButton";
            digDownButton.Size = new Size(75, 23);
            digDownButton.TabIndex = 26;
            digDownButton.Text = "Dig Down";
            digDownButton.UseVisualStyleBackColor = true;
            digDownButton.Click += digDownButton_Click;
            // 
            // digUpButton
            // 
            digUpButton.Location = new Point(254, 424);
            digUpButton.Name = "digUpButton";
            digUpButton.Size = new Size(75, 23);
            digUpButton.TabIndex = 25;
            digUpButton.Text = "Dig Up";
            digUpButton.UseVisualStyleBackColor = true;
            digUpButton.Click += digUpButton_Click;
            // 
            // digWestButton
            // 
            digWestButton.Location = new Point(29, 453);
            digWestButton.Name = "digWestButton";
            digWestButton.Size = new Size(75, 23);
            digWestButton.TabIndex = 24;
            digWestButton.Text = "Dig West";
            digWestButton.UseVisualStyleBackColor = true;
            digWestButton.Click += digWestButton_Click;
            // 
            // digEastButton
            // 
            digEastButton.Location = new Point(174, 453);
            digEastButton.Name = "digEastButton";
            digEastButton.Size = new Size(75, 23);
            digEastButton.TabIndex = 23;
            digEastButton.Text = "Dig East";
            digEastButton.UseVisualStyleBackColor = true;
            digEastButton.Click += digEastButton_Click;
            // 
            // digSouthButton
            // 
            digSouthButton.Location = new Point(102, 482);
            digSouthButton.Name = "digSouthButton";
            digSouthButton.Size = new Size(75, 23);
            digSouthButton.TabIndex = 22;
            digSouthButton.Text = "Dig South";
            digSouthButton.UseVisualStyleBackColor = true;
            digSouthButton.Click += digSouthButton_Click;
            // 
            // digNorthButton
            // 
            digNorthButton.Location = new Point(102, 424);
            digNorthButton.Name = "digNorthButton";
            digNorthButton.Size = new Size(75, 23);
            digNorthButton.TabIndex = 21;
            digNorthButton.Text = "Dig North";
            digNorthButton.UseVisualStyleBackColor = true;
            digNorthButton.Click += digNorthButton_Click;
            // 
            // copyNameAndDescCheckBox
            // 
            copyNameAndDescCheckBox.AutoSize = true;
            copyNameAndDescCheckBox.Checked = true;
            copyNameAndDescCheckBox.CheckState = CheckState.Checked;
            copyNameAndDescCheckBox.Location = new Point(3, 399);
            copyNameAndDescCheckBox.Name = "copyNameAndDescCheckBox";
            copyNameAndDescCheckBox.Size = new Size(210, 19);
            copyNameAndDescCheckBox.TabIndex = 20;
            copyNameAndDescCheckBox.Text = "Copy Room Name and Description";
            copyNameAndDescCheckBox.UseVisualStyleBackColor = true;
            // 
            // lockedCheckBox
            // 
            lockedCheckBox.AutoSize = true;
            lockedCheckBox.Location = new Point(204, 342);
            lockedCheckBox.Name = "lockedCheckBox";
            lockedCheckBox.Size = new Size(64, 19);
            lockedCheckBox.TabIndex = 19;
            lockedCheckBox.Text = "Locked";
            lockedCheckBox.UseVisualStyleBackColor = true;
            lockedCheckBox.CheckedChanged += lockedCheckBox_CheckedChanged;
            // 
            // closedCheckBox
            // 
            closedCheckBox.AutoSize = true;
            closedCheckBox.Location = new Point(136, 342);
            closedCheckBox.Name = "closedCheckBox";
            closedCheckBox.Size = new Size(62, 19);
            closedCheckBox.TabIndex = 18;
            closedCheckBox.Text = "Closed";
            closedCheckBox.UseVisualStyleBackColor = true;
            closedCheckBox.CheckedChanged += closedCheckBox_CheckedChanged;
            // 
            // windowCheckBox
            // 
            windowCheckBox.AutoSize = true;
            windowCheckBox.Location = new Point(60, 342);
            windowCheckBox.Name = "windowCheckBox";
            windowCheckBox.Size = new Size(70, 19);
            windowCheckBox.TabIndex = 17;
            windowCheckBox.Text = "Window";
            windowCheckBox.UseVisualStyleBackColor = true;
            windowCheckBox.CheckedChanged += windowCheckBox_CheckedChanged;
            // 
            // doorCheckBox
            // 
            doorCheckBox.AutoSize = true;
            doorCheckBox.Location = new Point(3, 342);
            doorCheckBox.Name = "doorCheckBox";
            doorCheckBox.Size = new Size(52, 19);
            doorCheckBox.TabIndex = 16;
            doorCheckBox.Text = "Door";
            doorCheckBox.UseVisualStyleBackColor = true;
            doorCheckBox.CheckedChanged += doorCheckBox_CheckedChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(3, 258);
            label5.Name = "label5";
            label5.Size = new Size(26, 15);
            label5.TabIndex = 15;
            label5.Text = "Exit";
            // 
            // exitDirectionComboBox
            // 
            exitDirectionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            exitDirectionComboBox.FormattingEnabled = true;
            exitDirectionComboBox.Items.AddRange(new object[] { "North", "East", "South", "West", "Up", "Down" });
            exitDirectionComboBox.Location = new Point(60, 255);
            exitDirectionComboBox.Name = "exitDirectionComboBox";
            exitDirectionComboBox.Size = new Size(269, 23);
            exitDirectionComboBox.TabIndex = 14;
            exitDirectionComboBox.SelectedIndexChanged += exitDirectionComboBox_SelectedIndexChanged;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(3, 229);
            label7.Name = "label7";
            label7.Size = new Size(40, 15);
            label7.TabIndex = 13;
            label7.Text = "Sector";
            // 
            // sectorComboBox
            // 
            sectorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            sectorComboBox.FormattingEnabled = true;
            sectorComboBox.Location = new Point(60, 226);
            sectorComboBox.Name = "sectorComboBox";
            sectorComboBox.Size = new Size(269, 23);
            sectorComboBox.TabIndex = 12;
            sectorComboBox.SelectedIndexChanged += sectorComboBox_SelectedIndexChanged;
            // 
            // exitDestinationTextBox
            // 
            exitDestinationTextBox.Location = new Point(60, 313);
            exitDestinationTextBox.Name = "exitDestinationTextBox";
            exitDestinationTextBox.Size = new Size(269, 23);
            exitDestinationTextBox.TabIndex = 7;
            exitDestinationTextBox.TextChanged += exitDestinationTextBox_TextChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(3, 316);
            label4.Name = "label4";
            label4.Size = new Size(39, 15);
            label4.TabIndex = 6;
            label4.Text = "Vnum";
            // 
            // exitDescriptionTextBox
            // 
            exitDescriptionTextBox.Location = new Point(60, 284);
            exitDescriptionTextBox.Name = "exitDescriptionTextBox";
            exitDescriptionTextBox.Size = new Size(269, 23);
            exitDescriptionTextBox.TabIndex = 5;
            exitDescriptionTextBox.TextChanged += exitDescriptionTextBox_TextChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(3, 287);
            label3.Name = "label3";
            label3.Size = new Size(54, 15);
            label3.TabIndex = 4;
            label3.Text = "Exit Desc";
            // 
            // roomDescTextBox
            // 
            roomDescTextBox.AcceptsReturn = true;
            roomDescTextBox.Location = new Point(60, 74);
            roomDescTextBox.Multiline = true;
            roomDescTextBox.Name = "roomDescTextBox";
            roomDescTextBox.ScrollBars = ScrollBars.Both;
            roomDescTextBox.Size = new Size(269, 146);
            roomDescTextBox.TabIndex = 3;
            roomDescTextBox.WordWrap = false;
            roomDescTextBox.TextChanged += roomDescTextBox_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(3, 74);
            label2.Name = "label2";
            label2.Size = new Size(32, 15);
            label2.TabIndex = 2;
            label2.Text = "Desc";
            // 
            // roomNameTextBox
            // 
            roomNameTextBox.Location = new Point(60, 47);
            roomNameTextBox.Name = "roomNameTextBox";
            roomNameTextBox.Size = new Size(269, 23);
            roomNameTextBox.TabIndex = 1;
            roomNameTextBox.TextChanged += roomNameTextBox_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(4, 50);
            label1.Name = "label1";
            label1.Size = new Size(39, 15);
            label1.TabIndex = 0;
            label1.Text = "Name";
            // 
            // saveMapImageButton
            // 
            saveMapImageButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            saveMapImageButton.Location = new Point(12, 614);
            saveMapImageButton.Name = "saveMapImageButton";
            saveMapImageButton.Size = new Size(195, 23);
            saveMapImageButton.TabIndex = 5;
            saveMapImageButton.Text = "Save Map Image";
            saveMapImageButton.UseVisualStyleBackColor = true;
            saveMapImageButton.Click += saveMapImageButton_Click;
            // 
            // saveWorldButton
            // 
            saveWorldButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            saveWorldButton.Location = new Point(12, 585);
            saveWorldButton.Name = "saveWorldButton";
            saveWorldButton.Size = new Size(195, 23);
            saveWorldButton.TabIndex = 6;
            saveWorldButton.Text = "Save Areas";
            saveWorldButton.UseVisualStyleBackColor = true;
            saveWorldButton.Click += saveWorldButton_Click;
            // 
            // ToolTip
            // 
            ToolTip.Popup += ToolTip_Popup;
            // 
            // drawWholeWorldCheckBox
            // 
            drawWholeWorldCheckBox.AutoSize = true;
            drawWholeWorldCheckBox.Location = new Point(12, 12);
            drawWholeWorldCheckBox.Name = "drawWholeWorldCheckBox";
            drawWholeWorldCheckBox.Size = new Size(121, 19);
            drawWholeWorldCheckBox.TabIndex = 1;
            drawWholeWorldCheckBox.Text = "Draw whole world";
            drawWholeWorldCheckBox.UseVisualStyleBackColor = true;
            // 
            // panel3
            // 
            panel3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panel3.Controls.Add(label11);
            panel3.Controls.Add(OverroomVnumText);
            panel3.Controls.Add(label10);
            panel3.Controls.Add(AreaCreditsText);
            panel3.Controls.Add(label9);
            panel3.Controls.Add(AreaNameText);
            panel3.Controls.Add(label8);
            panel3.Location = new Point(554, 12);
            panel3.Name = "panel3";
            panel3.Size = new Size(336, 131);
            panel3.TabIndex = 7;
            // 
            // label11
            // 
            label11.Location = new Point(0, 5);
            label11.Name = "label11";
            label11.Size = new Size(336, 14);
            label11.TabIndex = 35;
            label11.Text = "Area Details";
            label11.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // OverroomVnumText
            // 
            OverroomVnumText.Location = new Point(76, 80);
            OverroomVnumText.Name = "OverroomVnumText";
            OverroomVnumText.Size = new Size(255, 23);
            OverroomVnumText.TabIndex = 34;
            OverroomVnumText.TextChanged += OverroomVnumText_TextChanged;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(6, 83);
            label10.Name = "label10";
            label10.Size = new Size(64, 15);
            label10.TabIndex = 33;
            label10.Text = "OverRoom";
            // 
            // AreaCreditsText
            // 
            AreaCreditsText.Location = new Point(76, 51);
            AreaCreditsText.Name = "AreaCreditsText";
            AreaCreditsText.Size = new Size(253, 23);
            AreaCreditsText.TabIndex = 32;
            AreaCreditsText.TextChanged += AreaCreditsText_TextChanged;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(4, 54);
            label9.Name = "label9";
            label9.Size = new Size(44, 15);
            label9.TabIndex = 31;
            label9.Text = "Credits";
            // 
            // AreaNameText
            // 
            AreaNameText.Location = new Point(76, 22);
            AreaNameText.Name = "AreaNameText";
            AreaNameText.Size = new Size(253, 23);
            AreaNameText.TabIndex = 30;
            AreaNameText.TextChanged += AreaNameText_TextChanged;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(6, 25);
            label8.Name = "label8";
            label8.Size = new Size(39, 15);
            label8.TabIndex = 29;
            label8.Text = "Name";
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(902, 678);
            Controls.Add(panel3);
            Controls.Add(saveWorldButton);
            Controls.Add(saveMapImageButton);
            Controls.Add(panel2);
            Controls.Add(filterTextBox);
            Controls.Add(drawWholeWorldCheckBox);
            Controls.Add(panel1);
            Controls.Add(createSquareButton);
            Controls.Add(selectorTreeView);
            MinimumSize = new Size(918, 717);
            Name = "MainWindow";
            FormClosing += MainWindow_FormClosing;
            Load += MainWindow_Load;
            mapPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private NoMouseWheelPanel mapPanel;
        private TreeView selectorTreeView;
        private Button createSquareButton;
        private ZoomPanel panel1;
        private TextBox filterTextBox;
        private Panel panel2;
        private TextBox roomDescTextBox;
        private Label label2;
        private TextBox roomNameTextBox;
        private Label label1;
        private CheckBox lockedCheckBox;
        private CheckBox closedCheckBox;
        private CheckBox windowCheckBox;
        private CheckBox doorCheckBox;
        private Label label5;
        private ComboBox exitDirectionComboBox;
        private Label label7;
        private ComboBox sectorComboBox;
        private TextBox exitDestinationTextBox;
        private Label label4;
        private TextBox exitDescriptionTextBox;
        private Label label3;
        private Button saveMapImageButton;
        private Button saveWorldButton;
        private Button digDownButton;
        private Button digUpButton;
        private Button digWestButton;
        private Button digEastButton;
        private Button digSouthButton;
        private Button digNorthButton;
        private CheckBox copyNameAndDescCheckBox;
        private TextBox VnumText;
        private Label label6;
        private ToolTip ToolTip;
        private NoMouseWheelPictureBox pictureBox1;
        private CheckBox drawWholeWorldCheckBox;
        private Panel panel3;
        private TextBox OverroomVnumText;
        private Label label10;
        private TextBox AreaCreditsText;
        private Label label9;
        private TextBox AreaNameText;
        private Label label8;
        private Label label12;
        private Label label11;
    }
}