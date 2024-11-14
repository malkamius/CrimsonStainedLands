namespace CLSMapper
{
    partial class ResetsWindow
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
            this.ResetsTreeView = new System.Windows.Forms.TreeView();
            this.NewResetButton = new System.Windows.Forms.Button();
            this.DeleteReset = new System.Windows.Forms.Button();
            this.MoveResetUp = new System.Windows.Forms.Button();
            this.MoveResetDown = new System.Windows.Forms.Button();
            this.ResetTypesCombo = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.RoomCombo = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SpawnCombo = new System.Windows.Forms.ComboBox();
            this.CountText = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.MaxCountText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ResetsTreeView
            // 
            this.ResetsTreeView.HideSelection = false;
            this.ResetsTreeView.Location = new System.Drawing.Point(12, 12);
            this.ResetsTreeView.Name = "ResetsTreeView";
            this.ResetsTreeView.Size = new System.Drawing.Size(390, 347);
            this.ResetsTreeView.TabIndex = 0;
            this.ResetsTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ResetsTreeView_AfterSelect);
            // 
            // NewResetButton
            // 
            this.NewResetButton.Location = new System.Drawing.Point(12, 365);
            this.NewResetButton.Name = "NewResetButton";
            this.NewResetButton.Size = new System.Drawing.Size(129, 23);
            this.NewResetButton.TabIndex = 1;
            this.NewResetButton.Text = "New";
            this.NewResetButton.UseVisualStyleBackColor = true;
            this.NewResetButton.Click += new System.EventHandler(this.NewResetButton_Click);
            // 
            // DeleteReset
            // 
            this.DeleteReset.Location = new System.Drawing.Point(148, 365);
            this.DeleteReset.Name = "DeleteReset";
            this.DeleteReset.Size = new System.Drawing.Size(129, 23);
            this.DeleteReset.TabIndex = 2;
            this.DeleteReset.Text = "Delete";
            this.DeleteReset.UseVisualStyleBackColor = true;
            this.DeleteReset.Click += new System.EventHandler(this.DeleteReset_Click);
            // 
            // MoveResetUp
            // 
            this.MoveResetUp.Location = new System.Drawing.Point(12, 394);
            this.MoveResetUp.Name = "MoveResetUp";
            this.MoveResetUp.Size = new System.Drawing.Size(129, 23);
            this.MoveResetUp.TabIndex = 3;
            this.MoveResetUp.Text = "Move Up";
            this.MoveResetUp.UseVisualStyleBackColor = true;
            this.MoveResetUp.Click += new System.EventHandler(this.MoveResetUp_Click);
            // 
            // MoveResetDown
            // 
            this.MoveResetDown.Location = new System.Drawing.Point(148, 394);
            this.MoveResetDown.Name = "MoveResetDown";
            this.MoveResetDown.Size = new System.Drawing.Size(129, 23);
            this.MoveResetDown.TabIndex = 4;
            this.MoveResetDown.Text = "Move Down";
            this.MoveResetDown.UseVisualStyleBackColor = true;
            this.MoveResetDown.Click += new System.EventHandler(this.MoveResetDown_Click);
            // 
            // ResetTypesCombo
            // 
            this.ResetTypesCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ResetTypesCombo.FormattingEnabled = true;
            this.ResetTypesCombo.Location = new System.Drawing.Point(457, 12);
            this.ResetTypesCombo.Name = "ResetTypesCombo";
            this.ResetTypesCombo.Size = new System.Drawing.Size(121, 23);
            this.ResetTypesCombo.TabIndex = 5;
            this.ResetTypesCombo.SelectedIndexChanged += new System.EventHandler(this.ResetTypesCombo_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(408, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 15);
            this.label1.TabIndex = 6;
            this.label1.Text = "Type";
            // 
            // RoomCombo
            // 
            this.RoomCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RoomCombo.FormattingEnabled = true;
            this.RoomCombo.Location = new System.Drawing.Point(457, 41);
            this.RoomCombo.Name = "RoomCombo";
            this.RoomCombo.Size = new System.Drawing.Size(121, 23);
            this.RoomCombo.TabIndex = 7;
            this.RoomCombo.SelectedIndexChanged += new System.EventHandler(this.RoomCombo_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(408, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 15);
            this.label2.TabIndex = 8;
            this.label2.Text = "Room";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(408, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 15);
            this.label3.TabIndex = 10;
            this.label3.Text = "Spawn";
            // 
            // SpawnCombo
            // 
            this.SpawnCombo.FormattingEnabled = true;
            this.SpawnCombo.Location = new System.Drawing.Point(457, 70);
            this.SpawnCombo.Name = "SpawnCombo";
            this.SpawnCombo.Size = new System.Drawing.Size(121, 23);
            this.SpawnCombo.TabIndex = 9;
            this.SpawnCombo.SelectedIndexChanged += new System.EventHandler(this.SpawnCombo_SelectedIndexChanged);
            this.SpawnCombo.TextChanged += new System.EventHandler(this.SpawnCombo_TextChanged);
            // 
            // CountText
            // 
            this.CountText.Location = new System.Drawing.Point(480, 99);
            this.CountText.Name = "CountText";
            this.CountText.Size = new System.Drawing.Size(98, 23);
            this.CountText.TabIndex = 11;
            this.CountText.TextChanged += new System.EventHandler(this.CountText_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(408, 102);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 15);
            this.label4.TabIndex = 12;
            this.label4.Text = "Count";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(408, 131);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(66, 15);
            this.label5.TabIndex = 14;
            this.label5.Text = "Max Count";
            // 
            // MaxCountText
            // 
            this.MaxCountText.Location = new System.Drawing.Point(480, 128);
            this.MaxCountText.Name = "MaxCountText";
            this.MaxCountText.Size = new System.Drawing.Size(98, 23);
            this.MaxCountText.TabIndex = 13;
            this.MaxCountText.TextChanged += new System.EventHandler(this.MaxCountText_TextChanged);
            // 
            // ResetsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(597, 427);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.MaxCountText);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.CountText);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.SpawnCombo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.RoomCombo);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ResetTypesCombo);
            this.Controls.Add(this.MoveResetDown);
            this.Controls.Add(this.MoveResetUp);
            this.Controls.Add(this.DeleteReset);
            this.Controls.Add(this.NewResetButton);
            this.Controls.Add(this.ResetsTreeView);
            this.Name = "ResetsWindow";
            this.Text = "Resets";
            this.Load += new System.EventHandler(this.ResetsWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TreeView ResetsTreeView;
        private Button NewResetButton;
        private Button DeleteReset;
        private Button MoveResetUp;
        private Button MoveResetDown;
        private ComboBox ResetTypesCombo;
        private Label label1;
        private ComboBox RoomCombo;
        private Label label2;
        private Label label3;
        private ComboBox SpawnCombo;
        private TextBox CountText;
        private Label label4;
        private Label label5;
        private TextBox MaxCountText;
    }
}