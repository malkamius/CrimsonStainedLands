//using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrimsonStainedLands.Extensions;

namespace CrimsonStainedLands
{
    public class WizardNet
    {
        public enum Flags
        {
            On,
            Deaths,
            RoomCommunication,
            AreaCommunication,
            Tells,
            NewbieChannel,
            Newbies,
            Ticks,
            Logins,
            Connections,
            Spam,
            MobDeaths,
            Levels,
            Resets,
            Load,
            Restore,
            Snoops,
            Switches,
            OLC
        }

        public class MonitorEntry
        {
            public Flags Flag { get; set; }
            public int Level { get; set; } = 52;

            public MonitorEntry(Flags flag, int level = 52) { Flag = flag; Level = level; }
        }

        public static MonitorEntry[] MonitorEntries = new MonitorEntry[] {
            new MonitorEntry(Flags.On),
            new MonitorEntry(Flags.Deaths),
            new MonitorEntry(Flags.MobDeaths, 56),
            new MonitorEntry(Flags.RoomCommunication, 59),
            new MonitorEntry(Flags.AreaCommunication, 59),
            new MonitorEntry(Flags.Tells, 59),
            new MonitorEntry(Flags.NewbieChannel),
            new MonitorEntry(Flags.Newbies),
            new MonitorEntry(Flags.Ticks),
            new MonitorEntry(Flags.Logins, 59),
            new MonitorEntry(Flags.Connections, 59),
            new MonitorEntry(Flags.Spam, 55),
            new MonitorEntry(Flags.Resets, 55),
            new MonitorEntry(Flags.Restore, 59),
            new MonitorEntry(Flags.Snoops, 59),
            new MonitorEntry(Flags.Switches, 59),
            new MonitorEntry(Flags.Levels),
            new MonitorEntry(Flags.OLC, 59),
        };

        public static void Wiznet(Flags flag, string text, Character ch = null, ItemData item = null, params object[] arguments)
        {
            text = string.Format(text, arguments);

            game.log("WIZNET ({0}) :: {1} :: {2}", ch != null && !ch.Name.ISEMPTY() ? ch.Name : item != null ? "item " + item.Vnum : "nobody", flag, text);
            
            var monitorentry = MonitorEntries.FirstOrDefault(me => me.Flag == flag);

            if (monitorentry != null)
            {
                foreach (var imm in game.Instance.Info.connections)
                {
                    if (imm.state == Player.ConnectionStates.Playing && imm.socket != null && imm.Level >= monitorentry.Level && imm.WiznetFlags.ISSET(flag))
                    {
                        imm.send("\\rWIZNET ({0}) {1} :: {2} :: {3}\\x\n\r", 
                            ch != null ? ch.Name : item != null ? "item " + item.Vnum : "nobody", 
                            DateTime.Now.ToString(),
                            flag, text);
                    }
                }
            }
        }

        public static void DoWiznet(Character ch, string arguments)
        {
            Flags flag = Flags.On;
            if("on".StringPrefix(arguments))
            {
                ch.WiznetFlags.SETBIT(Flags.On);
            }
            else if("off".StringPrefix(arguments))
            {
                ch.WiznetFlags.REMOVEFLAG(Flags.On);
            }
            else if(Utility.GetEnumValue(arguments, ref flag))
            {
                if (ch.WiznetFlags.ISSET(flag))
                {
                    ch.WiznetFlags.REMOVEFLAG(flag);
                    ch.send("Wiznet flag {0} unset.\n\r", flag);
                }
                else
                {
                    ch.WiznetFlags.SETBIT(flag);
                    ch.send("Wiznet flag {0} set.\n\r", flag);
                }
            }
            else
            {
                ch.send("Choose a wiznet flag: {0}\n\r", string.Join(", ", from entry in MonitorEntries select entry.Flag));
                ch.send("Current wiznet flags: {0}\n\r", string.Join(", ", from entry in MonitorEntries where ch.WiznetFlags.ISSET(entry.Flag) select entry.Flag));
            }
            ch.send("WIZNET is \\g{0}\\x.\n\r", ch.WiznetFlags.ISSET(Flags.On) ? "ON" : "OFF");
        }
    }
}
