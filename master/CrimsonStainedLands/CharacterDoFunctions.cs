using CrimsonStainedLands.Extensions;
using CrimsonStainedLands.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    public class CharacterDoFunctions
    {
        public static void PerformTell(Character ch, Character victim, string arguments)
        {
            if (victim == null || ((victim is Player) && ((Player)victim).connection == null))
            {
                ch.send("They aren't here.\n\r");
                return;
            }
            if (victim.Position == Positions.Sleeping)
            {
                ch.send("They can't hear you right now.\n\r");
                return;
            }
            // maybe move striphidden to dotell, let hidden mobs stay hidden while they tell?
            ch.StripHidden();

            ch.Act("{1}$n tells you '{0}'{2}\n\r", victim, null, null, ActType.ToVictim, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Tell),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
            ch.Act("{1}You tell $N '{0}'{2}\n\r", victim, null, null, ActType.ToChar, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Tell),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));

            if (!ch.IsNPC)
                victim.ReplyTo = ch;
        }




        /// <summary>
        /// Handles the recall action for the character.
        /// </summary>
        /// <param name="ch">The character recalling.</param>
        /// <param name="arguments">Additional arguments for the recall.</param>
        public static void DoRecall(Character ch, string arguments)
        {
            // Get the recall room for the character
            var room = ch.GetRecallRoom();

            if (room != null)
            {
                // If a valid recall room is found, perform the recall action

                // Display a message to the room indicating the character's prayer
                ch.Act("$n prays for transportation and disappears.\n\r", type: ActType.ToRoom);

                // Remove the character from the current room
                ch.RemoveCharacterFromRoom();

                // Add the character to the recall room
                ch.AddCharacterToRoom(ch.GetRecallRoom());

                // Send a message to the character indicating the successful recall
                ch.SendToChar("You pray for transportation to your temple.\n\r");

                // Display a message to the room indicating the character's arrival
                ch.Act("$n appears before the altar.\n\r", type: ActType.ToRoom);

                // Update the character's view with the newly arrived room
                //DoLook(ch, "auto");
            }
            else
            {
                // If the recall room is not found, send an error message to the character
                ch.SendToChar("Room not found.\n\r");
            }
        }

        /// <summary>
        /// Handles the crawling action for the character.
        /// </summary>
        /// <param name="ch">The character crawling.</param>
        /// <param name="arguments">Additional arguments for the crawling.</param>
        public static void DoCrawl(Character ch, string arguments)
        {
            if (!string.IsNullOrEmpty(arguments))
            {
                Direction direction = Direction.North;

                // Get the direction from the arguments
                if (Utility.GetEnumValueStrPrefix(arguments, ref direction))
                {
                    // If a valid direction is obtained from the arguments, initiate crawling in that direction
                    ch.moveChar(direction, true, true);
                }
                else
                {
                    ch.send("Crawl West, East, South, West, Up, or Down?\n\r");
                }
            }
            else
            {
                ch.send("Crawl in which direction?\n\r");
            }
        }

        /// <summary>
        /// Handles the movement in a specific direction for the character.
        /// </summary>
        /// <param name="ch">The character initiating the movement.</param>
        /// <param name="direction">The direction of the movement.</param>
        /// <param name="arguments">Additional arguments for the movement.</param>
        public static void DoMoveDirection(Character ch, Direction direction, string arguments)
        {
            if (!string.IsNullOrEmpty(arguments) && ch.HasBuilderPermission(ch.Room))
            {
                // If additional arguments are provided and the character has builder permission in the current room,
                // update the exit description of the room in the specified direction.

                // Update the exit in the current room
                ch.Room.exits[(int)direction] = ch.Room.exits[(int)Direction.East] ?? new ExitData() { direction = direction };
                ch.Room.exits[(int)direction].description = arguments;

                // Update the original exit in the current room
                ch.Room.OriginalExits[(int)direction] = ch.Room.OriginalExits[(int)direction] ?? new ExitData() { direction = direction };
                ch.Room.OriginalExits[(int)direction].description = arguments;

                // Mark the area as unsaved
                ch.Room.Area.saved = false;
            }
            else
            {
                // If no additional arguments or if the character doesn't have builder permission, initiate the regular movement.
                ch.moveChar(direction, false, false);
            }
        }

        /// <summary>
        /// Handles movement in the north direction.
        /// </summary>
        /// <param name="ch">The character initiating the movement.</param>
        /// <param name="arguments">Additional arguments for the movement.</param>
        public static void DoNorth(Character ch, string arguments)
        {
            DoMoveDirection(ch, Direction.North, arguments);
        }

        /// <summary>
        /// Handles movement in the east direction.
        /// </summary>
        /// <param name="ch">The character initiating the movement.</param>
        /// <param name="arguments">Additional arguments for the movement.</param>
        public static void DoEast(Character ch, string arguments)
        {
            DoMoveDirection(ch, Direction.East, arguments);
        }

        /// <summary>
        /// Handles movement in the south direction.
        /// </summary>
        /// <param name="ch">The character initiating the movement.</param>
        /// <param name="arguments">Additional arguments for the movement.</param>
        public static void DoSouth(Character ch, string arguments)
        {
            DoMoveDirection(ch, Direction.South, arguments);
        }

        /// <summary>
        /// Handles movement in the west direction.
        /// </summary>
        /// <param name="ch">The character initiating the movement.</param>
        /// <param name="arguments">Additional arguments for the movement.</param>
        public static void DoWest(Character ch, string arguments)
        {
            DoMoveDirection(ch, Direction.West, arguments);
        }

        /// <summary>
        /// Handles movement upwards.
        /// </summary>
        /// <param name="ch">The character initiating the movement.</param>
        /// <param name="arguments">Additional arguments for the movement.</param>
        public static void DoUp(Character ch, string arguments)
        {
            DoMoveDirection(ch, Direction.Up, arguments);
        }

        /// <summary>
        /// Handles movement downwards.
        /// </summary>
        /// <param name="ch">The character initiating the movement.</param>
        /// <param name="arguments">Additional arguments for the movement.</param>
        public static void DoDown(Character ch, string arguments)
        {
            DoMoveDirection(ch, Direction.Down, arguments);
        }

        public static void DoOpen(Character ch, string arguments)
        {
            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };

            ItemData container;
            Direction direction = Direction.North;
            ExitData exit;
            int count = 0;

            if ((ch.Form == null && ch.Race != null && !ch.Race.parts.ISSET(PartFlags.Hands)) || (ch.Form != null && !ch.Form.Parts.ISSET(PartFlags.Hands)))
            {
                ch.send("You don't have hands to open that.\n\r");
                return;
            }

            if ((exit = ch.Room.GetExit(arguments, ref count)) != null && exit.destination != null)
            {
                direction = exit.direction;
                if (exit.flags.Contains(ExitFlags.Locked))
                    ch.Act("{0} is locked.\n\r", null, null, null, ActType.ToChar, exit.display);
                else if (exit.flags.Contains(ExitFlags.Closed))
                {
                    ch.send("You open {0}.\n\r", exit.display);
                    ch.Act("$n opens {0}.", null, null, null, ActType.ToRoom, exit.display);
                    exit.flags.REMOVEFLAG(ExitFlags.Closed);
                    ExitData otherSide;
                    if ((otherSide = exit.destination.exits[(int)reverseDirections[direction]]) != null &&
                        otherSide.destination == ch.Room &&
                        otherSide.flags.Contains(ExitFlags.Closed) &&
                        !otherSide.flags.Contains(ExitFlags.Locked))

                        otherSide.flags.Remove(ExitFlags.Closed);
                }
                else
                    ch.Act("{0} isn't closed.\n\r", null, null, null, ActType.ToChar, exit.display);
            }
            //else if (ch.Form != null)
            //{
            //    ch.send("You can't seem to manage that.\n\r");
            //    return;
            //}
            else if ((container = ch.GetItemHere(arguments)) != null)
            {
                if (!container.extraFlags.Contains(ExtraFlags.Closed))
                {
                    ch.send("{0} is already open.\n\r", container.Display(ch));
                    return;
                }
                else if (container.extraFlags.Contains(ExtraFlags.Locked))
                {
                    ch.send("It's locked.\n\r");
                    return;
                }
                else if (container.extraFlags.Contains(ExtraFlags.Closed))
                {
                    container.extraFlags.Remove(ExtraFlags.Closed);
                    ch.send("You open {0}.\n\r", container.Display(ch));
                    ch.Act("$n opens $p.\n\r", null, container, null, ActType.ToRoom);

                    Programs.ExecutePrograms(Programs.ProgramTypes.Open, ch, container, "");

                    return;
                }
                else
                    ch.send("You can't open that.\n\r");
            }
            else
                ch.send("You can't open that.\n\r");
        }

        public static void DoClose(Character ch, string arguments)
        {
            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };

            ItemData container;
            int count = 0;

            Direction direction = Direction.North;
            ExitData exit;

            if ((ch.Form == null && ch.Race != null && !ch.Race.parts.ISSET(PartFlags.Hands)) || (ch.Form != null && !ch.Form.Parts.ISSET(PartFlags.Hands)))
            {
                ch.send("You don't have hands to close that.\n\r");
                return;
            }

            if ((exit = ch.Room.GetExit(arguments, ref count)) != null && exit.destination != null)
            {
                direction = exit.direction;
                if (!exit.flags.Contains(ExitFlags.Door) && !exit.flags.Contains(ExitFlags.Window))
                    ch.send("There's no door or window to the " + exit.direction.ToString().ToLower() + ".\n\r");
                else if (exit.flags.Contains(ExitFlags.Closed))
                {
                    ch.send("{0} is already closed.\n\r", exit.display.TOUPPERFIRST());
                }
                else if (exit.flags.Contains(ExitFlags.Door) || exit.flags.Contains(ExitFlags.Window))
                {
                    ch.send("You close {0}.\n\r", exit.display);
                    ch.Act("$n closes {0}.\n\r", null, null, null, ActType.ToRoom, exit.display);
                    exit.flags.ADDFLAG(ExitFlags.Closed);

                    ExitData otherSide;
                    if ((otherSide = exit.destination.exits[(int)reverseDirections[direction]]) != null &&
                        otherSide.destination == ch.Room &&
                        (otherSide.flags.ISSET(ExitFlags.Door) || otherSide.flags.ISSET(ExitFlags.Window)) &&
                        !otherSide.flags.Contains(ExitFlags.Closed))

                        otherSide.flags.ADDFLAG(ExitFlags.Closed);
                }
            }

            else if ((container = ch.GetItemHere(arguments, ref count)) != null)
            {
                if (container.extraFlags.Contains(ExtraFlags.Closed))
                {
                    ch.send("{0} is already closed.\n\r", container.Display(ch));
                    return;
                }

                else if (container.extraFlags.Contains(ExtraFlags.Closable))
                {
                    container.extraFlags.ADDFLAG(ExtraFlags.Closed);
                    ch.send("You close {0}.\n\r", container.Display(ch));
                    ch.Act("$n closes $p.\n\r", null, container, null, ActType.ToRoom);

                    Programs.ExecutePrograms(Programs.ProgramTypes.Close, ch, container, "");

                    return;
                }
                else
                    ch.send("You can't close that.\n\r");
            }

            else
                ch.send("You can't close that.\n\r");
        }

        public static void DoLock(Character ch, string arguments)
        {
            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };

            Direction direction = Direction.North;
            ExitData exit;
            ItemData container;
            if ((exit = ch.Room.GetExit(arguments)) != null && exit.destination != null)
            {
                direction = exit.direction;
                if (!exit.flags.Contains(ExitFlags.Closed))
                {
                    ch.send("It isn't closed.\n\r");
                }
                else if (exit.flags.Contains(ExitFlags.Locked))
                {
                    ch.send("It's already locked.\n\r");
                }
                else if (exit.flags.Contains(ExitFlags.Door) || exit.flags.Contains(ExitFlags.Window))
                {
                    bool found = false;
                    foreach (var key in exit.keys)
                    {
                        foreach (var item in ch.Inventory)
                            if (key == item.Vnum)
                            {
                                found = true;
                                break;
                            }
                        if (!found)
                            foreach (var item in ch.Equipment.Values)
                                if (key == item.Vnum)
                                {
                                    found = true;
                                    break;
                                }

                        if (found) break;
                    }
                    if (exit.keys.Count == 0)
                    {
                        ch.send("There is no key for that.\n\r");
                        return;
                    }
                    else if (!found)
                    {
                        ch.send("You don't have the key for that.\n\r");
                        return;
                    }
                    else
                    {
                        ch.send("You lock " + exit.display + ".\n\r");
                        ch.Act("$n locks " + exit.display + ".\n\r", type: ActType.ToRoom);
                        exit.flags.ADDFLAG(ExitFlags.Locked);
                        ExitData otherSide;
                        if ((otherSide = exit.destination.exits[(int)reverseDirections[direction]]) != null && otherSide.destination == ch.Room && otherSide.flags.Contains(ExitFlags.Closed) && !otherSide.flags.Contains(ExitFlags.Locked))
                            otherSide.flags.ADDFLAG(ExitFlags.Locked);

                    }
                }
            }
            else if ((container = ch.GetItemHere(arguments)) != null)
            {
                if (container.extraFlags.ISSET(ExtraFlags.Locked))
                {
                    ch.send("It's already locked.\n\r");
                    return;
                }
                else if (!container.extraFlags.ISSET(ExtraFlags.Closed))
                {
                    ch.send("It isn't closed.\n\r");
                    return;
                }
                bool found = false;
                foreach (var key in container.Keys)
                {
                    foreach (var item in ch.Inventory)
                        if (key == item.Vnum)
                        {
                            found = true;
                            break;
                        }
                    if (!found)
                        foreach (var item in ch.Equipment.Values)
                            if (key == item.Vnum)
                            {
                                found = true;
                                break;
                            }

                    if (found) break;
                }

                if (container.Keys.Count == 0)
                {
                    ch.send("There is no key for that.\n\r");
                    return;
                }
                else if (!found)
                {
                    ch.send("You don't have the key for that.\n\r");
                    return;
                }
                else
                {
                    ch.Act("You lock $p.", null, container);
                    ch.Act("$n locks $p.", null, container, type: ActType.ToRoom);
                    container.extraFlags.ADDFLAG(ExtraFlags.Locked);

                }
            }
            else
                ch.send("You don't see that here.\n\r");
        }

        public static void DoUnlock(Character ch, string arguments)
        {
            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };

            Direction direction = Direction.North;
            ExitData exit;
            ItemData container;

            if ((exit = ch.Room.GetExit(arguments)) != null && exit.destination != null)
            {
                direction = exit.direction;
                if (!exit.flags.Contains(ExitFlags.Closed))
                {
                    ch.send("It isn't closed.\n\r");
                }
                else if (exit.flags.Contains(ExitFlags.Locked))
                {
                    bool found = false;
                    foreach (var key in exit.keys)
                    {
                        foreach (var item in ch.Inventory)
                            if (key == item.Vnum)
                            {
                                found = true;
                                break;
                            }
                        if (!found)
                            foreach (var item in ch.Equipment.Values)
                                if (key == item.Vnum)
                                {
                                    found = true;
                                    break;
                                }

                        if (found) break;
                    }
                    if (exit.keys.Count == 0)
                    {
                        ch.send("There is no key for that door.\n\r");
                        return;
                    }
                    else if (!found)
                    {
                        ch.send("You don't have the key for that.\n\r");
                        return;
                    }
                    else
                    {
                        ch.send("You unlock " + exit.display + ".\n\r");
                        ch.Act("$n unlocks " + exit.display + ".\n\r", type: ActType.ToRoom);
                        exit.flags.REMOVEFLAG(ExitFlags.Locked);
                        ExitData otherSide;
                        if ((otherSide = exit.destination.exits[(int)reverseDirections[direction]]) != null && otherSide.destination == ch.Room && otherSide.flags.Contains(ExitFlags.Closed) && otherSide.flags.Contains(ExitFlags.Locked))
                            otherSide.flags.REMOVEFLAG(ExitFlags.Locked);
                    }
                }
                else
                    ch.send("It isn't locked.");
            }
            else if ((container = ch.GetItemHere(arguments)) != null)
            {
                if (!container.extraFlags.ISSET(ExtraFlags.Locked))
                {
                    ch.send("It's not locked.\n\r");
                    return;
                }
                bool found = false;
                foreach (var key in container.Keys)
                {
                    foreach (var item in ch.Inventory)
                        if (key == item.Vnum)
                        {
                            found = true;
                            break;
                        }
                    if (!found)
                        foreach (var item in ch.Equipment.Values)
                            if (key == item.Vnum)
                            {
                                found = true;
                                break;
                            }

                    if (found) break;
                }

                if (container.Keys.Count == 0)
                {
                    ch.send("There is no key for that.\n\r");
                    return;
                }
                else if (!found)
                {
                    ch.send("You don't have the key for that.\n\r");
                    return;
                }
                else
                {
                    foreach (var npc in ch.Room.Characters.OfType<NPCData>())
                    {
                        if (npc.Programs.Any(prog => prog.Types.ISSET(Programs.ProgramTypes.BeforeUnlock)))
                        {
                            var progs = npc.Programs.Where(prog => prog.Types.ISSET(Programs.ProgramTypes.BeforeUnlock));
                            foreach (var prog in progs)
                            {
                                if (prog.Execute(ch, npc, null, container, null, Programs.ProgramTypes.BeforeUnlock, arguments))
                                {
                                    // prog will send a message
                                    return;
                                }
                            }
                        }
                    }
                    ch.Act("You unlock $p.", null, container);
                    ch.Act("$n unlocks $p.", null, container, type: ActType.ToRoom);
                    container.extraFlags.REMOVEFLAG(ExtraFlags.Locked);

                }
            }
            else
                ch.send("You don't see that here.\n\r"); // ch.send("You can't unlock that.\n\r");
        }

        public static void DoSave(Character ch, string arguments)
        {
            if (ch is Player)
            {
                ((Player)ch).SaveCharacterFile();
                ch.SendToChar("Character data saved.\n\r");
            }

        }

        public static void DoNotes(Character ch, string arguments)
        {
            if (!(ch is Player))
                return;
            var connection = (Player)ch;
            string command = "";
            arguments = arguments.OneArgument(ref command);
            if ("read".StringCmp(command) || command.ISEMPTY())
            {
                DateTime lastReadDate = DateTime.MinValue;
                bool found = false;
                foreach (var note in NoteData.Notes.OrderBy(tempnote => tempnote.Sent))
                {
                    if (note.Sent > connection.LastReadNote && (note.To.IsName("all", true) || note.To.IsName(ch.Name, true)))
                    {
                        connection.LastReadNote = note.Sent;
                        using (new Character.Page(ch))
                            ch.send("To: " + note.To + "\nFrom: " + note.Sender + "\nSubject: " + note.Subject + "\n\r" + "Body: " + note.Body + "\n\r");
                        found = true;
                        break;
                    }
                    else if (note.Sent > connection.LastReadNote)
                        lastReadDate = note.Sent;


                }
                if (lastReadDate > connection.LastReadNote)
                    connection.LastReadNote = lastReadDate;
                if (!found)
                    ch.send("No unread notes.\n\r");
            }
            else if ("list".StringCmp(command))
            {
                foreach (var note in NoteData.Notes.ToArray().OrderBy(tempnote => tempnote.Sent))
                {
                    // purge old notes
                    if (DateTime.Now > note.Sent.AddMonths(1))
                    {
                        NoteData.Notes.Remove(note);
                    }
                    else if (note.Sent > connection.LastReadNote && (note.To.IsName("all", true) || note.To.IsName(ch.Name, true)))
                    {
                        //connection.LastReadNote = note.Sent;

                        ch.send(note.Sender + " - " + note.Subject + "\n\r");
                    }


                }
            }
            else if ("show".StringCmp(command))
            {
                ch.send("To: " + connection.UnsentNote.To + "\n\rSubject: " + connection.UnsentNote.Subject + "\n\r" + "Body: " + connection.UnsentNote.Body + "\n\r");
            }
            else if ("to".StringCmp(command))
            {
                connection.UnsentNote.To = arguments;
                ch.send("Note to {0}.\n\r", arguments);
            }
            else if ("subject".StringCmp(command))
            {
                connection.UnsentNote.Subject = arguments;
                ch.send("Note suject {0}.\n\r", arguments);
            }
            else if ("+".StringCmp(command))
            {
                if (connection.UnsentNote.Body.Length + arguments.Length > 800 || (connection.UnsentNote.Body + arguments).Count(c => c == '\n') > 80)
                {
                    ch.send("Note too long.\n\r");
                }
                else
                {
                    connection.UnsentNote.Body = connection.UnsentNote.Body + "\n\r" + arguments;
                    ch.send("OK.\n\r");
                }
            }
            else if ("-".StringCmp(command))
            {
                connection.UnsentNote.Body = connection.UnsentNote.Body.Substring(0, connection.UnsentNote.Body.LastIndexOf("\n\r"));
            }
            else if ("clear".StringCmp(command))
            {
                connection.UnsentNote = new NoteData();
            }
            else if ("send".StringCmp(command))
            {
                if (NoteData.Notes.Count(n => n.Sender == ch.Name) > 100)
                {
                    ch.send("You have too many notes already.\n\r");
                }
                else if (!connection.UnsentNote.Subject.ISEMPTY() && !connection.UnsentNote.Body.ISEMPTY() && !connection.UnsentNote.To.ISEMPTY())
                {
                    connection.UnsentNote.Sent = DateTime.Now;
                    connection.UnsentNote.Sender = ch.Name;
                    NoteData.Notes.Add(connection.UnsentNote);
                    NoteData.SaveNotes();
                    connection.UnsentNote = new NoteData();
                    ch.send("Note sent.\n\r");
                }
                else
                    ch.send("One or more of the note fields are blank\n\r");
            }
            else
                ch.send("Syntax: note [list|read|to|subject|+|-|show]\n\r");
        }

        public static void DoQuit(Character ch, string arguments)
        {
            if (ch is Player player)
            {
                ch.Act("The form of $n disappears before you.", null, null, null, ActType.ToRoom);
                // Remove keys
                foreach (var item in ch.Inventory.ToArray())
                {
                    if (item.ItemType.ISSET(ItemTypes.Key))
                    {
                        ch.Inventory.Remove(item);
                        item.Dispose();
                    }
                }

                foreach (var item in ch.Equipment.ToArray())
                {
                    if (item.Value.ItemType.ISSET(ItemTypes.Key))
                    {
                        ch.Equipment.Remove(item.Key);
                    }
                }
                // end remove keys
                player.SaveCharacterFile();
                player.SendRaw("\nGoodbye!\n\r", true);
                ch.RemoveCharacterFromRoom(ExecutePrograms: false);
                ch.Dispose();
                Game.CloseSocket((Player)ch, true);
            }
        }

        public static void DoWhere(Character ch, string arguments)
        {
            if (ch.IsAffected(AffectFlags.Blind))
            {
                ch.send("You can't see anything!\n\r");
            }
            else if (ch.Room != null && ch.Room.Area != null)
            {
                StringBuilder others = new StringBuilder();

                if (!arguments.ISEMPTY())
                {
                    foreach (var other in (from room in ch.Room.Area.Rooms.Values from c in room.Characters select c))
                    {
                        if (other.Name.IsName(arguments))
                        {
                            var display = (other.Display(ch));
                            ch.send((other == ch ? "*   " : "    ") + display.PadRight(20) + (TimeInfo.IS_NIGHT && !other.Room.NightName.ISEMPTY() ? other.Room.NightName : other.Room.Name) + "\n\r");
                            return;
                        }
                    }

                    ch.send("You don't see them.\n\r");
                    return;
                }
                else
                {
                    foreach (var other in ch.Room.Area.People)
                    {

                        var display = (other.Display(ch));
                        if (other.Room != null && ch.CanSee(other))
                            others.Append((other == ch ? "*   " : "    ") + display.PadRight(20) + (TimeInfo.IS_NIGHT && !other.Room.NightName.ISEMPTY() ? other.Room.NightName : other.Room.Name) + "\n\r");
                    }
                    if (others.Length == 0)
                        others.AppendLine("    No one nearby.\n\r");
                    ch.SendToChar("Players nearby:\n\r" + others.ToString());
                }

            }
            else
                ch.SendToChar("You aren't in an area where people can be tracked!\n\r");
        }

        public static void DoWho(Character ch, string arguments)
        {
            StringBuilder whoList = new StringBuilder();
            int playersOnline = 0;
            whoList.AppendLine("You can see:");

            foreach (var player in Game.Instance.Info.Connections)
            {
                if (player.state == Player.ConnectionStates.Playing && player.connection != null && (!player.Flags.ISSET(ActFlags.WizInvis) || ch.Flags.ISSET(ActFlags.HolyLight) && ch.Level >= player.Level))
                {
                    whoList.AppendFormat("[{0} {1}] {2}{3}{4}{5}{6}\n\r",
                        player.Level.ToString().PadRight(4),
                        player.Guild.whoName,
                        player.IsAFK ? "\\r(AFK)\\x" : "     ",
                        player == ch ? "*" : " ",
                        player.Name,
                        (!player.Title.ISEMPTY() ? ((!player.Title.ISEMPTY() && player.Title.StartsWith(",") ? player.Title : " " + player.Title)) : ""),
                        (!player.ExtendedTitle.ISEMPTY() ? (!player.ExtendedTitle.StartsWith(",") ? " " : "") + player.ExtendedTitle : ""));
                    playersOnline++;
                }
            }
            whoList.Append("Visible players: " + playersOnline + "\n\r");
            whoList.Append("Max players online at once since last reboot: " + Game.Instance.MaxPlayersOnline + "\n\r");
            whoList.Append("Max players online at once ever: " + Game.MaxPlayersOnlineEver + "\n\r");

            using (new Character.Page(ch))
                ch.SendToChar(whoList.ToString());
        }

        public static void DoFollow(Character ch, string arguments)
        {
            int count = 0;
            Character follow = null;
            if (arguments.equals("self") || (follow = ch.GetCharacterFromRoomByName(arguments, ref count)) == ch)
            {
                if (ch.Following != null)
                {
                    ch.send("You stop following " + (ch.Following.Display(ch)) + ".\n\r");
                    if (ch.Following.Group.Contains(ch))
                        ch.Following.Group.Remove(ch);
                    ch.Following.send(ch.Display(ch.Following) + " stops following you.\n\r");

                    foreach (var other in Character.Characters.ToArray())
                    {
                        if (other.Leader == ch.Leader && other != ch)
                        {
                            other.Act("$N leaves the group.", ch);
                        }
                    }

                    ch.Following = null;
                    ch.Leader = null;
                }
                else
                    ch.send("You aren't following anybody.\n\r");
            }
            else if (follow != null)
            {
                if (ch.Following != null && ch.Following.Group.Contains(ch))
                {
                    ch.Following.Group.Remove(ch);
                    foreach (var other in Character.Characters.ToArray())
                    {
                        if (other.Leader == ch.Leader && other != ch)
                        {
                            other.Act("$N leaves the group.", ch);
                        }
                    }
                    ch.Leader = null;
                    ch.Following.send(ch.Display(ch.Following) + " stops following you.\n\r");

                }

                if (follow.Flags.ISSET(ActFlags.NoFollow))
                {
                    ch.send("They don't allow followers.\n\r");
                }
                else
                {
                    ch.Leader = null;
                    ch.Following = follow;
                    ch.send("You start following " + (follow.Display(ch)) + ".\n\r");
                    ch.Act("$n begins following $N.", follow, type: ActType.ToRoomNotVictim);
                    follow.send(ch.Display(follow) + " starts following you.\n\r");
                }
            }
            else
                ch.send("You don't see them here.\n\r");

        }

        public static void DoNofollow(Character ch, string arguments)
        {
            if (!ch.Flags.ISSET(ActFlags.NoFollow))
            {
                ch.Flags.ADDFLAG(ActFlags.NoFollow);
                ch.NoFollow();
            }
            else
            {
                ch.Flags.REMOVEFLAG(ActFlags.NoFollow);
                ch.send("You now allow followers.\n\r");
            }
        }

        public static void DoGroup(Character ch, string arguments)
        {
            Character groupWith = null;
            int count = 0;

            if (string.IsNullOrEmpty(arguments))
            {
                var groupLeader = (ch.Following != null && ch.Following.Group.Contains(ch)) ? ch.Following : ch;

                if (groupLeader.Group.Count > 0)
                {
                    StringBuilder members = new StringBuilder();
                    int percentHP;
                    int percentMana;
                    int percentMove;

                    percentHP = (int)((float)groupLeader.HitPoints / (float)groupLeader.MaxHitPoints * 100f);
                    percentMana = (int)((float)groupLeader.ManaPoints / (float)groupLeader.MaxManaPoints * 100f);
                    percentMove = (int)((float)groupLeader.MovementPoints / (float)groupLeader.MaxMovementPoints * 100f);

                    members.AppendLine("Group leader: " + groupLeader.Display(ch).PadRight(20) + " Lvl " + groupLeader.Level + " " + percentHP + "%hp " + percentMana + "%m " + percentMove + "%mv");
                    foreach (var member in groupLeader.Group)
                    {
                        if (member == groupLeader)
                            continue;
                        percentHP = (int)((float)member.HitPoints / (float)member.MaxHitPoints * 100f);
                        percentMana = (int)((float)member.ManaPoints / (float)member.MaxManaPoints * 100f);
                        percentMove = (int)((float)member.MovementPoints / (float)member.MaxMovementPoints * 100f);

                        members.AppendLine("              " + member.Display(ch).PadRight(20) + " Lvl " + member.Level + " " + percentHP + "%hp " + percentMana + "%m " + percentMove + "%mv");
                    }

                    ch.send(members.ToString());
                }
                else
                    ch.send("You aren't in a group.\n\r");
            }
            else if (!ch.IsAwake)
            {
                ch.send("In your dreams, or what?\n\r");
                return;
            }
            else if (arguments.equals("self") || (groupWith = ch.GetCharacterFromRoomByName(arguments, ref count)) == ch)
            {
                ch.send("You can't group with yourself.\n\r");
            }
            else if (groupWith != null)
            {
                if (ch.Following != null && ch.Following.Group.Contains(ch))
                {
                    ch.send("You aren't the group leader.\n\r");
                }
                else if (groupWith.Following != ch)
                    ch.send("They aren't following you.\n\r");
                else if (ch.Group.Contains(groupWith))
                {
                    groupWith.Leader = null;
                    ch.Group.Remove(groupWith);
                    ch.send("You remove " + groupWith.Display(ch) + " from the group.\n\r");
                    ch.Act("$n removes $N from their group.\n\r", groupWith, type: ActType.ToRoomNotVictim);
                    groupWith.send(ch.Display(groupWith) + " removes you from the group.\n\r");
                }
                else
                {
                    if (!ch.Group.Contains(ch))
                        ch.Group.Add(ch);
                    ch.Group.Add(groupWith);
                    groupWith.Leader = ch;
                    ch.send("You add " + (groupWith.Display(ch)) + " to the group.\n\r");
                    ch.Act("$n adds $N to their group.", groupWith, type: ActType.ToRoomNotVictim);
                    groupWith.send(ch.Display(groupWith) + " adds you to the group.\n\r");
                }
            }
        }

        public static void DoStand(Character ch, string arguments)
        {
            if (ch.Position == Positions.Resting || ch.Position == Positions.Sitting)
            {
                ch.send("You stand up.\n\r");
                ch.Act("$n stands up.\n\r", type: ActType.ToRoom);
            }
            else if (ch.Position == Positions.Sleeping)
            {
                if (ch.IsAffected(AffectFlags.Sleep) || ch.IsAffected(SkillSpell.SkillLookup("strangle")))
                {
                    ch.send("You try but can't wake up!\n\r");
                    return;
                }
                ch.send("You wake and stand up.\n\r");
                ch.Act("$n wakes and stands up.\n\r", type: ActType.ToRoom);
            }
            else
            {
                ch.send("You can't do that.\n\r");
                return;
            }
            ch.Position = Positions.Standing;
        }

        public static void DoRest(Character ch, string arguments)
        {
            if (ch.Position == Positions.Standing || ch.Position == Positions.Sitting)
            {
                ch.send("You start resting.\n\r");
                ch.Act("$n starts resting.\n\r", type: ActType.ToRoom);
            }
            else if (ch.Position == Positions.Sleeping)
            {
                if (ch.IsAffected(AffectFlags.Sleep) || ch.IsAffected(SkillSpell.SkillLookup("strangle")))
                {
                    ch.send("You try but can't wake up!\n\r");
                    return;
                }
                ch.send("You wake and start resting.\n\r");
                ch.Act("$n wakes and starts resting.\n\r", type: ActType.ToRoom);
            }
            else
            {
                ch.send("You can't do that.\n\r");
                return;
            }
            ch.Position = Positions.Resting;
        }

        public static void DoSit(Character ch, string arguments)
        {
            if (ch.Position == Positions.Standing || ch.Position == Positions.Resting)
            {
                ch.send("You sit down.\n\r");
                ch.Act("$n sits down.\n\r", type: ActType.ToRoom);
            }
            else if (ch.Position == Positions.Sleeping)
            {
                if (ch.IsAffected(AffectFlags.Sleep) || ch.IsAffected(SkillSpell.SkillLookup("strangle")))
                {
                    ch.send("You try but can't wake up!\n\r");
                    return;
                }
                ch.send("You wake and sit down.\n\r");
                ch.Act("$n wakes and sits down.\n\r", type: ActType.ToRoom);
            }
            else
            {
                ch.send("You can't do that.\n\r");
                return;
            }
            ch.Position = Positions.Sitting;
        }

        public static void DoSleep(Character ch, string arguments)
        {
            if (ch.Position == Positions.Standing || ch.Position == Positions.Resting || ch.Position == Positions.Sitting)
            {
                ch.send("You lay down and go to sleep.\n\r");
                ch.Act("$n lays down and goes to sleep.\n\r", type: ActType.ToRoom);
                ch.Position = Positions.Sleeping;
            }
            else if (ch.Position == Positions.Sleeping)
            {
                ch.Act("You are already asleep.");
            }
            else
            {
                ch.send("You can't do that.\n\r");
            }

        }
        public static void DoCamp(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("camp");
            int chance = ch.GetSkillPercentage(skill);
            if (chance <= 1)
            {
                ch.send("You don't know how to camp.\n\r");
                return;
            }
            if (ch.Fighting != null || ch.Position == Positions.Fighting)
            {
                ch.send("You can't camp while fighting!\n\r");
                return;
            }
            if (!ch.Room.IsWilderness)
            {
                ch.send("You can only camp in the wild.\n\r");
                return;
            }
            if (chance + 20 < Utility.NumberPercent())
            {
                ch.send("You tried to set up camp but failed.\n\r");
                ch.CheckImprove(skill, false, 1);
                return;
            }

            ch.send("You prepare a camp and quickly lay down to rest.\n\r");
            ch.Act("$n prepares a camp and quickly lays down to rest.\n\r", type: ActType.ToRoom);

            foreach (var groupmember in ch.Room.Characters.ToArray())
            {
                if (groupmember.IsSameGroup(ch) && (ch.Position == Positions.Standing || ch.Position == Positions.Resting || ch.Position == Positions.Sitting))
                {
                    if (ch != groupmember)
                    {
                        groupmember.Act("$N has prepared a camp, you quickly join $M in resting.\n\r", ch);
                        groupmember.Act("$n joins $N at $S camp and quicly lays down to rest.\n\r", type: ActType.ToRoomNotVictim);
                        groupmember.Position = Positions.Sleeping;
                    }
                    var aff = new AffectData();
                    aff.skillSpell = skill;
                    aff.where = AffectWhere.ToAffects;
                    aff.duration = -1;
                    aff.displayName = "camp";
                    aff.endMessage = "You feel refreshed as you break camp.\n\r";
                    groupmember.AffectToChar(aff);
                }
            }
            ch.Position = Positions.Sleeping;
            ch.CheckImprove(skill, true, 1);
        }

        public static void DoWake(Character ch, string arguments)
        {
            Character other;
            int count = 0;
            if (string.IsNullOrEmpty(arguments))
            {
                if (ch.IsAffected(AffectFlags.Sleep) || ch.IsAffected(SkillSpell.SkillLookup("strangle")))
                {
                    ch.send("You try but can't wake up!\n\r");
                    return;
                }
                else if (ch.Position == Positions.Sleeping)
                {
                    ch.send("You wake and stand up.\n\r");
                    ch.Act("$n wakes and stands up.\n\r", type: ActType.ToRoom);
                }
                else
                {
                    ch.send("You aren't sleeping!\n\r");
                    return;
                }
                ch.Position = Positions.Standing;
            }
            else if ((other = ch.GetCharacterFromRoomByName(arguments, ref count)) != null)
            {
                if (other.IsAffected(AffectFlags.Sleep))
                {
                    ch.Act("They can't be woken up.");
                }
                else if (other.Position == Positions.Sleeping)
                {
                    other.send(ch.Display(other) + " wakes you up and you stand up.\n\r");
                    ch.Act("$n wakes $N up.\n\r", other, type: ActType.ToRoom);
                    ch.Act("You wake $N up.", other, type: ActType.ToChar);
                    other.Position = Positions.Standing;
                }
                else
                    ch.send("They aren't sleeping.\n\r");
            }
        }

        public static void DoFly(Character ch, string arguments)
        {
            RoomData overroom = null;

            if (ch.GetSkillPercentage("airform fly") <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
            }
            else if (ch.Room == null || ch.Room.Area == null || ch.Room.Area.OverRoomVnum == 0 || !RoomData.Rooms.TryGetValue(ch.Room.Area.OverRoomVnum, out overroom))
            {
                ch.send("You can't fly from here.\n\r");
            }
            else
            {
                ch.Act("$n flies into the air above.", type: ActType.ToRoom);
                ch.Act("You fly into the air above.", type: ActType.ToChar);
                ch.RemoveCharacterFromRoom();
                ch.AddCharacterToRoom(overroom);
                ch.Act("$n flies in from below.", type: ActType.ToRoom);
                // Character.DoLook(ch, "auto");
            }
        }
        public static void DoPractice(Character ch, string arguments)
        {
            // var skills = from skill in ch.learned where skill.Key.skillType.Contains(SkillSpellTypes.Skill) && (skill.Key.skillLevel.ContainsKey(ch.guild.name) || skill.Value > 0) orderby skill.Key.skillLevel.ContainsKey(ch.guild.name) ? skill.Key.skillLevel[ch.guild.name] : 100 select skill;
            if (string.IsNullOrEmpty(arguments.Trim()))
            {
                int column = 0;
                var text = new StringBuilder();
                foreach (var skill in from tempskill in SkillSpell.Skills
                                      where
                                        tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Skill) ||
                                        tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Spell) ||
                                        tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Commune) ||
                                        tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Song) ||
                                        (tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.InForm) && tempskill.Value.spellFun != null)
                                      orderby ch.GetLevelSkillLearnedAtOutOfForm(tempskill.Value) // skill.Value.skillLevel.ContainsKey(ch.Guild.name) ? skill.Value.skillLevel[ch.Guild.name] : 60
                                      select tempskill.Value)
                {
                    var percent = ch.GetSkillPercentageOutOfForm(skill); // ch.Learned.TryGetValue(skill, out int percent);
                    //skill.skillLevel.TryGetValue(ch.Guild.name, out int lvl);
                    var lvl = ch.GetLevelSkillLearnedAtOutOfForm(skill);

                    if (percent > 1 || ch.Level >= lvl && skill.PrerequisitesMet(ch)) // && (lvl < (game.LEVEL_HERO + 1)) || ch.IS_IMMORTAL))
                    {

                        text.Append("    " + (skill.name + " " + (ch.Level >= lvl || percent > 1 ? percent + "%" : "N/A").PadLeft(4)).PadLeft(30).PadRight(25));

                        if (column == 1)
                        {
                            text.AppendLine();
                            column = 0;
                        }
                        else
                            column++;
                    }
                }
                ch.send(text + "\n\rYou have {0} practice sessions left.\n\r", ch.Practices);
            }
            else
            {
                if (!ch.IsAwake)
                {
                    ch.SendToChar("In your dreams or what?\n\r");
                    return;
                }

                if (ch.Form != null)
                {
                    ch.send("You can't do that in form.\n\r");
                    return;
                }
                SkillSpell skill;
                skill = (from tempskill in SkillSpell.Skills orderby tempskill.Key
                         where
                           (tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Skill) ||
                           tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Spell) ||
                           tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Commune) ||
                           tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Song) ||
                           (tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.InForm) && tempskill.Value.spellFun != null))
                         && (ch.Level >= ch.GetLevelSkillLearnedAtOutOfForm(tempskill.Value) && ch.GetSkillPercentageOutOfForm(tempskill.Value) >= 1)
                         && tempskill.Value.name.StringPrefix(arguments)
                         //orderby ch.GetLevelSkillLearnedAtOutOfForm(tempskill.Value) // skill.Value.skillLevel.ContainsKey(ch.Guild.name) ? skill.Value.skillLevel[ch.Guild.name] : 60
                         select tempskill.Value).FirstOrDefault();
                //skill = SkillSpell.SkillLookup(arguments);
                Character practiceMob = null;
                foreach (var mob in ch.Room.Characters)
                {
                    if (mob.Flags.ISSET(ActFlags.GuildMaster)) // && mob.guild == ch.guild)
                    {
                        practiceMob = mob;
                        break;
                    }
                }

                if (practiceMob != null && practiceMob.Guild != null && practiceMob.Guild != ch.Guild)
                {
                    ch.send("They won't teach you because you aren't a {0}.\n\r", practiceMob.Guild.name);
                    return;
                }
                if (skill != null)
                {
                    var learned = ch.GetSkillPercentage(skill);
                    //ch.Learned.TryGetValue(skill, out int learned);

                    if (ch.Level >= ch.GetLevelSkillLearnedAt(skill) && ch.Practices >= 1 && practiceMob != null && learned < 75)
                    {
                        ch.LearnSkill(skill, Math.Min(learned + PhysicalStats.IntelligenceApply[ch.GetCurrentStat(PhysicalStatTypes.Intelligence)].Learn, 75));
                        ch.Practices -= 1;
                        ch.send("You practice and learn in the ability {0}.\n\r", skill.name);
                        if (ch.Learned[skill].Percentage == 75)
                        {
                            ch.send("\\gYou are now learned in the ability {0}.\\x\n\r", skill.name);
                        }
                    }
                    else if (learned >= 75)
                    {
                        ch.send("You are already learned at " + skill.name + ".\n\r");
                    }
                    else if (practiceMob == null)
                        ch.send("Find a guild master and have practices.\n\r");
                    else if (ch.Practices == 0)
                    {
                        ch.send("You don't have enough practice sessions.\n\r");
                    }
                    else
                        ch.send("What skill is that?\n\r");
                }
                else
                    ch.send("What skill is that?\n\r");
            }
        }

        public static void DoTrain(Character ch, string arguments)
        {
            Character Trainer = null;

            foreach (var mob in ch.Room.Characters)
            {
                if (mob.Flags.ISSET(ActFlags.Trainer))
                {
                    Trainer = mob;
                    break;
                }
            }

            if (Trainer != null)
            {
                if (ch.Form != null)
                {
                    ch.send("You can't do that in form.\n\r");
                    return;
                }

                if (arguments.ISEMPTY())
                {

                    ch.send("You can train: \n\r");
                    foreach (var stat in Enum.GetValues(typeof(PhysicalStatTypes)).OfType<PhysicalStatTypes>())//(var stat = PhysicalStatTypes.Strength; stat < PhysicalStatTypes.Charisma; stat++)
                    {
                        if (ch.PermanentStats[stat] < ch.PcRace.MaxStats[stat])
                        {
                            ch.send("\t" + stat.ToString() + "\n\r");
                        }
                    }
                    ch.send("\tHitpoints\n\r\tMana\n\r");
                    ch.send("You have " + ch.Trains + " trains.\n\r");
                    return;
                }


                if (ch.Trains > 0)
                {
                    foreach (var stat in Enum.GetValues(typeof(PhysicalStatTypes)).OfType<PhysicalStatTypes>())//(var stat = PhysicalStatTypes.Strength; stat < PhysicalStatTypes.Charisma; stat++)
                    {
                        if (stat.ToString().StringPrefix(arguments) && ch.PermanentStats[stat] < ch.PcRace.MaxStats[stat])
                        {
                            ch.send("You train your " + stat.ToString() + ".\n\r");
                            ch.Act("$n's " + stat.ToString() + " increases.\n\r", type: ActType.ToRoom);
                            ch.PermanentStats[stat] = ch.PermanentStats[stat] + 1;
                            ch.Trains--;
                            return;
                        }
                    }

                    if ("hitpoints".StringPrefix(arguments) || arguments.StringCmp("hp"))
                    {
                        ch.send("You train your hitpoints.\n\r");
                        ch.Act("$n's hitpoints increase.\n\r", type: ActType.ToRoom);
                        ch.MaxHitPoints += 10;
                        ch.Trains--;
                        return;
                    }

                    if ("mana".StringPrefix(arguments))
                    {
                        ch.send("You train your mana.\n\r");
                        ch.Act("$n's mana increases.\n\r", type: ActType.ToRoom);
                        ch.MaxManaPoints += 10;
                        ch.Trains--;
                        return;
                    }

                    ch.send("You can't train that.\n\r");
                }
                else
                    ch.send("You have no trains.\n\r");
            }
            else
                ch.send("There is no trainer here.\n\r");

        }

        public static void DoGain(Character ch, string arguments)
        {
            Character Trainer = null;

            foreach (var mob in ch.Room.Characters)
            {
                if (mob.Flags.ISSET(ActFlags.Trainer))
                {
                    Trainer = mob;
                    break;
                }
            }

            if (Trainer != null)
            {
                if (ch.Form != null)
                {
                    ch.send("You can't do that in form.\n\r");
                    return;
                }

                if ("revert".StringPrefix(arguments))
                {
                    if (ch.Trains > 0)
                    {
                        ch.Practices += 10;
                        ch.Trains--;
                        ch.Act("You convert 1 train into 10 practices.");
                    }
                    else
                        ch.send("You have no trains.\n\r");
                }
                else if ("convert".StringPrefix(arguments))
                {
                    if (ch.Practices >= 10)
                    {
                        ch.Practices -= 10;
                        ch.Trains++;
                        ch.Act("You convert 10 practices into 1 train.");
                    }
                    else
                        ch.send("You don't have enough practices.\n\r");
                }
                else
                    ch.send("Gain [convert|revert]");
            }
            else
                ch.send("There is no trainer here.\n\r");

        }


        public static void DoOutfit(Character ch, string arguments)
        {
            var anyEquipped = false;
            ItemData wearing;

            foreach (var itemTemplate in ItemTemplateData.Templates.Values)
            {
                if (itemTemplate.extraFlags.ISSET(ExtraFlags.Outfit))
                {
                    if (itemTemplate.itemTypes.ISSET(ItemTypes.Instrument) && !ch.Guild.name.StringCmp("bard"))
                        continue;
                    bool given = false;
                    foreach (var slot in Character.WearSlots)
                    {

                        if (!ch.Equipment.TryGetValue(slot.id, out wearing) || wearing == null)
                        {
                            if (itemTemplate.wearFlags.Contains(slot.flag))
                            {
                                var item = new ItemData(itemTemplate, ch, true);
                                anyEquipped = true;
                                given = true;
                            }
                        }
                    }

                    if (itemTemplate.itemTypes.ISSET(ItemTypes.Instrument)
                        && !given
                        && (!ch.Equipment.TryGetValue(WearSlotIDs.Held, out var held)
                            || held == null
                            || !held.ItemType.ISSET(ItemTypes.Instrument)))
                        _ = new ItemData(itemTemplate, ch, true);
                }
            }
            if (!ch.Equipment.TryGetValue(WearSlotIDs.Wield, out wearing) || wearing == null)
            {
                int index;
                ItemTemplateData weapontemplate;
                int highestSkill = 0;
                var weapons = new string[] { "sword", "axe", "spear", "staff", "dagger", "mace", "whip", "flail", "polearm" };
                var weaponVNums = new int[] { 40000, 40001, 40004, 40005, 40002, 40003, 40006, 40007, 40020 };
                ItemTemplateData bestWeaponTemplate = null;
                for (index = 0; index < weapons.Length; index++)
                {
                    if (ItemTemplateData.Templates.TryGetValue(weaponVNums[index], out weapontemplate))
                    {
                        //var weapon = new ItemData(item, this, true);
                        var skill = SkillSpell.SkillLookup(weapons[index]);
                        if (skill != null && ch.GetSkillPercentage(skill) > highestSkill)
                        {
                            bestWeaponTemplate = weapontemplate;
                            highestSkill = ch.GetSkillPercentage(skill);
                        }
                    }
                }

                if (bestWeaponTemplate != null)
                {
                    var item = new ItemData(bestWeaponTemplate, ch, true);
                    anyEquipped = true;
                }
            }

            if (anyEquipped)
            {
                if (ItemTemplateData.Templates.TryGetValue(40010, out var itemTemplate))
                {
                    var item = new ItemData(itemTemplate, ch, false);
                }

                if (ItemTemplateData.Templates.TryGetValue(40011, out itemTemplate))
                {
                    var item = new ItemData(itemTemplate, ch, false);
                }

                if (ItemTemplateData.Templates.TryGetValue(40012, out itemTemplate))
                {
                    var item = new ItemData(itemTemplate, ch, false);
                }

                ch.send("The gods have equipped you.\n\r");
            }
            else
                ch.send("The gods found you wanting nothing.\n\r");
        }

        public static void DoDelete(Character ch, string arguments)
        {
            if (ch.IsNPC)
            {
                return;
            }
            else if (!(from aff in ch.AffectsList where aff.name == "DeleteConfirm" select aff).Any())
            {
                ch.AffectsList.Add(new AffectData { duration = 2, displayName = "Contemplating Deletion", name = "DeleteConfirm" });
                ch.send("Type 'delete yes' to confirm.\n\r");
            }
            else if ("yes".StringCmp(arguments))
            {
                ((Player)ch).Delete();

            }
            else
            {
                ch.AffectsList.RemoveAll(aff => aff.name == "DeleteConfirm");
                ch.send("Delete cancelled.\n\r");
            }
        }

        public static void DoPassword(Character ch, string arguments)
        {
            string password = "";
            string passwordConfirm = "";

            if (ch.IsNPC)
            {
                return;
            }

            arguments = arguments.OneArgument(ref password);
            arguments = arguments.OneArgument(ref passwordConfirm);

            if (password.ISEMPTY() || passwordConfirm.ISEMPTY())
            {
                ch.send("Syntax: Password {New Password} {Confirm New Password}\n\r");
            }
            else if (password != passwordConfirm)
            {
                ch.send("Passwords do not match.\n\r");
            }
            else
            {
                ((Player)ch).SetPassword(password);
                
                ((Player)ch).SaveCharacterFile();

                ch.send("Password changed and character file saved.\n\r");
            }
        }



        public static void DoArea(Character ch, string arguments)
        {
            if (ch.Room == null || ch.Room.Area == null)
                ch.send("You aren't anywhere.");
            else
                ch.send(ch.Room.Area.Name.TOSTRINGTRIM().PadRight(20) + " - " + (ch.Room.Area.Credits.TOSTRINGTRIM()).PadLeft(20));

        }

        public static void DoAreas(Character ch, string arguments)
        {
            using (new Character.Page(ch))
            {
                if (ch.IsImmortal)
                    foreach (var area in from a in AreaData.Areas orderby a.VNumStart select a)
                        ch.send("{0,-20} - {1,-30} {2,-5} - {3,-5}\n\r", area.Name.EscapeColor().TOSTRINGTRIM(), area.Credits.EscapeColor().TOSTRINGTRIM(), area.VNumStart, area.VNumEnd);
                else
                    foreach (var area in from a in AreaData.Areas where a.Rooms.Any() orderby a.Credits select a)
                        ch.send("{0,-20} - {1,-30} {2,-5} - {3,-5}\n\r", area.Name.EscapeColor().TOSTRINGTRIM(), area.Credits.EscapeColor().TOSTRINGTRIM(), area.VNumStart, area.VNumEnd);
            }
        }

        public static void DoHide(Character ch, string arguments)
        {
            SkillSpell fog = SkillSpell.SkillLookup("faerie fog");
            SkillSpell fire = SkillSpell.SkillLookup("faerie fire");

            if (ch.GetSkillPercentage("hide") <= 1)
            {
                ch.send("You don't know how to hide.\n\r");
                return;
            }

            if (ch.IsAffected(fog) || ch.IsAffected(fire) || ch.IsAffected(AffectFlags.FaerieFire))
            {
                ch.send("You can't hide while glowing.\n\r");
                return;
            }

            if (!(new SectorTypes[] { SectorTypes.City, SectorTypes.Inside, SectorTypes.Forest, SectorTypes.Cave, SectorTypes.Road }).Contains(ch.Room.sector))
            {
                ch.send("The shadows here are too natural to blend with.\n\r");
                return;
            }

            ch.send("You attempt to hide.\n\r");

            if (ch.IsAffected(AffectFlags.Hide))
                return;

            if (Utility.NumberPercent() < ch.GetSkillPercentage("hide"))
            {
                ch.AffectedBy.SETBIT(AffectFlags.Hide);
                ch.CheckImprove("hide", true, 3);
            }
            else
                ch.CheckImprove("hide", false, 3);

            return;
        }

        public static void DoSneak(Character ch, string arguments)
        {
            AffectData af;
            SkillSpell fog = SkillSpell.SkillLookup("faerie fog");
            SkillSpell fire = SkillSpell.SkillLookup("faerie fire");

            if (ch.GetSkillPercentage("sneak") <= 1)
            {
                ch.send("You don't know how to sneak.\n\r");
                return;
            }

            if (ch.IsAffected(fog) || ch.IsAffected(fire) || ch.IsAffected(AffectFlags.FaerieFire))
            {
                ch.send("You can't hide while glowing.\n\r");
                return;
            }

            ch.send("You attempt to move silently.\n\r");
            if (ch.IsAffected(AffectFlags.Sneak))
                return;

            if (Utility.NumberPercent() < ch.GetSkillPercentage("sneak"))
            {
                ch.CheckImprove("sneak", true, 3);
                af = new AffectData();
                af.displayName = "sneak";
                af.where = AffectWhere.ToAffects;
                af.skillSpell = SkillSpell.SkillLookup("sneak");
                af.affectType = AffectTypes.Skill;
                af.level = ch.Level;
                af.duration = ch.Level;
                af.location = ApplyTypes.None;
                af.modifier = 0;
                _ = af.flags.SETBIT(AffectFlags.Sneak);
                ch.AffectToChar(af);
                ch.send("You begin sneaking.\n\r");
            }
            else
                ch.CheckImprove("sneak", true, 3);

            return;
        }

        public static void DoDetectHidden(Character ch, string arguments)
        {
            AffectData affect;
            int number = 0;
            if ((number = ch.GetSkillPercentage("detect hidden") + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
            }
            else if (ch.IsAffected(AffectFlags.DetectHidden))
            {
                ch.send("You are already as alert as you can be.\n\r");
            }
            else if (Utility.NumberPercent() > number)
            {
                ch.send("You peer into the shadows but your vision stays the same.\n\r");
                ch.CheckImprove("detect hidden", false, 2);
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = SkillSpell.SkillLookup("detect hidden");
                affect.displayName = "detect hidden";
                affect.affectType = AffectTypes.Skill;
                affect.level = ch.Level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.DetectHidden);
                affect.duration = 6 + ch.Level / 3;
                affect.endMessage = "You can no longer see the hidden.\n\r";
                ch.AffectToChar(affect);

                ch.send("You focus your awareness on things in the shadows.\n\r");
                ch.CheckImprove("detect hidden", true, 2);
            }
        }

        public static void DoHeightenedAwareness(Character ch, string arguments)
        {
            AffectData affect;
            int number = 0;
            if ((number = ch.GetSkillPercentage("heightened awareness") + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");

            }
            else if (ch.IsAffected(AffectFlags.DetectInvis))
            {
                ch.send("Your awareness is already heightened.\n\r");
                return;
            }
            else if (Utility.NumberPercent() > number)
            {
                ch.send("You fail to heighten your awareness.\n\r");
                ch.CheckImprove("heightened awareness", false, 2);
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = SkillSpell.SkillLookup("heightened awareness");
                affect.displayName = "heightened awareness";
                affect.affectType = AffectTypes.Skill;
                affect.level = ch.Level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.DetectInvis);
                affect.duration = 6 + (ch.Level / 3);
                affect.endMessage = "Your awareness lessens slightly.\n\r";
                ch.AffectToChar(affect);

                ch.send("You heighten your senses to things hidden by magic.\n\r");
                ch.CheckImprove("heightened awareness", true, 2);
            }
        }

        public static void DoVisible(Character ch, string arguments)
        {
            ch.StripHidden();
            ch.StripInvis();
            ch.StripSneak();
            ch.StripCamouflage();

            if (ch.IsAffected(AffectFlags.Burrow))
            {
                ch.AffectedBy.REMOVEFLAG(AffectFlags.Burrow);
                ch.Act("$n leaves $s burrow.", type: ActType.ToRoom);
                ch.Act("You leave your burrow.");
            }
        }

        public static void DoSuicide(Character ch, string arguments)
        {
            ch.Act("$n commits suicide!", type: ActType.ToRoom);
            ch.Act("You commit suicide!", type: ActType.ToChar);
            ch.HitPoints = -15;
            Combat.CheckIsDead(null, ch, 15);
        }



        public static void DoDeposit(Character ch, string argument)
        {
            Character banker;
            string arg = "";
            int amount;



            banker = (from npc in ch.Room.Characters where npc.IsNPC && npc.Flags.ISSET(ActFlags.Banker) select npc).FirstOrDefault();

            if (banker == null)
            {
                ch.send("You can't do that here.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                ch.send("You can't speak to the banker.\n\r");
                return;
            }

            argument = argument.OneArgument(ref arg);
            if (arg.ISEMPTY())
            {
                ch.send("Deposit how much of which coin type?\n\r");
                return;
            }
            if (!int.TryParse(arg, out amount))
            {
                ch.send("Deposit how much of which type of coin?\n\r");
                return;
            }

            argument = argument.OneArgument(ref arg);
            if (amount <= 0 || (!arg.StringCmp("gold") && !arg.StringCmp("silver")))
            {
                ch.Act("$N tells you, 'Sorry, deposit how much of which coin type?'", banker, type: ActType.ToChar);
                return;
            }

            if (arg.StringCmp("gold"))
            {
                if (ch.Gold < amount)
                {
                    ch.Act("$N tells you, 'You don't have that much gold on you!'", banker, type: ActType.ToChar);
                    return;
                }
                ch.SilverBank += amount * 1000;
                ch.Gold -= amount;
            }
            else if (arg.StringCmp("silver"))
            {
                if (ch.Silver < amount)
                {
                    ch.Act("$N tells you, 'You don't have that much silver on you!'", banker, type: ActType.ToChar);
                    return;
                }
                ch.SilverBank += amount;
                ch.Silver -= amount;
            }
            ch.send("You deposit {0} {1}.\n\r", amount, arg.ToLower());
            ch.send("Your new balance is {0} silver.\n\r", ch.SilverBank);
            return;
        }

        public static void DoWithdraw(Character ch, string argument)
        {
            Character banker;
            string arg = "";
            int amount;


            banker = (from npc in ch.Room.Characters where npc.IsNPC && npc.Flags.ISSET(ActFlags.Banker) select npc).FirstOrDefault();

            if (banker == null)
            {
                ch.send("You can't do that here.\n\r");
                return;
            }
            int charges;
            if (ch.Form != null)
            {
                ch.send("You can't speak to the banker.\n\r");
                return;
            }

            argument = argument.OneArgument(ref arg);
            if (arg.ISEMPTY())
            {
                ch.send("Withdraw how much of which coin type?\n\r");
                return;
            }

            if (!int.TryParse(arg, out amount))
            {
                ch.send("Withdraw how much of which type of coin?\n\r", ch);
                return;
            }

            argument = argument.OneArgument(ref arg);
            if (amount <= 0 || (!arg.StringCmp("gold") && !arg.StringCmp("silver")))
            {
                PerformTell(banker, ch, "{0} Sorry, withdraw how much of which coin type?");
                return;
            }
            charges = 10 * amount;
            charges /= 100;

            if (arg.StringCmp("gold"))
            {
                if (ch.SilverBank < amount * 1000)
                {
                    PerformTell(banker, ch, "Sorry, we don't give loans.");
                    //ch.Act("$N tells you, 'Sorry you do not have we don't give loans.'", banker, type: ActType.ToChar);
                    return;
                }
                ch.SilverBank -= amount * 1000;
                ch.Gold += amount;
                ch.Gold -= charges;
            }
            else if (arg.StringCmp("silver"))
            {
                if (ch.SilverBank < amount)
                {
                    PerformTell(banker, ch, "{0} You don't have that much silver in the bank.");
                    //ch.Act("$N tells you, 'You don't have that much silver in the bank.'", banker, type: ActType.ToChar);
                    return;
                }
                ch.SilverBank -= amount;
                ch.Silver += amount;
                ch.Silver -= charges;
            }

            ch.send("You withdraw {0} {1}.\n\r", amount, arg.ToLower());
            ch.send("You are charged a small fee of {0} {1}.\n\r", charges, arg.ToLower());
            return;
        }

        public static void DoBalance(Character ch, string argument)
        {
            Character banker;

            banker = (from npc in ch.Room.Characters where npc.IsNPC && npc.Flags.ISSET(ActFlags.Banker) select npc).FirstOrDefault();

            if (banker == null)
            {
                ch.send("You can't do that here.\n\r");
                return;
            }
            if (ch.Form != null)
            {
                ch.send("You can't speak to the banker.\n\r");
                return;
            }
            if (ch.SilverBank == 0)
                ch.send("You have no account here!\n\r");
            else
                ch.send("You have {0} silver in your account.\n\r", ch.SilverBank);
            return;
        }

        public static void DoLore(Character ch, string arguments)
        {
            ItemData item;

            item = ch.GetItemHere(arguments);

            if (item == null)
            {
                ch.send("You don't see that here.\n\r");
            }
            else
            {
                var buffer = new StringBuilder();
                buffer.Append(new string('-', 80) + "\n\r");
                buffer.AppendFormat("Object {0} can be referred to as '{1}'\n\rIt is of type {2} and level {3}\n\r", item.ShortDescription.TOSTRINGTRIM(), item.Name,
                    string.Join(" ", (from flag in item.ItemType.Distinct() select flag.ToString())), item.Level);
                buffer.AppendFormat("It is worth {0} silver and weighs {1} pounds.\n\r", item.Value, item.Weight);

                if (item.ItemType.ISSET(ItemTypes.Weapon))
                {
                    if (item.WeaponDamageType != null)
                        buffer.AppendFormat("Damage Type is {0}\n\r", item.WeaponDamageType.Keyword);
                    buffer.AppendFormat("Weapon Type is {0} with damage dice of {1} (avg {2})\n\r", item.WeaponType.ToString(), item.DamageDice.ToString(), item.DamageDice.Average);

                }

                if (item.ItemType.ISSET(ItemTypes.Container))
                {
                    buffer.AppendFormat("It can hold {0} pounds.", item.MaxWeight);
                }

                if (item.ItemType.ISSET(ItemTypes.Food))
                {
                    buffer.AppendFormat("It is edible and provides {0} nutrition.\n\r", item.Nutrition);
                }

                if (item.ItemType.ISSET(ItemTypes.DrinkContainer))
                {
                    buffer.AppendFormat("Nutrition {0}, Drinks left {1}, Max Capacity {2}, it is filled with '{3}'\n\r", item.Nutrition, item.Charges, item.MaxCharges, item.Liquid);
                }

                buffer.AppendFormat("It is made out of '{0}'\n\r", item.Material);
                if (item.timer > 0)
                    buffer.AppendFormat("It will decay in {0} hours.\n\r", item.timer);

                if (item.ItemType.ISSET(ItemTypes.Armor) || item.ItemType.ISSET(ItemTypes.Clothing))
                {
                    buffer.AppendFormat("It provides armor against bash {0}, slash {1}, pierce {2}, magic {3}\n\r", item.ArmorBash, item.ArmorSlash, item.ArmorPierce, item.ArmorExotic);
                }
                buffer.AppendFormat("It can be worn on {0} and has extra flags of {1}.\n\r", string.Join(", ", (from flag in item.wearFlags.Distinct() select flag.ToString())),
                    string.Join(", ", (from flag in item.extraFlags.Distinct() select flag.ToString())));

                buffer.AppendFormat("Affects: \n   {0}\n\r", string.Join("\n   ", (from aff in item.affects where aff.@where == AffectWhere.ToObject select aff.location.ToString() + " " + aff.modifier)));

                if (item.ItemType.ISSET(ItemTypes.Staff) || item.ItemType.ISSET(ItemTypes.Wand) || item.ItemType.ISSET(ItemTypes.Scroll) || item.ItemType.ISSET(ItemTypes.Potion))
                {
                    buffer.AppendFormat("It contains the following spells:\n\r   {0}", string.Join("\n   ", from itemspell in item.Spells select (itemspell.SpellName + " [lvl " + itemspell.Level + "]")));
                }

                if (item.ItemType.ISSET(ItemTypes.Staff) || item.ItemType.ISSET(ItemTypes.Wand))
                {
                    buffer.AppendFormat("It has {0} of {1} charges left", item.Charges, item.MaxCharges);
                }

                buffer.Append(new string('-', 80) + "\n\r");
                ch.send(buffer.ToString());
            }
        }

        public static void DoForage(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("forage");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to forage for food.\n\r");
                return;
            }
            else if (ch.Form == null)
            {
                ch.send("Only animals can forage.\n\r");
                return;
            }
            else if (!(new SectorTypes[] { SectorTypes.Swamp, SectorTypes.Field, SectorTypes.Forest, SectorTypes.Trail, SectorTypes.Hills,
             SectorTypes.Mountain}.Contains(ch.Room.sector)))
            {
                ch.send("You aren't in the right environment.\n\r");
                return;
            }
            else if (chance < Utility.NumberPercent())
            {
                ch.Act("You forage around for food but find nothing.");
                ch.Act("$n forages around for food but finds nothing.", type: ActType.ToRoom);
            }
            else
            {
                ch.Act("You forage around for food and drink and find your fill.");
                ch.Act("$n forages around for food and drink and seems to find their fill.", type: ActType.ToRoom);
                ch.Thirst = 60;
                ch.Hunger = 60;
                ch.Dehydrated = 0;
                ch.Starving = 0;
            }
        }

        public static void DoScavenge(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("scavenge");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to scavenge for food.\n\r");
                return;
            }
            //else if (ch.Form == null)
            //{
            //    ch.send("Only animals can forage.\n\r");
            //    return;
            //}
            //else if (!(new SectorTypes[] { SectorTypes.Swamp, SectorTypes.Field, SectorTypes.Forest, SectorTypes.Trail, SectorTypes.Hills,
            // SectorTypes.Mountain}.Contains(ch.Room.sector)))
            //{
            //    ch.send("You aren't in the right environment.\n\r");
            //    return;
            //}
            else if (chance < Utility.NumberPercent())
            {
                ch.Act("You scavenge for food but find nothing.");
                ch.Act("$n scavenges for food but finds nothing.", type: ActType.ToRoom);
            }
            else
            {
                ch.Act("You scavenge for food and drink and find your fill.");
                ch.Act("$n scavenges for food and drink and seems to find their fill.", type: ActType.ToRoom);
                ch.Thirst = 60;
                ch.Hunger = 60;
                ch.Dehydrated = 0;
                ch.Starving = 0;
            }
        }

        public static void DoSprint(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("sprint");
            int chance;
            Direction direction = Direction.North;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to sprint.\n\r");
                return;
            }
            else if (ch.Form == null)
            {
                ch.send("Only animals can sprint.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() || !Utility.GetEnumValueStrPrefix(arguments, ref direction))
            {
                ch.send("Sprint in which direction?\n\r");
                return;
            }
            else
            {
                ch.WaitState(skill.waitTime);
                for (int i = 0; i < 3; i++)
                {
                    ch.Act("You sprint {0}.", args: direction.ToString().ToLower());
                    ch.Act("$n sprints {0}.", type: ActType.ToRoom, args: direction.ToString().ToLower());
                    ch.moveChar(direction, true, false);
                }
            }
        }


        public static void DoCamouflage(Character ch, string arguments)
        {
            SkillSpell fog = SkillSpell.SkillLookup("faerie fog");
            SkillSpell fire = SkillSpell.SkillLookup("faerie fire");
            int chance;

            if ((chance = ch.GetSkillPercentage("camouflage") + 20) <= 21)
            {
                ch.send("You attempt to hide behind a leaf.\n\r");
                return;
            }

            if (ch.IsAffected(fog) || ch.IsAffected(fire) || ch.IsAffected(AffectFlags.FaerieFire))
            {
                ch.send("You can't camouflage while glowing.\n\r");
                return;
            }

            if (!ch.Room.IsWilderness || ch.Room.IsWater)
            {
                ch.send("There's no cover here.\n\r");
                return;
            }

            ch.send("You attempt to blend in with your surroundings.\n\r");


            if (Utility.NumberPercent() < chance)
            {
                if (!ch.IsAffected(AffectFlags.Camouflage))
                    ch.AffectedBy.SETBIT(AffectFlags.Camouflage);
                ch.CheckImprove("camouflage", true, 3);
            }
            else
                ch.CheckImprove("camouflage", false, 3);

            return;
        }

        public static void DoBurrow(Character ch, string arguments)
        {
            //SkillSpell fog = SkillSpell.SkillLookup("faerie fog");
            //SkillSpell fire = SkillSpell.SkillLookup("faerie fire");
            int chance;
            if ((chance = ch.GetSkillPercentage("burrow") + 20) <= 21)
            {
                ch.send("You attempt to hide behind a leaf.\n\r");
                return;
            }

            //if (ch.IsAffected(fog) || ch.IsAffected(fire) || ch.IsAffected(AffectFlags.FaerieFire))
            //{
            //    ch.send("You can't burrow while glowing.\n\r");
            //    return;
            //}

            if (!ch.Room.IsWilderness || ch.Room.IsWater)
            {
                ch.send("There is no suitable soil here.\n\r");
                return;
            }

            if (ch.IsAffected(AffectFlags.Burrow))
            {
                ch.send("You are already burrowed.\n\r");
                return;
            }

            ch.send("You attempt to create a burrow.\n\r");
            if (Utility.NumberPercent() < chance)
            {
                Combat.StopFighting(ch, true);
                ch.AffectedBy.SETBIT(AffectFlags.Burrow);
                ch.send("You successfully create a burrow to hide in.\n\r");
            }

            return;
        }

        public static void DoPlayDead(Character ch, string arguments)
        {
            int chance;
            if ((chance = ch.GetSkillPercentage("play dead") + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if (ch.IsAffected(AffectFlags.PlayDead))
            {
                ch.send("You are already playing dead.\n\r");
                return;
            }

            ch.send("You attempt to play dead.\n\r");
            if (Utility.NumberPercent() < chance)
            {
                Combat.StopFighting(ch, true);
                ch.AffectedBy.SETBIT(AffectFlags.PlayDead);
                ch.send("You successfully trick your enemy.\n\r");
            }

            return;
        }

        public static void DoAcuteVision(Character ch, string arguments)
        {
            AffectData affect;
            int number = 0;
            if ((number = ch.GetSkillPercentage("acute vision")) <= 1)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if (ch.IsAffected(AffectFlags.AcuteVision))
            {
                ch.send("You are already as alert as you can be.\n\r");
                return;
            }
            else
            {
                if (Utility.NumberPercent() > number)
                {
                    ch.send("You fail.\n\r");
                    ch.CheckImprove("acute vision", false, 2);
                    return;
                }
                affect = new AffectData();
                affect.skillSpell = SkillSpell.SkillLookup("acute vision");
                affect.displayName = "acute vision";
                affect.affectType = AffectTypes.Skill;
                affect.level = ch.Level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.AcuteVision);
                affect.duration = 6;
                affect.endMessage = "You can no longer see the camouflaged.\n\r";
                ch.AffectToChar(affect);



                ch.send("Your awareness improves.\n\r");
                ch.CheckImprove("acute vision", true, 2);
            }
        }
        public static void DoFindWater(Character ch, string arguments)
        {

            var skill = SkillSpell.SkillLookup("findwater");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (ch.Room.sector == SectorTypes.City)
            {
                ch.send("You cannot poke through the floor to find water.\n\r");
                ch.Act("$n pokes the floor, trying to find water, but nothing happens.\n\r", type: ActType.ToRoom);
                return;
            }
            ItemTemplateData template;
            if (ItemTemplateData.Templates.TryGetValue(23, out template))

            {
                if (chance > Utility.NumberPercent())
                {
                    var spring = new ItemData(template);
                    ch.Room.items.Insert(0, spring);
                    spring.Room = ch.Room;
                    spring.timer = 7;
                    ch.send("You poke around and create a spring of water flowing out of the ground.\n\r");
                    ch.Act("$n pokes around and creates a spring of water flowing out of the ground.\n\r", type: ActType.ToRoom);
                    ch.CheckImprove(skill, true, 1);
                    return;
                }
                else
                {
                    ch.send("You poke the ground a bit, but nothing happens.\n\r");
                    ch.Act("$n pokes the ground a bit, but nothing happens.\n\r", type: ActType.ToRoom);
                    ch.CheckImprove(skill, false, 1);
                    return;
                }
            }
        }

        public static void DoSpecialize(Character ch, string arguments)
        {
            if (ch is Player)
            {
                var player = (Player)ch;

                if (player.WeaponSpecializations < 1)
                {
                    ch.send("You have no weapon specializations available.\n\r");
                }
                else
                {
                    if ("spear".StringPrefix(arguments) || "staff".StringPrefix(arguments))
                        arguments = "spear/staff specialization";
                    else if ("whip".StringPrefix(arguments) || "flail".StringPrefix(arguments))
                        arguments = "whip/flail specialization";

                    var specializationskill = (from sk in SkillSpell.Skills where sk.Value.SkillTypes.ISSET(SkillSpellTypes.WarriorSpecialization) && sk.Value.name.StringPrefix(arguments) && ch.GetSkillPercentage(sk.Value) <= 1 select sk.Value).FirstOrDefault();
                    var prereqsnotmet = (from l in ch.Learned where l.Key.PrerequisitesMet(ch) == false select l).ToArray();


                    if (specializationskill != null)
                    {
                        ch.LearnSkill(specializationskill, 100);
                        player.WeaponSpecializations--;
                        foreach (var prereqnotmet in prereqsnotmet)
                        {
                            if (prereqnotmet.Key.PrerequisitesMet(ch))
                            {
                                ch.send("\\CYou feel a rush of insight into {0}!\\x\n\r", prereqnotmet.Key.name);
                            }
                        }
                        ch.send("You have chosen {0}.\n\r", specializationskill.name);
                        player.SaveCharacterFile();
                    }
                    else
                    {
                        ch.send("Valid weapon specializations are {0}.\n\r",
                            string.Join(", ",
                                from sk in SkillSpell.Skills
                                where sk.Value.SkillTypes.ISSET(SkillSpellTypes.WarriorSpecialization) && ch.GetSkillPercentage(sk.Value, true) <= 1
                                select sk.Value.name));
                    }
                }
                if (SkillSpell.Skills.Any(sk => sk.Value.SkillTypes.ISSET(SkillSpellTypes.WarriorSpecialization) && ch.GetSkillPercentage(sk.Value, true) > 1))
                    ch.send("Weapon specializations are {0}.\n\r",
                                string.Join(", ",
                                    from sk in SkillSpell.Skills
                                    where sk.Value.SkillTypes.ISSET(SkillSpellTypes.WarriorSpecialization) && ch.GetSkillPercentage(sk.Value, true) > 1
                                    select sk.Value.name));

                if (player.WeaponSpecializations > 0)
                    ch.send("You have {0} weapon specializations left.\n\r", player.WeaponSpecializations);
            }
            else
                ch.send("You have no weapon specializations available.\n\r");
        }


        public static void DoButcher(Character ch, string arguments)
        {

            var skill = SkillSpell.SkillLookup("butcher");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            int count = 0;
            ItemData corpse = null;
            if ((corpse = ch.GetItemRoom(arguments, ref count)) == null)
            {
                ch.send("You don't see that here.\n\r");
                return;
            }
            else if (!corpse.ItemType.ISSET(ItemTypes.NPCCorpse) && !corpse.ItemType.ISSET(ItemTypes.PC_Corpse))
            {
                ch.send("That is not a corpse.\n\r");
                return;
            }
            else if (corpse.ItemType.ISSET(ItemTypes.PC_Corpse) && corpse.Contains.Count > 0)
            {
                ch.send("You cannot butcher another players corpse unless it is empty.\n\r");
                return;
            }
            else if (corpse.Size == CharacterSize.Tiny)
            {
                ch.send("That corpse is too tiny to butcher.\n\r");
                return;
            }


            ItemTemplateData template;
            if (ItemTemplateData.Templates.TryGetValue(2971, out template))

            {
                if (chance > Utility.NumberPercent())
                {
                    var steakcount = corpse.Size == CharacterSize.Small ? 1 : corpse.Size == CharacterSize.Medium ? Utility.Random(1, 3)
                        : corpse.Size == CharacterSize.Large ? Utility.Random(2, 4) : Utility.Random(3, 5);

                    for (int i = 0; i < steakcount; i++)
                    {
                        var steak = new ItemData(template);
                        steak.ShortDescription = string.Format(steak.ShortDescription, corpse.ShortDescription);
                        steak.LongDescription = string.Format(steak.LongDescription, corpse.ShortDescription);
                        steak.Description = string.Format(steak.Description, corpse.ShortDescription);
                        ch.Room.items.Insert(0, steak);
                        steak.Room = ch.Room;
                    }
                    foreach (var contained in corpse.Contains)
                    {
                        ch.Room.items.Insert(0, contained);
                        contained.Room = ch.Room;
                        contained.Container = null;

                    }
                    corpse.Contains.Clear();
                    corpse.Dispose();

                    ch.Act("You carefully carve up $p and produce {0} steaks.\n\r", null, corpse, type: ActType.ToChar, args: steakcount);
                    ch.Act("$n carefully carves up $p and produces {0} steaks.\n\r", null, corpse, type: ActType.ToRoom, args: steakcount);
                    ch.CheckImprove(skill, true, 1);
                    return;
                }
                else
                {
                    foreach (var contained in corpse.Contains)
                    {
                        ch.Room.items.Insert(0, contained);
                        contained.Room = ch.Room;
                        contained.Container = null;

                    }
                    corpse.Contains.Clear();
                    corpse.Dispose();

                    ch.Act("You try to carve up $p but you destroy it in the process.\n\r", null, corpse, type: ActType.ToChar);
                    ch.Act("$n tries to carve up $p but destroys it in the process.\n\r", null, corpse, type: ActType.ToRoom);
                    ch.CheckImprove(skill, false, 1);
                    return;
                }
            }
        }
        public static void DoCreep(Character ch, string arguments)
        {
            if (!string.IsNullOrEmpty(arguments))
            {
                Direction direction = Direction.North;

                if (Utility.GetEnumValueStrPrefix(arguments, ref direction))
                {
                    if (ch.AffectedBy.ISSET(AffectFlags.Camouflage))
                        ch.moveChar(direction, true, false, true);
                    else ch.send("You must be camouflaged before attempting to creep.\n\r");
                }
                else
                    ch.send("Creep West, east, south, west, up or down?\n\r");
            }
            else
                ch.send("Creep in which direction?\n\r");
        }

        public static void DoOrder(Character ch, string arguments)
        {
            string name = "";
            string command = "";
            string commandargs = "";
            Character pet;

            arguments = arguments.OneArgument(ref name);
            arguments = arguments.OneArgument(ref command);
            arguments = arguments.OneArgument(ref commandargs);

            if (name.ISEMPTY() || command.ISEMPTY())
            {
                ch.send("Order who to do what?\n\r");
                return;
            }

            if (commandargs.StringCmp("self"))
                commandargs = ch.GetName;

            if (name.StringCmp("all"))
            {
                foreach (var other in ch.Room.Characters.ToArray())
                {
                    if (other.IsNPC && other.Leader == ch)
                    {
                        other.DoCommand(command + " " + commandargs);
                    }
                }
                ch.send("OK.\n\r");
            }
            else if ((pet = ch.GetCharacterFromRoomByName(name)) != null && pet.IsNPC && pet.Leader == ch)
            {
                pet.DoCommand(command + " " + commandargs + " " + arguments);
                ch.send("OK.\n\r");
            }
            else
            {
                ch.send("You couldn't order them to do anything.\n\r");
                return;
            }

        }

        public static void DoPickLock(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("pick lock");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (!(from item in ch.Inventory.Concat(ch.Equipment.Values) where item.ItemType.ISSET(ItemTypes.ThiefPick) select item).Any())
            {
                ch.send("You don't have the necessary tool to pick a lock.\n\r");
                return;
            }

            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };

            Direction direction = Direction.West;
            ExitData exit;
            ItemData container;

            if ((exit = ch.Room.GetExit(arguments)) != null && exit.destination != null)
            {
                direction = exit.direction;
                if (!exit.flags.Contains(ExitFlags.Closed))
                {
                    ch.send("It isn't closed.\n\r");
                }
                else if (exit.flags.ISSET(ExitFlags.PickProof))
                {
                    ch.Act(exit.display + " cannot be picked.\n\r");
                }
                else if (exit.flags.Contains(ExitFlags.Locked))
                {

                    if (chance > Utility.NumberPercent())
                    {
                        ch.Act("You unlock " + exit.display + ".\n\r");
                        ch.Act("$n unlocks " + exit.display + ".\n\r", type: ActType.ToGroupInRoom);
                        exit.flags.REMOVEFLAG(ExitFlags.Locked);
                        ExitData otherSide;
                        if ((otherSide = exit.destination.exits[(int)reverseDirections[direction]]) != null && otherSide.destination == ch.Room && otherSide.flags.Contains(ExitFlags.Closed) && otherSide.flags.Contains(ExitFlags.Locked))
                            otherSide.flags.REMOVEFLAG(ExitFlags.Locked);
                        ch.CheckImprove("pick lock", true, 1);
                    }
                    else
                    {
                        ch.send("You try to unlock " + exit.display + " but fail.\n\r");
                        ch.Act("$n tries to unlock " + exit.display + " but fails.\n\r", type: ActType.ToGroupInRoom);
                        ch.CheckImprove("pick lock", false, 1);
                        return;
                    }

                }
                else
                    ch.send("It isn't locked.");
            }
            else if ((container = ch.GetItemHere(arguments)) != null)
            {
                if (!container.extraFlags.ISSET(ExtraFlags.Locked))
                {
                    ch.send("It's not locked.\n\r");
                    return;
                }

                foreach (var npc in ch.Room.Characters.OfType<NPCData>())
                {
                    if (npc.Programs.Any(prog => prog.Types.ISSET(Programs.ProgramTypes.BeforeUnlock)))
                    {
                        var progs = npc.Programs.Where(prog => prog.Types.ISSET(Programs.ProgramTypes.BeforeUnlock));
                        foreach (var prog in progs)
                        {
                            if (prog.Execute(ch, npc, null, container, null, Programs.ProgramTypes.BeforeUnlock, arguments))
                            {
                                // prog will send a message
                                return;
                            }
                        }
                    }
                }
                if (container.extraFlags.ISSET(ExtraFlags.PickProof))
                {
                    ch.Act("$p cannot be picked.\n\r", item: container);
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.Act("You unlock $p.", null, container);
                    ch.Act("$n unlocks $p.", null, container, type: ActType.ToGroupInRoom);
                    container.extraFlags.REMOVEFLAG(ExtraFlags.Locked);
                    ch.CheckImprove("pick lock", true, 1);

                }
                else
                {
                    ch.Act("You try to unlock $p but fail.\n\r", item: container);
                    ch.Act("$n tries to unlock $p but fails.\n\r", item: container, type: ActType.ToGroupInRoom);
                    ch.CheckImprove("pick lock", false, 1);
                    return;
                }
            }
            else
                ch.send("You don't see that here.\n\r"); // ch.send("You can't unlock that.\n\r");
        }
        public static void DoRelock(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("relock");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (!(from item in ch.Inventory.Concat(ch.Equipment.Values) where item.ItemType.ISSET(ItemTypes.ThiefPick) select item).Any())
            {
                ch.send("You don't have the necessary tool to do that.\n\r");
                return;
            }

            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };

            Direction direction = Direction.North;
            ExitData exit;
            ItemData container;

            if ((exit = ch.Room.GetExit(arguments)) != null && exit.destination != null)
            {
                direction = exit.direction;
                if (!exit.flags.Contains(ExitFlags.Closed))
                {
                    ch.send("It isn't closed.\n\r");
                }
                else if (!exit.originalFlags.ISSET(ExitFlags.Locked))
                {
                    ch.send("It can't be locked.\n\r");
                }
                else if (exit.flags.ISSET(ExitFlags.PickProof))
                {
                    ch.Act(exit.display + " cannot be relocked.\n\r");
                }
                else if (!exit.flags.Contains(ExitFlags.Locked))
                {

                    if (chance > Utility.NumberPercent())
                    {
                        ch.Act("You relock " + exit.display + ".\n\r");
                        ch.Act("$n relocks " + exit.display + ".\n\r", type: ActType.ToGroupInRoom);
                        exit.flags.ADDFLAG(ExitFlags.Locked);
                        ExitData otherSide;
                        if ((otherSide = exit.destination.exits[(int)reverseDirections[direction]]) != null && otherSide.destination == ch.Room && otherSide.flags.Contains(ExitFlags.Closed) && !otherSide.flags.Contains(ExitFlags.Locked))
                            otherSide.flags.ADDFLAG(ExitFlags.Locked);
                        ch.CheckImprove("relock", true, 1);
                    }
                    else
                    {
                        ch.send("You try to relock " + exit.display + " but fail.\n\r");
                        ch.Act("$n tries to relock " + exit.display + " but fails.\n\r", type: ActType.ToGroupInRoom);
                        ch.CheckImprove("relock", false, 1);
                        return;
                    }

                }
                else
                    ch.send("It's already locked.");
            }
            else if ((container = ch.GetItemHere(arguments)) != null)
            {
                if (container.extraFlags.ISSET(ExtraFlags.Locked))
                {
                    ch.send("It's already locked.\n\r");
                    return;
                }
                if (!container.extraFlags.ISSET(ExtraFlags.Closed))
                {
                    ch.Act("It isn't closed.");
                    return;
                }
                if (!container.Keys.Any())
                {
                    ch.Act("It can't be locked.");
                    return;
                }

                foreach (var npc in ch.Room.Characters.OfType<NPCData>())
                {
                    if (npc.Programs.Any(prog => prog.Types.ISSET(Programs.ProgramTypes.BeforeRelock)))
                    {
                        var progs = npc.Programs.Where(prog => prog.Types.ISSET(Programs.ProgramTypes.BeforeRelock));
                        foreach (var prog in progs)
                        {
                            if (prog.Execute(ch, npc, null, container, null, Programs.ProgramTypes.BeforeRelock, arguments))
                            {
                                // prog will send a message
                                return;
                            }
                        }
                    }
                }
                if (container.extraFlags.ISSET(ExtraFlags.PickProof))
                {
                    ch.Act("$p cannot be locked without a key.\n\r", item: container);
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.Act("You relock $p.", null, container);
                    ch.Act("$n relocks $p.", null, container, type: ActType.ToGroupInRoom);
                    container.extraFlags.ADDFLAG(ExtraFlags.Locked);
                    ch.CheckImprove("relock", false, 1);

                }
                else
                {
                    ch.Act("You try to relock $p but fail.\n\r", item: container);
                    ch.Act("$n tries to relock $p but fails.\n\r", item: container, type: ActType.ToGroupInRoom);
                    ch.CheckImprove("relock", false, 1);
                    return;
                }
            }
            else
                ch.send("You don't see that here.\n\r"); // ch.send("You can't unlock that.\n\r");
        }
        public static void DoInfiltrate(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("infiltrate");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };

            Direction direction = Direction.North;
            ExitData exit;
            ItemData container;

            if ((exit = ch.Room.GetExit(arguments)) != null && exit.destination != null)
            {
                direction = exit.direction;
                if (!exit.flags.Contains(ExitFlags.Closed))
                {
                    ch.send("It isn't closed.\n\r");
                }
                else if (exit.flags.Contains(ExitFlags.Locked))
                {

                    if (chance > Utility.NumberPercent())
                    {
                        ch.Act("You unlock " + exit.display + ".\n\r");
                        ch.Act("$n unlocks " + exit.display + ".\n\r", type: ActType.ToGroupInRoom);
                        exit.flags.REMOVEFLAG(ExitFlags.Locked);
                        ExitData otherSide;
                        if ((otherSide = exit.destination.exits[(int)reverseDirections[direction]]) != null && otherSide.destination == ch.Room && otherSide.flags.Contains(ExitFlags.Closed) && otherSide.flags.Contains(ExitFlags.Locked))
                            otherSide.flags.REMOVEFLAG(ExitFlags.Locked);
                        ch.CheckImprove("infiltrate", true, 1);
                    }
                    else
                    {
                        ch.send("You try to unlock " + exit.display + " but fail.\n\r");
                        ch.Act("$n tries to unlock " + exit.display + " but fails.\n\r", type: ActType.ToGroupInRoom);
                        ch.CheckImprove("infiltrate", false, 1);
                        return;
                    }

                }
                else
                    ch.send("It isn't locked.");
            }
            else if ((container = ch.GetItemHere(arguments)) != null)
            {
                if (!container.extraFlags.ISSET(ExtraFlags.Locked))
                {
                    ch.send("It's not locked.\n\r");
                    return;
                }

                //foreach (var npc in ch.Room.Characters.OfType<NPCData>())
                //{
                //    if (npc.Programs.Any(prog => prog.Types.ISSET(Programs.ProgramTypes.BeforeUnlock)))
                //    {
                //        var progs = npc.Programs.Where(prog => prog.Types.ISSET(Programs.ProgramTypes.BeforeUnlock));
                //        foreach (var prog in progs)
                //        {
                //            if (prog.Execute(ch, npc, null, container, null, Programs.ProgramTypes.BeforeUnlock, arguments))
                //            {
                //                // prog will send a message
                //                return;
                //            }
                //        }
                //    }
                //}

                if (chance > Utility.NumberPercent())
                {
                    ch.Act("You unlock $p.", null, container);
                    ch.Act("$n unlocks $p.", null, container, type: ActType.ToGroupInRoom);
                    container.extraFlags.REMOVEFLAG(ExtraFlags.Locked);
                    ch.CheckImprove("infiltrate", true, 1);

                }
                else
                {
                    ch.Act("You try to unlock $p but fail.\n\r", item: container);
                    ch.Act("$n tries to unlock $p but fails.\n\r", item: container, type: ActType.ToGroupInRoom);
                    ch.CheckImprove("infiltrate", false, 1);
                    return;
                }
            }
            else
                ch.send("You don't see that here.\n\r"); // ch.send("You can't unlock that.\n\r");
        }
        public static void DoGentleWalk(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("gentle walk");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (chance > Utility.NumberPercent())
            {
                var aff = (from a in ch.AffectsList where a.skillSpell == skill && a.duration != -1 select a).FirstOrDefault();
                if (aff != null)
                {
                    aff.duration = 10;
                }
                else
                {
                    aff = new AffectData();
                    aff.skillSpell = skill;
                    aff.hidden = true;
                    aff.duration = 10;
                    ch.AffectToChar(aff);
                }
                ch.CheckImprove("gentle walk", true, 1);
            }
            else
            {
                ch.CheckImprove("gentle walk", false, 1);
            }
            ch.Act("You attempt to walk gently.\n\r");
        }

        public static void DoSteal(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("steal");
            int chance;
            Character victim = null;
            string itemname = "";
            ItemData item = null;

            if (ch.Fighting != null)
            {
                victim = ch.Fighting;
                skill = SkillSpell.SkillLookup("combat steal");
            }
            if (ch.Fighting != null && (chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.Act("You cannot steal while fighting yet.\n\r");
            }
            else if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
            }

            else if (arguments.ISEMPTY() || ((arguments = arguments.OneArgument(ref itemname)).ISEMPTY()) && ch.Fighting == null)
            {
                ch.Act("Steal what from who?\n\r");
            }
            else if (ch.Fighting == null && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.Act("You don't see them here.\n\r");
            }
            else if (Combat.CheckIsSafe(ch, victim))
            {

            }
            else if (!(new string[] { "gold", "silver", "coins" }.Any(str => str.StringCmp(itemname))) && (item = victim.GetItemInventory(itemname)) == null)
            {
                ch.Act("They aren't carrying that.\n\r");
            }
            else if (chance > Utility.NumberPercent() || !victim.IsAwake)
            {
                if (item == null) // only way it got here was from typing gold, silver or coins
                {
                    var gold = Math.Min(victim.Gold, victim.Gold * Utility.Random(1, ch.Level) / 60);
                    var silver = Math.Min(victim.Silver, victim.Silver * Utility.Random(1, ch.Level) / 60);
                    if (gold <= 0 && silver <= 0)
                    {
                        ch.Act("You couldn't get any coins.\n\r");
                    }
                    else if (gold > 0 && silver > 0)
                    {
                        ch.Act("Bingo!  You got {0} silver and {1} gold coins.\n\r", null, null, null, ActType.ToChar, silver, gold);
                    }
                    else if (gold > 0)
                    {
                        ch.Act("Bingo!  You got {0} gold coins.\n\r", null, null, null, ActType.ToChar, gold);
                    }
                    else
                    {
                        ch.Act("Bingo!  You got {0} silver coins.\n\r", null, null, null, ActType.ToChar, silver);
                    }
                    victim.Gold -= gold;
                    victim.Silver -= silver;
                    ch.Gold += gold;
                    ch.Silver += silver;
                    ch.CheckImprove(skill, true, 1);
                }
                else
                {
                    victim.Inventory.Remove(item);
                    ch.Inventory.Insert(0, item);
                    item.CarriedBy = ch;
                    ch.CheckImprove(skill, true, 1);
                    ch.Act("You successfully steal $p from $N.\n\r", victim, item);
                }
            }
            else
            {
                ch.CheckImprove(skill, false, 1);
                if (item != null)
                {
                    ch.Act("You fail to steal $p from $N.\n\r", victim, item);
                }
                else ch.Act("You fail to steal from $N.\n\r", victim);
                DoActCommunication.DoYell(victim, "Keep your hands out there!");
                Combat.SetFighting(victim, ch);
            }
        } // end of steal

        public static void DoGreaseItem(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("grease item");
            int chance;
            ItemData item = null;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.Act("You don't know how to do that.\n\r");
            }
            else if ((arguments.ISEMPTY()) || (item = ch.GetItemInventoryOrEquipment(arguments, false)) == null)
            {
                ch.Act("Which item did you want to grease?\n\r");
            }
            else if (item.IsAffected(skill))
            {
                ch.Act("$p is already greased.\n\r", item: item);
            }
            else if (chance < Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n tries to grease $p, but fails.", null, item, type: ActType.ToRoom);
                ch.Act("You try to grease $p but fail.", item: item, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n successfully applies grease to $p.", item: item, type: ActType.ToRoom);
                ch.Act("You successfully apply grease to $p.", item: item, type: ActType.ToChar);

                var affect = new AffectData();
                affect.duration = 2;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToWeapon;
                affect.skillSpell = skill;
                bool v = affect.flags.SETBIT(AffectFlags.Greased);
                item.affects.Add(affect);
                affect.endMessage = "The grease on $p wears off.\n\r";
                ch.CheckImprove(skill, true, 1);
            }

        } // end grease item
        public static void DoDrag(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("drag");
            int chance;
            Character victim = null;
            string dirArg = null;
            Direction direction = Direction.North;
            string victimname = null;
            arguments = arguments.OneArgument(ref victimname);
            arguments = arguments.OneArgument(ref dirArg);
            ExitData exit = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
            }
            else if (!victimname.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(victimname)) == null)
            {
                ch.send("You don't see them here.\n\r");
            }
            else if (dirArg.ISEMPTY() || !Utility.GetEnumValueStrPrefix(dirArg, ref direction))
            {
                ch.Act("Which direction did you want to drag $N in?\n\r", victim);
            }
            else if (!victim.IsAffected(AffectFlags.BindHands) ||
                !victim.IsAffected(AffectFlags.BindLegs) || (!victim.IsAffected(AffectFlags.Sleep)))
            {
                ch.Act("You cannot drag them if they're awake or their hands and legs are unbound.\n\r");
            }
            else if ((exit = ch.Room.GetExit(direction)) == null || exit.destination == null
                || exit.flags.ISSET(ExitFlags.Closed) || exit.flags.ISSET(ExitFlags.Window) ||
                (!victim.IsImmortal && !victim.IsNPC && (exit.destination.MinLevel > victim.Level || exit.destination.MaxLevel < victim.Level)))
            {
                ch.Act("You can't drag $N that way.", victim);
            }
            else if (chance > Utility.NumberPercent())
            {
                var wasinroom = ch.Room;

                ch.Act("You drag $N {0}.", victim, args: direction.ToString().ToLower());
                ch.Act("$n drags $N {0}.", victim, type: ActType.ToRoomNotVictim, args: direction.ToString().ToLower());

                ch.moveChar(direction, true, false, false, false);

                if (ch.Room != wasinroom)
                {
                    victim.RemoveCharacterFromRoom();
                    victim.AddCharacterToRoom(exit.destination);
                    //DoLook(victim, "auto");
                    ch.CheckImprove(skill, true, 1);

                    ch.Act("$n drags in $N.", victim, type: ActType.ToRoomNotVictim);
                }
            }
            else
            {
                ch.Act("You try to drag $N {0} but fail.", victim, args: direction.ToString().ToLower());
                ch.Act("$n tries to drag $N {0} but fails.", victim, type: ActType.ToRoomNotVictim, args: direction.ToString().ToLower());
                ch.CheckImprove(skill, false, 1);
            }
        } // end drag
        public static void DoArcaneVision(Character ch, string arguments)
        {
            AffectData affect;
            int number = 0;
            var skill = SkillSpell.SkillLookup("arcane vision");

            if ((number = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
            }

            else if (ch.IsAffected(AffectFlags.ArcaneVision))
            {
                ch.send("You can already see magical items.\n\r");

            }
            else if (Utility.NumberPercent() > number)
            {
                ch.send("You fail to heighten your awareness.\n\r");
                ch.CheckImprove(skill, false, 1);
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = skill;
                affect.displayName = "arcane awareness";
                affect.affectType = AffectTypes.Skill;
                affect.level = ch.Level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.ArcaneVision);
                affect.duration = 12;
                affect.endMessage = "Your awareness of magical items falls.\n\r";
                ch.AffectToChar(affect);

                ch.send("Your awareness of magical items improves.\n\r");
                ch.CheckImprove(skill, true, 1);
            }
        }

        public static void DoFirstAid(Character ch, string argument)
        {
            AffectData af;
            var skill = SkillSpell.SkillLookup("first aid");
            Character victim = null;

            if (ch.GetSkillPercentage(skill) <= 1)
            {
                ch.send("Huh?\n\r");
            }
            else if (argument.ISEMPTY() && (victim = ch) == null)
            {

            }
            else if (!argument.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(argument)) == null)
            {
                ch.Act("You don't see them here.\n\r");
            }
            else if (ch.IsAffected(AffectFlags.ApplyingFirstAid))
            {
                ch.send("You are already delivering first aid.\n\r");
            }
            else if (victim.IsAffected(AffectFlags.FirstAidBeingApplied))
            {
                if (victim == ch)
                    ch.Act("You are receiving first aid already.\n\r");
                else
                    ch.Act("$N is already receiving first aid.", victim);

            }
            else if (victim.IsAffected(skill))
            {
                if (victim == ch)
                    ch.Act("You are still benefiting from first aid.\n\r");
                else
                    ch.Act("$N is still benefiting from first aid.\n\r", victim);
            }

            else if (Utility.NumberPercent() > ch.GetSkillPercentage(skill))
            {

                ch.Act("$n drops the bandages $e was perparing for $N.", victim, type: ActType.ToRoomNotVictim);
                if (ch != victim)
                {
                    ch.Act("You drop the bandages you were preparing for $N.", victim);
                    ch.Act("$n drops the bandages $e was preparing for you.", victim, type: ActType.ToVictim);
                }
                else
                    ch.Act("You drop the bandages you were preparing for yourself.");
                ch.WaitState(skill.waitTime);
                ch.CheckImprove(skill, false, 1);
            }
            else
            {
                if (ch != victim)
                {
                    ch.Act("You begin applying first aid to $N.", victim);
                    ch.Act("$n begins applying first aid to $N.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n begins applying first aid to you.", victim, type: ActType.ToVictim);
                }
                else
                {
                    ch.Act("You begin applying first aid to yourself.");
                    ch.Act("$n begins applying first aid to $mself.", type: ActType.ToRoom);
                }

                af = new AffectData();
                af.affectType = AffectTypes.Skill;
                af.hidden = true;
                af.flags.SETBIT(AffectFlags.ApplyingFirstAid);
                af.duration = 3;
                af.frequency = Frequency.Violence;
                af.ownerName = victim.Name;
                af.RemoveAndSaveFlags.SETBITS(AffectData.StripAndSaveFlags.DoNotSave,
                    AffectData.StripAndSaveFlags.RemoveOnCombat,
                    AffectData.StripAndSaveFlags.RemoveOnMove,
                    AffectData.StripAndSaveFlags.RemoveOnPositionChange);
                af.tickProgram = "AffectFirstAidTick";
                af.endProgram = "AffectFirstAidEnd";
                ch.AffectToChar(af);

                af = new AffectData();
                af.affectType = AffectTypes.Skill;
                af.hidden = true;
                af.flags.SETBIT(AffectFlags.FirstAidBeingApplied);
                af.duration = 3;
                af.frequency = Frequency.Violence;
                af.ownerName = ch.Name;
                af.RemoveAndSaveFlags.SETBITS(AffectData.StripAndSaveFlags.DoNotSave,
                    AffectData.StripAndSaveFlags.RemoveOnCombat,
                    AffectData.StripAndSaveFlags.RemoveOnMove,
                    AffectData.StripAndSaveFlags.RemoveOnPositionChange);
                af.tickProgram = "AffectFirstAidTick";
                af.endProgram = "AffectFirstAidEnd";
                victim.AffectToChar(af);

                //EndFirstAid(ch, victim);
            }
        } // end first aid

        public static void DoFlyto(Character ch, string arguments)
        {

            Character other;
            int count = 0;
            var skill = SkillSpell.SkillLookup("flyto");
            if (ch.GetSkillPercentage(skill) <= 1)
            {
                ch.Act("You don't know how to do that.");
            }
            else if ((other = ch.GetCharacterFromListByName(Character.Characters, arguments, ref count)) == null)
            {
                ch.Act("Who did you want to fly to?");
            }
            else if (other.Room == null || other.Room.Area != ch.Room.Area)
            {
                ch.Act("They are too far away to fly to.");
            }
            else
            {
                ch.Act("$n flaps $s wings, then quickly flys away.", type: ActType.ToRoom);
                ch.RemoveCharacterFromRoom();

                ch.AddCharacterToRoom(other.Room);
                ch.Act("You fly to $N.", other);
                ch.Act("$n suddenly lands beside you with a brisk flap of $s wings.", type: ActType.ToRoom);
                //Character.DoLook(ch, "auto");
            }
        }

        public static void DoGlobalEcho(Character ch, string arguments)
        {
            if (arguments.ISEMPTY())
            {
                ch.send("GlobalEcho what?\n\r");
            }
            else
            {
                foreach (var player in Game.Instance.Info.Connections)
                {
                    if (player.state == Player.ConnectionStates.Playing)
                    {
                        if (ch != null && player.Level >= ch.Level)
                        {
                            player.send("({0}) {1}\n\r", ch.Display(player), arguments);
                        }
                        else
                            player.send("{0}\n\r", arguments);
                    }
                }
            }
        }

        public static void DoAreaEcho(Character ch, string arguments)
        {
            if (arguments.ISEMPTY())
            {
                ch.send("AreaEcho what?\n\r");
            }
            else
            {
                foreach (var player in Game.Instance.Info.Connections)
                {
                    if (player.state == Player.ConnectionStates.Playing &&
                        player.Room != null && ch.Room != null && player.Room.Area == ch.Room.Area)
                    {
                        if (ch != null && player.Level >= ch.Level)
                        {
                            player.send("({0}) {1}\n\r", ch.Display(player), arguments);
                        }
                        else
                            player.send("{0}\n\r", arguments);
                    }
                }
            }
        }

        public static void DoEcho(Character ch, string arguments)
        {
            if (arguments.ISEMPTY())
            {
                ch.send("Echo what?\n\r");
            }
            else
            {
                foreach (var player in Game.Instance.Info.Connections)
                {
                    if (player.state == Player.ConnectionStates.Playing &&
                        player.Room != null && ch != null && ch.Room != null && player.Room == ch.Room)
                    {
                        if (ch != null && player.Level >= ch.Level)
                        {
                            player.send("({0}) {1}\n\r", ch.Display(player), arguments);
                        }
                        else
                            player.send("{0}\n\r", arguments);
                    }
                }
            }
        }
    }
}
