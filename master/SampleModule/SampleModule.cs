namespace CrimsonStainedLands;
using System.Runtime.CompilerServices;
public class SampleModule : CrimsonStainedLands.Module
{
    public static void DoColorTest(CrimsonStainedLands.Character character, string arguments)
    {
        character.send("This is a test of color codes.\r\n");
        character.send("{RRed text{x\r\n");
        character.send("{GGreen text{x\r\n");
        character.send("{BBlue text{x\r\n");
    }

    public SampleModule(string dllPath, System.Reflection.Assembly assembly) : base(dllPath, assembly)
    {
        this.Name = "ColorTest : SampleModule";
        this.Description = "A sample module for testing color features.";

        CrimsonStainedLands.Command.Commands.Add(new CrimsonStainedLands.Command()
        {
            Name = "colortest",
            Info = "Tests color output",
            Action = DoColorTest,
            MinimumLevel = 0,
            MinimumPosition = CrimsonStainedLands.Positions.Dead,
            NPCCommand = false,
            Skill = null
        });
    }

}
