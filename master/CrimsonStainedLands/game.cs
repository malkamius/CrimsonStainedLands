/***************************************************************************
*  Original Diku Mud copyright (C) 1990, 1991 by Sebastian Hammer,        *
*  Michael Seifert, Hans Henrik St{rfeldt, Tom Madsen, and Katja Nyboe.   *
*                                                                         *
*  Merc Diku Mud improvments copyright (C) 1992, 1993 by Michael          *
*  Chastain, Michael Quan, and Mitchell Tse.                              *
*                                                                         *
*  In order to use any part of this Merc Diku Mud, you must comply with   *
*  both the original Diku license in 'license.doc' as well the Merc       *
*  license in 'license.txt'.  In particular, you may not remove either of *
*  these copyright notices.                                               *
*                                                                         *
*  Thanks to abaddon for proof-reading our comm.c and pointing out bugs.  *
*  Any remaining bugs are, of course, our work, not his.  :)              *
*                                                                         *
*  Much time and thought has gone into this software and you are          *
*  benefitting.  We hope that you share your changes too.  What goes      *
*  around, comes around.                                                  *
***************************************************************************/

/***************************************************************************
*	ROM 2.4 is copyright 1993-1996 Russ Taylor			   *
*	ROM has been brought to you by the ROM consortium		   *
*	    Russ Taylor (rtaylor@pacinfo.com)				   *
*	    Gabrielle Taylor (gtaylor@pacinfo.com)			   *
*	    Brian Moore (rom@rom.efn.org)				   *
*	By using this code, you have agreed to follow the terms of the	   *
*	ROM license, in the file Tartarus/doc/rom.license                  *
***************************************************************************/

/***************************************************************************
*       Tartarus code is copyright (C) 1997-1998 by Daniel Graham          *
*	In using this code you agree to comply with the Tartarus license   *
*       found in the file /Tartarus/doc/tartarus.doc                       *
***************************************************************************/

using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Schema;
using static CrimsonStainedLands.WeatherData;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.AxHost;

namespace CrimsonStainedLands
{
    public class game : IDisposable
    {
        /*
         *  TODO
         * 
         * Help files
         * 
         * Look into using Lua or Javascript for mob/obj progs
         * 
         * Consider using loot tables (race/body part/lvl based, visible on mob spawn?)
         *
         * 
         * Look into expression trees like Drocks WoW bot used to use for Mob Class Scripting
         * 
         * impassable exits, impassable night exits, night/day hidden exits
         * 
         * MOTD
         * 
         * Cabals
         * 
         * autoloot in form
         * 
         * can see everything while sleeping
         * 
         * already flying, can't go in fly only room
         * 
         *
         * ---- was grouped but not getting xp - wasn't clearing leader, needs more testing
         * ---- healer heal command
         * ---- rescue skill, intercept for hares
         * ---- gain convert/revert
         * ---- reset timer messed up, needs more testing
         * ---- can't see skills from practice when in form
         * ---- faerie fire stacks
         * ---- can't open doors if no hands
         * ---- hitpoints in group
         * ---- armor class in score
         * ---- list while sitting
         * ---- no form skills for ferret
         * ---- > com identify boots - You can't pray to the gods about that.
         * ---- shapefocus offensive in addition to offense
         * ---- can't commune heal in combat
         * ---- halve damage of berserker strike
         * ---- enchant armor not working, was working just not to_object
         * ---- look at container should list its contents
         * ---- reply
         * ---- Mob guild 
         * ---- Shapeshift
         * ---- Fast Movement / Immortal commands - more than one command processed in a pulse
         * ---- Export ROM Areas
         * ---- Night descriptions/names
         * ---- Quests
         * ---- Racial offense flags handled ( give skills like with NPCs? )
         * ---- Enchant Weapon/Armor
         * ---- if player disconnects keep their character around for a while
         * ---- make use of exit keywords
         * ---- Pets
         * ---- Weight / max weight / drop weapon if weakened
         * ---- max # of items carried
         * ---- Identify/Lore
         * ---- give/drop gold/silver
         * ---- Bank - Deposit / Withdraw
         * ---- Protected/Guarded rooms - guild guard, cabal guard
         * ---- class titles
         * ---- Saves vs spells
         * ---- Players leave a trail / mobs follow on the tick and attack by pulse if fled from, maybe trail fades over time so it can go cold?
         * ---- List all items command / List all NPCs command
         * ---- Hit return to continue on n lines ( lines / scroll command ) default to 40 or 80 lines ( PageToChar method /buffer all text of a command output/certain commands only )
         * ---- racial max stats, stat modifiers
         * ---- hidden exits
         * ---- Terrain types
         * ---- Notes
         * ---- Vendors
         * ---- magical items
         * ---- Autoassist, autoloot, autosacrifice, autogold, autosplit
         * ---- Guildmaster / Trainer / Trains / Practice
         * ---- Extra Descriptions
         * ---- Player/Door size
         * ---- Items use default value of template, only saves differences
         * ---- gameshadow races
         * ---- no math/consideration of armor class in damage
         * ---- item affects aren't being saved from gameshadow to this mud like damageroll and hitroll bonus health etc
         * ---- Renumber Area command
         * ---- Greeting
         * ---- Socials / Emote
         * ---- Auto Save
         * ---- if player is already logged in ask if want to disconnect them
         * ---- Stat [room, obj, mob]
         * ---- String [obj, mob]
         * ---- Load [obj, mob] vnum
         * ---- items hidden if no long description
         * ---- Player Descriptions
         * ---- Set [obj, mob]
         */

        public class gameInfo
        {
            public List<Player> connections = new List<Player>();
            public StreamWriter logWriter = null;
            public bool exiting = false;
            public int port = 4000;
            public object logLock = new object();
            public StringBuilder log = new StringBuilder();
            public IAsyncResult launchResult;
            public MainForm mainForm;
            public Action<gameInfo> launchMethod;



            public void LogLine(string text)
            {
                lock (logLock)
                {
                    var newText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " :: " + text;
                    log.AppendLine(newText);
                    if (logWriter != null)
                    {
                        logWriter.WriteLine(newText);
                        logWriter.Flush();
                    }
                }
            }

            public string RetrieveLog()
            {
                lock (logLock)
                {
                    var value = log.ToString();

                    log.Clear();
                    return value;
                }
            }

        }
        public static game Instance;

        public static int MaxPlayersOnlineEver = 0;

        public gameInfo Info;

        public Random random = new Random();

        public DateTime GameStarted = DateTime.Now;

        public int MaxPlayersOnline = 0;


        public static void Launch(int port, MainForm form)
        {
            if (Instance != null)
                Instance.Dispose();

            Instance = new game(port, form);
        }

        private Socket listeningSocket;
        private game(int port, MainForm form)
        {
            var launchMethod = new Action<gameInfo>(launch);



            Info = new gameInfo() { mainForm = form, port = port };
            lock (Info.logLock)
            {
                Info.launchMethod = launchMethod;
                Info.launchResult = launchMethod.BeginInvoke(Info, launched, Info);
                Info.logWriter = new StreamWriter("logs.txt");
            }

        }

        private void launched(IAsyncResult ar)
        {

        }

        public static void log(string text, params object[] parameters)
        {
            if (Instance != null)
                Instance.Info.LogLine(string.Format(text, parameters));
        }

        public static void bug(string text, params object[] parameters)
        {
            log(text, parameters);
        }

        private void SetupListeningSocket(gameInfo state)
        {
            listeningSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            //var s = string.Join(", ", (from flag in utility.GetEnumValues<AffectFlags>() select flag.ToString()));
            // listen on all adapters at port specified for new connections
            listeningSocket.Bind(new System.Net.IPEndPoint(0, state.port));
            listeningSocket.Listen(50);
            state.log.AppendLine("Listening on port " + state.port);
        }

        private void LoadData()
        {


            Liquid.loadLiquids();
            game.log("{0} liquids loaded.", Liquid.Liquids.Count);


            Race.LoadRaces();
            game.log("{0} races loaded.", Race.Races.Count);
            //Race.SaveRaces();

            PcRace.LoadRaces();
            game.log("{0} PC races loaded.", PcRace.PcRaces.Count);
            //PcRace.SaveRaces();

            SkillSpellGroup.LoadSkillSpellGroups();

            GuildData.LoadGuilds();

            WeaponDamageMessage.LoadWeaponDamageMessages();

            //game.Instance.WriteSampleArea();

            AreaData.LoadAreas();

            Social.LoadSocials();

            NoteData.LoadNotes();

            // Load corpses and pits before resetting areas so pits aren't duplicated
            ItemData.LoadCorpsesAndPits();

        }
        private void launch(gameInfo state)
        {
            SetupListeningSocket(state);

            LoadData();

            AreaData.resetAreas(); // areas aren't reset till they're all loaded just in case there are cross area references for resets

            WeatherData.Initialize();

            ShapeshiftForm.LoadShapeshiftForms();

            //ShapeshiftForm.SaveShapeshiftForms();

            Command.LinkCommandSkills();

            Command.CommandAttribute.AddAttributeCommands();

            GuildData.WriteGuildSkillsHtml();
            game.log("Accepting connections...");

            mainLoop(state);
        }

        private void mainLoop(gameInfo state)
        {
            try
            {
                while (!state.exiting)
                {
                    try
                    {
                        var time = DateTime.Now;

                        // Accept the new connections
                        while (listeningSocket.Poll(1, SelectMode.SelectRead))
                        {
                            var player = new Player(this, listeningSocket.Accept());
                            state.connections.Add(player);
                            if (game.CheckIsBanned(string.Empty, player.Address))
                            {
                                try
                                {
                                    WizardNet.Wiznet(WizardNet.Flags.Connections, "New Banned Connection - {0}", null, null, player.Address);

                                    player.sendRaw("You are banned.\n\r");
                                    game.CloseSocket(player, true, true);
                                }
                                catch
                                {

                                }
                            }
                            else
                                WizardNet.Wiznet(WizardNet.Flags.Connections, "New Connection - {0}", null, null, player.Address);

                        }


                        // Check for input
                        foreach (var connection in new List<Player>(state.connections))
                        {
                            if (connection.LastSaveTime != DateTime.MinValue && (DateTime.Now - connection.LastSaveTime).Minutes >= 5)
                            {
                                connection.SaveCharacterFile();
                                connection.send("\n\rAuto-saved.\n\r");
                            }

                            if (connection.Daze > 0)
                                --connection.Daze;

                            if (connection.Level == MAX_LEVEL && connection.Wait > 0)
                                connection.Wait = 0;

                            if (connection.Wait > 0)
                            {
                                --connection.Wait;
                                continue;
                            }

                            try
                            {
                                if (connection.socket != null && connection.socket.Poll(1, SelectMode.SelectRead))
                                {
                                    byte[] buffer = new byte[255];
                                    int received = connection.socket.Receive(buffer);

                                    if (received == 0)
                                    {
                                        connection.socket.Dispose();
                                        connection.socket = null;
                                        connection.inanimate = DateTime.Now;
                                        //state.connections.Remove(connection);
                                        //throw new Exception("Socket Exception");
                                    }
                                    else
                                    {
                                        var position = connection.input.Length;
                                        foreach (var Byte in buffer)
                                        {
                                            if (Byte == 8 && position > 0)
                                            {
                                                connection.input.Remove(connection.input.Length - 1, 1);
                                                position -= 1;
                                            }
                                            else if (Byte == 13 || Byte == 10 || (Byte >= 32 && Byte <= 126))
                                            {
                                                connection.input.Append((char)Byte);
                                                position++;
                                                if (connection.input.Length > 4200) // not writing a novel yet?
                                                {

                                                    connection.socket.Send(ASCIIEncoding.ASCII.GetBytes("Too much data to process at once.\n\r"));
                                                    game.CloseSocket(connection, false, true);
                                                    connection.inanimate = DateTime.Now;
                                                }
                                            }
                                        }
                                        // Above method of adding input makes sure only characters space through 126 are allowed into the game and also supports backspace for telnet
                                        //connection.input.Append(System.Text.ASCIIEncoding.ASCII.GetChars(buffer, 0, received));
                                    }


                                }

                                if (connection.socket != null)
                                {

                                    if (connection.state == Player.ConnectionStates.Playing && connection.Level == game.MAX_LEVEL)
                                    {
                                        while (connection.ProcessInput())
                                            if (!connection.HasPageText)
                                                connection.SittingAtPrompt = false;
                                    }
                                    else
                                    {
                                        if (connection.ProcessInput())
                                            if (!connection.HasPageText)
                                                connection.SittingAtPrompt = false;
                                    }
                                    if (connection.socket != null && connection.socket.Poll(1, SelectMode.SelectError))
                                        throw new Exception("Socket Exception");
                                }

                                if ((connection.state != Player.ConnectionStates.Playing && connection.socket == null) || (connection.inanimate.HasValue && connection.inanimate.Value.AddMinutes(5) < DateTime.Now))
                                {
                                    

                                    WizardNet.Wiznet(WizardNet.Flags.Connections, "Connection {0} removed from list of active connections.", connection, null, connection.Address);

                                    connection.Act("$n disappears into the void.", null, null, null, ActType.ToRoom);
                                    if (connection.state == Player.ConnectionStates.Playing)
                                        connection.SaveCharacterFile();
                                    if (connection.Room != null)
                                    {
                                        connection.RemoveCharacterFromRoom();
                                    }
                                    connection.Dispose();

                                    
                                    state.connections.Remove(connection);
                                }

                                if (connection.socket == null && !connection.inanimate.HasValue)
                                    connection.inanimate = DateTime.Now;
                                //if(connection.inanimate)
                            }
                            catch (Exception ex)
                            {
                                Info.LogLine(ex.ToString());
                                //if (connection.state == Player.ConnectionStates.Playing)
                                //    connection.SaveCharacterFile();
                                //if (connection.Room != null)
                                //{
                                //    connection.RemoveCharacterFromRoom();
                                //}
                                //connection.Dispose();
                                //if (state.connections.Contains(connection))
                                //    state.connections.Remove(connection);
                                try
                                {
                                    if (connection.socket != null)
                                    {
                                        game.CloseSocket(connection, false, false);
                                        connection.Act("$n loses their animation.", null, null, null, ActType.ToRoom);
                                    }
                                    connection.socket = null;
                                    connection.inanimate = DateTime.Now;

                                }
                                catch (Exception disposeEx)
                                {
                                    Info.LogLine(disposeEx.ToString());
                                }


                            }
                        }

                        // Update everything, combat happens here too
                        updateHandler();

                        // Check for pending output
                        foreach (var connection in new List<Player>(state.connections))
                        {
                            try
                            {
                                if (connection.socket != null)
                                    connection.ProcessOutput();
                            }
                            catch (Exception ex)
                            {
                                Info.LogLine(ex.ToString());
                                //if (connection.state == Player.ConnectionStates.Playing)
                                //    connection.SaveCharacterFile();
                                //if (connection.Room != null)
                                //{
                                //    if (connection.Room.characters.Contains(connection))
                                //        connection.Room.characters.Remove(connection);
                                //    connection.Room = null;
                                //}
                                //connection.Dispose();
                                //state.connections.Remove(connection);

                                try
                                {
                                    if (connection.socket != null)
                                    {
                                        try { connection.socket.Dispose(); } catch { }
                                        connection.Act("$n loses their animation.", null, null, null, ActType.ToRoom);
                                    }
                                    connection.socket = null;
                                    connection.inanimate = DateTime.Now;

                                }
                                catch (Exception disposeEx)
                                {
                                    Info.LogLine(disposeEx.ToString());
                                }

                            }
                        }
                        var timeToSleep = (int)Math.Max(1f, 250f - (DateTime.Now - time).TotalMilliseconds);

                        if ((DateTime.Now - time).TotalMilliseconds > 250)
                            log((DateTime.Now - time).TotalMilliseconds + "ms to loop once");
                        // wait until next pulse or connection attempt
                        //var poll = listeningSocket.Poll(timeToSleep, SelectMode.SelectRead);
                        System.Threading.Thread.Sleep(timeToSleep);
                    }
                    catch (Exception ex)
                    {
                        Info.LogLine(ex.ToString());
                    }
                } // end while ! exiting

                //exiting, one last attempt at sending any remaining output
                foreach (var connection in new List<Player>(state.connections))
                {
                    try
                    {
                        if (connection.socket != null && connection.socket.Poll(1, SelectMode.SelectRead))
                        {
                            byte[] buffer = new byte[255];
                            int received = connection.socket.Receive(buffer);

                            if (received == 0)
                                throw new Exception("Socket Read Exception");
                        }

                        if (connection.socket != null && connection.socket.Poll(1, SelectMode.SelectError))
                            throw new Exception("Socket Exception");

                        connection.ProcessOutput();
                        if (connection.state == Player.ConnectionStates.Playing)
                            connection.SaveCharacterFile();
                    }
                    catch (Exception ex)
                    {
                        Info.LogLine(ex.ToString());
                        try
                        {
                            if (connection.socket != null)
                            {
                                try
                                {
                                    connection.socket.Dispose();
                                }
                                catch { }
                                connection.Act("$n loses their animation.", null, null, null, ActType.ToRoom);
                            }
                            connection.socket = null;
                            connection.inanimate = DateTime.Now;

                        }
                        catch (Exception disposeEx)
                        {
                            Info.LogLine(disposeEx.ToString());
                        }
                    }
                }
            }
            catch (Exception gameEx)
            {
                Info.LogLine(gameEx.ToString());
            }
        }
        public static void shutdown()
        {

            foreach (var connection in game.Instance.Info.connections.ToArray())
            {
                try
                {
                    connection.SaveCharacterFile();
                    connection.sendRaw("Shutting down NOW!\n\r");
                    if (connection.socket != null)
                        try { connection.socket.Dispose(); } catch { }

                }
                catch { }

            }

            NoteData.SaveNotes();

            game.log("Notes saved.");

            ItemData.SaveCorpsesAndPits(true);

            game.Instance.Info.mainForm.exit = true;
            game.Instance.Info.exiting = true;


            game.Instance.Info.mainForm.Invoke(new Action(game.Instance.Info.mainForm.Close));
            try
            {
                game.Instance.Dispose();
            }
            catch
            {
            }
            try
            {
                game.Instance.Info.logWriter.Close();

            }
            catch
            { }
        }

        public static void reboot()
        {
            foreach (var connection in game.Instance.Info.connections.ToArray())
            {
                try
                {
                    connection.SaveCharacterFile();
                    connection.sendRaw("Rebooting NOW!\n\r");
                    if (connection.socket != null)
                        try { connection.socket.Dispose(); } catch { }

                }
                catch { }

            }

            NoteData.SaveNotes();
            game.log("Notes saved.");

            System.Diagnostics.Process.Start(Application.ExecutablePath);

            game.Instance.Info.mainForm.exit = true;
            game.Instance.Info.exiting = true;
            game.Instance.Info.mainForm.Invoke(new Action(game.Instance.Info.mainForm.Close));
            try
            {
                game.Instance.Dispose();
            }
            catch
            {
            }
            try
            {
                game.Instance.Info.logWriter.Close();

            }
            catch
            { }

        }

        public static int LEVEL_HERO = 51;
        public static int LEVEL_IMMORTAL = 52;
        internal static readonly int MAX_LEVEL = 60;

        DateTime PulseArea = DateTime.MinValue;
        DateTime PulseViolence = DateTime.MinValue;
        DateTime PulseMobile = DateTime.MinValue;
        DateTime PulseTick = DateTime.MinValue;
        DateTime PulseBet = DateTime.MinValue;
        DateTime PulseTrack = DateTime.MinValue;
        DateTime PulsRoomAffect = DateTime.MinValue;

        public const int MILLISECONDS_PER_PULSE = 250;
        public const int PULSE_PER_SECOND = 4;
        public const int PULSE_VIOLENCE = (3 * PULSE_PER_SECOND);
        public const int PULSE_MOBILE = (4 * PULSE_PER_SECOND);
        public const int PULSE_TICK = (30 * PULSE_PER_SECOND);
        public const int PULSE_AREA = (120 * PULSE_PER_SECOND);
        public const int PULSE_TRACK = (20 * PULSE_PER_SECOND);
        public const int PULSE_RIOT = (2 * PULSE_PER_SECOND);
        public const int PULSE_RAFFECT = (3 * PULSE_MOBILE);
        public const int PULSE_IPROG_PULSE = PULSE_PER_SECOND;
        public const int PULSE_BET = (18 * PULSE_PER_SECOND);

        public void PerformTick()
        {
            PulseTick = DateTime.Now;
            //Info.LogLine("TICK :: " + pulseCount / (60 * 4));
            UpdateWeather();
            UpdateCharacters();
            UpdateObjects();
            AreaData.resetAreas();
        }

        private void updateHandler()
        {
            //game.log("PULSE :: " + pulseCount);

            //if (pulseCount % (60 * 4) == 0 || pulseCount == 1)
            //    program.Log("TICK :: " + pulseCount / (60 * 4));
            if (DateTime.Now > PulseTick.AddMilliseconds(PULSE_TICK * MILLISECONDS_PER_PULSE))
            {
                PerformTick();
            }

            if (DateTime.Now > PulseViolence.AddMilliseconds(PULSE_VIOLENCE * MILLISECONDS_PER_PULSE))
            {
                PulseViolence = DateTime.Now;
                UpdateCombat();
            }
            //AreaData.resetAreas();

            UpdateAggro();
        }

        private void UpdateObjects()
        {
            foreach (var item in ItemData.Items.ToArray())
            {
                foreach (var affect in item.affects.ToArray())
                {
                    // resist flags can expire and such?
                    //if (affect.where == AffectWhere.ToObject || affect.where == AffectWhere.ToAffects)
                    //{
                    if (affect.frequency == Frequency.Tick && affect.duration > 0 && item.Template == null)
                    {
                        affect.duration--;
                    }
                    else if (affect.frequency == Frequency.Tick && affect.duration == 0)
                    {
                        //item.AffectFromObject(affect);
                        if (affect.endMessage != null && item.CarriedBy != null)
                        {
                            item.CarriedBy.Act(affect.endMessage, item.CarriedBy, item, item);
                        }
                        if (affect.endMessageToRoom != null && (item.CarriedBy != null || item.Room != null))
                        {
                            if (item.CarriedBy != null)
                                item.CarriedBy.Act(affect.endMessageToRoom, item.CarriedBy, item, item, ActType.ToRoom);
                            else
                            {
                                foreach (var person in item.Room.Characters)
                                    person.Act(affect.endMessageToRoom, null, item, item);
                            }
                        }
                        item.affects.Remove(affect);
                    }
                    //}

                } // end countdown affects

                if (item.timer <= 0 || --item.timer > 0)
                {
                    continue;
                }
                else
                {
                    string message = "";
                    if (item.ItemType.ISSET(ItemTypes.Fountain))
                    {
                        message = "$p dries up.";
                    }
                    else if (item.ItemType.ISSET(ItemTypes.Corpse) || item.ItemType.ISSET(ItemTypes.NPCCorpse))
                    {
                        message = "$p decays into dust.";
                    }
                    else if (item.ItemType.ISSET(ItemTypes.Food))
                    {
                        message = "$p decomposes.";
                    }
                    else if (item.ItemType.ISSET(ItemTypes.Potion))
                    {
                        message = "$p has evaporated from disuse.";
                    }
                    else if (item.ItemType.ISSET(ItemTypes.Portal))
                    {
                        message = "$p fades out of existence.";
                    }
                    else
                    {
                        message = "$p crumbles into dust.";
                    }

                    if (item.CarriedBy != null)
                    {
                        if (item.CarriedBy.IsNPC
                            && item.CarriedBy.IsShop)
                            item.CarriedBy.Gold += item.Value / 5;
                        else
                        {
                            item.CarriedBy.Act(message, null, item, null, ActType.ToChar);
                        }
                    }
                    else if (item.Room != null && item.Room.Characters.Count > 0)
                    {
                        item.Room.Characters.First().Act(message, null, item, null, ActType.ToRoom);
                        item.Room.Characters.First().Act(message, null, item, null, ActType.ToChar);
                    }

                    if (item.Contains.Count > 0)
                    {
                        if (item.CarriedBy != null)
                        {
                            foreach (var contained in item.Contains.ToArray())
                            {
                                item.Contains.Remove(contained);
                                item.CarriedBy.AddInventoryItem(contained);
                            }
                        }
                        else if (item.Room != null)
                        {
                            if (item.ItemType.ISSET(ItemTypes.Corpse))
                            {
                                var owner = (from player in Player.Characters.OfType<Player>() where player.Name == item.Owner select player).FirstOrDefault();

                                if (owner != null && owner.state == Player.ConnectionStates.Playing)
                                {

                                    foreach (var contained in item.Contains.ToArray())
                                    {
                                        item.Contains.Remove(contained);
                                        owner.Act("$p appears in your hands.", null, contained, null, ActType.ToChar);
                                        owner.Act("$p appears in $n's hands.", null, contained, null, ActType.ToRoom);
                                        owner.AddInventoryItem(contained);
                                        contained.CarriedBy = owner;
                                        contained.Container = null;
                                    }
                                }
                                else
                                {
                                    RoomData recallroom = null;
                                    ItemData pit;
                                    if (item.Alignment == Alignment.Good)
                                    {
                                        RoomData.Rooms.TryGetValue(19089, out recallroom);
                                    }
                                    else if (item.Alignment == Alignment.Evil)
                                    {
                                        RoomData.Rooms.TryGetValue(19090, out recallroom);

                                    }
                                    else
                                    {
                                        RoomData.Rooms.TryGetValue(19091, out recallroom);
                                    }
                                    //var recallroom = 
                                    if (recallroom != null)
                                    {
                                        pit = (from obj in recallroom.items where obj.Vnum == 19000 select obj).FirstOrDefault();

                                        if (pit != null)
                                        {
                                            foreach (var contained in item.Contains.ToArray())
                                            {
                                                item.Contains.Remove(contained);
                                                pit.Contains.Add(contained);

                                                contained.Container = pit;
                                            }
                                        }
                                        else
                                            DumpItems(item);
                                    }
                                    else
                                        DumpItems(item);
                                }
                            }
                            else
                            {
                                DumpItems(item);
                            }
                        }
                        else if (item.Container != null)
                        {
                            foreach (var contained in item.Contains.ToArray())
                            {
                                item.Contains.Remove(contained);
                                item.Container.Contains.Add(contained);
                                contained.Container = item.Container;
                            }
                        }
                    }

                    item.Dispose();

                }

            }
        }

        public void DumpItems(ItemData item)
        {
            foreach (var contained in item.Contains.ToArray())
            {
                item.Contains.Remove(contained);
                item.Room.items.Add(contained);
                contained.Room = item.Room;
                contained.Container = null;
            }
        }
        private void UpdateCharacters()
        {
            var trackskill = SkillSpell.SkillLookup("track");
            foreach (var ch in new List<Character>(Character.Characters))
            {
                // if (!ch.isNPC && ((Connection)ch).state != Connection.connectionState.playing)
                //     continue;
                if (ch is Player && ((Player)ch).state != Player.ConnectionStates.Playing)
                    continue;

                //if(ch is Player && ch.Guild != null && ch.Guild.name == "mage")
                //    ShapeshiftForm.CheckGainForm(ch);
                ExitData exit;
                if (ch.IsNPC && ch.LastFighting != null && ch.Position == Positions.Standing && ch.Fighting == null && !ch.Flags.ISSET(ActFlags.Sentinel))
                {
                    var trackAffect = (from aff in ch.Room.affects where aff.skillSpell == trackskill && aff.owner == ch.LastFighting select aff).FirstOrDefault();
                    if (trackAffect != null &&
                        (exit = ch.Room.exits[trackAffect.Modifier]) != null &&
                        exit.destination != null &&
                        (!ch.Flags.ISSET(ActFlags.StayArea) || exit.destination.Area == ch.Room.Area))
                    {
                        ch.Act("$n checks the ground for tracks.", null, null, null, ActType.ToRoom);
                        ch.moveChar((Direction)trackAffect.Modifier, false, false);

                        CheckTrackAggro(ch);
                    }
                    if (ch.IsNPC && ch.LastFighting != null && ch.Position == Positions.Standing && ch.Fighting == null && !ch.Flags.ISSET(ActFlags.Sentinel))
                    {
                        trackAffect = (from aff in ch.Room.affects where aff.skillSpell == trackskill && aff.owner == ch.LastFighting select aff).FirstOrDefault();
                        if (trackAffect != null &&
                            (exit = ch.Room.exits[trackAffect.Modifier]) != null &&
                        exit.destination != null &&
                        (!ch.Flags.ISSET(ActFlags.StayArea) || exit.destination.Area == ch.Room.Area))
                        {
                            ch.Act("$n checks the ground for tracks.", null, null, null, ActType.ToRoom);
                            ch.moveChar((Direction)trackAffect.Modifier, false, false);

                            CheckTrackAggro(ch);
                        }

                    }
                }

                if (ch.Position >= Positions.Stunned)
                {
                    if (ch.HitPoints < ch.MaxHitPoints)
                    {
                        var hpgain = ch.HitpointGain();
                        ch.HitPoints = Math.Min(ch.MaxHitPoints, ch.HitPoints + hpgain);
                    }

                    if (ch.Form == null && ch is Player)
                    {
                        float managain = ch.ManaPointsGain();
                        var meditation = ch.GetSkillPercentage("meditation");
                        var trance = ch.GetSkillPercentage("trance");

                        if (meditation > Utility.NumberPercent())
                        {
                            managain *= 1.5f;
                            ch.CheckImprove("meditation", true, 1);
                            if (trance > Utility.NumberPercent())
                            {
                                ch.CheckImprove("trance", true, 1);
                                managain *= 2;
                            }
                            else
                                ch.CheckImprove("trance", false, 1);
                        }
                        else
                            ch.CheckImprove("meditation", false, 1);

                        if (ch.IsAffected(AffectFlags.Slow))
                        {
                            managain = Utility.Random(managain * 2, managain * 3);
                        }

                        if (ch.ManaPoints < ch.MaxManaPoints)
                            ch.ManaPoints = (int)(Math.Ceiling(Math.Min(ch.MaxManaPoints, ch.ManaPoints + managain)));
                    }
                    else if (ch is Player)
                    {
                        var manadrain = 10 + (10 * ((100 - ch.GetSkillPercentage(ch.Form.FormSkill)) / 100));

                        var meditation = ch.GetSkillPercentage("meditation");
                        var trance = ch.GetSkillPercentage("trance");

                        if (meditation > Utility.NumberPercent())
                        {
                            manadrain /= 2;
                            ch.CheckImprove("meditation", true, 1);
                            if (trance > Utility.NumberPercent())
                            {
                                ch.CheckImprove("trance", true, 1);
                                manadrain /= 4;
                            }
                            else
                                ch.CheckImprove("trance", false, 1);
                        }
                        else
                            ch.CheckImprove("meditation", false, 1);

                        if (ch.ManaPoints < manadrain) ShapeshiftForm.DoRevert(ch, "");

                        ch.ManaPoints = Math.Max(ch.ManaPoints - manadrain, 0);


                    }
                    if (ch.MovementPoints < ch.MaxMovementPoints)
                        ch.MovementPoints = Math.Min(ch.MaxMovementPoints, ch.MovementPoints + ch.MovementPointsGain());
                }

                if (ch.HitPoints > ch.MaxHitPoints) ch.HitPoints = ch.MaxHitPoints;
                if (ch.ManaPoints > ch.MaxManaPoints) ch.ManaPoints = ch.MaxManaPoints;
                if (ch.MovementPoints > ch.MaxMovementPoints) ch.MovementPoints = ch.MaxMovementPoints;

                if (ch.Position == Positions.Stunned)
                    Combat.UpdatePosition(ch);



                if (!ch.IsNPC && !ch.IsImmortal)
                {
                    int survivalist = 0;
                    if ((ch.GetSkillPercentage("slow metabolism") <= 1 && (survivalist = ch.GetSkillPercentage("survivalist")) <= 1) || Utility.Random(0, 15) == 0)
                    {
                        if (survivalist > 1) ch.CheckImprove("survivalist", false, 1);
                        if (ch.Hunger > 0 && !ch.IsAffected(AffectFlags.Sated) && !ch.IsAffected(AffectFlags.Ghost))
                            ch.Hunger--;
                        else if (ch.Hunger <= 0 && !ch.IsAffected(AffectFlags.Sated) && !ch.IsAffected(AffectFlags.Ghost))
                            ch.Starving++;
                        if (ch.Thirst > 0 && !ch.IsAffected(AffectFlags.Quenched) && !ch.IsAffected(AffectFlags.Ghost))
                            ch.Thirst--;
                        else if (ch.Thirst <= 0 && !ch.IsAffected(AffectFlags.Quenched) && !ch.IsAffected(AffectFlags.Ghost))
                            ch.Dehydrated++;

                        if (ch.Level > 10 && ch.Level <= game.LEVEL_HERO)
                        {
                            if (ch.Hunger == 0 && !ch.IsAffected(AffectFlags.Sated) && !ch.IsAffected(AffectFlags.Ghost))
                                ch.Starving++;
                            if (ch.Thirst == 0 && !ch.IsAffected(AffectFlags.Quenched) && !ch.IsAffected(AffectFlags.Ghost))
                                ch.Dehydrated++;
                        }
                        else
                        {
                            ch.Starving = 0;
                            ch.Dehydrated = 0;
                        }

                        if (ch.Hunger > 0
                            && ch.Starving > 0)
                        {
                            var counter = ch.Starving;
                            if (counter <= 4)
                                ch.send("You are no longer famished.\n\r");
                            else
                                ch.send("You are no longer starving.\n\r");
                            ch.Starving = 0;
                            //ch.hunger = 2;
                        }

                        if (ch.Thirst > 0
                            && ch.Dehydrated > 0)
                        {
                            var counter = ch.Dehydrated;
                            if (counter <= 5)
                                ch.send("You are no longer dehydrated.\n\r");
                            else
                                ch.send("You are no longer dying of thirst.\n\r");
                            ch.Dehydrated = 0;
                            //ch.thirst = 2;
                        }


                        if (ch.Hunger < 4 && !ch.IsAffected(AffectFlags.Sated) && !ch.IsAffected(AffectFlags.Ghost))
                        {
                            if (ch.Starving < 2)
                                ch.send("You are hungry.\n\r");


                        }
                        if (ch.Thirst < 4 && !ch.IsAffected(AffectFlags.Quenched) && !ch.IsAffected(AffectFlags.Ghost))
                        {
                            if (ch.Dehydrated < 2)
                                ch.send("You are thirsty.\n\r");
                        }

                        if (ch.Starving > 1 && !ch.IsAffected(AffectFlags.Sated) && !ch.IsAffected(AffectFlags.Ghost))
                        {
                            var counter = ch.Starving;
                            if (counter <= 5)
                                ch.send("You are famished!\n\r");
                            else if (counter <= 8)
                                ch.send("You are beginning to starve!\n\r");
                            else
                            {
                                ch.send("You are starving!\n\r");
                                if (ch.Level > 10 && !ch.IsAffected(AffectFlags.Sated) && !ch.IsAffected(AffectFlags.Ghost))
                                    Combat.Damage(ch, ch, Utility.Random(counter - 3, 2 * (counter - 3)), SkillSpell.SkillLookup("starvation"));
                            }

                        }

                        if (ch.Dehydrated > 1 && !ch.IsAffected(AffectFlags.Quenched) && !ch.IsAffected(AffectFlags.Ghost))
                        {
                            int counter = ch.Dehydrated;
                            if (counter <= 2)
                                ch.send("Your mouth is parched!\n\r");
                            else if (counter <= 5)
                                ch.send("You are beginning to dehydrate!\n\r");
                            else
                            {
                                ch.send("You are dying of thirst!\n\r");
                                if (ch.Level > 10 && !ch.IsAffected(AffectFlags.Quenched) && !ch.IsAffected(AffectFlags.Ghost))
                                    Combat.Damage(ch, ch, Utility.Random(counter, 2 * counter), SkillSpell.SkillLookup("dehydration"));
                            }
                        }
                    }
                    else
                    {
                        if (survivalist > 1) ch.CheckImprove("survivalist", true, 1);
                    }

                } // end !isnpc && !isimmortal

                if (ch.Drunk > 0)
                {
                    ch.Drunk = Math.Max(Math.Min(ch.Drunk - 3, ch.Drunk / 2), 0);
                    if (ch.Drunk == 0)
                    {
                        ch.send("You are sober.\n\r");
                    }
                }

                foreach (var affect in new List<AffectData>(ch.AffectsList))
                {
                    if (affect.frequency == Frequency.Tick && affect.duration > 0)
                    {
                        affect.duration--;
                        if (affect.skillSpell != null && affect.skillSpell.TickFunction != null)
                            affect.skillSpell.TickFunction(ch, affect);

                        if (affect.flags.ISSET(AffectFlags.Ghost) && affect.duration < 5)
                        {
                            ch.send("You feel your body start to transition back to a physical form.\n\r");
                        }

                        if (Programs.AffectProgramLookup(affect.tickProgram, out var prog))
                        {
                            prog.Execute(ch, affect, null, null, affect.skillSpell, Programs.ProgramTypes.AffectTick, "");
                        }
                    }
                    else if (affect.frequency == Frequency.Tick && affect.duration == 0)
                    {
                        ch.AffectFromChar(affect);

                        if (affect.skillSpell != null && affect.skillSpell.EndFunction != null)
                            affect.skillSpell.EndFunction(ch, affect);

                        if (Programs.AffectProgramLookup(affect.endProgram, out var prog))
                        {
                            prog.Execute(ch, affect, null, null, affect.skillSpell, Programs.ProgramTypes.AffectEnd, "");
                        }
                    }
                } // end countdown affects

                if (ch.Position == Positions.Incapacitated && Utility.Random(0, 1) == 0)
                {
                    Combat.Damage(ch, ch, 1, "", WeaponDamageTypes.None);
                }
                else if (ch.Position == Positions.Mortal)
                {
                    Combat.Damage(ch, ch, 1, "", WeaponDamageTypes.None);
                }

                if (ch.Room == null || ch.Room.Area == null || ch.Room.Area.People.Count == 0)
                    continue;

                if (ch.IsNPC)
                {
                    if (ch.Flags.ISSET(ActFlags.Scavenger) && Utility.Random(0, 8) == 0)
                    {
                        ItemData best = null;

                        foreach (var obj in ch.Room.items)
                        {
                            if (best == null || (best.Value < obj.Value && obj.wearFlags.ISSET(WearFlags.Take)))
                            {
                                best = obj;
                            }
                        }

                        if (best != null)
                        {
                            ch.GetItem(best);
                        }
                    }
                    Direction direction = Direction.North;

                    if (!ch.Flags.ISSET(ActFlags.Sentinel) && !ch.Flags.ISSET(ActFlags.NoWander) && ch.Position == Positions.Standing && Utility.Random(0, 8) == 0
                        && ch.Room != null
                        && (exit = ch.Room.exits[(int)(direction = Utility.GetEnumValues<Direction>().SelectRandom())]) != null
                        && exit.destination != null
                        && (!ch.Flags.ISSET(ActFlags.StayArea) || exit.destination.Area == ch.Room.Area)
                        && (!ch.Flags.ISSET(ActFlags.Outdoors) || (exit.destination.sector != SectorTypes.Inside && !exit.destination.flags.ISSET(RoomFlags.Indoors)))
                        && (!ch.Flags.ISSET(ActFlags.Indoors) || exit.destination.sector == SectorTypes.Inside || exit.destination.flags.ISSET(RoomFlags.Indoors))
                        && !exit.flags.ISSET(ExitFlags.Window)
                        )
                    {
                        if (exit.flags.ISSET(ExitFlags.Closed))
                        {
                            Character.DoOpen(ch, direction.ToString());
                        }
                        if (!exit.flags.ISSET(ExitFlags.Closed))
                        {
                            ch.moveChar(direction, true, false);
                        }
                    }
                }
            }

            ItemData.SaveCorpsesAndPits();
        }

        public long pulseCount { get; set; }

        private void UpdateCombat()
        {
            foreach (var ch in new List<Character>(Character.Characters))
            {
                foreach (var affect in new List<AffectData>(ch.AffectsList))
                {
                    if (affect.frequency == Frequency.Violence && affect.duration > 0)
                    {
                        affect.duration--;
                        if (affect.skillSpell != null && affect.skillSpell.TickFunction != null)
                            affect.skillSpell.TickFunction(ch, affect);

                        if (Programs.AffectProgramLookup(affect.tickProgram, out var prog))
                        {
                            prog.Execute(ch, affect, null, null, affect.skillSpell, Programs.ProgramTypes.AffectTick, "");
                        }
                    }
                    else if (affect.frequency == Frequency.Violence && affect.duration == 0)
                    {
                        ch.AffectFromChar(affect);

                        if (affect.skillSpell != null && affect.skillSpell.EndFunction != null)
                            affect.skillSpell.EndFunction(ch, affect);

                        if (Programs.AffectProgramLookup(affect.endProgram, out var prog))
                        {
                            prog.Execute(ch, affect, null, null, affect.skillSpell, Programs.ProgramTypes.AffectEnd, "");
                        }
                    }
                } // end countdown affects

                if (ch.Fighting != null)
                {
                    //Combat.oneHit(ch, ch.fighting);
                    Combat.multiHit(ch, ch.Fighting);

                    if (ch.Fighting != null) // didn't die already?
                        Combat.CheckAssist(ch, ch.Fighting);
                }
                else if (ch.Position == Positions.Fighting) { ch.Position = Positions.Standing; }

                if (ch.IsNPC && ch.Position == Positions.Standing && ch.Wait == 0 && (ch.Flags.ISSET(ActFlags.Healer) || ch.Flags.ISSET(ActFlags.Cleric)) && Utility.Random(0, 6) == 0)
                {
                    var target = (from other in ch.Room.Characters where !other.IsNPC && ch.CanSee(other) select other).SelectRandom();

                    if (target != null)
                    {
                        var autoskill = (from sk in ch.Learned
                                         where
                                         (sk.Key.targetType == TargetTypes.targetCharDefensive || sk.Key.targetType == TargetTypes.targetItemCharDef) &&
                                         sk.Key.minimumPosition >= Positions.Fighting &&
                                         sk.Key.BoolAutoCast &&
                                         (sk.Key.AutoCast == null || sk.Key.AutoCast(target))
                                         && (sk.Key.AutoCastScript.ISEMPTY() || RoslynScripts.ExecuteCharacterBoolScript(target, sk.Key.AutoCastScript))
                                         select sk).SelectRandom();
                        //var skills = (from sk in ch.Learned where sk.Key.targetType == TargetTypes.targetCharDefensive && sk.Key.minimumPosition == Positions.Standing && (sk.Key.BoolAutoCast && (sk.Key.AutoCast == null || sk.Key.AutoCast(target))) select sk).ToArray();
                        if (autoskill.Key != null && autoskill.Key.spellFun != null)
                        {
                            Magic.CastCommuneOrSing(ch, "'" + autoskill.Key.name + "' " + target.Name, ch.Guild.CastType);
                        }
                    }
                }

                if (ch.IsNPC && ch.Position == Positions.Standing && ch.Wait == 0 && ch.Guild != null && ch.Guild.CastType != Magic.CastType.None && Utility.Random(0, 6) == 0)
                {
                    var autoskill = (from sk in ch.Learned
                                     where
                                     sk.Key.targetType == TargetTypes.targetCharDefensive &&
                                     sk.Key.minimumPosition == Positions.Standing &&
                                     sk.Key.BoolAutoCast &&
                                     (sk.Key.AutoCast == null || sk.Key.AutoCast(ch))
                                     && (sk.Key.AutoCastScript.ISEMPTY() || RoslynScripts.ExecuteCharacterBoolScript(ch, sk.Key.AutoCastScript))
                                     select sk).SelectRandom();

                    if (autoskill.Key != null && autoskill.Key.spellFun != null)
                    {
                        Magic.CastCommuneOrSing(ch, "'" + autoskill.Key.name + "'", ch.Guild.CastType);
                    }
                }
                //else if (ch.IsNPC && ch.Position == Positions.Standing && ch.Wait == 0 && ch.Guild != null && ch.Guild.CastType == Magic.CastType.Sing && Utility.Random(0, 6) == 0)
                //{
                //    var autoskill = (from sk in ch.Learned where sk.Key.SkillTypes.ISSET(SkillSpellTypes.Song) && (sk.Key.BoolAutoCast && (sk.Key.AutoCast == null || sk.Key.AutoCast(ch))) select sk).SelectRandom();

                //    if (autoskill.Key != null && autoskill.Key.spellFun != null)
                //    {
                //        Magic.CastCommuneOrSing(ch, "'" + autoskill.Key.name + "'", ch.Guild.CastType);
                //    }
                //}
                else if (ch.IsNPC && ch.Position == Positions.Standing && ch.Wait == 0 && ch.Guild != null && ch.Guild.CastType == Magic.CastType.None && Utility.Random(0, 6) == 0)
                {
                    var autoskill = (from sk in ch.Learned
                                     where
                                     sk.Key.spellFun == null &&
                                     ch.Position >= sk.Key.minimumPosition &&
                                     sk.Key.BoolAutoCast &&
                                     (sk.Key.AutoCast == null || sk.Key.AutoCast(ch))
                                     && (sk.Key.AutoCastScript.ISEMPTY() || RoslynScripts.ExecuteCharacterBoolScript(ch, sk.Key.AutoCastScript))
                                     select sk).SelectRandom();

                    foreach (var command in Command.Commands)
                    {
                        if (autoskill.Key != null && command.Name == autoskill.Key.name.Replace(" ", "")) // bind wounds, sneak, hide
                        {
                            command.Action(ch, "");
                        }
                    }
                }

            } // foreach ch
        }

        private void UpdateAggro()
        {
            foreach (var aggressor in Character.Characters.ToArray())
            {
                if (!aggressor.IsNPC && aggressor.Form != null && Utility.NumberPercent() > 110)
                    aggressor.CheckImprove(aggressor.Form.FormSkill, true, 100);

                if (aggressor.Fighting != null && aggressor.Fighting.Room != aggressor.Room && aggressor.Position == Positions.Fighting)
                {
                    aggressor.Position = Positions.Standing;
                    aggressor.Fighting = null;
                }

                if (aggressor.Form != null && aggressor.HitPoints < aggressor.MaxHitPoints && Utility.Random(0, 3) == 0)
                {
                    var superregen = aggressor.GetSkillPercentage("super regeneration");
                    var greaterregen = aggressor.GetSkillPercentage("greater regeneration");
                    var majorregen = aggressor.GetSkillPercentage("major regeneration");
                    var minorregen = aggressor.GetSkillPercentage("minor regeneration");

                    float regen = 0;

                    if (superregen > 1 && Utility.NumberPercent() < superregen)
                    {
                        regen = Utility.Random(30, 50);
                    }
                    else if (greaterregen > 1 && Utility.NumberPercent() < greaterregen)
                    {
                        regen = Utility.Random(25, 40);
                    }
                    else if (majorregen > 1 && Utility.NumberPercent() < majorregen)
                    {
                        regen = Utility.Random(20, 30);
                    }
                    else if (minorregen > 1 && Utility.NumberPercent() < minorregen)
                    {
                        regen = Utility.Random(15, 20);
                    }

                    if (aggressor.IsAffected(AffectFlags.Regeneration) && Utility.NumberPercent() < 100)
                    {
                        regen += Utility.Random(20, 30);
                    }

                    if (regen > 0)
                        aggressor.HitPoints = (int)Math.Min(aggressor.MaxHitPoints, aggressor.HitPoints + regen);
                }

                if (!aggressor.IsNPC) // this is for NPCs only
                    continue;

                if (aggressor.IsNPC && aggressor.Wait > 0)
                {
                    if (--aggressor.Wait > 0)
                        continue;
                }

                if (aggressor.IsNPC && aggressor.Flags.ISSET(ActFlags.Aggressive) && aggressor.Fighting == null && aggressor.IsAwake
                    && !aggressor.IsAffected(AffectFlags.Calm)
                    && !((aggressor.Flags.ISSET(ActFlags.Wimpy) && aggressor.HitPoints < aggressor.MaxHitPoints / 5)))
                {
                    if (aggressor.Room != null)
                    {
                        var randomPlayer = (from ch in aggressor.Room.Characters
                                            where !ch.IsNPC && aggressor.CanSee(ch) &&
                                            !ch.IsAffected(AffectFlags.Ghost) &&
                        !(from aff in ch.AffectsList where aff.skillSpell == SkillSpell.SkillLookup("gentle walk") && aff.duration == -1 select aff).Any()
                        && !ch.IsAffected(AffectFlags.PlayDead) && ch.Level < aggressor.Level + 5
                                            select ch).SelectRandom();

                        if (randomPlayer != null && (aggressor.Alignment != Alignment.Good || randomPlayer.Alignment == Alignment.Evil))
                        {
                            aggressor.Act("$n screams and attacks YOU!", randomPlayer, type: ActType.ToVictim);
                            aggressor.Act("$n screams and attacks $N!", randomPlayer, type: ActType.ToRoomNotVictim);
                            aggressor.Fighting = randomPlayer;
                            Combat.multiHit(aggressor, randomPlayer);
                        }
                    }
                }
                else
                {
                    CheckTrackAggro(aggressor);
                }
            }
        }

        public static void CheckTrackAggro(Character aggressor)
        {
            if (aggressor.IsNPC && aggressor.LastFighting != null && aggressor.Fighting == null && aggressor.IsAwake
                    && !aggressor.IsAffected(AffectFlags.Calm)
                    && !((aggressor.Flags.ISSET(ActFlags.Wimpy) && aggressor.HitPoints < aggressor.MaxHitPoints / 5)))
            {
                if (aggressor.LastFighting.Room == aggressor.Room && aggressor.CanSee(aggressor.LastFighting) && !aggressor.LastFighting.IsAffected(AffectFlags.PlayDead) && !aggressor.LastFighting.IsAffected(AffectFlags.Ghost))
                {
                    aggressor.Act("$n screams and attacks YOU!", aggressor.LastFighting, type: ActType.ToVictim);
                    aggressor.Act("$n screams and attacks $N!", aggressor.LastFighting, type: ActType.ToRoomNotVictim);

                    //aggressor.LastFighting = null;
                    Combat.multiHit(aggressor, aggressor.LastFighting);

                }
            }
        }

        private long _lasthour;

        public void UpdateWeather()
        {
            var buf = new StringBuilder();
            int diff;

            if (_lasthour != TimeInfo.Hour)
                switch (TimeInfo.Hour)
                {
                    case 5:
                        buf.Append("The day has begun.\n\r");
                        break;

                    case 6:
                        buf.Append("The sun rises in the east.\n\r");
                        break;

                    case 19:
                        buf.Append("The sun slowly disappears in the west.\n\r");
                        break;

                    case 20:
                        buf.Append("The night has begun.\n\r");
                        break;
                }

            _lasthour = TimeInfo.Hour;

            /*
            * Weather change.
            */
            if (TimeInfo.Month >= 9 && TimeInfo.Month <= 16)
                diff = WeatherData.mmhg > 985 ? -2 : 2;
            else
                diff = WeatherData.mmhg > 1015 ? -2 : 2;

            WeatherData.change += diff * Utility.dice(1, 4) + Utility.dice(2, 6) - Utility.dice(2, 6);
            WeatherData.change = Math.Max(WeatherData.change, -12);
            WeatherData.change = Math.Min(WeatherData.change, 12);

            WeatherData.mmhg += WeatherData.change;
            WeatherData.mmhg = Math.Max(WeatherData.mmhg, 960);
            WeatherData.mmhg = Math.Min(WeatherData.mmhg, 1040);

            switch (WeatherData.Sky)
            {
                default:
                    bug("Weather_update: bad sky %d.", WeatherData.Sky);
                    WeatherData.Sky = SkyStates.Cloudlesss;
                    break;

                case SkyStates.Cloudlesss:
                    if (WeatherData.mmhg < 990
                        || (WeatherData.mmhg < 1010 && Utility.Random(0, 2) == 0))
                    {
                        buf.Append("The sky is getting cloudy.\n\r");
                        WeatherData.Sky = SkyStates.Cloudy;
                    }
                    break;

                case SkyStates.Cloudy:
                    if (WeatherData.mmhg < 970
                        || (WeatherData.mmhg < 990 && Utility.Random(0, 2) == 0))
                    {
                        buf.Append("It starts to rain.\n\r");
                        WeatherData.Sky = SkyStates.Raining;
                    }

                    if (WeatherData.mmhg > 1030 && Utility.Random(0, 2) == 0)
                    {
                        buf.Append("The clouds disappear.\n\r");
                        WeatherData.Sky = SkyStates.Cloudlesss;
                    }
                    break;

                case SkyStates.Raining:
                    if (WeatherData.mmhg < 970 && Utility.Random(0, 2) == 0)
                    {
                        buf.Append("Lightning flashes in the sky.\n\r");
                        WeatherData.Sky = SkyStates.Lightning;
                    }

                    if (WeatherData.mmhg > 1030
                        || (WeatherData.mmhg > 1010 && Utility.Random(0, 2) == 0))
                    {
                        buf.Append("The rain stopped.\n\r");
                        WeatherData.Sky = SkyStates.Cloudy;
                    }
                    break;

                case SkyStates.Lightning:
                    if (WeatherData.mmhg > 1010
                        || (WeatherData.mmhg > 990 && Utility.Random(0, 2) == 0))
                    {
                        buf.Append("The lightning has stopped.\n\r");
                        WeatherData.Sky = SkyStates.Raining;
                        break;
                    }
                    break;
            }

            if (buf.Length > 0)
            {
                var buffer = buf.ToString();
                foreach (var player in game.Instance.Info.connections)
                {
                    if (player.state == Player.ConnectionStates.Playing
                        && player.IS_OUTSIDE
                        && player.IsAwake)
                        player.send(buffer);
                }
            }

            return;
        }
        public void Dispose()
        {
            Info.exiting = true;


            Info.launchMethod.EndInvoke(Info.launchResult);

            listeningSocket.Dispose();
            foreach (var connection in game.Instance.Info.connections.ToArray())
            {
                try
                {
                    connection.Dispose();
                    if (connection.socket != null)
                        connection.socket.Dispose();
                    game.Instance.Info.connections.Remove(connection);
                }
                catch (Exception exception)
                {
                    game.log(exception.ToString());
                }
            }
        }

        public void WriteSampleArea()
        {
            var sampleArea = new AreaData();

            sampleArea.vnumStart = 0;
            sampleArea.vnumEnd = 1;
            sampleArea.credits = "{ALL} Seen on areas command";
            sampleArea.info = "Not used at the moment";
            sampleArea.name = "Sample Area";

            var room = new RoomData();

            room.Vnum = 0;
            room.Area = sampleArea;

            room.sector = SectorTypes.Inside;
            room.Name = "Sample Room";
            room.NightName = "Sample Room at Night";
            room.Description = "This is a sample description";
            room.NightDescription = "This is a sample description at night";

            room.ExtraDescriptions.Add(new ExtraDescription("sign", "You look at the sign. It doesn't say anything."));

            room.exits[(int)Direction.Up] = new ExitData() { destinationVnum = 0, description = "You don't see anything in this direction.", ExitSize = CharacterSize.Giant, direction = Direction.Up };
            room.exits[(int)Direction.Up].originalFlags.AddRange(from exitflag in Utility.GetEnumValues<ExitFlags>() select exitflag);
            room.exits[(int)Direction.Up].flags.AddRange(room.exits[(int)Direction.Up].originalFlags);
            for (int i = 0; i < room.exits.Length; i++)
                room.OriginalExits[i] = new ExitData(room.exits[i]);

            sampleArea.rooms.Add(room.Vnum, room);

            var item = new ItemTemplateData();

            item.Vnum = 0;
            item.Area = sampleArea;
            item.Name = "keywords 'to type'";
            item.ShortDescription = "You wear [a sample item]";
            item.LongDescription = "[A sample item lies on the ground here.]";
            item.NightShortDescription = "You wear [a sample item at night]";
            item.NightLongDescription = "[A sample item lies on the ground here at night.]";
            item.Description = "What you see when you look at the item";
            item.extraFlags.AddRange(from extraflag in Utility.GetEnumValues<ExtraFlags>().Distinct() select extraflag);
            item.wearFlags.AddRange(from wearflag in Utility.GetEnumValues<WearFlags>().Distinct() select wearflag);
            item.itemTypes.AddRange(from itemtype in Utility.GetEnumValues<ItemTypes>().Distinct() select itemtype);
            item.Charges = 1;
            item.MaxCharges = 3;
            item.MaxWeight = 100;
            item.Weight = 10;
            item.ArmorBash = 10;
            item.ArmorPierce = 10;
            item.ArmorSlash = 10;
            item.ArmorExotic = 15;

            item.DamageDice = new Dice(10, 10, 10);

            item.WeaponDamageType = WeaponDamageMessage.WeaponDamageMessages.FirstOrDefault();
            item.WeaponType = WeaponTypes.Sword;

            item.Value = 1000;
            item.Level = 60;
            item.ExtraDescriptions.Add(new ExtraDescription("keywords", "something nice"));
            item.spells.Add(new ItemSpellData(20, "sanctuary"));
            item.affects.Add(new AffectData()
            {
                affectType = AffectTypes.Spell,
                displayName = "Not used",
                duration = -1,
                hidden = false,
                beginMessage = "Seen when affect is applied to a character",
                beginMessageToRoom = "broadcast when item is worn and affect applied",
                endMessage = "seen when affect ends/item is removed",
                endMessageToRoom = "broadcast when affect is removed",
                level = 20,
                location = ApplyTypes.Strength,
                modifier = 5,
                name = "not used",
                ownerName = "not used",
                skillSpell = SkillSpell.SkillLookup("giant strength"),
                where = AffectWhere.ToObject
            });
            item.affects.First().flags.AddRange(from aff_flag in Utility.GetEnumValues<AffectFlags>() select aff_flag);

            sampleArea.ItemTemplates.Add(item.Vnum, item);

            var npc = new NPCTemplateData();

            npc.Vnum = 0;
            npc.Area = sampleArea;

            npc.Name = "keywords 'to type'";
            npc.ShortDescription = "You give something to [your text here].";
            npc.LongDescription = "[Your mob stands here idly.]";
            npc.NightShortDescription = "You give something to [your text here at night].";
            npc.NightLongDescription = "[Your mob stands here idly at night.]";
            npc.Description = "What you see when you look at me.";
            npc.AffectedBy.AddRange(from affectedby in Utility.GetEnumValues<AffectFlags>() select affectedby);
            npc.Flags.AddRange(from actflag in Utility.GetEnumValues<ActFlags>() select actflag);
            npc.DamageDice = new Dice(10, 10, 10);
            npc.DamageRoll = 30;
            npc.HitRoll = 30;
            npc.HitPointDice = new Dice(100, 10, 300);
            npc.ManaPointDice = new Dice(100, 10, 100);
            npc.Alignment = Alignment.Neutral;
            npc.ArmorBash = 100;
            npc.ArmorPierce = 100;
            npc.ArmorSlash = 100;
            npc.ArmorExotic = 100;
            npc.ArmorClass = -20;
            npc.BuyTypes.AddRange(from itemtype in Utility.GetEnumValues<ItemTypes>() select itemtype);
            npc.BuyProfitPercent = 100;
            npc.SellProfitPercent = 100;
            npc.Ethos = Ethos.Neutral;
            npc.Gold = 1000;
            npc.Silver = 999;
            npc.Size = CharacterSize.Medium;
            npc.AffectsList.Add(new AffectData()
            {
                affectType = AffectTypes.Spell,
                displayName = "Not used",
                duration = -1,
                hidden = false,
                beginMessage = "Seen when affect is applied to a character",
                beginMessageToRoom = "broadcast when item is worn and affect applied",
                endMessage = "seen when affect ends/item is removed",
                endMessageToRoom = "broadcast when affect is removed",
                level = 20,
                location = ApplyTypes.Strength,
                modifier = 5,
                name = "not used",
                ownerName = "not used",
                skillSpell = SkillSpell.SkillLookup("giant strength"),
                where = AffectWhere.ToObject
            });
            npc.Race = Race.GetRace("human");
            npc.LearnSkill(SkillSpell.SkillLookup("second attack"), 80);
            npc.Guild = GuildData.GuildLookup("warrior");
            npc.Protects.Add(0);

            var reset = new ResetData();
            reset.spawnVnum = 0;
            reset.roomVnum = 0;
            reset.maxCount = 4;
            reset.count = 4;
            reset.resetType = ResetTypes.NPC;
            sampleArea.resets.Add(reset);

            reset = new ResetData();
            reset.spawnVnum = 0;
            reset.roomVnum = 0;
            reset.maxCount = 4;
            reset.count = 4;
            reset.resetType = ResetTypes.Equip;
            sampleArea.resets.Add(reset);

            reset = new ResetData();
            reset.spawnVnum = 0;
            reset.roomVnum = 0;
            reset.maxCount = 4;
            reset.count = 4;
            reset.resetType = ResetTypes.Give;
            sampleArea.resets.Add(reset);

            reset = new ResetData();
            reset.spawnVnum = 0;
            reset.roomVnum = 0;
            reset.maxCount = 4;
            reset.count = 4;
            reset.resetType = ResetTypes.Item;
            sampleArea.resets.Add(reset);

            reset = new ResetData();
            reset.spawnVnum = 0;
            reset.roomVnum = 0;
            reset.maxCount = 4;
            reset.count = 4;
            reset.resetType = ResetTypes.Put;
            sampleArea.resets.Add(reset);

            sampleArea.NPCTemplates.Add(npc.Vnum, npc);
            sampleArea.fileName = "SampleArea.xml";
            sampleArea.save();
        }

        public static void DoBug(Character ch, string arguments)
        {
            System.IO.File.AppendAllText("bugs.txt", string.Format("Player {0} reported the following bug on {1}: {2}{3}", ch.Name, DateTime.Now.ToString(), arguments, Environment.NewLine));
            log(string.Format("Player {0} reported the following bug on {1}: {2}", ch.Name, DateTime.Now.ToString(), arguments));
            ch.send("Your bug report has been logged.\n\r");
        }

        public static void DoConnections(Character ch, string arguments)
        {

            ch.send("Current players:\n\r");
            if (!Character.Characters.Any(c => c is Player))
            {
                ch.send("None.\n\r");
            }
            else
            {
                ch.send("{0,20} {1,20} {2, 10}\n\r", "Name", "Address", "State");
                foreach (var player in Character.Characters.OfType<Player>())
                {
                    ch.send("{0,20} {1,20} {2,10}\n\r", player.Name, player.Address, player.state.ToString());
                }

                ch.send(Character.Characters.OfType<Player>().Count() + " players online.\n\r");
            }


        }


        public static void DoBanByName(Character ch, string arguments)
        {
            string name = string.Empty;
            string duration = string.Empty;

            duration = arguments.OneArgument(ref name);

            if (name.ISEMPTY())
            {
                ch.send("Syntax: BanByName $playername\n\r");
                return;
            }
            if (duration.ISEMPTY())
                duration = (DateTime.MaxValue - DateTime.Now - TimeSpan.FromMinutes(1)).ToString();

            string banspath = @"data\bans.xml";
            XElement bans = new XElement("bans");

            if (System.IO.File.Exists(banspath))
            {
                bans = XElement.Load(banspath);
            }

            bans.Add(new XElement("ban", new XAttribute("EndDate", DateTime.Now + TimeSpan.Parse(duration)), new XAttribute("name", name), new XAttribute("address", "")));
            bans.Save(banspath);
            Character bancharacter;
            if ((bancharacter = Character.GetCharacterWorld(ch, name, true, false)) != null && bancharacter is Player)
            {
                ((Player)bancharacter).sendRaw("You have been banned.\n\r");
                game.CloseSocket(((Player)bancharacter), true);
                ch.send("Character banned.\n\r");
            }

            ch.send($"Ban entry added. In effect for {duration}.\n\r");
        }

        public static void DoBanByAddress(Character ch, string arguments)
        {
            string nameOrAddress = string.Empty;
            string duration = string.Empty;
            string address;
            duration = arguments.OneArgument(ref nameOrAddress);

            if (duration.ISEMPTY())
                duration = (DateTime.MaxValue - DateTime.Now - TimeSpan.FromMinutes(1)).ToString();

            if (nameOrAddress.ISEMPTY())
            {
                ch.send("Syntax: BanByAddress [$playername|$address]\n\r");
                return;
            }

            Character bancharacter;
            if (nameOrAddress.All(c => (c >= '0' && c <= '9') || c == '.'))
            {
                address = nameOrAddress;
            }
            else if ((bancharacter = Character.GetCharacterWorld(ch, nameOrAddress, true, false)) != null && bancharacter is Player)
            {
                address = ((Player) bancharacter).Address;
                ((Player)bancharacter).sendRaw("You have been banned.\n\r");
                game.CloseSocket(((Player) bancharacter), true);
                ch.send("Character banned.\n\r");
            }
            else
            {
                ch.send("Player not found or ip address not in proper format.\n\r");
                return;
            }

            foreach (var player in Character.Characters.OfType<Player>())
            {
                try // player may already be kicked
                {
                    if (player.Address.StartsWith(address))
                    {
                        player.sendRaw("You have been banned.\n\r");
                        CloseSocket(player);
                    }
                }
                catch
                {

                }
            }

            string banspath = @"data\bans.xml";
            XElement bans = new XElement("bans");

            if (System.IO.File.Exists(banspath))
            {
                bans = XElement.Load(banspath);
            }

            bans.Add(new XElement("ban", new XAttribute("EndDate", DateTime.Now + TimeSpan.Parse(duration)), new XAttribute("address", address)));
            bans.Save(banspath);


            ch.send($"Ban entry added. In effect for {duration}.\n\r");
        }

        public static void CloseSocket(Player player, bool remove = false, bool settonull = true)
        {
            WizardNet.Wiznet(WizardNet.Flags.Connections, "Connection from {0}({1}) terminated, state was {2}.", player, null, player.Address, !player.Name.ISEMPTY() ? player.Name : "", player.state);

            try { player.socket.Close(); } catch { }
            try { player.socket.Dispose(); } catch { }

            if (settonull)
                player.socket = null;

            if(remove)
            {
                game.Instance.Info.connections.Remove(player);
            }
        }

        public static bool CheckIsBanned(string name, string ipAddress)
        {
            string banspath = @"data\bans.xml";
            if (System.IO.File.Exists(banspath))
            {
                DateTime BanEndDate;
                var bans = XElement.Load(banspath);

                // name is not empty and an element name matches case insensitive or element address is not empty and ipaddress starts with it
                return bans.Elements().Any(element =>
                    DateTime.TryParse(element.GetAttributeValue("EndDate", DateTime.MaxValue.ToString()), out BanEndDate) &&
                    BanEndDate > DateTime.Now &&
                    (
                        (!name.ISEMPTY() || !element.GetAttributeValue("name").StringCmp(name)) ||

                                (!element.GetAttributeValue("address").ISEMPTY() ||
                                !ipAddress.StartsWith(element.GetAttributeValue("address"))
                        )
                    ));
            }

            return false;
        }
    }

    public enum Alignment
    {
        Good = 1,
        None = 2,
        Evil = 3,
        Unknown = 2,
        Neutral = 2
    }

    public enum Ethos
    {
        Orderly = 1,
        Lawful = 1,
        Neutral = 2,
        Chaotic = 3,
        Unknown = Neutral,
        None = Neutral
    }

}
