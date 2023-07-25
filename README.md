# CrimsonStainedLands
A MUD Server in C# Based on ROM

If it is running, you can connect to host Games.mywire.org on port 4000


To get started with this project locally:
Clone the repository at https://github.com/malkamius/CrimsonStainedLands

Open the solution under CrimsonStainedLands\master CrimsonStainedLands.sln with visual studio 2022

Edit the settings master\CrimsonStainedLands\bin\Debug\Settings.xml if you want to change the port.

Run the solution in debug or release mode. It should load all the areas under the bin\Debug\Data\Areas folder. It will spit out a lot of logs to the screen as it loads.

Use a mud client to connect to the localhost at the port(default 4000) specified in settings.xml

Player files will be saved as XML to the bin\Debug\data\players folder. You can log out and edit your player level using a text editor like notepad++(highly recommend this)

Once you are level 60 I recommend doing a 'help olc'. OLC is custom for this mud, so it doesn't work like other ROMs...

I use prompt <%h/%Hhp ~%m/~%Mm %v/%Vmv %W \Mvnum %R\x> ; for my prompt which shows the vnum of the room I am in at 60

If there is any additional information I can supply, let me know.
