using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    class Liquid
    {
        public static Dictionary<string, Liquid> Liquids = new Dictionary<string, Liquid>();

        public string name;
        public string color;
        public int proof;
        public int full;
        public int thirst;
        public int food;
        public int ssize;

        public static void loadLiquids()
        {
            var element = XElement.Load(@"data\liquids.xml");

            foreach(var liquidElement in element.Elements())
            {
                var liquid = new Liquid { name = liquidElement.GetAttributeValue("Name"), color = liquidElement.GetAttributeValue("Color"),
                 proof = liquidElement.GetAttributeValueInt("Proof"), food = liquidElement.GetAttributeValueInt("Food"), full = liquidElement.GetAttributeValueInt("Full"),
                 thirst = liquidElement.GetAttributeValueInt("Thirst"), ssize = liquidElement.GetAttributeValueInt("SSize")};
                Liquids.Add(liquid.name, liquid);
            }
        }
    }
}
