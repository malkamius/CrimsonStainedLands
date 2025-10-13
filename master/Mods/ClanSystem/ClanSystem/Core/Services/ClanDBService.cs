using System.Xml;
using System.Xml.Serialization;
using FxSsh.Messages;

namespace ClanSystemMod
{
    public static class ClanDBService
    {
        private static readonly List<Clan> _clans = [];
        private static readonly List<ClanRoom> _clanRooms = [];
        private static readonly List<ClanCreationRequest> _clanCreationRequets = [];
        //private static readonly List<PvpEnabledRoom> _pvpEnabledRooms = [];



        //---Clan functions
        public static bool addClan(Clan clan, out string errMsg)
        {
            errMsg = "";
            if (!ClanExists(clan.Name))
            {
                _clans.Add(clan);
                return true;
            }
            else
            {
                errMsg = "This clan already exists.";
                return false;
            }
        }

        public static bool removeClan(string clanName, out string errMsg)
        {
            errMsg = "";
            int removedCount = _clans.RemoveAll(c => c.Name.Equals(clanName, StringComparison.CurrentCultureIgnoreCase));
            if (removedCount > 0) return true;
            errMsg = "No clan by that name was found.";
            return false;
        }


        public static bool UpdateClanRecord(Clan clan, out string errMsg)
        {
            errMsg = "";
            foreach (Clan clanEntry in _clans)
            {
                if (clanEntry.Name.Equals(clan.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    _clans.Remove(clanEntry);
                    _clans.Add(clan);
                    return true;
                }
            }
            errMsg = "That clan does not exist.";
            return false;
        }


        public static Clan? GetClan(string clanName, out string errMsg)
        {
            errMsg = "";
            var clan = _clans.FirstOrDefault(c => c.Name.Equals(clanName, StringComparison.CurrentCultureIgnoreCase));
            if (clan == null)
                errMsg = "No Clan by that name found.";
            return clan;
        }


        public static Clan? GetClanByPlayerName(string playerName, out string errMsg)
        {
            errMsg = "";
            var clan = _clans.FirstOrDefault(c => c.Members.Any(m => m.playerName.Equals(playerName, StringComparison.CurrentCultureIgnoreCase)));

            if (clan == null)
                errMsg = "No clans have that player as a member.";
            return clan;
        }


        public static bool ClanExists(string clanName)
        {
            foreach (Clan clan in _clans)
            {
                if (clan.Name.Equals(clanName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }


        public static List<Clan> GetAllClans()
        {
            return _clans;
        }


        public static int GetNumberOfClans()
        {
            return _clans.Count;
        }


        public static bool WriteToFileClans(out string errMsg)
        {
            errMsg = "";
            try
            {
                var serializer = new XmlSerializer(typeof(List<Clan>));

                using (var writer = new StreamWriter(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemDataFile)))
                {
                    serializer.Serialize(writer, new List<Clan>(_clans));
                }
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                errMsg += $"Error: Access to the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemDataFile)}' is denied.\n{ex.Message}";

            }
            catch (DirectoryNotFoundException ex)
            {
                errMsg += $"Error: The directory for the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemDataFile)}' was not found.{ex.Message}";
            }
            catch (IOException ex)
            {
                errMsg += $"Error: A file I/O error occurred while writing to '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemDataFile)}'.\n{ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                errMsg += $"Error: An XML serialization error occurred.\n";
                // Check the inner exception for the real cause
                if (ex.InnerException != null)
                {
                    errMsg += $"Inner Exception: {ex.InnerException.Message}";
                }
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                errMsg += $"An unexpected error occurred during serialization: {ex.Message}";
            }
            return false;
        }


        public static bool ReadFromFileClans(out string errMsg)
        {
            errMsg = "";
            try
            {
                var serializer = new XmlSerializer(typeof(List<Clan>));
                List<Clan>? readList = new List<Clan>();
                using (var reader = new StreamReader(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemDataFile)))
                {
                    readList = (List<Clan>?)serializer.Deserialize(reader);
                }

                _clans.Clear();
                if (readList != null)
                {
                    foreach (Clan clan in readList)
                    {
                        addClan(clan, out string errMsgAddClan);
                    }
                }
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                errMsg += $"Error: Access to the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemDataFile)}' is denied.\n{ex.Message}";

            }
            catch (DirectoryNotFoundException ex)
            {
                errMsg += $"Error: The directory for the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemDataFile)}' was not found.{ex.Message}";
            }
            catch (IOException ex)
            {
                errMsg += $"Error: A file I/O error occurred while writing to '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemDataFile)}'.\n{ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                errMsg += $"Error: An XML serialization error occurred.\n";
                // Check the inner exception for the real cause
                if (ex.InnerException != null)
                {
                    errMsg += $"Inner Exception: {ex.InnerException.Message}";
                }
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                errMsg += $"An unexpected error occurred during serialization: {ex.Message}";
            }
            return false;
        }

        //--- End clan functions


        //--- Clan rooms functions

         public static void addClanRoom(ClanRoom clanRoom)
        {
            _clanRooms.Add(clanRoom);
        }

        public static void removeClanRoom(ClanRoom clanRoom)
        {
            _clanRooms.Remove(clanRoom);
        }

        public static List<ClanRoom> getAllClanRooms()
        {
            return _clanRooms;
        }

        public static int getNumberOfClanRooms()
        {
            return _clanRooms.Count;
        }


        public static bool WriteToFileClanRooms(out string errMsg)
        {
            errMsg = "";
            try
            {
                var serializer = new XmlSerializer(typeof(List<ClanRoom>));

                using (var writer = new StreamWriter(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanRoomsFile)))
                {
                    serializer.Serialize(writer, new List<ClanRoom>(_clanRooms));
                }
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                errMsg += $"Error: Access to the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanRoomsFile)}' is denied.\n{ex.Message}";

            }
            catch (DirectoryNotFoundException ex)
            {
                errMsg += $"Error: The directory for the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanRoomsFile)}' was not found.{ex.Message}";
            }
            catch (IOException ex)
            {
                errMsg += $"Error: A file I/O error occurred while writing to '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanRoomsFile)}'.\n{ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                errMsg += $"Error: An XML serialization error occurred.\n";
                // Check the inner exception for the real cause
                if (ex.InnerException != null)
                {
                    errMsg += $"Inner Exception: {ex.InnerException.Message}";
                }
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                errMsg += $"An unexpected error occurred during serialization: {ex.Message}";
            }
            return false;
        }


        public static bool ReadFromFileClanRooms(out string errMsg)
        {
            errMsg = "";
            try
            {
                var serializer = new XmlSerializer(typeof(List<ClanRoom>));
                List<ClanRoom>? readList = new List<ClanRoom>();
                using (var reader = new StreamReader(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanRoomsFile)))
                {
                    readList = (List<ClanRoom>?)serializer.Deserialize(reader);
                }

                _clanCreationRequets.Clear();
                if (readList != null)
                {
                    foreach (ClanRoom room in readList)
                    {
                        addClanRoom(room);
                    }
                }
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                errMsg += $"Error: Access to the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanRoomsFile)}' is denied.\n{ex.Message}";

            }
            catch (DirectoryNotFoundException ex)
            {
                errMsg += $"Error: The directory for the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanRoomsFile)}' was not found.{ex.Message}";
            }
            catch (IOException ex)
            {
                errMsg += $"Error: A file I/O error occurred while writing to '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanRoomsFile)}'.\n{ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                errMsg += $"Error: An XML serialization error occurred.\n";
                // Check the inner exception for the real cause
                if (ex.InnerException != null)
                {
                    errMsg += $"Inner Exception: {ex.InnerException.Message}";
                }
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                errMsg += $"An unexpected error occurred during serialization: {ex.Message}";
            }
            return false;
        }

        //--- End Clan rooms functions


        //--- Clan creation requests functions
         public static void addClanRequest(ClanCreationRequest request)
        {
            _clanCreationRequets.Add(request);
        }


        public static void removeClanRequest(ClanCreationRequest request)
        {
            _clanCreationRequets.Remove(request);
        }


        public static List<ClanCreationRequest> getAllClanRequests()
        {
            return _clanCreationRequets;
        }


        public static int getNumberOfClanRequests()
        {
            return _clanCreationRequets.Count;
        }


        public static bool WriteToFileClanCreationRequests(out string errMsg)
        {
            errMsg = "";
            try
            {
                var serializer = new XmlSerializer(typeof(List<ClanCreationRequest>));

                using (var writer = new StreamWriter(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanCreationRequestFile)))
                {
                    serializer.Serialize(writer, new List<ClanCreationRequest>(_clanCreationRequets));
                }
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                errMsg += $"Error: Access to the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanCreationRequestFile)}' is denied.\n{ex.Message}";

            }
            catch (DirectoryNotFoundException ex)
            {
                errMsg += $"Error: The directory for the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanCreationRequestFile)}' was not found.{ex.Message}";
            }
            catch (IOException ex)
            {
                errMsg += $"Error: A file I/O error occurred while writing to '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanCreationRequestFile)}'.\n{ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                errMsg += $"Error: An XML serialization error occurred.\n";
                // Check the inner exception for the real cause
                if (ex.InnerException != null)
                {
                    errMsg += $"Inner Exception: {ex.InnerException.Message}";
                }
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                errMsg += $"An unexpected error occurred during serialization: {ex.Message}";
            }
            return false;
        }


        public static bool ReadFromFileClanCreationRequests(out string errMsg)
        {
            errMsg = "";
            try
            {
                var serializer = new XmlSerializer(typeof(List<ClanCreationRequest>));
                List<ClanCreationRequest>? readList = new List<ClanCreationRequest>();
                using (var reader = new StreamReader(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanCreationRequestFile)))
                {
                    readList = (List<ClanCreationRequest>?)serializer.Deserialize(reader);
                }

                _clanCreationRequets.Clear();
                if (readList != null)
                {
                    foreach (ClanCreationRequest request in readList)
                    {
                        addClanRequest(request);
                    }
                }
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                errMsg += $"Error: Access to the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanCreationRequestFile)}' is denied.\n{ex.Message}";

            }
            catch (DirectoryNotFoundException ex)
            {
                errMsg += $"Error: The directory for the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanCreationRequestFile)}' was not found.{ex.Message}";
            }
            catch (IOException ex)
            {
                errMsg += $"Error: A file I/O error occurred while writing to '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanCreationRequestFile)}'.\n{ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                errMsg += $"Error: An XML serialization error occurred.\n";
                // Check the inner exception for the real cause
                if (ex.InnerException != null)
                {
                    errMsg += $"Inner Exception: {ex.InnerException.Message}";
                }
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                errMsg += $"An unexpected error occurred during serialization: {ex.Message}";
            }
            return false;
        }



        // --- Pvp rooms functions
        /*
        public static void AddPvpEnabledRoom(PvpEnabledRoom room)
        {
            _pvpEnabledRooms.Add(room);
        }


        public static List<PvpEnabledRoom> GetAllPvpEnabledRooms()
        {
            return _pvpEnabledRooms;
        }


        public static void RemovePvpEnabledRoom(int vNum)
        {
            _pvpEnabledRooms.RemoveAll(room => room.RoomVnum == vNum);
        }


        public static int GetNumberOfPvpEnabledRooms()
        {
            return _pvpEnabledRooms.Count;
        }


        public static bool IsRoomInPvpEnabledRoomList(int vNum)
        {
            return _pvpEnabledRooms.Any(room => room.RoomVnum == vNum);
        }


        public static bool ReadFromFilePvpRooms(out string errMsg)
        {
            errMsg = "";
            try
            {
                var serializer = new XmlSerializer(typeof(List<PvpEnabledRoom>));
                List<PvpEnabledRoom> readList = new List<PvpEnabledRoom>();
                using (var reader = new StreamReader(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemPvpRoomFile)))
                {
                    readList = (List<PvpEnabledRoom>)serializer.Deserialize(reader);
                }

                _pvpEnabledRooms.Clear();
                foreach (PvpEnabledRoom room in readList)
                {
                    _pvpEnabledRooms.Add(room);
                }
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                errMsg += $"Error: Access to the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemPvpRoomFile)}' is denied.\n{ex.Message}";

            }
            catch (DirectoryNotFoundException ex)
            {
                errMsg += $"Error: The directory for the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemPvpRoomFile)}' was not found.{ex.Message}";
            }
            catch (IOException ex)
            {
                errMsg += $"Error: A file I/O error occurred while writing to '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemPvpRoomFile)}'.\n{ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                errMsg += $"Error: An XML serialization error occurred.\n";
                // Check the inner exception for the real cause
                if (ex.InnerException != null)
                {
                    errMsg += $"Inner Exception: {ex.InnerException.Message}";
                }
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                errMsg += $"An unexpected error occurred during serialization: {ex.Message}";
            }
            return false;
        }


        public static bool WriteToFilePvpRooms(out string errMsg)
        {
            errMsg = "";
            try
            {
                var serializer = new XmlSerializer(typeof(List<PvpEnabledRoom>));

                using (var writer = new StreamWriter(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemPvpRoomFile)))
                {
                    serializer.Serialize(writer, new List<PvpEnabledRoom>(_pvpEnabledRooms));
                }
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                errMsg = $"Error: Access to the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemPvpRoomFile)}' is denied.\n{ex.Message}";

            }
            catch (DirectoryNotFoundException ex)
            {
                errMsg = $"Error: The directory for the path '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemPvpRoomFile)}' was not found.{ex.Message}";
            }
            catch (IOException ex)
            {
                errMsg = $"Error: A file I/O error occurred while writing to '{Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemPvpRoomFile)}'.\n{ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                errMsg = $"Error: An XML serialization error occurred.\n";
                // Check the inner exception for the real cause
                if (ex.InnerException != null)
                {
                    errMsg = $"Inner Exception: {ex.InnerException.Message}";
                }
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                errMsg = $"An unexpected error occurred during serialization: {ex.Message}";
            }
            return false;
        }

        //--- End pvp rooms functions
        */

        //--- Misc
        public static void EnsureFileExists(out string errMsg)
        {
            errMsg = "";
            try
            {
                Directory.CreateDirectory(GameSettings.ClanSystemDataFolder);

                if (!File.Exists(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemDataFile)))
                {
                    //Create a clean|empty xml document.
                    WriteToFileClans(out string errMsgWriteToFileClans);
                    if (errMsgWriteToFileClans != "")
                        errMsg += errMsgWriteToFileClans;
                }
                if (!File.Exists(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanCreationRequestFile)))
                {
                    //Create a clean|empty xml document.
                    WriteToFileClanCreationRequests(out string errMsgWriteToFileClanCreationRequests);
                    if (errMsgWriteToFileClanCreationRequests != "")
                        errMsg += errMsgWriteToFileClanCreationRequests;
                }
                if (!File.Exists(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanRoomsFile)))
                {
                    //Create a clean|empty xml document.
                    WriteToFileClanRooms(out string errMsgWriteToFileClanRooms);
                    if (errMsgWriteToFileClanRooms != "")
                        errMsg += errMsgWriteToFileClanRooms;
                }
                /*
                if (!File.Exists(Path.Combine(GameSettings.ClanSystemDataFolder, GameSettings.ClanSystemPvpRoomFile)))
                {
                    //Create a clean|empty xml document.
                    WriteToFileClanRooms(out string errMsgWriteToFileClanRooms);
                    if (errMsgWriteToFileClanRooms != "")
                        errMsg += errMsgWriteToFileClanRooms;
                }
                */

            }
            catch (Exception ex)
            {
                errMsg += $"Exception occured in trying to ensure that folder : {GameSettings.ClanSystemDataFolder}" +
                            $" and its contents exist. Exception: {ex.Message}";
            }
        }
              
    }
}