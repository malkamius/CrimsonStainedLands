using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public class WeaponDamageMessage
    {
        public static List<WeaponDamageMessage> WeaponDamageMessages = new List<WeaponDamageMessage>();

        public string Keyword;
        public string Message;
        public WeaponDamageTypes Type;

        public WeaponDamageMessage(XElement element)
        {
            Keyword = element.GetAttributeValue("keyword", "none");
            Message  = element.GetAttributeValue("message", "hit");
            Utility.GetEnumValue<WeaponDamageTypes>(element.GetAttributeValue("type"), ref Type, WeaponDamageTypes.Pound);
        }

        public static void LoadWeaponDamageMessages()
        {
            var loadedMessages = new List<WeaponDamageMessage>();
            if (File.Exists(System.IO.Path.Join(Settings.DataPath, "damage_messages.xml")))
            {
                XElement WeaponDamageMessageElement = XElement.Load(System.IO.Path.Join(Settings.DataPath, "damage_messages.xml"));

                foreach (var weaponDamageMessageElement in WeaponDamageMessageElement.Elements())
                {
                    var message = new WeaponDamageMessage(weaponDamageMessageElement);
                    loadedMessages.Add(message);
                }
            }

            WeaponDamageMessages.Clear();
            WeaponDamageMessages.AddRange(loadedMessages);
        }

        public static WeaponDamageMessage GetWeaponDamageMessage(string keyword)
        {
            WeaponDamageMessage noneMessage = null;
            foreach(var message in WeaponDamageMessages)
            {
                if (message.Keyword.ToLower() == keyword.ToLower())
                    return message;
                else if(noneMessage == null && message.Keyword.ToLower() == "none")
                        noneMessage = message;
            }
            return noneMessage;
        }
    }

}
