using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using global::CrimsonStainedLands.Extensions;

namespace CrimsonStainedLands
{
    public class HelpData
    {
        public static ConcurrentList<HelpData> Helps = new ConcurrentList<HelpData>();

        public AreaData area;
        public int vnum;
        public int level;
        public string keyword;
        public string text;
        public string file;
        public string lastEditedBy;
        public DateTime lastEditedOn = DateTime.MinValue;
        public bool deleted;

        public HelpData() { }

        public HelpData(AreaData area, XElement element)
        {
            this.area = area;
            this.file = area.FileName;
            vnum = element.GetElementValueInt("vnum", element.GetAttributeValueInt("vnum"));
            if(vnum == 0)
            {
                vnum = Math.Max(this.area.VNumStart, this.area.Helps.Any()? this.area.Helps.Max(h => h.vnum) + 5 : 1);
                area.saved = false;
            }
            level = element.GetElementValueInt("level", element.GetAttributeValueInt("level"));
            
            keyword = element.GetElementValue("keyword", element.GetAttributeValue("KeyWord")).Trim();
            text = element.GetElementValue("text", element.Value).Trim();
            lastEditedBy = element.GetElementValue("LastEditedBy", element.GetAttributeValue("LastEditedBy"));
            
            DateTime.TryParse(element.GetElementValue("LastEditedOn", element.GetAttributeValue("LastEditedOn", DateTime.Now.ToString())), out lastEditedOn);

            area.Helps.Add(this);
            Helps.Add(this);
        }

        public XElement Element => new XElement("Help",
            new XAttribute("VNum", vnum),
            new XAttribute("Level", level),
            new XAttribute("Keyword", keyword),
            new XAttribute("LastEditedBy", lastEditedBy),
            new XAttribute("LastEditedOn", lastEditedOn),
            text);
    }
}
