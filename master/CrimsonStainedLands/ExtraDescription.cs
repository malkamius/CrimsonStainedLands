using System;
using System.Text.RegularExpressions;

namespace CrimsonStainedLands
{
    public class ExtraDescription
    {
        private string _description = "";
        public string Keywords { get; set; }
        public string Description
        {
            get
            {
                var regex = new Regex("(?m)^\\s+");
                if (_description.StartsWith(".")) return _description.Replace("\r\n", "\n").Replace("\r\n", "\n");
                return regex.Replace(_description.Trim(), "");
            }
            set 
            { 
                if (value != null) _description = value.Replace("\r\n", "\n").Replace("\r\n", "\n"); else _description = ""; 
            }
        }
        public ExtraDescription(string Keywords, string Description)
        {
            this.Keywords = Keywords;
            this.Description = Description;
        }
    }
}