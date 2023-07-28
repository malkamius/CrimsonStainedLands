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
            if (loading)
            {
                loading = false;
                //Hide();
                notifyIcon.Text = "Crimson Stained Lands Server";
                notifyIcon.Icon = this.Icon;
                notifyIcon.DoubleClick += notifyIcon_DoubleClick;
                notifyIcon.Visible = true;
                notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { new MenuItem("Show", notifyIcon_Show), new MenuItem("Exit", notifyIcon_Exit) });

                if (!System.IO.File.Exists("Settings.xml"))
                {
                    var settings = new XElement("Settings", new XAttribute("Port", 4000), new XAttribute("MaxPlayersOnlineEver", 0));
                    settings.Save("Settings.xml");
                }
                else
                {
                    var settings = XElement.Load("Settings.xml");
                    game.MaxPlayersOnlineEver = settings.GetAttributeValueInt("MaxPlayersOnlineEver", 0);
                    port = settings.GetAttributeValueInt("Port", 4000);
                    
                }
                game.Launch(port, this);
                syncTimer.Enabled = true;
            }
        }

        private void notifyIcon_Exit(object sender, EventArgs e)
        {
            try
            {
                game.Instance.Dispose();
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
            game.shutdown();

            Close();
        }

        private void restartButton_Click(object sender, EventArgs e)
        {
            exit = true;
            game.shutdown();

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
                    ((Player)selected).socket.Send(System.Text.ASCIIEncoding.ASCII.GetBytes("You have been kicked.\n\r"));
                    ((Player)selected).socket.Close();
                    ((Player)selected).socket = null;
                }
            }
            catch { }

        }

        private void banButton_Click(object sender, EventArgs e)
        {

        }

        public void sync(game.gameInfo info)
        {
            try
            {
                if (info != null && info.log != null)
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


                    playersListBox.DataSource = new List<Player>(from connection in info.connections where connection.socket != null select connection);
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
            sync(game.Instance.Info);
            logRichTextBox.ResumeLayout();
            ResumeLayout();
        }
    }
}
