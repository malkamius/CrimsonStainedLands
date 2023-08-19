using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    public class LoadTimer : IDisposable
    {
        public string Name { get; set; }
        public Func<object> AdditionalTextFunction { get; set; }
        DateTime started { get; set; } = DateTime.Now;

        public LoadTimer(string name, Func<object> additionaltext)
        {
            this.Name = name;
            this.AdditionalTextFunction = additionaltext;
        }

        public LoadTimer(string name)
        {
            this.Name = name;
            this.AdditionalTextFunction = null;
        }

        public void Dispose()
        {
            Game.log("{0} in {1}", string.Format(Name, AdditionalTextFunction != null ? AdditionalTextFunction() : ""), DateTime.Now - started);
        }
    }
}
