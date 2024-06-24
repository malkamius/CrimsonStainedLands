using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    internal class DoActCommunication
    {
        /// <summary>
        /// Handles the execution of the "yell" command.
        /// </summary>
        /// <param name="ch">The character executing the command.</param>
        /// <param name="arguments">The arguments for the command.</param>
        public static void DoYell(Character ch, string arguments)
        {
            if (ch.Room == null)
            {
                ch.send("You are not in a room.\n\r");
                return;
            }

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Yell what?\n\r");
                return;
            }

            if ((ch.Race != null && !ch.Race.CanSpeak) || ch.IsAffected(AffectFlags.Silenced))
            {
                ch.Act("You can't speak.");
                return;
            }
            ch.StripHidden();

            //ch.Act("\\y$n ye '{0}'\\x\n\r", null, null, null, ActType.ToRoom, arguments);
            foreach (var other in ch.Room.Area.People)
            {
                if (other != ch)
                    ch.Act("{1}$n yells '{0}'{2}\n\r", other, null, null, ActType.ToVictim, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Yell),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
            }

            ch.Act("{1}You yell '{0}'{2}\n\r", null, null, null, ActType.ToChar, arguments,
                ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Yell),
                ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
        }

        public static void DoSay(Character ch, string arguments)
        {
            if (ch.Room == null)
            {
                ch.send("You are not in a room.\n\r");
                return;
            }

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Say what?\n\r");
                return;
            }

            if (ch.Form != null)
            {
                ch.send("You can't speak.\n\r");
                return;
            }

            ch.StripHidden();
            using (new Character.CaptureCommunications())
            {
                ch.Act("{1}$n says '{0}'{2}\n\r", null, null, null, ActType.ToRoom, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Say),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
                ch.SendToChar("{1}You say '{0}'{2}\n\r", arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Say),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
            }
            //Character.ExecuteSayProgs(ch, arguments);
            if(ch is Player)
            Programs.ExecutePrograms(Programs.ProgramTypes.Say, ch, null, ch.Room, arguments);

        }

        public static void DoSayTo(Character ch, string arguments)
        {
            string target = "";
            Character victim = null;
            if (ch.Room == null)
            {
                ch.send("You are not in a room.\n\r");
                return;
            }

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Say what to whom?\n\r");
                return;
            }

            arguments = arguments.OneArgument(ref target);

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Say what to whom?\n\r");
                return;
            }
            else if ((victim = ch.GetCharacterFromRoomByName(target)) == null)
            {
                ch.send("They aren't here.\n\r");
                return;
            }
            else if (victim == ch)
            {
                ch.send("You can't say something to yourself.\n\r");
                return;
            }

            ch.StripHidden();

            using (new Character.CaptureCommunications())
            {
                if (victim.Position != Positions.Sleeping)
                    ch.Act("{1}$n says to you '{0}'{2}\n\r", victim, null, null, ActType.ToVictim, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Say),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
                ch.Act("{1}$n says to $N '{0}'{2}\n\r", victim, null, null, ActType.ToRoomNotVictim, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Say),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
                ch.Act("{1}You says to $N '{0}'{2}\n\r", victim, null, null, ActType.ToChar, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Say),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
            }

        }

        public static void DoWhisper(Character ch, string arguments)
        {
            if (ch.Room == null)
            {
                ch.send("You are not in a room.\n\r");
                return;
            }

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Whiser what?\n\r");
                return;
            }
            using (new Character.CaptureCommunications())
            {
                ch.Act("{1}$n whisper '{0}'{2}\n\r", null, null, null, ActType.ToRoom, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Whisper),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
                ch.Act("{1}You whisper '{0}'{2}\n\r", null, null, null, ActType.ToChar, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Whisper),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
            }

        }

        public static void DoWhisperTo(Character ch, string arguments)
        {
            string target = "";
            Character victim = null;
            if (ch.Room == null)
            {
                ch.send("You are not in a room.\n\r");
                return;
            }

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Whisper what to whom?\n\r");
                return;
            }

            arguments = arguments.OneArgument(ref target);

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Whisper what to whom?\n\r");
                return;
            }
            else if ((victim = ch.GetCharacterFromRoomByName(target)) == null)
            {
                ch.send("They aren't here.\n\r");
                return;
            }
            else if (victim == ch)
            {
                ch.send("You can't whisper to yourself.\n\r");
                return;
            }
            using (new Character.CaptureCommunications())
            {
                if (victim.Position != Positions.Sleeping)
                    ch.Act("{1}$n whisper to you '{0}'{2}\n\r", victim, null, null, ActType.ToVictim, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Whisper),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
                ch.Act("{1}$n whisper to $N '{0}'{2}\n\r", victim, null, null, ActType.ToRoomNotVictim, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Whisper),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
                ch.Act("{1}You whisper to $N '{0}'{2}\n\r", victim, null, null, ActType.ToChar, arguments,
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Communication_Whisper),
                        ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset));
            }
        }

        public static void DoTell(Character ch, string arguments)
        {
            string target = "";
            Character victim = null;

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Tell what to whom?\n\r");
                return;
            }

            arguments = arguments.OneArgument(ref target);

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Tell what to whom?\n\r");
                return;
            }
            else if ((victim = Character.GetCharacterWorld(ch, target)) == null)
            {
                ch.send("You don't see them.\n\r");
                return;
            }
            using (new Character.CaptureCommunications())
            {
                CharacterDoFunctions.PerformTell(ch, victim, arguments);
            }
        }

        public static void DoReply(Character ch, string arguments)
        {
            if (ch.ReplyTo != null && ch is Player && ((Player)ch.ReplyTo).socket != null)
            {
                if (ch.ReplyTo.Position == Positions.Sleeping)
                {
                    ch.send("They can't hear you right now.\n\r");
                    return;
                }
                var victim = ch.ReplyTo;
                ch.StripHidden();
                using (new Character.CaptureCommunications())
                {
                    ch.Act("\\r$n tells you '{0}'\\x\n\r", victim, null, null, ActType.ToVictim, arguments);
                    ch.Act("\\rYou tell $N '" + arguments + "'\\x\n\r", victim);
                }
                victim.ReplyTo = ch;
            }
            else
                ch.send("They aren't here.\n\r");
        }

        public static void DoGTell(Character ch, string arguments)
        {
            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Tell what to the group?\n\r");
                return;
            }

            ch.StripHidden();

            using (new Character.CaptureCommunications())
            {
                foreach (var other in Character.Characters)
                {
                    if (other != ch && other.IsSameGroup(ch))
                        ch.Act("\\M$n tells the group '{0}'\\x\n\r", other, null, null, ActType.ToVictim, arguments);
                }
                ch.Act("\\MYou tell the group '{0}'\\x\n\r", null, null, null, ActType.ToChar, arguments);
            }
        }

        public static void DoPray(Character ch, string arguments)
        {
            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Pray what to the gods?\n\r");
                return;
            }
            using (new Character.CaptureCommunications())
            {
                foreach (var other in Character.Characters)
                {
                    if (other != ch && other.IsImmortal)
                        ch.Act("\\r$n prays '{0}'\\x\n\r", other, null, null, ActType.ToVictim, arguments);
                }
            }
            Game.log("{0} prays '{1}'", ch.Name, arguments);
            ch.Act("\\rYou pray to the gods for help!\\x\n\r", null, null, null, ActType.ToChar, arguments);
        }

        public static void DoNewbie(Character ch, string arguments)
        {
            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                if (ch.Flags.ISSET(ActFlags.NewbieChannel))
                {
                    ch.Flags.REMOVEFLAG(ActFlags.NewbieChannel);
                }
                else
                    ch.Flags.SETBIT(ActFlags.NewbieChannel);

                ch.send("Newbie channel is \\g{0}\\x.\n\r", ch.Flags.ISSET(ActFlags.NewbieChannel) ? "ON" : "OFF");

            }
            else
            {
                if (!ch.Flags.ISSET(ActFlags.NewbieChannel))
                {
                    ch.Flags.SETBIT(ActFlags.NewbieChannel);
                    ch.send("Newbie channel is \\gON\\x.\n\r");
                }
                using (new Character.CaptureCommunications())
                {
                    foreach (var other in Character.Characters)
                    {
                        if (other != ch && other.Flags.ISSET(ActFlags.NewbieChannel))
                            ch.Act("\\cNEWBIE ($n): {0}\\x\n\r", other, null, null, ActType.ToVictim, arguments);
                    }
                    Game.log("{0} newbies '{1}'", ch.Name, arguments);
                    ch.send("\\cNEWBIE (You): {0}\\x\n\r", arguments);
                }
            }
        }

        public static void DoReplay(Character ch, string arguments)
        {
            ch.send("Last {0} communications:\n\r", ch.Communications.Count);

            foreach(var communication in ch.Communications)
            {
                ch.send(communication);
            }
        }
    } // end class
} // end namespace
