﻿/***************************************************************************
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
using CrimsonStainedLands.World;
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
using System.Xml.Linq;
using System.Xml.Schema;
using static CrimsonStainedLands.WeatherData;
using static System.Net.WebRequestMethods;

namespace CrimsonStainedLands
{
    public class Game : IDisposable
    {
        /*
         *  TODO
         * 
         * Help files
         * 
         * ---- Look into using Lua or Javascript for mob/obj progs
         * 
         * Consider using loot tables (race/body part/lvl based, visible on mob spawn?)
         *
         * 
         * Look into expression trees like Drocks WoW bot used to use for Mob Class Scripting
         * 
         * impassable exits, impassable night exits, night/day hidden exits
         * 
         * ---- MOTD
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

        public class GameInfo
        {
            public ConcurrentList<Player> Connections = new ConcurrentList<Player>();
            public StreamWriter LogWriter = null;
            public bool Exiting = false;
            public int Port = 4000;
            public object LogLock = new object();
            public StringBuilder Log = new StringBuilder();
            public IAsyncResult launchResult;
            //public MainForm MainForm;
            public Action<GameInfo> LaunchMethod;

            public Task MainLoopTask { get; internal set; }

            public void LogLine(string text)
            {
                lock (LogLock)
                {
                    var newText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " :: " + text;
                    Log.AppendLine(newText);
                    if (LogWriter != null)
                    {
                        try
                        {
                            LogWriter.WriteLine(newText);
                            LogWriter.Flush();
                        } catch { }
                    }
                }
            }

            public string RetrieveLog()
            {
                lock (LogLock)
                {
                    var value = Log.ToString();

                    Log.Clear();
                    return value;
                }
            }

        }
        public static Game Instance;

        public static int MaxPlayersOnlineEver = 0;

        public GameInfo Info;

        public Random random = new Random();

        public DateTime GameStarted = DateTime.Now;

        public int MaxPlayersOnline = 0;


        public static void Launch(int port)
        {
            if (Instance != null)
                Instance.Dispose();

            Instance = new Game(port);
        }

        private Game(int port)
        {
            var launchMethod = new Action<GameInfo>(launch);

            Info = new GameInfo() { Port = port };
            lock (Info.LogLock)
            {
                Info.LaunchMethod = launchMethod;
                Info.LogWriter = new StreamWriter("logs.txt");
                LaunchAsync(Info);
            }

        }
        private void LaunchAsync(GameInfo info)
        {
            LaunchTask = Task.Run(() => launch(info));
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

        private Task SetupListeningSocket(GameInfo state)
        {
            ConnectionManager connectionManager = new ConnectionManager();
            var task = connectionManager.RunAsync();
            return task;
            // listeningSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            // // listen on all adapters at port specified for new connections
            // listeningSocket.Bind(new System.Net.IPEndPoint(0, state.Port));
            // listeningSocket.Listen(50);
            // state.Log.AppendLine("Listening on port " + state.Port);

            // if (Settings.SSLPort != 0)
            // {
            //     ssllisteningSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            //     // listen on all adapters at port specified for new connections
            //     ssllisteningSocket.Bind(new System.Net.IPEndPoint(0, Settings.SSLPort));
            //     ssllisteningSocket.Listen(50);
            //     state.Log.AppendLine("Listening for SSL connections on port " + Settings.SSLPort);
            // }
        }

        public void LoadData()
        {
            using (new LoadTimer("LoadLiquids loaded {0} liquids", () => Liquid.Liquids.Count))
                Liquid.loadLiquids();

            using (new LoadTimer("LoadRaces loaded {0} races", () => Race.Races.Count))
                Race.LoadRaces();
            //Race.SaveRaces();

            using (new LoadTimer("LoadPCRaces loaded {0} player races", () => PcRace.PcRaces.Count))
                PcRace.LoadRaces();
            //PcRace.SaveRaces();

            SkillSpellGroup.LoadSkillSpellGroups();

            using (new LoadTimer("LoadGuilds loaded {0} guilds", () => GuildData.Guilds.Count))
                GuildData.LoadGuilds();

            using (new LoadTimer("LoadWeaponDamageMessages loaded {0} messages", () => WeaponDamageMessage.WeaponDamageMessages.Count))
                WeaponDamageMessage.LoadWeaponDamageMessages();

            //game.Instance.WriteSampleArea();
            using (new LoadTimer("LoadPrograms loaded {0} programs", () => NLuaPrograms.Programs.Count))
                NLuaPrograms.LoadPrograms();

            using (new LoadTimer("LoadAreas loaded {0} areas", () => AreaData.Areas.Count))
                AreaData.LoadAreas();

            using (new LoadTimer("Updated {0} helps to database", () => HelpData.Helps.Count))
            {
                var db = new Data.Database();
                foreach (var help in HelpData.Helps)
                {
                    db.AddHelp(help);
                }
            }

            using (new LoadTimer("LoadSocials loaded {0} socials", () => Social.Socials.Count))
                Social.LoadSocials();

            using (new LoadTimer("LoadNotes loaded {0} notes", () => NoteData.Notes.Count))
                NoteData.LoadNotes();

            // Load corpses and pits before resetting areas so pits aren't duplicated
            ItemData.LoadCorpsesAndPits();

        }
        private void launch(GameInfo state)
        {
            
            try
            {
                var connectionManagerTask = SetupListeningSocket(state);
                Module.LoadModules();
                using (new LoadTimer("Game loaded"))
                {

                    LoadData();

                    using (new LoadTimer("Reset areas"))
                        AreaData.ResetAreas(); // areas aren't reset till they're all loaded just in case there are cross area references for resets

                    WeatherData.Initialize();

                    using (new LoadTimer("LoadShapeshiftForms loaded {0} forms", () => ShapeshiftForm.Forms.Count))
                        ShapeshiftForm.LoadShapeshiftForms();

                    //ShapeshiftForm.SaveShapeshiftForms();

                    Command.LinkCommandSkills();

                    Command.CommandAttribute.AddAttributeCommands();

                    GuildData.WriteGuildSkillsHtml();

                    AreaData.SaveAreaListJson();
                    Game.log("Accepting connections... Standard port {0}, SSL Port {1}", Settings.Port, Settings.SSLPort);
                }

                Game.Instance.Info.MainLoopTask = Task.Run(() => mainLoop(state));
                //connectionManagerTask.Wait();
            }
            catch (Exception ex)
            {
                Game.bug("FATAL EXCEPTION: {0}", ex.ToString());
            }
        }

        private void ConsoleHandler()
        {
            while (!Game.Instance.Info.Exiting)
            {
                var command = Console.ReadLine();

                if(command.StringCmp("shutdown") || command.StringCmp("exit") || command.StringCmp("quit"))
                {
                    Game.log("Shutting down ...");
                    Game.shutdown();
                }
            }
        }

        private void mainLoop(GameInfo state)
        {
            var read = Task.Run(ConsoleHandler);
            
            Discord.Instance.OnMessageReceived += DiscordMessageReceived;

            Task.Run(() => Discord.Instance.StartAsync(Settings.BotToken));
            try
            {
                //AcceptNewSockets();
                while (!state.Exiting)
                {
                    try
                    {
                        var time = DateTime.Now;

                        // Accept the new connections


                        // Check for input
                        CheckConnectionsForAndProcessInput();

                        // Update everything, combat happens here too
                        UpdateHandler();

                        // Check for pending output
                        ProcessConnectionsOutput();

                        var timeToSleep = (int)Math.Max(1f, Game.MILLISECONDS_PER_PULSE - (DateTime.Now - time).TotalMilliseconds);

                        if ((DateTime.Now - time).TotalMilliseconds > Game.MILLISECONDS_PER_PULSE)
                            log((DateTime.Now - time).TotalMilliseconds + "ms to loop once");

                        /// Had issues with listening socket poll on windows, using Sleep instead
                        // wait until next pulse or connection attempt
                        //var poll = listeningSocket.Poll(timeToSleep, SelectMode.SelectRead);

                        System.Threading.Thread.Sleep(timeToSleep);
                    }
                    catch (Exception ex)
                    {
                        Info.LogLine(ex.ToString());
                    }
                } // end while ! exiting

                LastDitchAttemptToSendOutput();
            }
            catch (Exception gameEx)
            {
                Info.LogLine(gameEx.ToString());
            }
        }

        private void DiscordMessageReceived(string username, string channel, string content)
        {
            if(channel == "ooc-channel")
            {
                using (new Character.CaptureCommunications())
                {
                    foreach (var ch in Character.Characters)
                    {
                        if (ch.Flags.ISSET(ActFlags.NewbieChannel))
                            ch.send("\\cDISCORD OOC ({0}): {1}\\x\r\n", username, content);
                    }
                }
            }
            else if (channel == "newbie-channel")
            {
                using (new Character.CaptureCommunications())
                {
                    foreach (var ch in Character.Characters)
                    {
                        if (ch.Flags.ISSET(ActFlags.NewbieChannel))
                            ch.send("\\cDISCORD NEWBIE ({0}): {1}\\x\r\n", username, content);
                    }
                }
            }
        }

        //private void EndAcceptNewSocket(IAsyncResult result)
        //{
        //    var socket = result.AsyncState as Socket;

        //    lock (Info.Connections)
        //    {
        //        try
        //        {
        //            var player = new Player(this, socket.EndAccept(result), socket == ssllisteningSocket, socket == sshlisteningSocket);


        //        }
        //        catch { }

        //        try
        //        {
        //            socket.BeginAccept(EndAcceptNewSocket, socket);
        //        }
        //        catch { }
        //    }
        //}

        /// <summary>
        /// Poll for pending connections and accept them
        /// </summary>
        //private void AcceptNewSockets()
        //{

        //    listeningSocket.BeginAccept(EndAcceptNewSocket, listeningSocket);

        //    if (ssllisteningSocket != null)
        //        ssllisteningSocket.BeginAccept(EndAcceptNewSocket, ssllisteningSocket);

        //    if (sshlisteningSocket != null)
        //        sshlisteningSocket.BeginAccept(EndAcceptNewSocket, sshlisteningSocket);
        //}

        public void SocketAccepted(Player player)
        {
            if (Game.CheckIsBanned(string.Empty, player.Address))
            {
                try
                {
                    WizardNet.Wiznet(WizardNet.Flags.Connections, "New Banned Connection - {0}", null, null, player.Address);

                    player.SendRaw("You are banned.\r\n");
                    Game.CloseSocket(player, true, true);
                }
                catch
                {

                }
            }
            else
            {
                WizardNet.Wiznet(WizardNet.Flags.Connections, "New Connection - {0}", null, null, player.Address);

                //player.SendRaw(TelnetProtocol.ClientGetDoEcho);
                /// IAC TType Negotiation
                /// https://tintin.mudhalla.net/protocols/mtts/
                /// https://tintin.mudhalla.net/rfc/rfc854/
                /// https://www.rfc-editor.org/rfc/rfc1091
                /// https://tintin.mudhalla.net/info/ansicolor/
                player.SendRaw(TelnetProtocol.ServerGetDoTelnetType, true);

            }
        }






        /// <summary>
        /// Poll for pending data to be received and receive it, handles IAC TType Negotiation
        /// </summary>
        /// <param name="player">Player Connection containing the socket to be handled</param>
        private void ReceiveSocketBytes(Player player)
        {
            if (player.connection != null)
            {
                byte[] buffer;// = new byte[256];
                buffer = player.connection.Read();
                if(buffer != null)
                    player.ProcessBytes(buffer, buffer.Length);
            }
        }



        /// <summary>
        /// Decrement the Wait Lag counter
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>False if needs to wait, true if good to go ahead</returns>
        private bool DecrementDazeAndWait(Player connection)
        {
            if (connection.Daze > 0)
                --connection.Daze;

            if (connection.Level == MAX_LEVEL && connection.Wait > 0)
                connection.Wait = 0;

            if (connection.Wait > 0)
            {
                --connection.Wait;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Call a player's process input method
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="Exception"></exception>
        private void ProcessPlayerInput(Player connection)
        {
            if (connection.connection != null)
            {

                if (connection.state == Player.ConnectionStates.Playing && connection.Level == Game.MAX_LEVEL)
                {
                    // immortals process all received input immediately
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
                //if (connection.socket != null && connection.socket.Poll(1, SelectMode.SelectError))
                //    throw new Exception("Socket Exception");
            }
        }

        private void KickInanimatePlayer(Player connection)
        {
            if ((connection.state != Player.ConnectionStates.Playing && (connection.connection == null || connection.connection.Status == Connections.BaseConnection.ConnectionStatus.Disconnected)) || (connection.inanimate.HasValue && connection.inanimate.Value.AddMinutes(5) < DateTime.Now))
            {


                WizardNet.Wiznet(WizardNet.Flags.Connections, "Connection {0} removed from list of active connections.", connection, null, connection.Address);

                //if (connection.state == Player.ConnectionStates.Playing)
                //{
                //    connection.Act("$n disappears into the void.", null, null, null, ActType.ToRoom);
                //    //connection.SaveCharacterFile();
                //}

                CloseSocket(connection, true, true);
            }

            else if ((connection.connection == null || connection.connection.Status == Connections.BaseConnection.ConnectionStatus.Disconnected) && !connection.inanimate.HasValue)
            {
                CloseSocket(connection, connection.state != Player.ConnectionStates.Playing, true);
                if(connection.state == Player.ConnectionStates.Playing)
                    connection.inanimate = DateTime.Now;
            }
        }

        private void ProcessConnectionsOutput()
        {
            foreach (var connection in Info.Connections)
            {
                try
                {
                    if (connection.connection != null)
                        connection.ProcessOutput();
                }
                catch (Exception ex)
                {
                    Info.LogLine(ex.ToString());

                    try
                    {
                        Game.CloseSocket(connection, false, true);
                    }
                    catch (Exception disposeEx)
                    {
                        Info.LogLine(disposeEx.ToString());
                    }
                    connection.inanimate = DateTime.Now;

                }
            }
        }

        private void CheckConnectionsForAndProcessInput()
        {
            foreach (var connection in Info.Connections)
            {
                if (connection.LastSaveTime != DateTime.MinValue && (DateTime.Now - connection.LastSaveTime).Minutes >= 5)
                {
                    connection.SaveCharacterFile();
                    connection.send("\r\nAuto-saved.\r\n");
                }

                if (!DecrementDazeAndWait(connection))
                    continue;

                try
                {
                    ReceiveSocketBytes(connection);

                    ProcessPlayerInput(connection);

                    KickInanimatePlayer(connection);
                }
                catch (Exception ex)
                {
                    if(ex is SocketException)
                    {
                        Info.LogLine(ex.Message);
                    }
                    else
                        Info.LogLine(ex.ToString());

                    try
                    {
                        if (connection.connection != null)
                        {
                            Game.CloseSocket(connection, false, true);
                            
                        }
                        
                        connection.inanimate = DateTime.Now;

                    }
                    catch (Exception disposeEx)
                    {
                        Info.LogLine(disposeEx.ToString());
                    }


                }
            }
        }

        private void LastDitchAttemptToSendOutput()
        {
            //exiting, one last attempt at sending any remaining output
            foreach (var connection in Info.Connections)
            {
                try
                {
                    
                    connection.ProcessOutput();
                    if (connection.state == Player.ConnectionStates.Playing)
                        connection.SaveCharacterFile();
                }
                catch (Exception ex)
                {
                    Info.LogLine(ex.ToString());
                    try
                    {
                        Game.CloseSocket(connection, false, true);
                    }
                    catch (Exception disposeEx)
                    {
                        Info.LogLine(disposeEx.ToString());
                    }
                    connection.inanimate = DateTime.Now;
                }
            }
        }

        private static bool shutdowncomplete = false;
        public static void shutdown()
        {
            if (!shutdowncomplete)
            {

                foreach (var connection in Game.Instance.Info.Connections)
                {
                    try
                    {
                        connection.SaveCharacterFile();
                        connection.SendRaw("Shutting down NOW!\r\n");
                        if (connection.connection != null)
                            try { connection.connection.Cleanup(); } catch { }

                    }
                    catch { }

                }

                NoteData.SaveNotes();

                Game.log("Notes saved.");

                ItemData.SaveCorpsesAndPits(true);

                Game.log("Corpses and pits saved.");

                Game.Instance.Info.Exiting = true;

                try
                {
                    Game.Instance.Dispose();
                }
                catch
                {
                }
                try
                {
                    Game.Instance.Info.LogWriter.Close();

                }
                catch
                { }
            }
            shutdowncomplete = true;
        }

        public static void reboot()
        {
            foreach (var connection in Game.Instance.Info.Connections)
            {
                try
                {
                    connection.SaveCharacterFile();
                    connection.SendRaw("Rebooting NOW!\r\n");
                    if (connection.connection != null)
                        try { connection.connection.Cleanup(); } catch { }

                }
                catch { }

            }

            NoteData.SaveNotes();
            Game.log("Notes saved.");
            
            System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

            //Game.Instance.Info.MainForm.exit = true;
            Game.Instance.Info.Exiting = true;
            //Game.Instance.Info.MainForm.Invoke(new Action(Game.Instance.Info.MainForm.Close));
            try
            {
                Game.Instance.Dispose();
            }
            catch
            {
            }
            try
            {
                Game.Instance.Info.LogWriter.Close();

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
            AreaData.ResetAreas();
        }

        private void UpdateHandler()
        {
            //game.log("PULSE :: " + pulseCount);

            //if (pulseCount % (60 * 4) == 0 || pulseCount == 1)
            //    program.Log("TICK :: " + pulseCount / (60 * 4));

            Module.PulseBefore();

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

            Module.PulseAfter();
        }

        private void UpdateObjects()
        {
            foreach (var item in ItemData.Items)
            {
                try
                {

                    foreach (var affect in item.affects)
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
                                foreach (var contained in item.Contains)
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

                                        foreach (var contained in item.Contains)
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
                                                foreach (var contained in item.Contains)
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
                                foreach (var contained in item.Contains)
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
                catch(Exception ex)
                {
                    Game.log(ex.Message);
                }
            }
        }

        public void DumpItems(ItemData item)
        {
            foreach (var contained in item.Contains)
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



                if (!ch.IsNPC && !ch.IsImmortal && !ch.IsInactive)
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

                        if (ch.Level > 10 && ch.Level <= Game.LEVEL_HERO)
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
                                ch.send("You are no longer famished.\r\n");
                            else
                                ch.send("You are no longer starving.\r\n");
                            ch.Starving = 0;
                            //ch.hunger = 2;
                        }

                        if (ch.Thirst > 0
                            && ch.Dehydrated > 0)
                        {
                            var counter = ch.Dehydrated;
                            if (counter <= 5)
                                ch.send("You are no longer dehydrated.\r\n");
                            else
                                ch.send("You are no longer dying of thirst.\r\n");
                            ch.Dehydrated = 0;
                            //ch.thirst = 2;
                        }


                        if (ch.Hunger < 4 && !ch.IsAffected(AffectFlags.Sated) && !ch.IsAffected(AffectFlags.Ghost))
                        {
                            if (ch.Starving < 2)
                                ch.send("You are hungry.\r\n");


                        }
                        if (ch.Thirst < 4 && !ch.IsAffected(AffectFlags.Quenched) && !ch.IsAffected(AffectFlags.Ghost))
                        {
                            if (ch.Dehydrated < 2)
                                ch.send("You are thirsty.\r\n");
                        }

                        if (ch.Starving > 1 && !ch.IsAffected(AffectFlags.Sated) && !ch.IsAffected(AffectFlags.Ghost))
                        {
                            var counter = ch.Starving;
                            if (counter <= 5)
                                ch.send("You are famished!\r\n");
                            else if (counter <= 8)
                                ch.send("You are beginning to starve!\r\n");
                            else
                            {
                                ch.send("You are starving!\r\n");
                                if (ch.Level > 10 && !ch.IsAffected(AffectFlags.Sated) && !ch.IsAffected(AffectFlags.Ghost))
                                    Combat.Damage(ch, ch, Utility.Random(counter - 3, 2 * (counter - 3)), SkillSpell.SkillLookup("starvation"));
                            }

                        }

                        if (ch.Dehydrated > 1 && !ch.IsAffected(AffectFlags.Quenched) && !ch.IsAffected(AffectFlags.Ghost))
                        {
                            int counter = ch.Dehydrated;
                            if (counter <= 2)
                                ch.send("Your mouth is parched!\r\n");
                            else if (counter <= 5)
                                ch.send("You are beginning to dehydrate!\r\n");
                            else
                            {
                                ch.send("You are dying of thirst!\r\n");
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
                        ch.send("You are sober.\r\n");
                    }
                }

                foreach (var affect in new List<AffectData>(ch.AffectsList))
                {
                    if (affect.frequency == Frequency.Tick && affect.duration != 0)
                    {
                        if(affect.duration > 0)
                        affect.duration--;
                        if (affect.skillSpell != null && affect.skillSpell.TickFunction != null)
                            affect.skillSpell.TickFunction(ch, affect);

                        if (affect.flags.ISSET(AffectFlags.Ghost) && affect.duration < 5)
                        {
                            ch.send("You feel your body start to transition back to a physical form.\r\n");
                        }

                        if (!affect.tickProgram.ISEMPTY())
                        {
                            if (Programs.AffectProgramLookup(affect.tickProgram, out var prog))
                            {
                                prog.Execute(ch, affect, null, null, affect.skillSpell, Programs.ProgramTypes.AffectTick, "");
                            }

                            if (NLuaPrograms.ProgramLookup(affect.tickProgram, out var luaprog))
                                luaprog.Execute(ch, null, ch.Room, null, affect.skillSpell, affect, Programs.ProgramTypes.AffectTick, "");

                            if (prog == null && luaprog == null)
                                Game.log("AffectTickProgram not found: {0}", affect.tickProgram);
                        }
                    }
                    else if (affect.frequency == Frequency.Tick && affect.duration == 0)
                    {
                        ch.AffectFromChar(affect, AffectRemoveReason.WoreOff);

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
                            CharacterDoFunctions.DoOpen(ch, direction.ToString());
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
                Programs.ExecutePrograms(Programs.ProgramTypes.PulseViolence, ch, null, null, ch.Room, string.Empty);

                foreach (var affect in new List<AffectData>(ch.AffectsList))
                {
                    if (affect.frequency == Frequency.Violence && affect.duration != 0)
                    {
                        if(affect.duration > 0)
                            affect.duration--;
                        if (affect.skillSpell != null && affect.skillSpell.TickFunction != null)
                            affect.skillSpell.TickFunction(ch, affect);
                        if (!affect.tickProgram.ISEMPTY())
                        {

                            if (Programs.AffectProgramLookup(affect.tickProgram, out var prog))
                            {
                                prog.Execute(ch, affect, null, null, affect.skillSpell, Programs.ProgramTypes.AffectTick, "");
                            }

                            if (NLuaPrograms.ProgramLookup(affect.tickProgram, out var luaprog))
                                luaprog.Execute(ch, null, ch.Room, null, affect.skillSpell, affect, Programs.ProgramTypes.AffectTick, "");

                            if (prog == null && luaprog == null)
                                Game.log("AffectTickProgram not found: {0}", affect.tickProgram);
                        }
                    }
                    else if (affect.frequency == Frequency.Violence && affect.duration == 0)
                    {
                        ch.AffectFromChar(affect, AffectRemoveReason.WoreOff);

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
            foreach (var aggressor in Character.Characters)
            {
                Programs.ExecutePrograms(Programs.ProgramTypes.Pulse, aggressor, null, null, aggressor.Room, string.Empty);

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
        public Task LaunchTask;

        public void UpdateWeather()
        {
            var buf = new StringBuilder();
            int diff;

            if (_lasthour != TimeInfo.Hour)
                switch (TimeInfo.Hour)
                {
                    case 5:
                        buf.Append("The day has begun.\r\n");
                        break;

                    case 6:
                        buf.Append("The sun rises in the east.\r\n");
                        break;

                    case 19:
                        buf.Append("The sun slowly disappears in the west.\r\n");
                        break;

                    case 20:
                        buf.Append("The night has begun.\r\n");
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
                        buf.Append("The sky is getting cloudy.\r\n");
                        WeatherData.Sky = SkyStates.Cloudy;
                    }
                    break;

                case SkyStates.Cloudy:
                    if (WeatherData.mmhg < 970
                        || (WeatherData.mmhg < 990 && Utility.Random(0, 2) == 0))
                    {
                        buf.Append("It starts to rain.\r\n");
                        WeatherData.Sky = SkyStates.Raining;
                    }

                    if (WeatherData.mmhg > 1030 && Utility.Random(0, 2) == 0)
                    {
                        buf.Append("The clouds disappear.\r\n");
                        WeatherData.Sky = SkyStates.Cloudlesss;
                    }
                    break;

                case SkyStates.Raining:
                    if (WeatherData.mmhg < 970 && Utility.Random(0, 2) == 0)
                    {
                        buf.Append("Lightning flashes in the sky.\r\n");
                        WeatherData.Sky = SkyStates.Lightning;
                    }

                    if (WeatherData.mmhg > 1030
                        || (WeatherData.mmhg > 1010 && Utility.Random(0, 2) == 0))
                    {
                        buf.Append("The rain stopped.\r\n");
                        WeatherData.Sky = SkyStates.Cloudy;
                    }
                    break;

                case SkyStates.Lightning:
                    if (WeatherData.mmhg > 1010
                        || (WeatherData.mmhg > 990 && Utility.Random(0, 2) == 0))
                    {
                        buf.Append("The lightning has stopped.\r\n");
                        WeatherData.Sky = SkyStates.Raining;
                        break;
                    }
                    break;
            }

            if (buf.Length > 0)
            {
                var buffer = buf.ToString();
                foreach (var player in Game.Instance.Info.Connections)
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
            Info.Exiting = true;


            //Info.LaunchMethod.EndInvoke(Info.launchResult);
            LaunchTask.Wait();
            //listeningSocket.Dispose();
            foreach (var connection in Game.Instance.Info.Connections)
            {
                try
                {
                    connection.Dispose();
                    if (connection.connection != null)
                        connection.connection.Cleanup();
                    lock (Game.Instance.Info.Connections)
                        Game.Instance.Info.Connections.Remove(connection);
                }
                catch (Exception exception)
                {
                    Game.log(exception.ToString());
                }
            }
        }

        public void WriteSampleArea()
        {
            var sampleArea = new AreaData();

            sampleArea.VNumStart = 0;
            sampleArea.VNumEnd = 1;
            sampleArea.Credits = "{ALL} Seen on areas command";
            sampleArea.info = "Not used at the moment";
            sampleArea.Name = "Sample Area";

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

            sampleArea.Rooms.Add(room.Vnum, room);

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
            sampleArea.Resets.Add(reset);

            reset = new ResetData();
            reset.spawnVnum = 0;
            reset.roomVnum = 0;
            reset.maxCount = 4;
            reset.count = 4;
            reset.resetType = ResetTypes.Equip;
            sampleArea.Resets.Add(reset);

            reset = new ResetData();
            reset.spawnVnum = 0;
            reset.roomVnum = 0;
            reset.maxCount = 4;
            reset.count = 4;
            reset.resetType = ResetTypes.Give;
            sampleArea.Resets.Add(reset);

            reset = new ResetData();
            reset.spawnVnum = 0;
            reset.roomVnum = 0;
            reset.maxCount = 4;
            reset.count = 4;
            reset.resetType = ResetTypes.Item;
            sampleArea.Resets.Add(reset);

            reset = new ResetData();
            reset.spawnVnum = 0;
            reset.roomVnum = 0;
            reset.maxCount = 4;
            reset.count = 4;
            reset.resetType = ResetTypes.Put;
            sampleArea.Resets.Add(reset);

            sampleArea.NPCTemplates.Add(npc.Vnum, npc);
            sampleArea.FileName = "SampleArea.xml";
            sampleArea.Save();
        }

        public static void DoBug(Character ch, string arguments)
        {
            System.IO.File.AppendAllText("bugs.txt", string.Format("Player {0} reported the following bug on {1}: {2}{3}", ch.Name, DateTime.Now.ToString(), arguments, Environment.NewLine));
            log(string.Format("Player {0} reported the following bug on {1}: {2}", ch.Name, DateTime.Now.ToString(), arguments));
            ch.send("Your bug report has been logged.\r\n");
        }

        public static void DoConnections(Character ch, string arguments)
        {

            ch.send("Current players:\r\n");
            if (!Character.Characters.Any(c => c is Player))
            {
                ch.send("None.\r\n");
            }
            else
            {
                ch.send("{0,-20}({1,-20}) {2,20} {3, 10}\r\n", "Name", "Type", "Address", "State");
                foreach (var player in Character.Characters.OfType<Player>())
                {
                    ch.send("{0,-20}({1,-20}) {2,20} {3,10}\r\n", player.Name, player.connection != null? player.connection.GetType().Name : "(null)", player.connection != null? player.Address : "disconnected", player.state.ToString());
                }

                ch.send(Character.Characters.OfType<Player>().Count() + " players online.\r\n");
            }


        }


        public static void DoBanByName(Character ch, string arguments)
        {
            string name = string.Empty;
            string duration = string.Empty;

            duration = arguments.OneArgument(ref name);

            if (name.ISEMPTY())
            {
                ch.send("Syntax: BanByName $playername\r\n");
                return;
            }
            if (duration.ISEMPTY())
                duration = (DateTime.MaxValue - DateTime.Now - TimeSpan.FromMinutes(1)).ToString();

            string banspath = Settings.DataPath + @"\bans.xml";
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
                ((Player)bancharacter).SendRaw("You have been banned.\r\n");
                Game.CloseSocket(((Player)bancharacter), true);
                ch.send("Character banned.\r\n");
            }

            ch.send($"Ban entry added. In effect for {duration}.\r\n");
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
                ch.send("Syntax: BanByAddress [$playername|$address]\r\n");
                return;
            }

            Character bancharacter;
            if (nameOrAddress.All(c => (c >= '0' && c <= '9') || c == '.'))
            {
                address = nameOrAddress;
            }
            else if ((bancharacter = Character.GetCharacterWorld(ch, nameOrAddress, true, false)) != null && bancharacter is Player)
            {
                address = ((Player)bancharacter).Address;
                ((Player)bancharacter).SendRaw("You have been banned.\r\n");
                Game.CloseSocket(((Player)bancharacter), true);
                ch.send("Character banned.\r\n");
            }
            else
            {
                ch.send("Player not found or ip address not in proper format.\r\n");
                return;
            }

            foreach (var player in Character.Characters.OfType<Player>())
            {
                try // player may already be kicked
                {
                    if (player.Address.StartsWith(address))
                    {
                        player.SendRaw("You have been banned.\r\n");
                        CloseSocket(player);
                    }
                }
                catch
                {

                }
            }

            string banspath = Settings.DataPath + @"\bans.xml";
            XElement bans = new XElement("bans");

            if (System.IO.File.Exists(banspath))
            {
                bans = XElement.Load(banspath);
            }

            bans.Add(new XElement("ban", new XAttribute("EndDate", DateTime.Now + TimeSpan.Parse(duration)), new XAttribute("address", address)));
            bans.Save(banspath);


            ch.send($"Ban entry added. In effect for {duration}.\r\n");
        }

        public static void CloseSocket(Player player, bool remove = false, bool settonull = true)
        {
            WizardNet.Wiznet(WizardNet.Flags.Connections, "Connection from {0}({1}) terminated, state was {2}.", player, null, player.Address, !player.Name.ISEMPTY() ? player.Name : "", player.state);

            if (player.connection != null)
            {
                try
                {
                    player.ProcessOutput();
                }
                catch { }

                try { player.connection.Cleanup(); } catch { }
                
            }

            if (settonull)
            {
                player.connection = null;
            }

            if (remove)
            {
                Game.Instance.Info.Connections.Remove(player);
                if (player.state == Player.ConnectionStates.Playing)
                {
                    player.state = Player.ConnectionStates.Disconnected;
                    player.SaveCharacterFile();
                    player.Act("$n disappears into the void.", null, null, null, ActType.ToRoom);
                }
                player.Dispose();
            }
        }

        public static bool CheckIsBanned(string name, string ipAddress)
        {
            string banspath = Settings.DataPath + @"\bans.xml";
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
        Neutral = 2,
        Evil = 3,
        None = 2,
        Unknown = 2,
        
    }

    public enum Ethos
    {
        Lawful = 1,
        Orderly = 1,
        Neutral = 2,
        Chaotic = 3,
        Unknown = Neutral,
        None = Neutral
    }

}
