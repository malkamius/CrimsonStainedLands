using CrimsonStainedLands;
using CrimsonStainedLands.World;
using CrimsonStainedLands.Extensions;
using SkiaSharp;
using System.Drawing.Imaging;
using System.Windows.Forms;


namespace CLSMapper
{

    public partial class MainWindow : Form
    {
        bool pauseUpdate = false;

        public AreaData EditingArea { get; private set; }

        AreaData? drawnArea = null;
        private Dictionary<RoomData, (int Zone, Drawer.Box Box)> RoomsDraw = new Dictionary<RoomData, (int Zone, Drawer.Box Box)>();

        // Picturebox panning
        private bool isDragging = false;
        private Point startPoint = new Point(0, 0);
        private float zoomFactor = 1.0f;
        public MainWindow()
        {
            InitializeComponent();
            //CrimsonStainedLands.Settings.DataPath = "..\\..\\..\\data";
            //CrimsonStainedLands.Settings.AreasPath = "..\\..\\..\\data\\areas";
            //CrimsonStainedLands.Settings.RacesPath = "..\\..\\..\\data\\races";
            //CrimsonStainedLands.Settings.GuildsPath = "..\\..\\..\\data\\guilds";
            //CrimsonStainedLands.Settings.PlayersPath = "..\\..\\..\\data\\players";
            panel1.ZoomChanged += Panel1_ZoomChanged; ;
            //panel1.MouseWheel += PictureBox1_MouseWheel;
        }

        private void Panel1_ZoomChanged(object sender, Mapper.ZoomPanel.ZoomEventArgs e)
        {
            zoomFactor = Math.Min(3f, Math.Max(0.1f, zoomFactor + ((float)e.Delta / 1000f)));
            Zoom();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {

            Settings.Load();
            WeaponDamageMessage.LoadWeaponDamageMessages();
            Race.LoadRaces();
            SkillSpellGroup.LoadSkillSpellGroups();

            GuildData.LoadGuilds();

            WeaponDamageMessage.LoadWeaponDamageMessages();

            AreaData.LoadAreas(false);

            HideAreaPanel();

            foreach (var area in AreaData.Areas)
                foreach (var exitfixroom in area.Rooms.Values)
                    foreach (var exit in exitfixroom.exits)
                    {
                        if (exit != null)
                        {
                            RoomData.Rooms.TryGetValue(exit.destinationVnum, out exit.destination);
                            exit.source = exitfixroom;
                        }

                    }
            sectorComboBox.Items.AddRange((from sector in Utility.GetEnumValues<SectorTypes>() select ((object)sector.ToString())).ToArray());

            selectorTreeView.Nodes.AddRange((from area in AreaData.Areas orderby area.Name select new TreeNode(area.Name) { Tag = area }).ToArray());

            foreach (var node in selectorTreeView.Nodes.OfType<TreeNode>())
            {
                var roomsnode = node.Nodes.Add("Rooms");
                var itemsnode = node.Nodes.Add("Items");
                var npcsnode = node.Nodes.Add("NPCs");
                var resetsnode = node.Nodes.Add("Resets");
                roomsnode.Nodes.AddRange((from room in ((AreaData)node.Tag).Rooms select new TreeNode(room.Key + " - " + room.Value.Name) { Tag = room.Value }).ToArray());

            }

        }
        bool wholemapdrawn = false;

        private void selectorTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            HideAreaPanel();
            if (e.Node == null) return;

            AreaData areadata = null;
            if (e.Node.Tag is AreaData directAreaData)
            {
                areadata = directAreaData;
            }
            else if (e.Node.Parent?.Tag is AreaData parentAreaData)
            {
                areadata = parentAreaData;
            }

            if (areadata != null)
            {
                if (e.Node.Tag is AreaData)
                {
                    ShowAreaPanel(areadata);
                    drawMap(areadata);
                }
                else if (e.Node.Tag is RoomData room)
                {
                    selectNode(room);
                }

                // Handle special node types
                if (e.Node.Parent != null)
                {
                    switch (e.Node.Text)
                    {
                        case "Items":
                            var itemsWindow = new ItemsWindow(areadata);
                            itemsWindow.Show(this);
                            break;

                        case "NPCs":
                            var NPCsWindow = new NPCsWindow(areadata);
                            NPCsWindow.Show(this);
                            break;

                        case "Resets":
                            var ResetsWindow = new ResetsWindow(areadata);
                            ResetsWindow.Show(this);
                            break;
                    }
                }
            }
        }

        private void ShowAreaPanel(AreaData areadata)
        {
            panel3.Visible = true;
            panel2.Top = panel3.Bottom + 6;
            pauseUpdate = true;
            EditingArea = areadata;

            OverroomVnumText.Text = EditingArea.OverRoomVnum.ToString();
            AreaNameText.Text = EditingArea.Name;
            AreaCreditsText.Text = EditingArea.Credits;

            pauseUpdate = false;
        }

        private void HideAreaPanel()
        {
            panel3.Visible = false;
            panel2.Top = panel3.Top;
        }

        Bitmap? drawBoxes(AreaData? area)
        {
            var bitmaps = new List<SKBitmap>();
            var ZoneXOffset = 0;
            foreach (var zone in RoomsDraw.Select(z => z.Value.Zone).Distinct())
            {
                Drawer.Boxes.Clear();

                foreach (var mappedroom in RoomsDraw.Where(b => b.Value.Zone == zone))
                {
                    var box = mappedroom.Value.Box;

                    if (box == null)
                    {
                        box = new Drawer.Box();
                        box.x = mappedroom.Value.Box.x;
                        box.y = mappedroom.Value.Box.y;

                        box.height = 50;
                        box.width = 50;
                    }

                    Drawer.Boxes.Add(box);
                    if (string.IsNullOrEmpty(box.text))
                    {
                        if (mappedroom.Key.Area == area || area == null)
                        {
                            box.BackColor = SkiaSharp.SKColors.LightYellow;
                            box.OriginalBackColor = box.BackColor;
                            box.text = mappedroom.Key.Name;
                        }
                        else
                        {
                            box.text = "To " + mappedroom.Key.Area.Name;
                            box.BackColor = SkiaSharp.SKColors.White;
                            box.OriginalBackColor = box.BackColor;
                        }
                    }
                }

                foreach (var rd in RoomsDraw.Where(b => b.Value.Zone == zone))
                {
                    if (rd.Value.Box.Exits.Count == 0)
                        foreach (var exit in rd.Key.exits)
                        {
                            KeyValuePair<RoomData, (int Zone, Drawer.Box Box)>? DestinationBox = null;

                            if (exit != null && exit.destination != null && (DestinationBox = RoomsDraw.FirstOrDefault(b => b.Key == exit.destination && b.Value.Zone == rd.Value.Zone)).HasValue && DestinationBox.Value.Value.Box != null)
                                rd.Value.Box.Exits.Add(exit.direction, DestinationBox.Value.Value.Box);
                        }
                }

                var skbmp = Drawer.Draw(ZoneXOffset);
                if (skbmp != null)
                {
                    bitmaps.Add(skbmp);
                    ZoneXOffset += skbmp.Width;
                }
            }


            if (bitmaps.Any())
            {
                var w = bitmaps.Sum(b => b.Width);
                var h = bitmaps.Max(b => b.Height);

                if (drawnArea != null)
                {
                    var bitmap = new Bitmap(w, h);
                    var x = 0;
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.Clear(Color.White);
                        foreach (var skbmp in bitmaps)
                        {
                            using (var tmp = new Bitmap(skbmp.Width, skbmp.Height))
                            {
                                var data = tmp.LockBits(new Rectangle(0, 0, tmp.Width, tmp.Height),
                                            System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                                IntPtr ptr = data.Scan0;
                                int size = skbmp.RowBytes * skbmp.Height;
                                System.Runtime.InteropServices.Marshal.Copy(skbmp.Bytes, 0, ptr, size);
                                skbmp.Dispose();
                                tmp.UnlockBits(data);
                                g.DrawImage(tmp, x, 0);
                                x += tmp.Width;
                            }

                        }
                    }
                    return bitmap;
                }
                else
                {
                    var x = 0;
                    var skbitmap = new SKBitmap(w, h);

                    using (var canvas = new SKCanvas(skbitmap))
                    {
                        foreach (var srcbmp in bitmaps)
                        {
                            using (var srcimg = SKImage.FromBitmap(srcbmp))
                            {
                                canvas.DrawImage(srcimg, new SKPoint(x, 0));
                            }

                            x += srcbmp.Width;
                        }
                    }
                    using (var skimg = SKImage.FromBitmap(skbitmap))
                    {

                        using (var data = skimg.Encode())
                        using (var stream = new System.IO.FileStream("CrimsonStainedLands-World.png", FileMode.Create, FileAccess.Write))
                        {
                            if (data != null)
                                data.SaveTo(stream);
                        }
                    }
                }


            }
            return null;
        }

        void drawMap(AreaData area)
        {
            //wholemapdrawn = drawWholeWorldCheckBox.Checked;

            mapPanel.Enabled = false;


            if (drawWholeWorldCheckBox.Checked)
            {
                var image = new Bitmap(panel1.Width, panel1.Height);
                using (var g = Graphics.FromImage(image))
                {
                    g.Clear(Color.White);
                    g.DrawString("Generating World Image", Font, Brushes.Black, 0, 0);
                }
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                }
                pictureBox1.Image = image;
                pictureBox1.Width = image.Width;
                pictureBox1.Height = image.Height;
                Application.DoEvents();
                drawWorld(area);
            }


            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }

            var mapper = new Mapper.AreaMapper();
            mapper.MapRooms(area);
            Text = mapper.roomPositions.Count + " rooms/area links mapped";
            drawnArea = area;
            RoomsDraw.Clear();
            foreach (var position in mapper.roomPositions)
            {
                RoomsDraw.Add(position.Key, (position.Value.Zone, new Drawer.Box() { x = position.Value.X, y = position.Value.Y }));
            }

            pictureBox1.Image = drawBoxes(area);
            Zoom();
            pictureBox1.Parent = mapPanel;
            mapPanel.ResumeLayout();
            Application.DoEvents();

            mapPanel.Enabled = true;
        }

        private void drawWorld(AreaData area)
        {
            var mapper = new Mapper.AreaMapper();
            mapper.MapRooms(area, RoomData.Rooms.Values);
            drawnArea = null;
            RoomsDraw.Clear();
            foreach (var position in mapper.roomPositions)
            {
                RoomsDraw.Add(position.Key, (position.Value.Zone, new Drawer.Box() { x = position.Value.X, y = position.Value.Y }));
            }
            drawBoxes(null);

        }

        private void selectNode(RoomData? room)
        {
            if (room == null)
            {
                return;
            }

            var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == room.Area select n).FirstOrDefault();

            if (areanode != null)
            {
                var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                if (roomsnode != null)
                {
                    var roomnode = (from r in roomsnode.Nodes.OfType<TreeNode>() where r.Tag == room select r).FirstOrDefault();
                    if (roomnode != null)
                    {
                        selectorTreeView.SelectedNode = roomnode;

                        if (room.Area != drawnArea)
                            drawMap(room.Area);

                        var selectedroomdraw = RoomsDraw.FirstOrDefault(kvp => kvp.Key == room);

                        if (selectedroomdraw.Key != null)
                        {
                            foreach (var artifact in RoomsDraw)
                                artifact.Value.Box.BackColor = artifact.Value.Box.OriginalBackColor;
                            selectedroomdraw.Value.Box.BackColor = SKColors.LightBlue;

                            selectorTreeView.SelectedNode = roomnode;

                            if (pictureBox1.Image != null)
                            {
                                pictureBox1.Image.Dispose();
                            }
                            pictureBox1.Image = drawBoxes(room.Area);
                            // Calculate the center position of the selected room within the panel
                            int roomCenterX = (int)((selectedroomdraw.Value.Box.drawlocation.X + selectedroomdraw.Value.Box.XOffsetForZone) * zoomFactor);
                            int roomCenterY = (int)((selectedroomdraw.Value.Box.drawlocation.Y) * zoomFactor);

                            // Calculate the new scroll values to center the room in the panel's viewport
                            int newHorizontalScrollValue = roomCenterX - (panel1.ClientSize.Width / 2);
                            int newVerticalScrollValue = roomCenterY - (panel1.ClientSize.Height / 2);

                            // Ensure the scroll values are within valid bounds
                            panel1.HorizontalScroll.Value = Math.Min(panel1.HorizontalScroll.Maximum, Math.Max(0, newHorizontalScrollValue));
                            panel1.VerticalScroll.Value = Math.Min(panel1.VerticalScroll.Maximum, Math.Max(0, newVerticalScrollValue));

                        }

                        pauseUpdate = true;
                        EditingRoom = room;
                        VnumText.Text = room.Vnum.ToString();
                        roomNameTextBox.Text = room.Name;
                        roomDescTextBox.Text = room.Description.Replace("\n", Environment.NewLine);
                        exitDirectionComboBox.SelectedIndex = 0;

                        sectorComboBox.SelectedIndex = sectorComboBox.Items.IndexOf(room.sector.ToString());
                        updateExit();
                        pauseUpdate = false;
                    }
                }
            }
        }

        public RoomData EditingRoom = null;

        private void filterTextBox_TextChanged(object sender, EventArgs e)
        {
            var node = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Text.ToLower().StartsWith(filterTextBox.Text.ToLower()) select n).FirstOrDefault();

            if (node != null)
            {
                selectorTreeView.SelectedNode = node;
            }

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void exitDirectionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateExit();
        }

        private void updateExit()
        {
            if (EditingRoom != null)
            {
                Direction direction = Direction.North;
                Utility.GetEnumValue<Direction>(exitDirectionComboBox.Text, ref direction);
                if (EditingRoom.exits[(int)direction] == null)
                {
                    //EditingRoom.room.exits[(int)direction] = new ExitData() { direction = direction };
                    exitDescriptionTextBox.Text = "";
                    //if (exit.destination != null)
                    //    exit.destinationVnum = exit.destination.vnum;
                    exitDestinationTextBox.Text = "0";

                    windowCheckBox.Checked = false;
                    doorCheckBox.Checked = false;
                    closedCheckBox.Checked = false;
                    lockedCheckBox.Checked = false;
                }
                else
                {
                    var exit = EditingRoom.exits[(int)direction];
                    exitDescriptionTextBox.Text = exit.description;
                    //if (exit.destination != null)
                    //    exit.destinationVnum = exit.destination.vnum;
                    exitDestinationTextBox.Text = exit.destinationVnum.ToString();

                    windowCheckBox.Checked = exit.flags.ISSET(ExitFlags.Window);
                    doorCheckBox.Checked = exit.flags.ISSET(ExitFlags.Door);
                    closedCheckBox.Checked = exit.flags.ISSET(ExitFlags.Closed);
                    lockedCheckBox.Checked = exit.flags.ISSET(ExitFlags.Locked);
                }
            }
        }

        private void SaveExit()
        {
            if (EditingRoom != null && !pauseUpdate)
            {
                Direction direction = Direction.North;
                Utility.GetEnumValue<Direction>(exitDirectionComboBox.Text, ref direction);
                if (EditingRoom.exits[(int)direction] == null)
                {
                    EditingRoom.exits[(int)direction] = new ExitData() { direction = direction };

                }
                var exit = EditingRoom.exits[(int)direction];
                exit.description = exitDescriptionTextBox.Text;
                if (int.TryParse(exitDestinationTextBox.Text, out var destvnum))
                    exit.destinationVnum = destvnum;

                if (windowCheckBox.Checked)
                    exit.flags.SETBIT(ExitFlags.Window);
                else
                    exit.flags.REMOVEFLAG(ExitFlags.Window);

                if (doorCheckBox.Checked)
                    exit.flags.SETBIT(ExitFlags.Door);
                else
                    exit.flags.REMOVEFLAG(ExitFlags.Door);

                if (closedCheckBox.Checked)
                    exit.flags.SETBIT(ExitFlags.Closed);
                else
                    exit.flags.REMOVEFLAG(ExitFlags.Closed);

                if (lockedCheckBox.Checked)
                    exit.flags.SETBIT(ExitFlags.Locked);
                else
                    exit.flags.REMOVEFLAG(ExitFlags.Locked);
                EditingRoom.Area.saved = false;
            }
        }

        private void exitDescriptionTextBox_TextChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void exitDestinationTextBox_TextChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void doorCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void windowCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void closedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void lockedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void roomDescTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {
                EditingRoom.Area.saved = false;
                EditingRoom.Description = roomDescTextBox.Text;
            }
        }

        private void roomNameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {
                EditingRoom.Name = roomNameTextBox.Text;
                EditingRoom.Area.saved = false;

                var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == EditingRoom.Area select n).FirstOrDefault();

                if (areanode != null)
                {
                    var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                    if (roomsnode != null)
                    {
                        var roomnode = (from r in roomsnode.Nodes.OfType<TreeNode>() where r.Tag == EditingRoom select r).FirstOrDefault();
                        if (roomnode != null)
                        {
                            roomnode.Text = EditingRoom.Vnum + " - " + EditingRoom.Name;
                        }
                    }
                }
            }
        }

        private void sectorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {
                Utility.GetEnumValue<SectorTypes>(sectorComboBox.SelectedItem.ToString(), ref EditingRoom.sector);
                EditingRoom.Area.saved = false;
            }
        }

        private void saveWorldButton_Click(object sender, EventArgs e)
        {
            var unsaved = AreaData.Areas.Count(a => a.saved == false);
            AreaData.DoASaveWorlds(null, null);
            var saved = AreaData.Areas.Count(a => a.saved == false);
            MessageBox.Show(unsaved - saved + " areas saved. " + saved + " unsaved.");
        }

        private void Dig(Direction direction)
        {
            var vnum = EditingRoom.Area.Rooms.Count > 0 ? EditingRoom.Area.Rooms.Max(r => r.Key) + 1 : EditingRoom.Area.VNumStart;
            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };
            RoomData room;
            if (RoomData.Rooms.ContainsKey(vnum))
            {
                //ch.send("Not yet implemented.\n\r");
                MessageBox.Show("That vnum is already taken.");
                return;
            }
            else
            {
                room = new RoomData();
                room.Vnum = vnum;
                room.Area = EditingRoom.Area;
                room.Area.Rooms.Add(vnum, room);
                pauseUpdate = true;
                if (copyNameAndDescCheckBox.Checked)
                {
                    room.Name = EditingRoom.Name;
                    room.Description = EditingRoom.Description;
                }
                else
                {
                    room.Name = "New Room";
                    room.Description = "";
                }
                room.sector = EditingRoom.sector;
                RoomData.Rooms.TryAdd(vnum, room);
            }
            room.Area.saved = false;
            EditingRoom.Area.saved = false;
            var revDirection = reverseDirections[direction];
            var flags = new List<ExitFlags>();

            room.exits[(int)revDirection] = new ExitData() { destination = EditingRoom, destinationVnum = EditingRoom.Vnum, direction = revDirection, description = "", flags = new HashSet<ExitFlags>(), originalFlags = new HashSet<ExitFlags>() };
            EditingRoom.exits[(int)direction] = new ExitData() { destination = room, direction = direction, description = "", flags = new HashSet<ExitFlags>(), originalFlags = new HashSet<ExitFlags>() };

            selectorTreeView.Nodes.OfType<TreeNode>().First(n => n.Tag == room.Area).Nodes.OfType<TreeNode>().First(n => n.Text == "Rooms").Nodes.Add(new TreeNode(room.Vnum + " - " + room.Name) { Tag = room });


            drawMap(EditingRoom.Area);

            selectNode(room);
        }

        private void digNorthButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.North);
        }

        private void digEastButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.East);
        }

        private void digSouthButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.South);
        }

        private void digWestButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.West);
        }

        private void digUpButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.Up);
        }

        private void digDownButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.Down);
        }

        private void saveMapImageButton_Click(object sender, EventArgs e)
        {
            var imagename = "Map.jpg";
            if (drawnArea != null && !string.IsNullOrEmpty(drawnArea.Name)) imagename = string.Format("Map of {0}.jpg", drawnArea.Name);
            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                imagename = imagename.Replace(ch, ' ');

            if (pictureBox1.Image != null)
                pictureBox1.Image.Save(imagename, ImageFormat.Jpeg);
        }

        private void selectorTreeView_Click(object sender, EventArgs e)
        {

        }

        private void VnumText_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {

                int vnum;
                if (int.TryParse(VnumText.Text, out vnum) && !RoomData.Rooms.ContainsKey(vnum))
                {
                    RoomData.Rooms[EditingRoom.Vnum] = null;
                    EditingRoom.Area.Rooms[EditingRoom.Vnum] = null;
                    EditingRoom.Vnum = vnum;
                    EditingRoom.Area.saved = false;
                    RoomData.Rooms[EditingRoom.Vnum] = EditingRoom;
                    EditingRoom.Area.Rooms[EditingRoom.Vnum] = EditingRoom;

                    var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == EditingRoom.Area select n).FirstOrDefault();
                    if (areanode != null)
                    {
                        var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                        if (roomsnode != null)
                        {
                            var roomnode = (from r in roomsnode.Nodes.OfType<TreeNode>() where r.Tag == EditingRoom select r).FirstOrDefault();
                            if (roomnode != null)
                            {

                                roomnode.Text = EditingRoom.Vnum + " - " + EditingRoom.Name;

                            }
                        }
                    }
                }
            }
        }

        private void ToolTip_Popup(object sender, PopupEventArgs e)
        {

        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            startPoint = new Point(e.X, e.Y);
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                panel1.AutoScrollPosition = new Point(-(panel1.AutoScrollPosition.X + e.X - startPoint.X),
                                          -(panel1.AutoScrollPosition.Y + e.Y - startPoint.Y));
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isDragging)
                {
                    isDragging = false;
                }
                else
                {
                    var mouseX = e.X;// * zoomFactor;
                    var mouseY = e.Y;// * zoomFactor;

                    var rd = RoomsDraw.FirstOrDefault(q => mouseX >= (q.Value.Box.drawlocation.X + q.Value.Box.XOffsetForZone) * zoomFactor && mouseX <= (q.Value.Box.drawlocation.Right + q.Value.Box.XOffsetForZone) * zoomFactor && mouseY >= (q.Value.Box.drawlocation.Y) * zoomFactor && mouseY <= (q.Value.Box.drawlocation.Bottom) * zoomFactor);
                    if (rd.Key != null)
                    {
                        if (rd.Key.Area != drawnArea)
                            selectorTreeView.SelectedNode = selectorTreeView.Nodes.OfType<TreeNode>().FirstOrDefault(n => n.Tag == rd.Key.Area);
                        selectNode(rd.Key);
                    }
                }
            }
        }

        private void Zoom()
        {
            if (pictureBox1.Image != null)
            {
                var size = new SizeF(pictureBox1.Image.Width, pictureBox1.Image.Height) * zoomFactor;
                pictureBox1.Size = new Size((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AreaData.Areas.Any(a => a.saved == false))
            {
                e.Cancel = MessageBox.Show("You have unsaved changes, exit?", "Warning", MessageBoxButtons.OKCancel) == DialogResult.Cancel;
            }
        }

        private void AreaNameText_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate)
            {
                EditingArea.Name = AreaNameText.Text;
                EditingArea.saved = false;
            }
        }

        private void AreaCreditsText_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate)
            {
                EditingArea.Credits = AreaCreditsText.Text;
                EditingArea.saved = false;
            }
        }

        private void OverroomVnumText_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate)
            {
                if(int.TryParse(OverroomVnumText.Text, out var overroom))
                {
                    EditingArea.OverRoomVnum = overroom;
                    EditingArea.saved = false;
                }
            }
        }
    }
}