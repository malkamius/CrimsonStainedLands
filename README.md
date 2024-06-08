# CrimsonStainedLands
A MUD Server in C# Based on ROM<br />
<br />
If it is running, you can connect to host Games.mywire.org or kbs-cloud.com on port 4000<br />
<br />
<br />
To get started with this project locally:<br />
Clone the repository at https://github.com/malkamius/CrimsonStainedLands<br />
<br />
Open the solution under CrimsonStainedLands\master CrimsonStainedLands.sln with visual studio 2022<br />
<br />
Edit the settings master\CrimsonStainedLands\bin\Debug\Settings.xml if you want to change the port.<br />
<br />
Run the solution in debug or release mode. It should load all the areas under the bin\Debug\Data\Areas folder. <br />
It will spit out a lot of logs to the screen as it loads.<br />
<br />
Use a mud client to connect to the localhost at the port(default 4000) specified in settings.xml<br />
<br />
Player files will be saved as XML to the bin\Debug\data\players folder. You can log out and edit your player level using a text editor like notepad++<br />(highly recommend this)<br />
<br />
Once you are level 60 I recommend doing a 'help olc'. OLC is custom for this mud, so it doesn't work like other ROMs...<br />
<br />
I use prompt <%h/%Hhp ~%m/~%Mm %v/%Vmv %W \Mvnum %R\x> ; for my prompt which shows the vnum of the room I am in at 60<br />
<br />
If there is any additional information I can supply, let me know. <br />
<br />
[Maps and EQ](https://mudmapbuilder.github.io/) by Roman Shapiro who used their tool at<br />
[Mud Map Builder](https://github.com/MUDMapBuilder/MUDMapBuilder) to generate resources<br />
