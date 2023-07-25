Under master\CrimsonStainedLands\bin\Debug is a file named skilllevels.xml. This file contains entries for what level different guilds get different skills at.
Begin by copying one of the skilllevel entries:
<SkillSpell
    Name="kick"
    MinimumPosition="Fighting"
    NounDamage="kick"
	AutoCast="true"
    SkillTypes="Skill">
    <SkillLevel
      Guild="warrior"
      Level="1"
      Rating="1" />
    <SkillLevel
      Guild="ranger"
      Level="8"
      Rating="1" />
    <SkillLevel
      Guild="bard"
      Level="6"
      Rating="1" />
    <SkillLevel
      Guild="paladin"
      Level="3"
      Rating="1" />
    <SkillLevel
      Guild="thief"
      Level="14"
      Rating="1" />
  </SkillSpell>
Change the name to something new.

The kick command happens to be located under Combat.cs
public static void DoKick(Character ch, string arguments)
Copy this method and change the name to match the skilllevel entry.

The command.cs static constructor contains a list of all the commands that can be typed in(excluding socials)
Copy an entry and change the data to match your new skill
        static Command()
        {
            Commands.Add(new Command { Name = "north", Action = Character.DoNorth, Info = "Walk north.", MinimumPosition = Positions.Standing, NPCCommand = false });

Commands.Add(new Command { Name = "newkick", Action = Combat.DoNewKick, Info = "Perform the master kick.", MinimumPosition = Positions.Fighting, NPCCommand = true });
