# CrimsonStainedLands
A MUD Server in C# Based on ROM

If it is running, you can connect to host `crimsonstainedlands.net` on port `4000`.

## World Information
CrimsonStainedLands features a variety of character customization options, including different races, classes, and alignments to shape your roleplay and combat experience.

### Races
* **Human**: Balanced stats, suited for any class. (Max: 20 across all stats)
* **Elf**: Highly intelligent and wise, agile but fragile. (Max: 25 Int, 20 Wis, 22 Dex)
* **Dwarf**: Tough and strong, boasting high constitution and strength. (Max: 25 Con, 22 Str)
* **Orc**: Physically imposing with great strength and constitution. (Max: 23 Str, 23 Con)
* **Minotaur**: Extremely powerful and resilient beings. (Max: 23 Str, 22 Con)

### Classes
* **Warrior**: Focus on weapon specialization, decent at taking and dealing damage. Masters shield block, dodge, parry, and feinting.
* **Paladin**: Takes the lead in combat. Can grant sanctuary to themselves and specialize in either two-handed damage or weapon-and-shield blocking.
* **Thief**: Excels at subterfuge. Can hide, sneak, pick locks, and incapacitate enemies by knocking them out or binding them.
* **Bard**: Boosts party morale and health, while dashing enemy spirits with offensive lyrics.
* **Mage**: Focuses on perfecting elemental magic. Unlocks new elemental spells upon mastering earlier ones.
* **Shapeshifter**: Masters of transformation, able to change into various animal forms starting at levels 5-8 with unique shapefocus paths.
* **Ranger**: Excels in the wilderness. Can gather healing herbs, ambush prey, and even forge imbued weapons in the wild.
* **Healer**: Masters of restoration. Can cleanse maladies, heal friends, and grant blessings, flight, sanctuary, or battle frenzy.
* **Assassin**: Martial arts specialists with improved kicks, dodging, and parrying skills. Can also bind wounds when hurt.

### Alignments & Ethos
Characters are shaped by their Alignment (Good, Neutral, Evil) and Ethos (Lawful/Orderly, Neutral, Chaotic). 
* **Good vs. Evil**: Slaying characters of the opposite alignment grants bonus experience.
* **Ethos**: Determines your character's adherence to rules, structure, and individual freedom (Lawful values society and rules; Chaotic values individual freedom and resists external codes; Neutral lies between).

## Getting Started Locally

1. Clone the repository at `https://github.com/malkamius/CrimsonStainedLands`
2. Open the solution under `CrimsonStainedLands\master\CrimsonStainedLands.sln` with Visual Studio 2022, or open the `CrimsonStainedLands` folder with VS Code after getting the C# Extension for it.
3. Edit the settings `master\CrimsonStainedLands\Settings.xml` if you want to change the port.
4. Run the solution. It should load all the areas under the `master\CrimsonStainedLands\data\Areas` folder. It will spit out a lot of logs to the screen as it loads.
5. Use a MUD client to connect to localhost at the port (default 4000) specified in `settings.xml`.

## Player Files & Leveling
Player files will be saved as XML to the `master\CrimsonStainedLands\data\players` folder. You can log out and edit your player level using a text editor.

Once you are level 60, I recommend doing a `help olc`. OLC is custom for this MUD, so it doesn't work like other ROMs...

I use `prompt <%h/%Hhp ~%m/~%Mm %v/%Vmv %W \Mvnum %R\x> ;` for my prompt which shows the vnum of the room I am in at 60.

If there is any additional information I can supply, let me know. 

## Resources
* [Maps and EQ](https://mudmapbuilder.github.io/) by Roman Shapiro who used their tool at:
* [Mud Map Builder](https://github.com/MUDMapBuilder/MUDMapBuilder) to generate resources
