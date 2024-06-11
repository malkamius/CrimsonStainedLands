using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public class Social
    {
        public static List<Social> Socials = new List<Social>();

        public string Name;
        public string CharNoArg;
        public string OthersNoArg;
        public string CharFound;
        public string OthersFound;
        public string VictimFound;
        public string CharNotFound;

        public string CharAuto;

        public string OthersAuto;

        public static void LoadSocials()
        {
            if (System.IO.File.Exists(System.IO.Path.Join(Settings.DataPath, "social.are")))
            {
                var file = new FileInfo(System.IO.Path.Join(Settings.DataPath, "social.are"));

                if (file.Exists)
                {
                    var stream = file.OpenText();
                    while (!stream.EndOfStream)
                    {
                        var line = stream.ReadLine().Trim();
                        if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                        {
                            var social = new Social();
                            var length = line.IndexOf(" ");
                            if (length >= 0)
                            {
                                social.Name = line.Substring(0, length);
                            }
                            else
                                social.Name = line;

                            if (string.IsNullOrEmpty(social.Name))
                            {
                                continue;
                            }
                            if (readSocialField(stream, out social.CharNoArg))
                                if (readSocialField(stream, out social.OthersNoArg))
                                    if (readSocialField(stream, out social.CharFound))
                                        if (readSocialField(stream, out social.OthersFound))
                                            if (readSocialField(stream, out social.VictimFound))
                                                if (readSocialField(stream, out social.CharNotFound))
                                                    if (readSocialField(stream, out social.CharAuto))
                                                        readSocialField(stream, out social.OthersAuto);

                            Socials.Add(social);
                        }
                    }

                    Game.log("{0} socials loaded.", Socials.Count);
                }
            }

            var xml = new XElement("Socials");
            var elements = from social in Socials
                           select new XElement("Social",
                new XAttribute("Keyword", social.Name),
                new XAttribute("CharNoArg", social.CharNoArg ?? ""),
                new XAttribute("OthersNoArg", social.OthersNoArg ?? ""),
                new XAttribute("CharFound", social.CharFound ?? ""),
                new XAttribute("OthersFound", social.OthersFound ?? ""),
                new XAttribute("VictimFound", social.VictimFound ?? ""),
                new XAttribute("CharNotFound", social.CharNotFound ?? ""),
                new XAttribute("CharAuto", social.CharAuto ?? ""),
                new XAttribute("OthersAuto", social.OthersAuto ?? "")
                );
            foreach(var element in elements)
            xml.Add(element);
            xml.Save(System.IO.Path.Join(Settings.DataPath, "socials.xml"));

            var commands = from com in Command.Commands select com.Name;
            System.IO.File.WriteAllText(System.IO.Path.Join(Settings.DataPath, "commandslist.txt"), string.Join(Environment.NewLine, commands));
        }
        private static bool readSocialField(StreamReader stream, out string socialField)
        {
            socialField = null;
            if (stream.EndOfStream) return false;
            var line = stream.ReadLine().Trim();
            if (line .StartsWith("#") || string.IsNullOrEmpty(line))
                return false;
            if (line != "$")
                socialField = line;
            return true;
        }
    }
}