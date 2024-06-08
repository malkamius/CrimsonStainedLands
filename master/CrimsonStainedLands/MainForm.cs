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
using System.Xml.Linq;
using System.Xml.Schema;

namespace CrimsonStainedLands
{
    public partial class MainForm : Form
    {
        private bool loading = true;
        public bool exit = false;

        private int port = 4000;
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Visible = false;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            //Settings.Save();
            if (loading)
            {
                loading = false;
                //Hide();
                
                notifyIcon.Icon = this.Icon;
                notifyIcon.DoubleClick += notifyIcon_DoubleClick;
                notifyIcon.Visible = true;
                // TODO ContextMenu is no longer supported. Use ContextMenuStrip instead. For more details see https://docs.microsoft.com/en-us/dotnet/core/compatibility/winforms#removed-controls
                notifyIcon.ContextMenuStrip = new ContextMenuStrip();
                //new MenuItem[] { new MenuItem("Show", notifyIcon_Show), new MenuItem("Exit", notifyIcon_Exit) });
                notifyIcon.ContextMenuStrip.Items.Add("Show", null, notifyIcon_Show);
                notifyIcon.ContextMenuStrip.Items.Add("Exit", null, notifyIcon_Exit);

                port = Settings.Port;
                Text = string.Format("CrimsonStainedLands Server - Standard port {0}, SSL port {1}", port, Settings.SSLPort);
                notifyIcon.Text = Text;
                Game.Launch(port, this);
                syncTimer.Enabled = true;
            }
        }

        private void notifyIcon_Exit(object sender, EventArgs e)
        {
            try
            {
                Game.Instance.Dispose();
            }
            catch { }
            exit = true;
            this.Close();
            this.Dispose();
        }

        private void notifyIcon_Show(object sender, EventArgs e)
        {
            this.Show();
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.WindowsShutDown && !exit)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void shutdownButton_Click(object sender, EventArgs e)
        {
            exit = true;
            Game.shutdown();

            Close();
        }

        private void restartButton_Click(object sender, EventArgs e)
        {
            exit = true;
            Game.shutdown();

            Close();
            System.Diagnostics.Process.Start(Application.ExecutablePath);
        }

        private void kickButton_Click(object sender, EventArgs e)
        {
            try
            {
                var selected = playersListBox.SelectedItem;
                if (selected != null)
                {
                    ((Player)selected).SendRaw("You have been kicked.\n\r");
                    Game.CloseSocket((Player)selected);
                }
            }
            catch { }

        }

        private void banButton_Click(object sender, EventArgs e)
        {

        }

        public void sync(Game.GameInfo info)
        {
            try
            {
                if (info != null && info.Log != null)
                {
                    var text = info.RetrieveLog();

                    if (logRichTextBox.Text.Length > 500000)
                    {
                        logRichTextBox.SelectAll();
                        logRichTextBox.SelectedText = logRichTextBox.Text.Substring(logRichTextBox.Text.Length - 500000);
                        this.logRichTextBox.ScrollToCaret();
                    }
                    if (text.Length > 0)
                    {
                        this.logRichTextBox.SelectionStart = logRichTextBox.TextLength;
                        this.logRichTextBox.SelectionLength = 0;
                        this.logRichTextBox.SelectedText = text;
                        this.logRichTextBox.SelectionStart = this.logRichTextBox.TextLength;
                        this.logRichTextBox.ScrollToCaret();
                    }
                    Player selectedPlayer = (Player)this.playersListBox.SelectedValue;

                    //this.playersListBox.Items.Clear();


                    playersListBox.DataSource = new List<Player>(from connection in info.Connections.ToArrayLocked() where connection.socket != null select connection);
                    //playersListBox.ValueMember = "name";
                    playersListBox.DisplayMember = "name";

                    if (((List<Player>)playersListBox.DataSource).Contains(selectedPlayer))
                        playersListBox.SelectedItem = selectedPlayer;
                    //foreach(var player in info.connections)
                    //{
                    //    playersListBox.Items.Add(player);
                    //}
                }
            }
            catch (Exception ex)
            {
                Text = ex.Message;
                logRichTextBox.ForeColor = Color.Red;
                logRichTextBox.Text = ex.ToString();
            }

        }

        private void syncTimer_Tick(object sender, EventArgs e)
        {
            SuspendLayout();
            logRichTextBox.SuspendLayout();
            sync(Game.Instance.Info);
            logRichTextBox.ResumeLayout();
            ResumeLayout();
        }
    }
}
