using CrimsonStainedLands;
using CrimsonStainedLands.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLSMapper
{
    public class ItemDisplay
    {
        public ItemTemplateData Item;

        public ItemDisplay(ItemTemplateData item)
        { this.Item = item; }

        public string Display { get { return Item.Vnum + " - " + Item.Name; } }
    }
}
