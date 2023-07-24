using System;

namespace CrimsonStainedLands
{
	public class ExtraDescription
	{
		public string Keywords { get; set; }
		public string Description { get; set; }
		public ExtraDescription(string Keywords, string Description)
		{
			this.Keywords = Keywords;
			this.Description = Description;
		}
	}
}