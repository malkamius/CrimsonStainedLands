using CrimsonStainedLands.Connections;
using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrimsonStainedLands.Web
{
    internal class WhoListController : ControllerBase
    {
        private class WhoEntry
        {
            public string Flags { get; set; } = "";
            public string Name { get; set; } = "";
            public string Title { get; set; } = "";
            public int Level { get; set; } = 1;

            public string Race { get; set; } = "";
            public string Class { get; set; } = "";

            public TimeSpan SessionTime {  get; set; }
        }

        private class WhoData
        {
            public List<WhoEntry> Players { get; set; } = new List<WhoEntry>();

        }

        public override string GetContent()
        {
            var data = new WhoData();

            foreach(var player in Game.Instance.Info.Connections.ToArray())
            {
                if(player.state == Player.ConnectionStates.Playing && !player.Flags.ISSET(ActFlags.WizInvis))
                {
                    var flags = "";
                    if(player.Flags.ISSET(ActFlags.AFK))
                    {
                        flags = flags + "(AFK)";
                    }
                    if(player.inanimate.HasValue)
                    {
                        flags = flags + "(INANIMATE)";
                    }
                    data.Players.Add(new WhoEntry
                    {
                        Name = player.Name,
                        Title = (!player.Title.ISEMPTY() ? ((!player.Title.ISEMPTY() && player.Title.StartsWith(",") ? player.Title : " " + player.Title)) : "") +
                            (!player.ExtendedTitle.ISEMPTY() ? (!player.ExtendedTitle.StartsWith(",") ? " " : "") + player.ExtendedTitle : ""),
                        Level = player.Level,
                        SessionTime = DateTime.Now - player.LoginTime,
                        Race = player.Race.name,
                        Class = player.Guild.whoName,
                        Flags = flags,
                    });
                    
                }
            }
            var content = JsonSerializer.Serialize(data, JsonSerializerOptions.Default);
            return content;
        }

    }
}
