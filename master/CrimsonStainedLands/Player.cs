using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using global::CrimsonStainedLands.Extensions;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.Eventing.Reader;

namespace CrimsonStainedLands
{
    public class Player : Character
    {
        public Socket socket;
        public SslStream sslsocket;

        public string Address { get; private set; }

        public DateTime? inanimate = null;
        public StringBuilder input = new StringBuilder();
        public StringBuilder output = new StringBuilder();
        private string password;

        public Game game;
        public DateTime LastSaveTime = DateTime.MinValue;
        public TimeSpan TotalPlayTime = TimeSpan.Zero;
        public DateTime LastReadNote;

        public NoteData UnsentNote = new NoteData();

        public List<QuestProgressData> Quests = new List<QuestProgressData>();

        public enum TelnetOptionFlags
        {
            ColorRGB,
            Color256,
            SuppressGoAhead,
            MUDeXtensionProtocol,
            WontMSSP,
            Ansi
        }
        public List<TelnetOptionFlags> TelnetOptions = new List<TelnetOptionFlags>();

        public Stack<byte[]> WriteBuffers = new Stack<byte[]>();
        public enum ConnectionStates
        {
            GetName = 1,
            GetNewPassword,
            ConfirmPassword,
            GetColorOn,
            GetPassword,
            GetPlayerAlreadyLoggedIn,
            GetSex,
            GetRace,
            GetAlignment,
            GetEthos,
            Playing,
            GetGuild,
            GetDefaultWeapon,
            Deleting,
            NegotiateSSH
        }

        public ConnectionStates state;

        private int roomVnum;
        public string Prompt;

        public ShapeshiftForm.FormType ShapeFocusMajor = ShapeshiftForm.FormType.None;
        public ShapeshiftForm.FormType ShapeFocusMinor = ShapeshiftForm.FormType.None;

        public int Wimpy = 0;
        public int WeaponSpecializations = 0;

        public bool SittingAtPrompt { get; internal set; }

        public static string InvalidNames = "self";
        internal IAsyncResult writeop;
        internal IAsyncResult readop;
        internal byte[] receivebuffer;
        public byte[] ReceiveBufferBacklog;
        public DateTime LastActivity { get; set; } = DateTime.Now;

        static Player()
        {
            if (System.IO.File.Exists(Settings.DataPath + "\\invalidnames.txt"))
            {
                InvalidNames = System.IO.File.ReadAllText(Settings.DataPath + "\\invalidnames.txt").Replace("\n", " ").Replace("\r", " ");
            }
        }

        public Player(Game game, Socket socket, bool ssl = false, bool ssh = false)
        {
            this.game = game;
            this.socket = socket;
            Address = ((IPEndPoint)socket.RemoteEndPoint).Address.MapToIPv4().ToString();
            Name = "new connection";
            Game.Instance.Info.Connections.Add(this);
            //var address = ((System.Net.IPEndPoint)socket.RemoteEndPoint).Address;
            //try
            //{
            //    var entry = System.Net.Dns.GetHostEntry(address);
            //    game.log("New connection from " + socket.RemoteEndPoint.ToString() + " @ " + entry.HostName);
            //}catch (Exception e)
            //{
            //    game.bug(e.ToString());
            Game.log("New connection from " + socket.RemoteEndPoint.ToString());
            //}

            if (ssl)
            {
                try
                {
                    state = ConnectionStates.NegotiateSSH;
                    inanimate = DateTime.Now;
                    this.sslsocket = new SslStream(new System.Net.Sockets.NetworkStream(socket));

                    var certificate = new X509Certificate2("kbs-cloud.com.pfx", "T3hposmud");
                    if (certificate != null)
                        this.sslsocket.BeginAuthenticateAsServer(certificate, new AsyncCallback((result) =>
                        {
                            try
                            {
                                this.sslsocket.EndAuthenticateAsServer(result);
                                Game.Instance.SocketAccepted(this);
                                state = ConnectionStates.GetName;

                                ReadHelp(this, "DIKU", true);
                                send("\n\rEnter your name: ");
                                if (receivebuffer == null) receivebuffer = new byte[255];
                                if (readop == null || readop.IsCompleted)
                                    readop = sslsocket.BeginRead(receivebuffer, 0, receivebuffer.Length, EndReceiveSsl, this.sslsocket);
                                inanimate = null;
                            }
                            catch { }

                        }), null);
                }
                catch { }
                //this.sslsocket.AuthenticateAsServer(certificate, false,true);

            }
            else
            {
                state = ConnectionStates.GetName;
                Game.Instance.SocketAccepted(this);
                ReadHelp(this, "DIKU", true);
                send("\n\rEnter your name: ");

                lock (this)
                {
                    if (receivebuffer == null) receivebuffer = new byte[255];
                    //int received = connection.socket.Receive(buffer);
                    if (readop == null || readop.IsCompleted)
                        readop = socket.BeginReceive(receivebuffer, 0, receivebuffer.Length, SocketFlags.None, EndReceive, this.socket);
                }
            }
        }

        public List<string> ClientTypes = new List<string>();

        private void ProcessBytes(byte[] buffer, int length)
        {
            lock (this)
            {
                var position = input.Length;
                byte[] data;

                if (ReceiveBufferBacklog != null)
                {
                    data = new byte[ReceiveBufferBacklog.Length + length];
                    ReceiveBufferBacklog.CopyTo(data, 0);
                    buffer.CopyTo(data, ReceiveBufferBacklog.Length);
                }
                else
                {
                    data = new byte[length];
                    Buffer.BlockCopy(buffer, 0, data, 0, length);
                }


                if (data.Length > 4200)
                {
                    sendRaw("Too much data to process at once.\n\r");
                    Game.CloseSocket(this, false, true);
                    inanimate = DateTime.Now;
                }

                ReceiveBufferBacklog = null;
                for (int byteindex = 0; byteindex < data.Length; byteindex++)// (var Byte in buffer)
                {
                    var singlecharacter = data[byteindex];

                    if (singlecharacter == 8 && position > 0) // backspace
                    {
                        input.Remove(input.Length - 1, 1);
                        position -= 1;
                    }
                    else if (singlecharacter == 13 || singlecharacter == 10 || (singlecharacter >= 32 && singlecharacter <= 126)) // new lines and standard characters
                    {
                        input.Append((char)singlecharacter);
                        position++;
                        if (input.Length > 4200) // not writing a novel yet?
                        {

                            sendRaw("Too much data to process at once.\n\r");
                            Game.CloseSocket(this, false, true);
                            inanimate = DateTime.Now;
                        }
                    }
                    else if (singlecharacter == (byte)TelnetProtocol.Options.InterpretAsCommand)
                    {
                        TelnetProtocol.ProcessInterpretAsCommand(this, data, byteindex, out var newbyteindex, out var carryover,
                            (sender, command) =>
                            {
                                if (command.Type == TelnetProtocol.Command.Types.WillTelnetType)
                                {
                                    sendRaw(TelnetProtocol.ServerGetWillTelnetType, true);
                                }
                                else if (command.Type == TelnetProtocol.Command.Types.ClientSendNegotiateType)
                                {
                                    if (command.Values.TryGetValue("TelnetType", out var telnettypes) && telnettypes != null && telnettypes.Length > 0)
                                    {
                                        var ClientString = telnettypes.First(); ;

                                        var TelnetOptionFlags = new Dictionary<string, Player.TelnetOptionFlags[]>
                                        {
                                            { "CMUD", new Player.TelnetOptionFlags[] { Player.TelnetOptionFlags.Color256 } },
                                            { "Mudlet", new Player.TelnetOptionFlags[] { Player.TelnetOptionFlags.Color256, Player.TelnetOptionFlags.ColorRGB } },
                                            { "Mushclient", new Player.TelnetOptionFlags[] { Player.TelnetOptionFlags.Color256, Player.TelnetOptionFlags.ColorRGB } },
                                            { "TRUECOLOR", new Player.TelnetOptionFlags[] { Player.TelnetOptionFlags.ColorRGB } },
                                            { "256COLOR", new Player.TelnetOptionFlags[] { Player.TelnetOptionFlags.Color256 } },
                                            { "VT100", new Player.TelnetOptionFlags[] { Player.TelnetOptionFlags.Ansi } },
                                            { "ANSI", new Player.TelnetOptionFlags[] { Player.TelnetOptionFlags.Ansi } },
                                        };

                                        var Options = (from option in TelnetOptionFlags 
                                                       where ClientString.StringPrefix(option.Key) || 
                                                            ClientString.ToUpper().Replace(" ", "").Contains(option.Key) 
                                                       select option.Value).FirstOrDefault();

                                        if (Options != null)
                                            foreach (var option in Options)
                                                TelnetOptions.SETBIT(option);

                                        Game.log(ClientString + " client detected.");
                                        if (!ClientTypes.Contains(ClientString))
                                        {
                                            ClientTypes.Add(ClientString);
                                            sendRaw(TelnetProtocol.ServerGetTelnetTypeNegotiate, true);
                                        }
                                        else
                                            sendRaw(TelnetProtocol.ServerGetWillMudServerStatusProtocol, true);
                                    }
                                    else
                                        sendRaw(TelnetProtocol.ServerGetWillMudServerStatusProtocol, true);
                                }
                                else if(command.Type == TelnetProtocol.Command.Types.DoMUDServerStatusProtocol)
                                {
                                    Game.log("SENDING MSSP DATA");
                                    var variables = new Dictionary<string, string[]>();
                                    variables["NAME"] = new string[] { "CRIMSON STAINED LANDS" };
                                    variables["PLAYERS"] = new string[] { game.Info.Connections.ToArrayLocked().Count(con => con.state == ConnectionStates.Playing ).ToString() };
                                    variables["UPTIME"] = new string[] { (DateTime.Now - Game.Instance.GameStarted).TotalSeconds.ToString() };
                                    variables["HOSTNAME"] = new string[] { "kbs-cloud.com" };
                                    variables["PORT"] = new string[] { Settings.Port.ToString() };
                                    variables["SSL"] = new string[] { Settings.SSLPort.ToString() };
                                    variables["CODEBASE"] = new string[] { "CUSTOM" };
                                    variables["WEBSITE"] = new string[] { "https://kbs-cloud.com" };

                                    sendRaw(TelnetProtocol.ServerGetNegotiateMUDServerStatusProtocol(variables), true);
                                    sendRaw(TelnetProtocol.ServerGetWillMUDExtensionProtocol, true);
                                }
                                else if(command.Type == TelnetProtocol.Command.Types.DontMUDServerStatusProtocol)
                                {
                                    Game.log("WONT MSSP");

                                    sendRaw(TelnetProtocol.ServerGetWillMUDExtensionProtocol, true);
                                }
                                else if(command.Type == TelnetProtocol.Command.Types.DoMUDExtensionProtocol)
                                {
                                    TelnetOptions.SETBIT(TelnetOptionFlags.MUDeXtensionProtocol);
                                    Game.log("MXP Enabled.");
                                }
                            });
                        if(newbyteindex > byteindex)
                        byteindex = newbyteindex;
                        this.ReceiveBufferBacklog = carryover;
                    }
                    // else skip the character
                }
            }
        }

        public void EndReceive(IAsyncResult result)
        {
            lock (Game.Instance.Info.Connections)
                try
                {
                    var socket = result.AsyncState as Socket;
                    //if (connection.socket == null) return;
                    var connections = Game.Instance.Info.Connections.ToArrayLocked();
                    var connection = (from c in connections where c.socket == socket select c).FirstOrDefault();
                    lock (connection)
                    {

                        var received = connection.socket.EndReceive(result);
                        if (received == 0)
                        {
                            Game.CloseSocket(connection, connection.state < ConnectionStates.Playing, true);
                            connection.inanimate = DateTime.Now;
                        }
                        else
                        {
                            connection.ProcessBytes(connection.receivebuffer, received);
                            if (connection.readop == null || connection.readop.IsCompleted)
                                connection.readop = connection.socket.BeginReceive(connection.receivebuffer, 0, connection.receivebuffer.Length, SocketFlags.None, EndReceive, socket);
                        }
                    }

                }
                catch { }
        }

        public void EndReceiveSsl(IAsyncResult result)
        {
            lock (Game.Instance.Info.Connections)
                try
                {
                    var socket = result.AsyncState as SslStream;
                    //if (connection.socket == null) return;
                    var connections = Game.Instance.Info.Connections.ToArrayLocked();
                    var connection = (from c in connections where c.sslsocket == socket select c).FirstOrDefault();
                    //if (connection.socket == null) return;
                    //var connections = Game.Instance.Info.Connections.ToArrayLocked();
                    //connection = (from c in connections where c.Name == connection.Name select c).FirstOrDefault();
                    lock (connection)
                    {

                        var received = socket.EndRead(result);
                        if (received == 0)
                        {
                            Game.CloseSocket(connection, connection.state < ConnectionStates.Playing, true);
                            connection.inanimate = DateTime.Now;
                        }
                        else
                        {
                            connection.ProcessBytes(connection.receivebuffer, received);
                            try
                            {
                                if (connection.readop == null || connection.readop.IsCompleted)
                                    connection.readop = connection.sslsocket.BeginRead(connection.receivebuffer, 0, connection.receivebuffer.Length, EndReceiveSsl, connection.sslsocket);
                            }
                            catch
                            {
                                try
                                {
                                    connection.socket.Close();
                                }
                                catch { }
                                connection.socket = null;

                            }
                        }
                    }

                }
                catch
                {

                }
        }
        internal void ProcessOutput()
        {
            if (socket == null) output.Clear();

            if (output.Length > 0)
            {
                if (state == ConnectionStates.Playing)
                {
                    if (Fighting != null)
                    {
                        string health = "";
                        var hp = (float)Fighting.HitPoints / (float)Fighting.MaxHitPoints;
                        if (hp == 1)
                            health = "is in perfect health.";
                        else if (hp > .8)
                            health = "is covered in small scratches.";
                        else if (hp > .7)
                            health = "has some small wounds.";
                        else if (hp > .6)
                            health = "has some larger wounds.";
                        else if (hp > .5)
                            health = "is bleeding profusely.";
                        else if (hp > .4)
                            health = "writhing in agony.";
                        else if (hp > 0)
                            health = "convulsing on the ground.";
                        else
                            health = "is dead.";
                        //output.Append(fighting.Display(this) + " " + health + "\n\r");
                        Act("$N " + health + "\n\r", Fighting);
                    }
                    //socket.Send(System.Text.ASCIIEncoding.ASCII.GetBytes("\n\r"));

                    DisplayPrompt();
                    if (HasPageText)
                    {
                        send("\n\r[Hit Enter to Continue]");
                        ((Player)this).SittingAtPrompt = true;
                    }
                    //output.AppendFormat("\n<{0}%hp {1}%m {2}%mv {3}> ",
                    //    HitPoints > 0 ? Math.Floor((float)HitPoints / (float)MaxHitPoints * 100) : 0,
                    //    ManaPoints > 0 ? Math.Floor((float)ManaPoints / (float)MaxManaPoints * 100) : 0,
                    //    MovementPoints > 0 ? Math.Floor((float)MovementPoints / (float)MaxMovementPoints * 100) : 0,
                    //    this.Room != null ? RoomData.sectors[Room.sector].display : "");
                }
                var text = output.ToString();
                if (state == ConnectionStates.Playing && (Position == Positions.Fighting || SittingAtPrompt) && !text.StartsWith("\n\r"))
                    text = "\n\r" + text;

                sendRaw(text);
                output.Clear();
                SittingAtPrompt = true;
            }
        }

        public void DisplayPrompt()
        {
            if (EditingArea != null)
            {
                send("\nEditing Area {0} - {1} - {2}", EditingArea.Name, EditingArea.VNumStart, EditingArea.VNumEnd);
            }
            if (EditingRoom != null)
            {
                send("\nEditing room \\y{0}\\x - \\Y{1}\\x", EditingRoom.Vnum, EditingRoom.Name.ISEMPTY() ? "no name" : EditingRoom.Name);
            }
            if (EditingNPCTemplate != null)
            {
                send("\nEditing npc {0} - {1}", EditingNPCTemplate.Vnum, EditingNPCTemplate.Name.ISEMPTY() ? "no name" : EditingNPCTemplate.Name);
            }
            if (EditingItemTemplate != null)
            {
                send("\nEditing item {0} - {1}", EditingItemTemplate.Vnum, EditingItemTemplate.Name.ISEMPTY() ? "no name" : EditingItemTemplate.Name);
            }
            if (EditingHelp != null)
            {
                send("\nEditing help {0} - {1}", EditingHelp.vnum, EditingHelp.keyword);
            }
            if (Prompt.ISEMPTY())
                send("\n" + FormatPrompt("<%1%%h %2%%m %3%%mv %W> "));
            else
                send("\n" + FormatPrompt(Prompt));

            //send("\n<{0}%hp {1}%m {2}%mv {3}> ",
            //            HitPoints > 0 ? Math.Floor((float)HitPoints / (float)MaxHitPoints * 100) : 0,
            //            ManaPoints > 0 ? Math.Floor((float)ManaPoints / (float)MaxManaPoints * 100) : 0,
            //            MovementPoints > 0 ? Math.Floor((float)MovementPoints / (float)MaxMovementPoints * 100) : 0,
            //            this.Room != null ? RoomData.sectors[Room.sector].display : "");
        }

        private bool NewCharacterInputHandler(string line)
        {
            if (state == ConnectionStates.GetName)
            {
                var numbers = new int[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '+', '=', '_' };
                if (line.Length < 3 || line.Length > 16)
                {
                    send("Name must be between 3 and 16 characters in length.\n\rWhat is your name: ");
                    return true;
                }
                else if (line.Contains(" ") || line.Any(c => numbers.Contains(c)))
                {
                    send("Name cannot contain spaces or numbers.\n\rWhat is your name: ");
                    return true;
                }
                else if (InvalidNames.IsName(line, true) || line.StringPrefix("self"))
                {
                    send("That name is not allowed.\n\rWhat is your name: ");
                    return true;
                }
                else if (Character.Characters.Any(npc => npc.IsNPC && npc.Name.IsName(line, true)))
                {
                    send("That name is taken.\n\rWhat is your name: ");
                    return true;
                }
                Name = line[0].ToString().ToUpper() + line.Substring(1).ToLower();

                if (Game.CheckIsBanned(Name, string.Empty))
                {
                    WizardNet.Wiznet(WizardNet.Flags.Logins, "Banned Login Attempt - {0} at {1}", null, null, Name, Address);

                    try
                    {
                        sendRaw("You are banned.\n\r");
                        Game.CloseSocket(this, true);
                    }
                    catch
                    {

                    }
                }

                send("What is your password? ");
                if (System.IO.File.Exists(Settings.PlayersPath + "\\" + Name + ".xml"))
                {
                    if (LoadCharacterFile(Settings.PlayersPath + "\\" + Name + ".xml"))
                        state = ConnectionStates.GetPassword;
                    else
                        state = ConnectionStates.GetNewPassword;
                }
                else if (System.IO.File.Exists(Settings.DataPath + "\\" + Name + ".chr"))
                {
                    if (LoadCharacterFile(Settings.DataPath + "\\" + Name + ".chr"))
                        state = ConnectionStates.GetPassword;
                    else
                        state = ConnectionStates.GetNewPassword;
                }
                else
                {
                    state = ConnectionStates.GetNewPassword;
                }
            }
            else if (state == ConnectionStates.GetNewPassword)
            {
                password = MD5.ComputeHash(line + "salt");
                send("Confirm your password: ");
                state = ConnectionStates.ConfirmPassword;
            }
            else if (state == ConnectionStates.ConfirmPassword)
            {
                if (MD5.ComputeHash(line + "salt") == password)
                {
                    SetState(ConnectionStates.GetColorOn);
                    //state = connectionState.getRace;
                    //send("What is your race? (" + string.Join(" ", (from race in Race.Races select race.name)) + ") ");
                }
                else
                {
                    send("Wrong password.\nEnter Password: ");
                    state = ConnectionStates.GetNewPassword;
                }
            }
            else if (state == ConnectionStates.GetColorOn)
            {
                if ("yes".StringPrefix(line))
                {
                    Flags.ADDFLAG(ActFlags.Color);
                }
                else if ("no".StringPrefix(line))
                {
                    Flags.REMOVEFLAG(ActFlags.Color);
                }
                else
                {
                    SetState(ConnectionStates.GetColorOn);
                    return true;
                }
                SetState(ConnectionStates.GetSex);

            }
            else if (state == ConnectionStates.GetPassword)
            {
                if (MD5.ComputeHash(line + "salt") == password)
                {
                    if (Game.Instance.Info.Connections.ToArrayLocked().Any(connection => connection.Name.equals(Name) && connection.state == ConnectionStates.Playing))
                    {
                        state = ConnectionStates.GetPlayerAlreadyLoggedIn;
                        send("This player is already connected, continuing will disconnect them. Continue? ");
                    }
                    else
                    {
                        ConnectExistingPlayer();
                    }
                }
                else
                {
                    sendRaw("Incorrect password!\n\r");
                    Game.CloseSocket(this, true, false);
                    return true;
                }
            }
            else if (state == ConnectionStates.GetPlayerAlreadyLoggedIn)
            {
                if ("yes".StringPrefix(line))
                {
                    foreach (var connection in Game.Instance.Info.Connections.ToArrayLocked().Where(connection => connection.Name == Name && connection != this))
                    {
                        try
                        {
                            connection.sendRaw("This character is being logged in elsewhere.\n\r");
                            if (connection.socket != null)
                                connection.socket.Close();
                            connection.socket = null;
                            connection.sslsocket = null;
                        }
                        catch { }

                        try
                        {
                            if (connection.Room != null && connection.socket != null)
                            {
                                connection.Act("$n loses their animation.", null, null, null, ActType.ToRoom);

                                try
                                {
                                    Game.CloseSocket(this, true, false);
                                }
                                catch { }


                                //connection.SaveCharacterFile();
                                //connection.RemoveCharacterFromRoom();
                            }
                            lock (game.Info.Connections)
                                lock (this)
                                    lock (connection)
                                    {
                                        game.Info.Connections.Remove(this);
                                        connection.inanimate = null;
                                        connection.readop = this.readop;
                                        connection.writeop = this.writeop;
                                        connection.socket = this.socket;
                                        connection.sslsocket = this.sslsocket;
                                        connection.receivebuffer = this.receivebuffer;
                                        connection.Address = this.Address;
                                        connection.TelnetOptions = this.TelnetOptions;
                                        //if (this.sslsocket != null)
                                        //{
                                        //    this.sslsocket.EndRead(this.readop);
                                        //    connection.sslsocket.BeginRead(connection.receivebuffer, 0, connection.receivebuffer.Length, connection.EndReceiveSsl, connection);
                                        //}
                                        //else if (this.socket != null)
                                        //{
                                        //    this.socket.EndReceive(this.readop);
                                        //    connection.socket.BeginReceive(connection.receivebuffer, 0, connection.receivebuffer.Length, SocketFlags.None, connection.EndReceive, connection);
                                        //}

                                        //this.readop = null;
                                        //this.writeop = null;
                                        this.socket = null;
                                        this.sslsocket = null;

                                        connection.ConnectExistingPlayer(true);


                                    }


                        }
                        catch { }
                    }
                    //ConnectExistingPlayer(true);
                }
                else
                {
                    sendRaw("Goodbye.\n\r");
                    Game.CloseSocket(this, true);
                }

            }
            else if (state == ConnectionStates.GetSex)
            {
                if ("male".StringPrefix(line))
                {
                    Sex = Sexes.Male;
                }
                else if ("female".StringPrefix(line))
                {
                    Sex = Sexes.Female;
                }
                else
                {
                    send("What is your sex? (male female) ");
                    return true;
                }
                SetState(ConnectionStates.GetRace);
            }
            else if (state == ConnectionStates.GetRace)
            {
                if ((PcRace = PcRace.GetRace(line, true)) == null)
                {
                    send("What is your race? (" + string.Join(" ", (from race in PcRace.PcRaces select race.name)) + ") ");
                    return true;
                }
                else
                {
                    Race = Race.GetRace(PcRace.name);
                    PermanentStats = new PhysicalStats(Race.Stats);
                    Size = Race.Size;
                    SetState(ConnectionStates.GetGuild);
                    //if (race.alignments.Count == 1)
                    //{
                    //    state = connectionState.getEthos;
                    //    send("You are " + race.alignments[0].ToString() + ".\n\r");
                    //    alignment = race.alignments[0];

                    //    if (race.ethosChoices.Count == 1)
                    //    {
                    //        send("You are " + race.ethosChoices[0].ToString() + ".\n\r");
                    //        ethos = race.ethosChoices[0];
                    //        SetState(connectionState.playing);
                    //    }
                    //    else
                    //        send("What is your ethos? (" + String.Join(", ", from ethos in race.ethosChoices select ethos.ToString()) + ") ");
                    //}
                    //else
                    //{
                    //    state = connectionState.getAlignment;

                    //    send("What is your alignment: (" + String.Join(", ", from alignment in race.alignments select alignment.ToString()) + ")");
                    //}
                }
            } // end get race
            else if (state == ConnectionStates.GetGuild)
            {
                foreach (var guild in GuildData.Guilds)
                    if (guild.races.Contains(PcRace) && guild.name.IsName(line))
                    {
                        this.Guild = guild;
                        Title = (Guild != null && Guild.Titles.ContainsKey(Level) ? "the " + Guild.Titles[Level].MaleTitle : "");
                    }

                if (this.Guild == null)
                    SetState(ConnectionStates.GetGuild);
                else if (this.Guild.startingWeapon == 0)
                    SetState(ConnectionStates.GetDefaultWeapon);
                else
                    SetState(ConnectionStates.GetAlignment);
            }
            else if (state == ConnectionStates.GetDefaultWeapon)
            {
                int index;
                var weapons = new string[] { "sword", "axe", "spear", "staff", "dagger", "mace", "whip", "flail", "polearm" };
                var weaponVNums = new int[] { 40000, 40001, 40004, 40005, 40002, 40003, 40006, 40007, 40020 };
                if (!string.IsNullOrEmpty(line.Trim()))
                {
                    for (index = 0; index < weapons.Length; index++)
                    {
                        ItemTemplateData item;
                        if (weapons[index].StringPrefix(line) && ItemTemplateData.Templates.TryGetValue(weaponVNums[index], out item))
                        {
                            var weapon = new ItemData(item, this, true);
                            var skill = SkillSpell.SkillLookup(weapons[index]);
                            if (skill != null)
                                LearnSkill(skill, 75);
                            SetState(ConnectionStates.GetAlignment);
                            return true;
                        }
                    }
                }
                SetState(ConnectionStates.GetDefaultWeapon);
                return true;
            }
            else if (state == ConnectionStates.GetAlignment)
            {
                Alignment alignmentValue = Alignment.Unknown;
                if (Utility.GetEnumValueStrPrefix<Alignment>(line, ref alignmentValue) && PcRace.alignments.Contains(alignmentValue) && Guild.alignments.Contains(alignmentValue))
                {
                    Alignment = alignmentValue;
                    send("You are " + Alignment.ToString() + ".\n\r");
                }
                else
                {
                    send("What is your alignment: (" + String.Join(", ", from alignment in PcRace.alignments where Guild.alignments.Contains(alignment) select alignment.ToString()) + ")");
                    return true;
                }
                send("What is your ethos? (" + String.Join(", ", from ethos in PcRace.ethosChoices select ethos.ToString()) + ") ");
                state = ConnectionStates.GetEthos;
            }
            else if (state == ConnectionStates.GetEthos)
            {
                Ethos ethosValue = Ethos.Unknown;
                if (Utility.GetEnumValueStrPrefix<Ethos>(line, ref ethosValue) && PcRace.ethosChoices.Contains(ethosValue))
                {
                    Ethos = ethosValue;
                    send("You are " + Ethos.ToString() + ".\n\r");
                }
                else
                {
                    send("What is your ethos? (" + String.Join(", ", from ethos in PcRace.ethosChoices select ethos.ToString()) + ") ");
                    state = ConnectionStates.GetEthos;
                    return true;
                }
                SetState(ConnectionStates.Playing);
            }
            else
                return false;

            return true;
        }
        internal bool ProcessInput()
        {
            if (input.ToString().Contains((char)10))
            {
                var line = input.Replace("\r", "").ToString();  //input.ToString();
                LastActivity = DateTime.Now;
                var newLineIndex = line.IndexOf((char)10);
                line = line.Substring(0, newLineIndex);//;
                input.Remove(0, newLineIndex + 1);
                line = line.Trim('\n', '\r');
                if (line.Length > 120 && this.Level < Game.LEVEL_IMMORTAL)
                {
                    line = "";
                    send("Line too long.\n\r");
                }
                if (!IsImmortal) line = line.EscapeColor();
                if (!NewCharacterInputHandler(line) && state == ConnectionStates.Playing)
                {

                    if (HasPageText && line.TOSTRINGTRIM() == "")
                    {
                        SendPage();
                    }
                    else if (HasPageText) { ClearPage(); send("Paged text cleared.\n\r"); }
                    else
                    {
                        this.DoCommand(line);
                        return true;
                    }
                }
            }
            return false;
        }

        private void ConnectExistingPlayer(bool reconnect = false)
        {
            state = ConnectionStates.Playing;
            LastActivity = DateTime.Now;
            int playersonline = 0;

            if ((playersonline = game.Info.Connections.ToArrayLocked().Count(p => p.state == ConnectionStates.Playing)) > Game.Instance.MaxPlayersOnline)
            {
                Game.Instance.MaxPlayersOnline = playersonline;
                if (playersonline > Game.MaxPlayersOnlineEver)
                {
                    Game.MaxPlayersOnlineEver = playersonline;
                    if (System.IO.File.Exists("Settings.xml"))
                    {
                        Settings.Save();
                    }
                }
            }
            WizardNet.Wiznet(WizardNet.Flags.Logins, "{0} logged in at {1}", null, null, Name, Address);
            Character.ReadHelp(this, "greeting", true);
            send("\n\rWelcome to the Crimson Stained Lands!\n\r\n\r");
            Character.ReadHelp(this, "MOTD", true);
            if(!Flags.ISSET(ActFlags.Color) && TelnetOptions.ISSET(TelnetOptionFlags.Ansi))
            {
                send("\n\rIt appears your client supports color. Type color to turn it on!\n\r\n\r");
            }
            var notecount = (from note in NoteData.Notes where note.Sent > LastReadNote && (note.To.IsName("all", true) || note.To.IsName(Name, true)) select note).Count();

            if (notecount > 0)
                send("{0} unread notes.\n\r", notecount);
            send("\n");
            BonusInfo.DoBonus(this, "");
            if (!reconnect)
                Position = Positions.Standing;
            if (Room == null)
            {
                RoomData room;
                if (!RoomData.Rooms.TryGetValue(roomVnum, out room))
                    RoomData.Rooms.TryGetValue(3760, out room);
                if (room != null)
                {
                    AddCharacterToRoom(room);
                }
                if (!reconnect)
                    Act("$n appears in the room.", type: ActType.ToRoom);
            }




            //DoLook(this, "auto");
            if (!reconnect)
                LoadPet();
            if (reconnect)
            {
                Act("$n regains their animation.", type: ActType.ToRoom);
                lock (game.Info.Connections)
                {
                    game.Info.Connections.Remove(this);
                    game.Info.Connections.Add(this);
                }
            }
        }

        /// <summary>
        /// Handle state for new players.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool SetState(ConnectionStates state)
        {
            if (state == ConnectionStates.GetColorOn)
            {
                this.state = ConnectionStates.GetColorOn;
                send("Would you like to turn color on? (Y/N) ");
            }
            else if (state == ConnectionStates.GetSex)
            {
                this.state = ConnectionStates.GetSex;
                send("What is your sex? (male female) ");
            }
            else if (state == ConnectionStates.GetRace)
            {
                this.state = ConnectionStates.GetRace;
                send("What is your race? (" + string.Join(" ", (from race in PcRace.PcRaces select race.name)) + ") ");
            }
            else if (state == ConnectionStates.GetEthos)
            {
                this.state = ConnectionStates.GetEthos;
                //send("You are " + race.alignments[0].ToString() + ".\n\r");
                //alignment = race.alignments[0];

                if (PcRace.ethosChoices.Count == 1)
                {
                    send("You are " + PcRace.ethosChoices[0].ToString() + ".\n\r");
                    Ethos = PcRace.ethosChoices[0];
                    SetState(ConnectionStates.Playing);
                }
                else
                    send("What is your ethos? (" + String.Join(", ", from ethos in PcRace.ethosChoices select ethos.ToString()) + ") ");
            }
            else if (state == ConnectionStates.GetAlignment)
            {
                if (PcRace.alignments.Count == 1)
                {
                    send("You are " + PcRace.alignments[0].ToString() + ".\n\r");
                    Alignment = PcRace.alignments[0];
                    SetState(ConnectionStates.GetEthos);
                }
                else if (Guild != null && Guild.alignments.Count == 1)
                {
                    send("You are " + Guild.alignments[0].ToString() + ".\n\r");
                    Alignment = Guild.alignments[0];
                    SetState(ConnectionStates.GetEthos);

                }
                else
                {
                    this.state = ConnectionStates.GetAlignment;
                    send("What is your alignment: (" + String.Join(", ", from alignment in PcRace.alignments where Guild.alignments.Contains(alignment) select alignment.ToString()) + ")");
                }
            }
            else if (state == ConnectionStates.GetGuild)
            {
                this.state = ConnectionStates.GetGuild;
                var guilds = new List<GuildData>();
                foreach (var guild in GuildData.Guilds)
                    if (guild.races.Contains(PcRace))
                        guilds.Add(guild);
                send("What is your guild? ({0}) ", string.Join(",", from guild in guilds select guild.name));
            }
            else if (state == ConnectionStates.GetDefaultWeapon)
            {
                var weapons = new string[] { "sword", "axe", "spear", "staff", "dagger", "mace", "whip", "flail", "polearm" };
                send("What weapon will you start with? ({0}) ", string.Join(",", weapons));
                this.state = ConnectionStates.GetDefaultWeapon;
            }
            else if (state == ConnectionStates.Playing)
            {
                var group = SkillSpellGroup.Lookup(Guild.guildGroup);

                foreach (var classSkill in SkillSpell.Skills)
                {
                    if (classSkill.Value.skillLevel.ContainsKey(Guild.name)) // && classSkill.Value.skillLevel[Guild.name] == 1)
                        LearnSkill(classSkill.Value, 1);
                    //if (!Learned.ContainsKey(classSkill.Value) || Learned[classSkill.Value] == null || Learned[classSkill.Value].Percentage == 0)
                    //{
                    //    Learned[classSkill.Value] = new LearnedSkillSpell();
                    //    Learned[classSkill.Value].Percentage = 1;
                    //    Learned[classSkill.Value].Level = classSkill.Value.skillLevel[Guild.name];
                    //}
                }


                if (group != null)
                    group.LearnGroup(this, 1);

                group = SkillSpellGroup.Lookup(Guild.guildBasicsGroup);
                if (group != null)
                    group.LearnGroup(this, 100);



                var skill = SkillSpell.SkillLookup("recall");
                if (skill != null)
                    LearnSkill(skill, 100);

                ItemTemplateData item;
                if (Guild.startingWeapon != 0 && ItemTemplateData.Templates.TryGetValue(Guild.startingWeapon, out item))
                {
                    int index;
                    var weapons = new string[] { "sword", "axe", "spear", "staff", "dagger", "mace", "whip", "flail", "polearm" };
                    var weaponVNums = new List<int> { 40000, 40001, 40004, 40005, 40002, 40003, 40006, 40007, 40020 };
                    index = weaponVNums.IndexOf(Guild.startingWeapon);

                    var weapon = new ItemData(item, this, true);

                    if (index >= 0)
                    {
                        skill = SkillSpell.SkillLookup(weapons[index]);
                        if (skill != null)
                            LearnSkill(skill, 75);
                    }
                }

                if (Guild.name == "mage") LearnSkill(SkillSpell.SkillLookup("hand to hand"), 75);

                if (System.IO.File.Exists(Settings.PlayersPath + "\\" + Name + ".xml"))
                {
                    sendRaw("It seems someone else has taken this name. Unable to continue.\n\r");
                    Game.CloseSocket(this, true);
                    return false;
                }

                this.state = ConnectionStates.Playing;
                LastActivity = DateTime.Now;
                WizardNet.Wiznet(WizardNet.Flags.Logins, "{0} logged in at {1}", null, null, Name, Address);
                int playersonline = 0;
                if ((playersonline = game.Info.Connections.ToArrayLocked().Count(p => p.state == ConnectionStates.Playing)) > Game.Instance.MaxPlayersOnline)
                {
                    Game.Instance.MaxPlayersOnline = playersonline;
                    if (playersonline > Game.MaxPlayersOnlineEver)
                    {
                        Game.MaxPlayersOnlineEver = playersonline;
                        if (System.IO.File.Exists("Settings.xml"))
                        {
                            Settings.Save();
                        }
                    }
                }

                Character.ReadHelp(this, "greeting", true);
                Character.ReadHelp(this, "MOTD", true);
                AddCharacterToRoom(RoomData.Rooms[3760]);

                Position = Positions.Standing;

                Act("$n appears in the room suddenly.\n\r", type: ActType.ToRoom);
                send("\n\rWelcome to the Crimson Stained Lands\n\r\n\r");
                if(!Flags.ISSET(ActFlags.Color) && TelnetOptions.ISSET(TelnetOptionFlags.Ansi))
                {
                    send("\n\rIt appears your client supports color. Type color to turn it on!\n\r\n\r");
                }
                Character.DoOutfit(this, "");

                HitPoints = MaxHitPoints;
                MovementPoints = MaxMovementPoints;

                Flags.ADDFLAG(ActFlags.AutoAssist);
                Flags.ADDFLAG(ActFlags.AutoExit);
                Flags.ADDFLAG(ActFlags.AutoGold);
                Flags.ADDFLAG(ActFlags.AutoSplit);
                Flags.ADDFLAG(ActFlags.AutoLoot);
                Flags.ADDFLAG(ActFlags.AutoSac);
                Flags.ADDFLAG(ActFlags.NewbieChannel);

                SaveCharacterFile();

                BonusInfo.DoBonus(this, "");
                DoLook(this, "auto");
            }

            return true;
        }

        public void SaveCharacterFile()
        {
            TotalPlayTime += DateTime.Now - LastSaveTime;
            LastSaveTime = DateTime.Now;

            if (!Directory.Exists(Settings.PlayersPath))
                Directory.CreateDirectory(Settings.PlayersPath);

            if (Name != null && password != null && Race != null && Room != null)
            {
                var element = this.Element;
                element.Name = "PlayerData";
                if (Quests.Any())
                    element.Add(new XElement("Quests", from quest in Quests select quest.Element));
                element.Add(new XElement("Prompt", Prompt));

                element.Add(new XElement("ShapeFocusMajor", ShapeFocusMajor.ToString()));
                element.Add(new XElement("ShapeFocusMinor", ShapeFocusMinor.ToString()));
                element.Add(new XElement("Wimpy", Wimpy.ToString()));
                if (WeaponSpecializations > 0)
                    element.Add(new XElement("WeaponSpecializations", WeaponSpecializations.ToString()));

                element.Add(new XElement("LastReadNote", LastReadNote.ToString()));
                element.Add(new XElement("TotalPlayTime", TotalPlayTime.ToString()));
                element.Add(new XElement("password", password));
                element.Save(Settings.PlayersPath + "\\" + Name + ".xml");

            }
            //File.WriteAllText("data\\" + name + ".chr", "Password " + password + "\n\r" + "Race " + race.Name + "\n\r" + "Alignment " + alignment.ToString() + "\n\r" + "Ethos " + ethos.ToString() + "\n\r" + (inRoom != null ? "Room " + inRoom.vnum + "\n\r" : "") + "Guild " + guild + "\n\r");
        }

        public bool LoadCharacterFile(string path)
        {
            LastSaveTime = DateTime.Now;
            if (!System.IO.Directory.Exists(Settings.PlayersPath))
                Directory.CreateDirectory(Settings.PlayersPath);

            if (path.EndsWith(".xml"))
            {
                var element = XElement.Load(path);

                if (element.Name.ToString().StringCmp("PlayerData"))
                {
                    var playerData = element;
                    Name = element.GetElement("name").Value;
                    password = element.GetElement("password").Value;
                    PcRace = PcRace.GetRace(element.GetElementValue("race", "human")) ?? PcRace.GetRace("human");
                    Race = Race.GetRace(element.GetElementValue("race", "human")) ?? Race.GetRace("human");
                    Utility.GetEnumValue<Alignment>(element.GetElementValue("alignment", "neutral"), ref Alignment);
                    Utility.GetEnumValue<Ethos>(element.GetElementValue("ethos", "neutral"), ref Ethos);
                    var inRoom = element.HasElement("room") ? element.GetElement("room").Value : "3760";
                    int.TryParse(inRoom, out this.roomVnum);
                    Guild = element.HasElement("guild") ? GuildData.GuildLookup(element.GetElementValue("guild")) : GuildData.GuildLookup("warrior");
                    Level = element.GetElementValueInt("Level", 1);
                    Title = element.HasElement("Title") && !element.GetElementValue("Title").ISEMPTY() ? element.GetElementValue("Title") : (Guild != null && Guild.Titles.ContainsKey(Level) ? "the " + Guild.Titles[Level].MaleTitle : "");
                    ExtendedTitle = element.GetElementValue("ExtendedTitle");
                    Prompt = element.GetElementValue("Prompt", "<%1%%h %2%%m %3%%mv %W> ");

                    Xp = element.GetElementValueInt("Xp", 0);
                    XpTotal = element.GetElementValueInt("XpTotal", 0);
                    MaxHitPoints = element.GetElementValueInt("maxhitpoints", 100);
                    MaxManaPoints = element.GetElementValueInt("maxmanapoints", 100);
                    MaxMovementPoints = element.GetElementValueInt("maxmovementpoints", 100);
                    HitPoints = element.GetElementValueInt("hitpoints", 100);
                    ManaPoints = element.GetElementValueInt("manapoints", 100);
                    MovementPoints = element.GetElementValueInt("movementpoints", 100);
                    Practices = element.GetElementValueInt("practices", 0);
                    Trains = element.GetElementValueInt("trains", 0);
                    HitRoll = element.GetElementValueInt("HitRoll", 0);
                    DamageRoll = element.GetElementValueInt("DamRoll", 0);
                    SavingThrow = element.GetElementValueInt("SavingThrow");
                    Utility.GetEnumValue<CharacterSize>(element.GetElementValue("Size", "Medium"), ref Size, CharacterSize.Medium);

                    Sexes sex = Sexes.Either;
                    Utility.GetEnumValue<Sexes>(element.GetElementValue("Sex", "none"), ref sex);
                    Sex = sex;

                    ArmorClass = element.GetElementValueInt("ArmorClass", 0);
                    ArmorBash = element.GetElementValueInt("ArmorBash");
                    ArmorSlash = element.GetElementValueInt("ArmorSlash");
                    ArmorPierce = element.GetElementValueInt("ArmorPierce");
                    ArmorExotic = element.GetElementValueInt("ArmorExotic");

                    Gold = element.GetElementValueLong("gold");
                    Silver = element.GetElementValueLong("silver");
                    //GoldBank = element.GetElementValueLong("goldbank");
                    SilverBank = element.GetElementValueLong("silverbank");

                    Utility.GetEnumValue(element.GetElementValue("ShapeFocusMajor"), ref ShapeFocusMajor, ShapeshiftForm.FormType.None);
                    Utility.GetEnumValue(element.GetElementValue("ShapeFocusMinor"), ref ShapeFocusMinor, ShapeshiftForm.FormType.None);

                    Hunger = element.GetElementValueInt("hunger", 48);
                    Thirst = element.GetElementValueInt("thirst", 48);
                    Drunk = element.GetElementValueInt("drunk", 0);
                    Dehydrated = element.GetElementValueInt("dehydrated", 0);
                    Starving = element.GetElementValueInt("starving", 0);
                    Wimpy = element.GetElementValueInt("Wimpy", 0);
                    WeaponSpecializations = element.GetElementValueInt("WeaponSpecializations");

                    var totalPlayTime = element.GetElementValue("TotalPlayTime");
                    TimeSpan.TryParse(totalPlayTime, out TotalPlayTime);

                    var lastReadNote = element.GetElementValue("LastReadNote");
                    DateTime.TryParse(lastReadNote, out LastReadNote);

                    Guild = GuildData.GuildLookup(element.GetElementValue("guild", "warrior")) ?? GuildData.GuildLookup("warrior");

                    bool Color256 = TelnetOptions.ISSET(TelnetOptionFlags.Color256), ColorRGB = TelnetOptions.ISSET(TelnetOptionFlags.ColorRGB);

                    if (element.HasElement("Flags"))
                        Utility.GetEnumValues<ActFlags>(element.GetElementValue("flags"), ref this.Flags);

                    if (Color256) TelnetOptions.SETBIT(TelnetOptionFlags.Color256);
                    if (ColorRGB) TelnetOptions.SETBIT(TelnetOptionFlags.ColorRGB);

                    Utility.GetEnumValues<WeaponDamageTypes>(element.GetElementValue("Immune"), ref this.ImmuneFlags);
                    Utility.GetEnumValues<WeaponDamageTypes>(element.GetElementValue("Resist"), ref this.ResistFlags);
                    Utility.GetEnumValues<WeaponDamageTypes>(element.GetElementValue("Vulnerable"), ref this.VulnerableFlags);


                    if (element.HasElement("affectedBy"))
                        Utility.GetEnumValues<AffectFlags>(element.GetElementValue("affectedBy"), ref this.AffectedBy);

                    if (element.HasElement("WiznetFlags"))
                        Utility.GetEnumValues<WizardNet.Flags>(element.GetElementValue("WiznetFlags"), ref this.WiznetFlags);

                    if (element.HasElement("PermanentStats"))
                    {
                        PermanentStats = new PhysicalStats(element.Element("PermanentStats"));
                    }

                    if (element.HasElement("ModifiedStats"))
                    {
                        ModifiedStats = new PhysicalStats(element.Element("ModifiedStats"));
                    }

                    if (element.HasElement("Learned"))
                    {
                        foreach (var skillSpellElement in element.Element("Learned").Elements())
                        {

                            SkillSpell skillSpell;
                            if (skillSpellElement.HasAttribute("Name") &&
                                (skillSpell = SkillSpell.SkillLookup(skillSpellElement.GetAttributeValue("Name"))) != null)
                            {
                                LearnSkill(skillSpell, skillSpellElement.GetAttributeValueInt("Value"), skillSpellElement.GetAttributeValueInt("Level", 60));
                            }
                        }
                    }

                    if (element.HasElement("Affects"))
                    {
                        var affects = element.GetElement("Affects");

                        foreach (var affElement in affects.Elements())
                        {
                            var newAff = new AffectData(affElement);
                            //if (newAff.flags.Any(f => f >= AffectFlags.Holy && f <= AffectFlags.Summon))
                            //{
                            //    var flaglist = string.Join(" ", from f in newAff.flags where f >= AffectFlags.Holy && f <= AffectFlags.Summon select f.ToString());
                            //    newAff.DamageTypes.AddRange(utility.LoadFlagList<WeaponDamageTypes>(flaglist));
                            //    newAff.flags.RemoveAll(f => f >= AffectFlags.Holy && f <= AffectFlags.Summon);
                            //}
                            this.AffectsList.Insert(0, newAff);
                        }
                    }

                    if (element.HasElement("Inventory"))
                    {
                        var inventoryElement = element.GetElement("Inventory");
                        foreach (var itemElement in inventoryElement.Elements())
                        {
                            var item = new ItemData(itemElement);
                            Inventory.Add(item);
                            item.CarriedBy = this;
                        }

                    }

                    if (element.HasElement("Equipment"))
                    {
                        var eqElement = element.GetElement("Equipment");
                        foreach (var slotElement in eqElement.Elements())
                        {
                            if (slotElement.HasAttribute("slotid") && slotElement.HasElement("Item"))
                            {
                                WearSlotIDs id = WearSlotIDs.Chest;
                                if (Utility.GetEnumValue<WearSlotIDs>(slotElement.GetAttribute("slotID").Value, ref id))
                                {
                                    Equipment[id] = new ItemData(slotElement.GetElement("Item"));
                                    Equipment[id].CarriedBy = this;
                                    // don't reapply affects saved to character stats
                                }
                            }

                        }
                    }

                    if (element.HasElement("Quests"))
                    {
                        var quests = element.GetElement("Quests");

                        foreach (var questelement in quests.Elements())
                        {
                            var quest = new QuestProgressData(questelement);
                            Quests.Add(quest);
                        }
                    }


                    return true;
                }
            }
            return false;
        } // End LoadCharacterFile

        public void LoadPet()
        {
            var element = XElement.Load(Settings.PlayersPath + "\\" + Name + ".xml");
            if (element.HasElement("Pet"))
            {
                var pet = new NPCData(element.GetElement("Pet").GetElement("Character"), Room);
                pet.Following = this;
                pet.Leader = this;
                this.Group.Add(pet);
                pet.Flags.SETBIT(ActFlags.AutoAssist);
                this.Pet = pet;
            }
        }

        public void Delete()
        {
            Act("The form of $n disappears, never to return.", type: ActType.ToRoom);

            Combat.StopFighting(this, true);
            if (Room != null && Room.Characters.Contains(this))
            {
                Room.Characters.Remove(this);
            }
            state = Player.ConnectionStates.Deleting;
            System.IO.File.Delete(Settings.PlayersPath + "\\" + Name + ".xml");
            sendRaw("Character deleted.\n\r");

            Game.CloseSocket(this, true);

        }

        public void SetPassword(string password)
        {
            if (!password.ISEMPTY() && password.Length >= 3 && password.Length <= 16)
            {
                this.password = MD5.ComputeHash(password + "salt");
            }
            else if (password.ISEMPTY())
            {
                send("You must provide a password.\n\r");
            }
            else
                send("Password must be between 3 and 16 characters.\n\r");
        }


        public string FormatPrompt(string prompt)
        {
            var buf = new StringBuilder();
            int i = 0;
            while (i < prompt.Length)
            {
                if (prompt[i] != '%')
                {
                    buf.Append(prompt[i++]);
                    continue;
                }
                ++i;
                switch (prompt[i])
                {
                    default:
                        buf.Append(' ');
                        break;
                    case '1':
                        buf.AppendFormat("{0}{1}\\x",
                            HitPoints < (MaxHitPoints * 4) / 10 ?
                            HitPoints < (MaxHitPoints * 2) / 10 ? "\\r" : "\\y" : "\\x",
                           Math.Floor((float)HitPoints / (float)MaxHitPoints * 100));
                        break;
                    case '2':
                        buf.Append(Math.Floor((float)ManaPoints / (float)MaxManaPoints * 100));
                        break;
                    case '3':
                        buf.Append(Math.Floor((float)MovementPoints / (float)MaxMovementPoints * 100));
                        break;
                    case 'e':
                        bool found = false;
                        if (!IsAffected(AffectFlags.Blind))
                        {
                            foreach (var exit in Room.exits.Where(e => e != null && e.destination != null
                            && !e.flags.ISSET(ExitFlags.Hidden)
                            && (!e.flags.ISSET(ExitFlags.HiddenWhileClosed) || !e.flags.ISSET(ExitFlags.Closed))))
                            {
                                found = true;
                                buf.Append(exit.direction.ToString().ToLower());
                            }
                        }

                        if (!found)
                            buf.Append("none");
                        break;
                    case 'c':
                        buf.AppendLine();
                        break;
                    case 'h':

                        buf.AppendFormat("{0}{1}\\x",
                        HitPoints < (MaxHitPoints * 4) / 10 ?
                        HitPoints < (MaxHitPoints * 2) / 10 ? "\\r" : "\\y" : "\\x",
                        HitPoints);
                        break;

                    case 'H':
                        buf.Append(MaxHitPoints);
                        break;
                    case 'm':
                        buf.Append(ManaPoints);
                        break;
                    case 'M':
                        buf.Append(MaxManaPoints);
                        break;
                    case 'v':
                        buf.Append(MovementPoints);
                        break;
                    case 'V':
                        buf.Append(MaxMovementPoints);
                        break;
                    case 'x':
                        buf.Append(XpTotal);
                        break;
                    case 'X':
                        buf.Append(XpToLevel * (Level) - XpTotal);
                        break;
                    case 'g':
                        buf.Append(Gold);
                        break;
                    case 's':
                        buf.Append(Silver);
                        break;
                    case 'a':
                        buf.Append(Alignment.ToString().ToLower());
                        break;
                    case 'r':
                        if (Room != null)
                            buf.Append(
                            Flags.ISSET(ActFlags.HolyLight) ||
                            (!IsAffected(AffectFlags.Blind) && !Room.IsDark)
                            ? (TimeInfo.IS_NIGHT && !Room.NightName.ISEMPTY() ? Room.NightName : Room.Name) : "darkness");
                        else
                            buf.Append(' ');
                        break;
                    case 'R':
                        if (IsImmortal && Room != null)
                            buf.Append(Room.Vnum);
                        else
                            buf.Append(' ');
                        break;
                    case 'z':
                        if (IsImmortal && Room != null)
                            buf.Append(Room.Area.Name);
                        else
                            buf.Append(' ');
                        break;
                    case 'Z':
                        buf.Append(Flags.ISSET(ActFlags.HolyLight) ? " HOLYLIGHT" : "");
                        break;
                    case '%':
                        buf.Append("%");
                        break;

                    case 't':
                        buf.Append(DateTime.Now.ToLongDateString());
                        break;
                    case 'T':
                        buf.Append((TimeInfo.Hour % 12 == 0 ? 12 : TimeInfo.Hour % 12) + (TimeInfo.Hour > 12 ? "PM" : "AM"));
                        break;
                    case 'w':
                        {
                            var sky_look = new string[]
                            {
                    "cloudless",
                    "cloudy",
                    "rainy",
                    "stormy"
                };

                            buf.Append(sky_look[(int)WeatherData.Sky]);

                            break;
                        }
                    case 'P':
                        buf.Append(Position.ToString().ToLower());
                        break;
                    case 'W':
                        buf.AppendFormat("{0} {1}",
                            Room != null ? (!Room.IsWilderness ? "\\wcivilized\\x" : "\\Gwilderness\\x") : "",
                            Room != null ? RoomData.sectors[Room.sector].display : ""
                            );
                        break;
                    case 'I':
                        buf.Append(Room.sector == SectorTypes.Inside || Room.flags.ISSET(RoomFlags.Indoors) ? "\\windoors\\x" : "\\coutdoors\\x");
                        break;
                }
                i++;
            }
            return buf.ToString();
        } // end FormatPrompt
    } // End Player

} // end CrimsonStainedLands
