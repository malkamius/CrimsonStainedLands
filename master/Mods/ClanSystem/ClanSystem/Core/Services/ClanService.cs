using CrimsonStainedLands;
using CrimsonStainedLands.World;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace ClanSystemMod
{
    public static class ClanService
    {
        public static void CommandCreateClan(Character ch, string arguments)
        {
            if (ch.Level < GameSettings.MinLevelRequiredForClanCreation)
            {
                ch.send("You cannot create|delete a clan.\r\n");
                return;
            }

            if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs_1))
            {
                // we've got a clan name, if more args then continue, else create clan with name only
                if (Helper.getNextArg(remainingArgs_1, out string clanLeader, out string remainingArgs_2))
                {
                    //Have clanLeader name
                    if (Helper.getNextArg(remainingArgs_2, out string clanTag, out string remainingArgs_3))
                    {
                        //have clantag
                        CreateClan(ch, clanName, clanLeader, clanTag);
                    }
                }
                else
                {
                    // create clan with name only              
                    CreateClan(ch, clanName);
                }
            }
            else
            {
                ch.send("No clan name was given. Type 'clan help'.\r\n");
            }
        }

        public static void CreateClan(Character ch, string clanName, string leaderName, string clanTag)
        {
            if (clanName.Length > GameSettings.MaxLengthOfClanName)
            {
                ch.send($"The clan name cannot be more than {GameSettings.MaxLengthOfClanName} characters long.\r\n");
                return;
            }

            if (clanTag.Length > GameSettings.MaxLengthOfClanTag)
            {
                ch.send($"The clan tag cannot be more than {GameSettings.MaxLengthOfClanTag} characters long.\r\n");
                return;
            }

            if (ClanDBService.GetNumberOfClans() >= GameSettings.MaxClansAllowed)
            {
                ch.send("The maximum ammount of clans have been reached. You cannot create more.\r\n");
                return;
            }

            if (IsPlayerInAnyClan(leaderName, out string outClanName))
            {
                ch.send("This player is part of a clan already.\r\n");
                return;
            }

            if (ClanDBService.ClanExists(clanName))
            {
                ch.send("This clan already exists.\r\n");
                return;
            }


            List<string> mudPlayers = getAllExistingPlayerNames();
            bool mudPlayerFound = false;
            foreach (string mudPlayer in mudPlayers)
            {
                if (leaderName.Equals(mudPlayer, StringComparison.CurrentCultureIgnoreCase))
                {
                    mudPlayerFound = true;
                    break;
                }
            }

            if (mudPlayerFound)
            {
                var newClan = new Clan
                {
                    Name = clanName,
                    Tag = clanTag,
                    LeaderPlayerName = leaderName,
                    Members = new List<ClanMember>
                    {
                        new ClanMember { playerName = leaderName, Rank = ClanRank.Leader}
                    }
                };

                if (ClanDBService.addClan(newClan, out string errMsgAddClan))
                {
                    ch.send("Clan created.\r\n");
                    ClanDBService.WriteToFileClans(out string errMsgWriteToClans);
                }
            }
            else
            {
                ch.send("The player name was not found. Active or Inactive.\r\n");
            }
        }


        private static void CreateClan(Character ch, string clanName)
        {

            if (clanName.Length > GameSettings.MaxLengthOfClanName)
            {
                ch.send($"The clan name cannot be more than {GameSettings.MaxLengthOfClanName} characters long.\r\n");
                return;
            }

            if (ClanDBService.GetNumberOfClans() >= GameSettings.MaxClansAllowed)
            {
                ch.send("The maximum ammount of clans have been reached. You cannot create more.\r\n");
                return;
            }

            if (ClanDBService.ClanExists(clanName))
            {
                ch.send("This clan already exists.\r\n");
                return;
            }

            var newClan = new Clan
            {
                Name = clanName,
                Tag = "",
                LeaderPlayerName = "",
            };

            if (ClanDBService.addClan(newClan, out string errMsgAddClan))
            {
                ch.send("Clan created\r\n");
                ClanDBService.WriteToFileClans(out string errMsgWriteToClans);
            }
        }


        public static void CommandRemoveClan(Character ch, string arguments)
        {
            if (ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs_1))
                {
                    if (ClanDBService.removeClan(clanName, out string errMsgRemoveClan))
                    {
                        ch.send("Clan removed.\r\n");
                    }
                }
                else
                {
                    ch.send("No clan name was specified. Type 'clan help'.\r\n");
                }
            }
            else
            {
                ch.send("You cannot remove a clan.\r\n");
            }
        }


        public static void CommandSetClanTag(Character ch, string arguments)
        {
            if ((GetPlayerRank(ch.Name) == ClanRank.Leader) || ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs_1))
                {
                    if (Helper.getNextArg(remainingArgs_1, out string clanTag, out string remainingArgs_2))
                    {
                        SetClanTag(ch, clanName, clanTag);
                    }
                    else
                    {
                        ch.send("No clan tag was specified. Type 'clan help'.\r\n");
                    }
                }
                else
                {
                    ch.send("No clan name was specified. Type 'clan help'.\r\n");
                }
            }
            else
            {
                ch.send("You cannot change the clan tag.\r\n");
            }
        }

        private static void SetClanTag(Character ch, string clanName, string clanTag)
        {

            if (clanTag.Length > GameSettings.MaxLengthOfClanTag)
            {
                ch.send($"The clan tag cannot be more than {GameSettings.MaxLengthOfClanTag} characters long.\r\n");
                return;
            }

            Clan? clan = ClanDBService.GetClan(clanName, out string errMsgGetClan);

            if (clan != null)
            {
                clan.Tag = clanTag;
                if (ClanDBService.UpdateClanRecord(clan, out string errMsgUpdateClanRecords))
                {
                    ch.send("Clan tag has been added|updated.\r\n");
                    ClanDBService.WriteToFileClans(out string errMsgWriteToClans);
                    return;
                }
            }
        }


        public static void CommandUpdateClanName(Character ch, string arguments)
        {
            if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs_1))
            {
                if (Helper.getNextArg(remainingArgs_1, out string newClanName, out string remainingArgs_2))
                {
                    UpdateClanName(ch, clanName, newClanName);
                }
                else
                {
                    ch.send("No new clan name was given. Type 'clan help'.\r\n");
                }
            }
            else
            {
                ch.send("No clan name was given. Type 'clan help'.\r\n");
            }
        }

        private static void UpdateClanName(Character ch, string clanName, string newClanName)
        {
            if ((IsPlayerInClan(clanName, ch.Name) && GetPlayerRank(ch.Name) == ClanRank.Leader) || ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                if (clanName.Length > GameSettings.MaxLengthOfClanName)
                {
                    ch.send($"The clan name cannot be more than {GameSettings.MaxLengthOfClanName} characters long.\r\n");
                    return;
                }

                Clan? clan = ClanDBService.GetClan(clanName, out string errMsgGetClan);
                if (clan != null)
                {
                    if (isClanNameUsed(clanName))
                    {
                        ch.send("This clan name is already in use.\r\n");
                        return;
                    }
                    clan.Name = newClanName;
                    if (ClanDBService.UpdateClanRecord(clan, out string errMsgUpdateClanRecords))
                    {
                        ClanDBService.WriteToFileClans(out string errMsgWriteToClans);
                        ch.send("Clan name has been updated.\r\n");
                    }
                }
            }
            else
            {
                ch.send("You cannot update a clan name\r\n");
            }
        }


        public static void CommandAddMember(Character ch, string arguments)
        {
            ClanMember clanMember = ch.GetVariable<ClanMember>("ClanMember");

            //--- Make sure this character does have a clan member object
            if (clanMember == null)
            {
                ClanMember tmpMember = addClanMemberObject(ch);
            }

            if (ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                if (Helper.getNextArg(arguments, out string outClanName, out string remainingArgs_1))
                {
                    if (Helper.getNextArg(remainingArgs_1, out string outPlayerName, out string remainingArgs_2))
                    {
                        AddMember(ch, outClanName, outPlayerName);
                    }
                    else
                    {
                        ch.send("No player name was specfied. Type 'clan help'.\r\n");
                    }
                }
                else
                {
                    ch.send("No clan name was specfied. Type 'clan help'.\r\n");
                }
            }
            else
            {
                string clanName = GetClanName(ch.Name);

                if (clanName != "" && GetPlayerRank(ch.Name) >= ClanRank.Captain)
                {
                    if (Helper.getNextArg(arguments, out string playerName, out string remainingArgs))
                    {
                        AddMember(ch, clanName, playerName);
                    }
                    else
                    {
                        ch.send("No player name was specfied. Type 'clan help'.\r\n");
                    }
                }
            }
        }

        private static void AddMember(Character ch, string clanName, string playerName)
        {
            if (IsPlayerInAnyClan(playerName, out string outClanName))
            {
                ch.send($"This player is part of a clan already. Clan member of : {outClanName}\r\n");
                return;
            }

            Player? player = getConnectedPlayer(playerName);

            if (player == null)
            {
                ch.send("The player was not found. Active or inactive.\r\n");
                return;
            }

            Clan? clan = ClanDBService.GetClan(clanName, out string errMsgGetClan);
            if (clan != null)
            {
                ClanMember tmpMember = new ClanMember();
                tmpMember.playerName = playerName;
                tmpMember.Rank = ClanRank.GreenHorn;
                tmpMember.ClanName = clanName;

                clan.Members.Add(tmpMember);

                findAndAttachClanMemberObject(player, clan, tmpMember);

                if (ClanDBService.UpdateClanRecord(clan, out string errMsgUpdateClanRecords))
                {
                    ch.send("Clan member added.\r\n");
                    ClanDBService.WriteToFileClans(out string errMsgWriteToClans);
                }
            }
        }


        public static void CommandRemoveMember(Character ch, string arguments)
        {
            if (ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs))
                {
                    if (Helper.getNextArg(remainingArgs, out string playerName, out string remainingArgs_1))
                    {
                        RemoveMember(ch, clanName, playerName);
                    }
                    else
                    {
                        ch.send("No player name was specfied. Type 'clan help'.\r\n");
                    }
                }
                else
                {
                    ch.send("No clan name was specfied. Type 'clan help'.\r\n");
                }
            }
            else
            {
                string clanName = GetClanName(ch.Name);

                if (clanName != "" && GetPlayerRank(ch.Name) >= ClanRank.Captain)
                {
                    if (Helper.getNextArg(arguments, out string playerName, out string remainingArgs))
                    {
                        RemoveMember(ch, clanName, playerName);
                    }
                    else
                    {
                        ch.send("No player name was specfied. Type 'clan help'.\r\n");
                    }
                }
            }
        }


        private static void RemoveMember(Character ch, string clanName, string playerName)
        {
            if (IsPlayerInClan(clanName, playerName))
            {
                Clan? clan = ClanDBService.GetClan(clanName, out string errMsgGetClan);
                if (clan != null)
                {
                    clan.Members.RemoveAll(member => member.playerName.Equals(playerName, StringComparison.CurrentCultureIgnoreCase));

                    Player? player = getConnectedPlayer(playerName);
                    if (player != null)
                    {
                        ClanMember tmpMember = new ClanMember();
                        tmpMember.playerName = playerName;
                        tmpMember.Rank = ClanRank.None;
                        tmpMember.ClanName = "";
                        findAndAttachClanMemberObject(player, clan, tmpMember);
                    }

                    if (IsAClanLeader(playerName))
                    {
                        clan.LeaderPlayerName = "";
                    }
                    if (ClanDBService.UpdateClanRecord(clan, out string errMsgUpdateClanRecords))
                    {
                        ch.send("Member has been removed.\r\n");
                        ClanDBService.WriteToFileClans(out string errMsgWriteToClans);
                    }
                }
            }
            else
            {
                ch.send("This player is not part of this clan.\r\n");
            }
        }


        public static void CommandPromoteMember(Character ch, string arguments)
        {
            if (ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs_1))
                {
                    if (Helper.getNextArg(remainingArgs_1, out string playerName, out string remainingArgs_2))
                    {
                        PromoteMember(ch, clanName, playerName);
                    }
                    else
                    {
                        ch.send("No player name was specfied. Type 'clan help'.\r\n");
                    }
                }
                else
                {
                    ch.send("No clan name was specfied. Type 'clan help'.\r\n");
                }
            }
            else
            {
                string clanName = GetClanName(ch.Name);

                if (clanName != "" && GetPlayerRank(ch.Name) >= ClanRank.Captain)
                {
                    if (Helper.getNextArg(arguments, out string playerName, out string remainingArgs))
                    {
                        PromoteMember(ch, clanName, playerName);
                    }
                    else
                    {
                        ch.send("No player name was specfied. Type 'clan help'.\r\n");
                    }
                }
            }
        }


        private static void PromoteMember(Character ch, string clanName, string playerName)
        {
            if (IsPlayerInClan(clanName, playerName))
            {
                Clan? clan = ClanDBService.GetClan(clanName, out string errMsgGetClan);
                if (clan != null)
                {
                    foreach (ClanMember member in clan.Members)
                    {
                        if (member.playerName == playerName)
                        {
                            if (member.Rank == ClanRank.Leader)
                            {
                                ch.send("Cannot promote a member that is a leader. This is the Highest level.");
                                return;
                            }

                            if (member.Rank + 1 == ClanRank.Leader)
                            {
                                if (ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
                                {
                                    if (getClanLeader(clanName, out string leaderName))
                                    {
                                        if (leaderName != "")
                                        {
                                            ch.send("Cannot promote this member to leader. This clan already has a leader.");
                                            return;
                                        }
                                        else
                                        {
                                            clan.LeaderPlayerName = playerName;
                                        }
                                    }
                                }
                                else
                                {
                                    ch.send("You cannot promote a member to leader. Please start a voting process to ellect a new Leader.");
                                    return;
                                }
                            }

                            member.Rank += 1;
                            Character? target = getConnectedPlayer(playerName);
                            if (target != null)
                            {
                                ClanMember tmpMember = new ClanMember();
                                tmpMember.playerName = playerName;
                                tmpMember.Rank = member.Rank;
                                tmpMember.ClanName = clanName;

                                findAndAttachClanMemberObject(ch, clan, tmpMember);
                            }

                            if (ClanDBService.UpdateClanRecord(clan, out string errMsgUpdateClanRecords))
                            {
                                ClanDBService.WriteToFileClans(out string errMsgWriteToClans);
                                string currentRank = ClanService.GetPlayerRankAsString(playerName);
                                ch.send($"Player now has the rank : {currentRank}");
                            }
                        }
                    }
                }
            }
            else
            {
                ch.send("Player is not part of this clan.\r\n");
            }
        }


        public static void CommandDemoteMember(Character ch, string arguments)
        {
            if (ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs_1))
                {
                    if (Helper.getNextArg(remainingArgs_1, out string playerName, out string remainingArgs_2))
                    {
                        DemoteMember(ch, clanName, playerName);
                    }
                    else
                    {
                        ch.send("No player name was specfied. Type 'clan help'.\r\n");
                    }
                }
                else
                {
                    ch.send("No clan name was specfied. Type 'clan help'.\r\n");
                }
            }
            else
            {
                string clanName = GetClanName(ch.Name);

                if (clanName != "" && GetPlayerRank(ch.Name) >= ClanRank.Captain)
                {
                    if (Helper.getNextArg(arguments, out string playerName, out string remainingArgs))
                    {
                        DemoteMember(ch, clanName, playerName);
                    }
                    else
                    {
                        ch.send("No player name was specfied. Type 'clan help'.\r\n");
                    }
                }
            }
        }

        public static void DemoteMember(Character ch, string clanName, string playerName)
        {
            if (IsPlayerInClan(clanName, playerName))
            {
                Clan? clan = ClanDBService.GetClan(clanName, out string errMsgGetClan);
                if (clan != null)
                {
                    foreach (ClanMember member in clan.Members)
                    {
                        if (member.playerName == playerName)
                        {

                            if (member.Rank == ClanRank.GreenHorn)
                            {
                                ch.send("Cannot demote a member that is a Green Horn. This is the lowest level.");
                                return;
                            }

                            
                            if((member.Rank == ClanRank.Leader) && (ch.Level >= GameSettings.MinLevelRequiredForClanCreation))
                            {   
                                clan.LeaderPlayerName = "";
                            }
                            else
                            {
                                ch.send("You cannot demote a clan leader. Please start a voting process to ellect a new Leader.");
                                return;
                            }
                            

                            member.Rank -= 1;

                            Player? player = getConnectedPlayer(playerName);
                            if (player != null)
                            {
                                ClanMember tmpMember = new ClanMember();
                                tmpMember.playerName = playerName;
                                tmpMember.Rank = member.Rank;
                                tmpMember.ClanName = clanName;
                                findAndAttachClanMemberObject(player, clan, tmpMember);
                            }


                            if (ClanDBService.UpdateClanRecord(clan, out string errMsgUpdateClanRecords))
                            {
                                ClanDBService.WriteToFileClans(out string errMsgWriteToClans);
                                string currentRank = ClanService.GetPlayerRankAsString(playerName);
                                ch.send($"Player now has the rank : {currentRank}");
                            }
                        }
                    }
                }
            }
            else
            {
                ch.send("Player is not part of this clan.\r\n");
            }
        }


        public static void CommandGetClanList(Character ch, string arguments)
        {
            string list = "";
            List<Clan> clans = ClanDBService.GetAllClans();

            if (clans != null)
            {
                if (clans.Count == 0)
                {
                    ch.send("There are no clans as yet.\r\n");
                    return;
                }

                list += $"{"\\rClan Name",-32}|{"Clan Tag",-30}|Clan Leader\\x\n";
                foreach (Clan clan in clans)
                {
                    list += $"{clan.Name,-30}|{clan.Tag,-30}|{clan.LeaderPlayerName}\n";
                }
                ch.send(list + "\r\n");
            }
        }


        public static void CommandGetMemberInfo(Character ch, string arguments)
        {
            if (Helper.getNextArg(arguments, out string playerName, out string remainingArgs))
            {
                if (IsPlayerInAnyClan(playerName, out string clanName))
                {
                    string rank = ClanService.GetPlayerRankAsString(playerName);
                    ch.send($"{playerName} has rank {rank} within clan {clanName}\r\n");
                }
                else
                {
                    ch.send("Player is not a member of any clan.\r\n");
                }
            }
            else
            {
                ch.send("No player name was given. Type \\rclan help\\x.\r\n");
            }
        }


        public static string GetPlayerRankAsString(string playerName)
        {
            List<Clan> clans = ClanDBService.GetAllClans();
            if (clans != null)
            {
                foreach (Clan clan in clans)
                {
                    foreach (ClanMember member in clan.Members)
                    {
                        if (member.playerName == playerName)
                        {
                            return member.Rank.ToString();
                        }
                    }
                }
                return "";
            }
            return "";
        }


        public static ClanRank GetPlayerRank(string playerName)
        {
            List<Clan> clans = ClanDBService.GetAllClans();
            if (clans != null)
            {
                foreach (Clan clan in clans)
                {
                    foreach (ClanMember member in clan.Members)
                    {
                        if (member.playerName.Equals(playerName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return member.Rank;
                        }
                    }
                }
                return ClanRank.None;
            }
            return ClanRank.None;
        }


        public static bool IsPlayerInClan(string clanName, string playerName)
        {
            Clan? clan = ClanDBService.GetClan(clanName, out string errMsgGetClan);
            if (clan != null)
            {
                foreach (ClanMember member in clan.Members)
                {
                    if (member.playerName.Equals(playerName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }


        public static bool IsPlayerInAnyClan(string playerName, out string clanName)
        {
            clanName = "";

            List<Clan> clans = ClanDBService.GetAllClans();
            if (clans != null)
            {
                foreach (Clan clan in clans)
                {
                    foreach (ClanMember member in clan.Members)
                    {
                        if (member.playerName.Equals(playerName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            clanName = clan.Name;
                            return true;
                        }
                    }
                }
                return false;
            }
            return false;
        }


        public static string GetClanName(string playerName)
        {

            List<Clan> clans = ClanDBService.GetAllClans();
            if (clans != null)
            {
                foreach (Clan clan in clans)
                {
                    foreach (ClanMember member in clan.Members)
                    {
                        if (member.playerName.Equals(playerName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return clan.Name;
                        }
                    }
                }
                return "";
            }
            return "";
        }


        public static void CommandGetClanMemberList(Character ch, string arguments)
        {
            if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs))
            {
                Clan? clan = ClanDBService.GetClan(clanName, out string errMsgGetClan);
                if (clan != null)
                {
                    string returnInfo = $"\\rClan\\x : {clan.Name} [{clan.Tag}]\n";
                    string leaderInfo = "";
                    string captainInfo = "";
                    string lieutenantInfo = "";
                    string memberInfo = "";
                    string greenHornInfo = "";

                    foreach (ClanMember member in clan.Members)
                    {
                        if (member.Rank == ClanRank.Leader)
                            leaderInfo += $"{member.playerName,-30} | \\y(***)\\xLeader\\y(***)\\x\n";

                        if (member.Rank == ClanRank.Captain)
                            captainInfo += $"{member.playerName,-30} | \\r(<**)\\xCaptain\\r(**>)\\x\n";

                        if (member.Rank == ClanRank.Lieutenant)
                            lieutenantInfo += $"{member.playerName,-30} | \\c(<*)\\xLieutenant\\c(*>)\\x\n";

                        if (member.Rank == ClanRank.Member)
                            memberInfo += $"{member.playerName,-30} | \\b(<)\\xMember\\b(>)\\x\n";

                        if (member.Rank == ClanRank.GreenHorn)
                            greenHornInfo += $"{member.playerName,-30} | \\g(^)\\xGreen-horn\\g(^)\\x\n";

                    }

                    returnInfo += leaderInfo + captainInfo + lieutenantInfo + memberInfo + greenHornInfo;
                    ch.send(returnInfo + "\r\n");
                }
                else
                {
                    ch.send("No Clan by that name not found.\r\n");
                }
            }
            else
            {
                ch.send("No clan name was given. Type 'clan help'.\r\n");
            }
        }


        public static bool getClanLeader(string clanName, out string clanLeader)
        {
            clanLeader = "";
            foreach (Clan clan in ClanDBService.GetAllClans())
            {
                if (clan.Name.Equals(clanName, StringComparison.CurrentCultureIgnoreCase))
                {
                    clanLeader = clan.LeaderPlayerName;
                    return true;
                }
            }
            return false;
        }


        public static bool IsAClanLeader(string playerName)
        {

            foreach (Clan clan in ClanDBService.GetAllClans())
            {
                if (clan.LeaderPlayerName.Equals(playerName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }


        public static bool isClanNameUsed(string clanName)
        {
            List<Clan> clans = ClanDBService.GetAllClans();

            foreach (Clan clan in clans)
            {
                if (clan.Name.Equals(clanName, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }


        public static List<string> getAllExistingPlayerNames()
        {
            List<string> players = new List<string>();
            foreach (string filePath in Directory.EnumerateFiles(GameSettings.PlayersDataFolder))
            {
                string fileName = Path.GetFileName(filePath);
                string name = GetSubstringBeforeDot(fileName);
                players.Add(name);
            }
            return players;
        }


        public static string GetSubstringBeforeDot(string input)
        {
            int dotIndex = input.IndexOf('.');

            if (dotIndex > -1)
            {
                return input.Substring(0, dotIndex);
            }
            else
            {
                return input;
            }
        }


        public static void CommandClanRequestCreation(Character ch, string arguments)
        {
            string playerName = ch.Name;
            if (ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                ClanListRequests(ch);// list all the request for the admin player
            }
            else if (ch.Level >= GameSettings.MinLevelToAskForClanCreation)// create a clan request
            {
                if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs))
                {
                    if (Helper.getNextArg(remainingArgs, out string clanTag, out string remainingArgs_1))
                    {
                        if (IsPlayerInAnyClan(ch.Name, out string outClanName))
                        {
                            ch.send("You are part of a clan already. You must leave your current clan first.");
                            return;
                        }

                        if (!HasClanRequest(playerName))
                        {
                            ClanCreationRequest(ch, playerName, clanName, clanTag);
                        }
                        else
                        {
                            ch.send("You already have a request for \\rclan creation\\x. Remove the old one before making a new request.");
                        }
                    }
                    else
                    {
                        ch.send("No \\rclan tag\\x was given. Type \\rclan help\\x.");
                    }
                }
                else
                {
                    ch.send("No \\rclan name\\x was given. Type \\rclan help\\x.");
                }
            }
            else
            {
                ch.send($"You have to be level {GameSettings.MinLevelToAskForClanCreation} or higher to make a request.");
            }
        }

        private static void ClanCreationRequest(Character ch, string playerName, string clanName, string clanTag)
        {
            ClanCreationRequest request = new ClanCreationRequest();

            request.playerName = playerName;
            request.clanName = clanName;
            request.clanTag = clanTag;

            ClanDBService.addClanRequest(request);
            ClanDBService.WriteToFileClanCreationRequests(out string errMsgWriteToClanCreationRequests);

            ch.send("Your request has been send.\r\n");
        }


        private static void ClanListRequests(Character ch)
        {
            List<ClanCreationRequest> requests = ClanDBService.getAllClanRequests();

            if (requests.Count == 0)
            {
                ch.send("No \\rrequests\\x were found.\r\n");
                return;
            }

            string ret = $"\\r{"Playername",-20}\\x|\\r{"Clan name requested",-30}\\x|\\r{"Clan tag requested",-30}\\x\n";
            foreach (ClanCreationRequest request in requests)
            {
                ret += $"{request.playerName,-20}|{request.clanName,-30}|{request.clanTag,-30}\n";
            }
            ch.send(ret + "\r\n");
        }


        public static bool HasClanRequest(string playerName)
        {
            List<ClanCreationRequest> requests = ClanDBService.getAllClanRequests();

            foreach (ClanCreationRequest request in requests)
            {
                if (request.playerName.Equals(playerName, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }


        public static void CommandDeleteClanRequest(Character ch, string arguments)
        {
            if (ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                if (Helper.getNextArg(arguments, out string playerName, out string remainingArgs))
                {
                    if (HasClanRequest(playerName))
                    {
                        deleteClanRequest(ch, playerName);
                    }
                    else
                    {
                        ch.send($"No clan request for \\r{playerName}\\x was found.\r\n");
                    }
                }
                else
                {
                    ch.send("No player name was given. Type \\rclan help\\x\r\n");
                }
            }
            else
            {
                if (HasClanRequest(ch.Name))
                {
                    deleteClanRequest(ch, ch.Name);
                }
                else
                {
                    ch.send("You do not have any \\rclan creation\\x requests.\r\n");
                }
            }

        }

        private static void deleteClanRequest(Character ch, string playerName)
        {
            List<ClanCreationRequest> requests = ClanDBService.getAllClanRequests();

            foreach (ClanCreationRequest request in requests)
            {
                if (request.playerName.Equals(playerName, StringComparison.CurrentCultureIgnoreCase))
                {
                    ClanDBService.removeClanRequest(request);
                    ClanDBService.WriteToFileClanCreationRequests(out string errMsgWriteToClanCreationRequests);
                    ch.send("Clan creation request removed.");
                    return;
                }
            }
            ch.send("No request found.\r\n");
        }


        public static void CommandCreateClanRoom(Character ch, string arguments)
        {
            if (!(ch.Level >= GameSettings.MinLevelRequiredForClanCreation))
            {
                ch.send("You cannot assign clan rooms.\r\n");
            }


            if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs))
            {
                var clan = ClanDBService.GetClan(clanName, out string errorMessage);
                if(clan == null)
                {
                    ch.send($"Error retrieving clan: {errorMessage}\r\n");
                    return;
                }
                else if (Helper.getNextArg(remainingArgs, out string vNum, out string remainingArgs_1))
                {
                    bool success = int.TryParse(vNum, out int vNumber);
                    if (!success)
                    {
                        ch.send("That is not a number. Type 'clan help'.\r\n");
                        return;
                    }

                    if (!HasClanRoom(vNumber))
                    {
                        ClanRoom room = new ClanRoom();

                        room.RoomVnum = vNumber;
                        room.ClanName = clanName;

                        ClanDBService.addClanRoom(room);
                        ClanDBService.WriteToFileClanRooms(out string errMsgWriteToClanRooms);
                        ch.send("Clan room has been added.\r\n");
                        if(RoomData.Rooms.TryGetValue(vNumber, out RoomData? cslRoom) && cslRoom != null)
                        {
                            cslRoom.Variables["ClanRoom"] = room;
                            cslRoom.Variables["Clan"] = clan;
                        }
                    }
                    else
                    {
                        ch.send("Clan room already exists.\r\n");
                    }
                }
                else
                {
                    ch.send("No room vnum was given. Type 'clan help'.\r\n");
                }
            }
            {
                ch.send("No arguments where given. Type 'clan help'.\r\n");
            }
        }


        private static bool HasClanRoom(int roomVNum)
        {
            List<ClanRoom> rooms = ClanDBService.getAllClanRooms();

            foreach (ClanRoom room in rooms)
            {
                if (room.RoomVnum == roomVNum)
                    return true;
            }
            return false;
        }


        public static void CommandDeleteClanRoom(Character ch, string arguments)
        {
            if (!(ch.Level >= GameSettings.MinLevelRequiredForClanCreation))
            {
                ch.send("You cannot remove clan rooms.\r\n");
            }

            if (Helper.getNextArg(arguments, out string vNum, out string remainingArgs))
            {
                bool success = int.TryParse(vNum, out int vNumber);
                if (!success)
                {
                    ch.send("That is not a number. Type 'clan help'.\r\n");
                    return;
                }

                if (HasClanRoom(vNumber))
                {
                    List<ClanRoom> rooms = ClanDBService.getAllClanRooms();

                    foreach (ClanRoom room in rooms)
                    {
                        if (room.RoomVnum == vNumber)
                        {
                            if(RoomData.Rooms.TryGetValue(vNumber, out RoomData? cslRoom) && cslRoom != null)
                            {
                                cslRoom.Variables.Remove("ClanRoom");
                                cslRoom.Variables.Remove("Clan");
                            }
                            ClanDBService.removeClanRoom(room);
                            ClanDBService.WriteToFileClanRooms(out string errMsgWriteToClanRooms);
                            ch.send("Clan room has been removed.");
                            return;
                        }
                    }
                }
                else
                {
                    ch.send("This clan room is not part of the clan room list.\r\n");
                }
            }
            else
            {
                ch.send("No vnum was given. Type 'clan help'.\r\n");
            }
        }


        public static void CommandListClanRooms(Character ch, string arguments)
        {
            if (!(ch.Level >= GameSettings.MinLevelRequiredForClanCreation))
            {
                ch.send("You cannot list clan rooms.\r\n");
            }

            if (Helper.getNextArg(arguments, out string clanName, out string remainingArgs))
            {
                ListAllClanRoomsForClanName(ch, clanName);
            }
            else
            {
                ListAllClanRooms(ch);
            }
        }


        private static void ListAllClanRoomsForClanName(Character ch, string clanName)
        {
            if (ClanDBService.getNumberOfClanRooms() > 0)
            {
                string ret = $"{"vNum",-10}|{"Belongs to",-30}\n";
                foreach (ClanRoom room in ClanDBService.getAllClanRooms())
                {
                    if (room.ClanName.Equals(clanName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        ret += $"{room.RoomVnum,10}|{room.ClanName,30}\n";
                    }
                }
                ch.send(ret + "\r\n");
            }
            else
            {
                ch.send("No clan rooms were found.\r\n");
            }
        }


        private static void ListAllClanRooms(Character ch)
        {
            string ret = $"{"vNum",10}|{"Belongs to",30}\n";
            foreach (ClanRoom room in ClanDBService.getAllClanRooms())
            {
                ret += $"{room.RoomVnum,10}|{room.ClanName,30}\n";
            }
            ch.send(ret + "\r\n");
        }

        public static void CommandStartVote(Character ch, string arguments)
        {
            if (!IsAClanLeader(ch.Name))
            {
                ch.send("Only the clan leader can start a vote for a new leader.\r\n");
                return;
            }

            string clanName = GetClanName(ch.Name);
            Clan? clan = ClanDBService.GetClan(clanName, out _);

            if (clan == null)
            {
                ch.send("Clan not found.\r\n");
                return;
            }
            
            if (clan.IsVotingActive)
            {
                ch.send($"A vote is already in progress. It will end on {(clan.VotingEnds == null ? "an unknown date" : clan.VotingEnds.Value.ToString("g"))}.\r\n");
                return;
            }

            clan.VotingEnds = DateTime.Now.AddDays(2);
            clan.Votes.Clear();
            ClanDBService.WriteToFileClans(out _);

            ch.send($"You have started a vote for a new clan leader. Captains have 2 days to cast their vote with 'clan vote <player>'.\r\n");
            
            // Announce to online clan members
            var onlinePlayers = Game.Instance.Info.Connections.Where(p => p.state == Player.ConnectionStates.Playing);
            foreach (var player in onlinePlayers)
            {
                if (IsPlayerInClan(clanName, player.Name) && player != ch)
                {
                    player.send($"\\y{ch.Name} has initiated a vote for a new clan leader! Voting is open for 2 days.\\x\r\n");
                }
            }
        }

        public static void CommandVote(Character ch, string arguments)
        {
            string clanName = GetClanName(ch.Name);
            if (string.IsNullOrEmpty(clanName))
            {
                ch.send("You are not in a clan.\r\n");
                return;
            }

            Clan? clan = ClanDBService.GetClan(clanName, out _);

            if (clan == null)
            {
                ch.send("Clan not found.\r\n");
                return;
            }
            else if (!clan.IsVotingActive)
            {
                ch.send("There is no active vote for a new leader at this time.\r\n");
                return;
            }

            if (GetPlayerRank(ch.Name) != ClanRank.Captain)
            {
                ch.send("Only captains can vote for a new leader.\r\n");
                return;
            }

            if (!Helper.getNextArg(arguments, out string candidateName, out _))
            {
                ch.send("Who do you want to vote for? Usage: clan vote <player>\r\n");
                return;
            }

            if (!IsPlayerInClan(clanName, candidateName))
            {
                ch.send($"'{candidateName}' is not a member of your clan.\r\n");
                return;
            }
            
            var existingVote = clan.Votes.FirstOrDefault(v => v.VoterName.Equals(ch.Name, StringComparison.OrdinalIgnoreCase));
            if (existingVote != null)
            {
                existingVote.CandidateName = candidateName;
                ch.send($"You have changed your vote to {candidateName}.\r\n");
            }
            else
            {
                clan.Votes.Add(new ClanVote { VoterName = ch.Name, CandidateName = candidateName });
                ch.send($"You have voted for {candidateName} to be the new leader.\r\n");
            }

            ClanDBService.WriteToFileClans(out _);
        }


        //--- This will both check the current status of the voting process and process the results
        //--- when the period has run out.
        public static void CommandTally(Character ch, string arguments)
        {
            string clanName = GetClanName(ch.Name);
            if (string.IsNullOrEmpty(clanName))
            {
                ch.send("You are not in a clan.\r\n");
                return;
            }

            Clan? clan = ClanDBService.GetClan(clanName, out _);
            if (clan == null)
            {
                ch.send("Could not find your clan data.\r\n");
                return;
            }

            // If voting is currently active, show the tally.
            if (clan.IsVotingActive)
            {
                ch.send($"A vote for a new leader is in progress. It ends on {(clan.VotingEnds == null ? "an unknown date" : clan.VotingEnds.Value.ToString("g"))}.\r\n");
                if (clan.Votes.Count == 0)
                {
                    ch.send("No votes have been cast yet.\r\n");
                    return;
                }

                var tally = clan.Votes
                    .GroupBy(v => v.CandidateName, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new { Candidate = group.Key, Count = group.Count() })
                    .OrderByDescending(x => x.Count);

                string result = "Current votes:\n";
                foreach (var entry in tally)
                {
                    result += $"{entry.Candidate}: {entry.Count} vote(s)\n";
                }
                ch.send(result + "\r\n");
            }
            // If voting is not active, but was (VotingEnds is in the past), process the results.
            else if (clan.VotingEnds.HasValue)
            {
                ProcessVoteResults(ch, clan);
            }
            // Otherwise, no vote is happening.
            else
            {
                ch.send("There is no active vote for a new leader at this time.\r\n");
            }
        }

        private static void ProcessVoteResults(Character ch, Clan? clan)
        {
            if(clan == null)
            {
                ch.send("Clan data is not available.\r\n");
                return;
            }

            if (clan.Votes.Count == 0)
            {
                ch.send("Voting has ended. No votes were cast.\r\n");
                clan.VotingEnds = null;
                ClanDBService.WriteToFileClans(out _);
                return;
            }

            var tally = clan.Votes
                .GroupBy(v => v.CandidateName, StringComparer.OrdinalIgnoreCase)
                .Select(group => new { Candidate = group.Key, Count = group.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var winner = tally.FirstOrDefault();
            var isTie = tally.Count > 1 && tally[0].Count == tally[1].Count;

            if (winner == null)
            {
                ch.send("An error occurred: No winning candidate found.\r\n");
            }
            else if (isTie)
            {
                ch.send("Voting has ended in a tie! No new leader has been chosen.\r\n");
            }
            else
            {
                string oldLeaderName = clan.LeaderPlayerName;
                ClanMember? oldLeaderMember = clan.Members.FirstOrDefault(m => m.playerName.Equals(oldLeaderName, StringComparison.OrdinalIgnoreCase));
                ClanMember? newLeaderMember = clan.Members.FirstOrDefault(m => m.playerName.Equals(winner.Candidate, StringComparison.OrdinalIgnoreCase));

                if (newLeaderMember == null)
                {
                    ch.send("An error occurred: The winning candidate is no longer in the clan.\r\n");
                }
                else
                {
                    // Demote old leader
                    if (oldLeaderMember != null)
                    {
                        oldLeaderMember.Rank = ClanRank.Captain;
                    }

                    // Promote new leader
                    newLeaderMember.Rank = ClanRank.Leader;
                    clan.LeaderPlayerName = newLeaderMember.playerName;

                    string announcement = $"\\yThe vote for a new leader has concluded! {newLeaderMember.playerName} is the new leader of {clan.Name}!\\x\r\n";
                    ch.send(announcement);
                    // Announce to online clan members
                    var onlinePlayers = Game.Instance.Info.Connections.Where(p => p.state == Player.ConnectionStates.Playing);
                    foreach (var player in onlinePlayers)
                    {
                        if (IsPlayerInClan(clan.Name, player.Name) && player != ch)
                        {
                            player.send(announcement);
                        }
                    }
                }
            }

            // Reset voting
            clan.VotingEnds = null;
            clan.Votes.Clear();
            ClanDBService.WriteToFileClans(out _);
        }

        public static void CommandHelp(Character ch, string arguments)
        {
            string clanHelpAdminPlayer = "";
            string clanHelpNonAdminPlayer = "";
            string clanHelpLeaderPlayer = "";

            // Only send this help list to 'Imm|Admins'
            if (ch.Level >= GameSettings.MinLevelRequiredForClanCreation)
            {
                clanHelpAdminPlayer = $"\\rHere follows the clan commands for Admin players.\\x\n" +
                                        $"\\g{"list",-30}\\x| {"List all clans.",-100}\n" +
                                        $"\\g{"members",-30}\\x| {"List the members with their ranks of a specified clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan members 'clan name'",-100}\n" +

                                        $"\\g{"add-clan",-30}\\x| {"Create a new clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan add-clan 'clan name' 'clan leader' 'clan tag'",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan add-clan 'clan name'",-100}\n" +
                                        $"\\g{"rem-clan",-30}\\x| {"Delete|remove a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan rem-clan 'clan name'",-100}\n" +
                                        $"\\g{"set-tag",-30}\\x| {"Set or update a clan tag.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan set-tag 'clan name' 'clan tag'",-100}\n" +
                                        $"\\g{"update-name",-30}\\x| {"Update|Change the name of a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan update-name 'clan name' 'new clan name'",-100}\n" +

                                        $"\\g{"add-member",-30}\\x| {"Add a new player to a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan add-member 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"rem-member",-30}\\x| {"Remove a player from a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan rem-member 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"promote",-30}\\x| {"Promote a player in a clan.",-100}\n" +
                                        $"{"",-30}| {"Repeat this call untill the player reaches the required rank.",-100}\n" +

                                        $"\\g{"startvote",-30}\\x| {"Start a vote for a new clan leader.",-100}\n" +
                                        $"\\g{"vote",-30}\\x| {"Vote for a new clan leader (Captains only).",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan vote 'player name'",-100}\n" +
                                        $"\\g{"tally",-30}\\x| {"Check the status of an ongoing vote or see the results.",-100}\n" +

                                        $"{"",-30}| {"\\rUsage :\\x clan promote 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"demote",-30}\\x| {"Demote a player in a clan.",-100}\n" +
                                        $"{"",-30}| {"Repeat this call untill the player reaches the required rank.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan demote 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"member-info",-30}\\x| {"Get the rank of a player.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan member-info 'player name'",-100}\n" +

                                        $"\\g{"request",-30}\\x| {"Get all the current clan creation requests.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan request",-100}\n" +
                                        $"\\g{"del-request",-30}\\x| {"Delete|remove a clan creation request of a player.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan del-request 'player name'",-100}\n" +

                                        $"\\g{"clan-rooms",-30}\\x| {"List all the clan assigned rooms.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan clan-rooms",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan clan-rooms 'clan name'",-100}\n" +
                                        $"\\g{"add-room",-30}\\x| {"Assign a room to a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan add-room 'clan name' 'room vNumber'",-100}\n" +
                                        $"\\g{"rem-room",-30}\\x| {"Remove|Unassign a clan room from any clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan rem-room 'room vNumber'",-100}\n";

                ch.send(clanHelpAdminPlayer + "\r\n");
            }
            // Only send this help list to 'clan leader' ranked players
            else if (ClanService.GetPlayerRank(ch.Name) == ClanRank.Leader)
            {
                clanHelpLeaderPlayer = $"\\rHere follows the clan commands for players of rank Leader.\\x\n" +
                                        $"\\g{"list",-30}\\x| {"List all clans.",-100}\n" +
                                        $"\\g{"members",-30}\\x| {"List the members with their ranks of a specified clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan members 'clan name'",-100}\n" +
                                        $"\\g{"set-tag",-30}\\x| {"Set or update a clan tag.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan set-tag 'clan name' 'clan tag'",-100}\n" +
                                        $"\\g{"update-name",-30}\\x| {"Update|Change the name of a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan update-name 'clan name' 'new clan name'",-100}\n" +
                                        $"\\g{"add-member",-30}\\x| {"Add a new player to a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan add-member 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"rem-member",-30}\\x| {"Remove a player from a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan rem-player 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"promote",-30}\\x| {"Promote a player in a clan.",-100}\n" +
                                        $"{"",-30}| {"Repeat this call untill the player reaches the required rank.",-100}\n" +

                                        $"\\g{"startvote",-30}\\x| {"Start a vote for a new clan leader.",-100}\n" +
                                        $"\\g{"vote",-30}\\x| {"Vote for a new clan leader (Captains only).",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan vote 'player name'",-100}\n" +
                                        $"\\g{"tally",-30}\\x| {"Check the status of an ongoing vote or see the results.",-100}\n" +

                                        $"{"",-30}| {"\\rUsage :\\x clan promote 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"demote",-30}\\x| {"Demote a player in a clan.",-100}\n" +
                                        $"{"",-30}| {"Repeat this call untill the player reaches the required rank.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan demote 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"member-info",-30}\\x| {"Get the rank of a player.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan member-info 'player name'",-100}\n";

                ch.send(clanHelpLeaderPlayer);
            }
            // Only send this help list to clan members of rank 'captain'
            else if (ClanService.GetPlayerRank(ch.Name) == ClanRank.Leader - 1)
            {
                clanHelpLeaderPlayer = $"\\rHere follows the clan commands for players of rank Captain.\\x\n" +
                                        $"\\g{"list",-30}\\x| {"List all clans.",-100}\n" +
                                        $"\\g{"members",-30}\\x| {"List the members with their ranks of a specified clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan members 'clan name'",-100}\n" +
                                        $"\\g{"add-member",-30}\\x| {"Add a new player to a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan add-member 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"rem-member",-30}\\x| {"Remove a player from a clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan rem-member 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"promote",-30}\\x| {"Promote a player in a clan.",-100}\n" +
                                        $"{"",-30}| {"Repeat this call untill the player reaches the required rank.",-100}\n" +

                                        $"\\g{"tally",-30}\\x| {"Check the status of an ongoing vote.",-100}\n" +
                                        $"\\g{"vote",-30}\\x| {"Vote for a new clan leader.",-100}\n" +

                                        $"{"",-30}| {"\\rUsage :\\x clan promote 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"demote",-30}\\x| {"Demote a player in a clan.",-100}\n" +
                                        $"{"",-30}| {"Repeat this call untill the player reaches the required rank.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan demote 'clan name' 'player name'",-100}\n" +
                                        $"\\g{"member-info",-30}\\x| {"Get the rank of a player.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan member-info 'player name'",-100}\n";

                ch.send(clanHelpLeaderPlayer);
            }
            else
            // Send this help list to any remaining 'ranked' players and 'non clan' members
            {
                clanHelpNonAdminPlayer = $"\\rClan Help.\\x\n" +
                                        $"\\g{"list",-30}\\x| {"List all clans.",-100}\n" +
                                        $"\\g{"members",-30}\\x| {"List the members with their ranks of a specified clan.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan members 'clan name'",-100}\n" +
                                        $"\\g{"member-info",-30}\\x| {"Get the rank of a player.",-100}\n" +
                                        $"\\g{"tally",-30}\\x| {"Check the status of an ongoing vote.",-100}\n" +

                                        $"{"",-30}| {"\\rUsage :\\x clan member-info 'player name'",-100}\n" +
                                        $"\\g{"request",-30}\\x| {"Request the immortals to create a clan for you.",-100}\n" +
                                        $"{"",-30}| {"You will have to be level 30 and above to do so.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan request 'clan name' 'clan tag'",-100}\n" +
                                        $"\\g{"del-request",-30}\\x| {"Delete|remove your clan creation request.",-100}\n" +
                                        $"{"",-30}| {"\\rUsage :\\x clan del-request",-100}\n";

                ch.send(clanHelpNonAdminPlayer);
            }
        }

        ///--- Logic for ClanSystem.cs
        public static void OnCharacterEnterRoom(CrimsonStainedLands.Character character, RoomData oldRoom, RoomData newRoom)
        {

            //--- Lets make sure that the current player does have a clan member object.
            ClanMember clanMember = character.GetVariable<ClanMember>("ClanMember");
            if (clanMember == null)
            {
                clanMember = addClanMemberObject(character);
            }

            //--- Lets make sure that the current room has a clan room object.
            ClanRoom clanRoom = newRoom.GetVariable<ClanRoom>("ClanRoom");
            if (clanRoom == null)
            {
                clanRoom = addClanRoomObject(newRoom);
            }

            //--- Make sure only clan members can exist|roam their respective clan rooms. Remove all other 
            //--- players or characters that does not belong to that clan.  
            if (clanRoom.ClanName != "" && clanRoom.RoomVnum != -1)
            {
                bool notAllowed = false;
                if (clanMember.ClanName == "")
                {
                    notAllowed = true;
                }
                if (!clanRoom.ClanName.Equals(clanMember.ClanName, StringComparison.CurrentCultureIgnoreCase))
                {
                    notAllowed = true;
                } 
                
                if (notAllowed)
                {
                    character.Act($"A \\cmagical\\x force surrounds {character.Name} teleporting him away!", type: ActType.ToRoom);
                    character.RemoveCharacterFromRoom();
                    character.AddCharacterToRoom(oldRoom);
                    character.Act($"a \\cmagical\\x force has teleported {character.Name} here for entering a clan members only room!", type: ActType.ToRoom);
                    character.send("You have been teleported! You cannot enter a clan room if you are not a member of that clan!\r\n");
                }
            }
        }


        public static void OnCharacterLoading(CrimsonStainedLands.Character character, XElement element)
        {
            //---Attach clan member object to player, does not save to file when character is saved.
            ClanMember member = new ClanMember();
            if (ClanService.IsPlayerInAnyClan(character.Name, out string clanName))
            {
                var clan = ClanDBService.GetClan(clanName, out string errMsg);
                member.playerName = character.Name;
                member.ClanName = clanName;
                member.Rank = ClanService.GetPlayerRank(character.Name);
                character.Variables["ClanMember"] = member;
                character.Variables["Clan"] = clan;
            }
            else
            {
                ClanMember? nullMember = addClanMemberObject(character);
            }
        }


        public static void OnDataLoaded()
        {
            // A helper function to handle loading errors consistently.
            bool HandleLoadError(string errorMessage, string systemName)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    Console.WriteLine(errorMessage);
                    Console.WriteLine($"[{systemName}] has been disabled due to a loading error.");
                    return false;
                }
                return true;
            }


            //--- Initialize Clan System
            ClanDBService.EnsureFileExists(out string errMsgEnsureFile);
            GameSettings.ClanSystemEnabled = HandleLoadError(errMsgEnsureFile, "Clan System");

            if (GameSettings.ClanSystemEnabled)
            {
                using (new LoadTimer("ClanSystem Service loaded {0} clans", () => ClanDBService.GetNumberOfClans()))
                {
                    ClanDBService.ReadFromFileClans(out string errMsgReadClans);
                    GameSettings.ClanSystemEnabled = HandleLoadError(errMsgReadClans, "Clan System");
                }
            }

            if (GameSettings.ClanSystemEnabled)
            {
                using (new LoadTimer("ClanSystem Service loaded {0} clan rooms", () => ClanDBService.getNumberOfClanRooms()))
                {
                    ClanDBService.ReadFromFileClanRooms(out string errMsgReadClanRooms);
                    GameSettings.ClanSystemEnabled = HandleLoadError(errMsgReadClanRooms, "Clan System");
                }
            }

            //--- Attach clan room object to rooms, does not save to file when roomdata object is saved.
            using (new LoadTimer("ClanSystem attached clan room object to rooms."))
            {
                if (GameSettings.ClanSystemEnabled)
                {
                    foreach (ClanRoom clanRoom in ClanDBService.getAllClanRooms())
                    {
                        // don't override clanroom if it already exists
                        if (RoomData.Rooms.TryGetValue(clanRoom.RoomVnum, out RoomData? room) && !room.Variables.ContainsKey("ClanRoom"))
                        {
                            room.Variables["ClanRoom"] = clanRoom;
                            room.Variables["Clan"] = ClanDBService.GetClan(clanRoom.ClanName, out string errMsg);
                        }
                    }
                }
            }
        }


        public static ClanRoom addClanRoomObject(RoomData room)
        {
            ClanRoom clanRoom = new ClanRoom();
            clanRoom.RoomVnum = -1;
            clanRoom.ClanName = "";
            room.Variables["ClanRoom"] = clanRoom;
            room.Variables["Clan"] = null;
            return clanRoom;
        }


        public static ClanMember addClanMemberObject(CrimsonStainedLands.Character character)
        {
            ClanMember clanMember = new ClanMember();
            clanMember.playerName = character.Name;
            clanMember.ClanName = "";
            clanMember.Rank = ClanRank.None;
            character.Variables["ClanMember"] = clanMember;
            return clanMember;
        }


        public static void findAndAttachClanMemberObject(CrimsonStainedLands.Character character, Clan? clan, ClanMember clanMemberObject)
        {
            var onlinePlayers = Game.Instance.Info.Connections.Where(p => p.state > Player.ConnectionStates.GetName);

            // Iterate through the list of online players.
            foreach (var player in onlinePlayers)
            {
                if (character.Name == player.Name)
                {
                    if (!character.Variables.ContainsKey("ClanMember"))
                    {
                        addClanMemberObject(character);
                    }
                    character.Variables["ClanMember"] = clanMemberObject;
                    character.Variables["Clan"] = clan;
                    break;
                }
            }
        }

        private static Player? getConnectedPlayer(string playerName)
        {
            var onlinePlayers = Game.Instance.Info.Connections.Where(p => p.state == Player.ConnectionStates.Playing);
            foreach (var player in onlinePlayers)
            {
                if (player.Name.Equals(playerName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return player;
                }
            }
            return null;
        }
    }
}