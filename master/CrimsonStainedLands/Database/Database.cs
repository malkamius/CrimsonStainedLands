using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrimsonStainedLands;

namespace CrimsonStainedLands.Data
{
    internal class Database
    {
        public bool HasDatabase { get; set; } = false;
        public Database()
        {
            var builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder();
            builder.Server = "localhost";
            builder.Port = 3306;
            builder.UserID = "csl";
            builder.Password = "csl";
            builder.Database = "csl";
            this.ConnectionStringBuilder = builder;

            HasDatabase = this.TestConnection();


        }

        private bool TestConnection()
        {
            try
            {
                using (var connection = new MySqlConnection(this.ConnectionStringBuilder.ConnectionString))
                {
                    connection.Open();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Game.log($"Error connecting to database to store help entries: {ex.Message}");
                return false;
            }
            return true;
        }

        public MySqlConnectionStringBuilder ConnectionStringBuilder { get; }
        
        public void AddHelp(HelpData data)
        {
            if (!this.HasDatabase)
                return;
            try
            {
                using (var connection = new MySqlConnection(this.ConnectionStringBuilder.ConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO helps (vnum, keywords, text, level, last_updated, last_updated_by) " +
                            "VALUES(@vnum, @keywords, @text, @level, @last_updated, @last_updated_by)" +
                            "ON DUPLICATE KEY UPDATE " +
                            "keywords = VALUES(keywords), " +
                            "level = VALUES(level), " +
                            "text = VALUES(text), " +
                            "last_updated = VALUES(last_updated)," +
                            "last_updated_by = VALUES(last_updated_by);";
                        command.Parameters.AddWithValue("@vnum", data.vnum);
                        command.Parameters.AddWithValue("@keywords", data.keyword);
                        command.Parameters.AddWithValue("@text", data.text);
                        command.Parameters.AddWithValue("@level", data.level);
                        command.Parameters.AddWithValue("@last_updated", data.lastEditedOn);
                        command.Parameters.AddWithValue("@last_updated_by", data.lastEditedBy);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Game.log($"Error adding help {data.vnum} - {data.keyword}: {ex.Message}");
            }
            
        }
    }
}
