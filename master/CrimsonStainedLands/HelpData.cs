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
        public static List<HelpData> Helps = new List<HelpData>();

        public AreaData area;
        public int level;
        public string keyword;
        public string text;
        public string file;
        public bool deleted;

        public HelpData(AreaData area, XElement element)
        {
            this.area = area;
            this.file = area.fileName;
            level = element.GetElementValueInt("level");
            keyword = element.GetElementValue("keyword").Trim();
            text = element.GetElementValue("text").Trim();
            area.Helps.Add(this);
            Helps.Add(this);
        }

        public XElement Element => new XElement("Help",
            new XElement("Level", level),
            new XElement("Keyword", keyword),
            new XElement("Text", text));
    }
}
