﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands.Data
{
    internal class Database
    {
        public Database() 
        {
            var builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder();
            builder.Server = "localhost";
            builder.Port = 3306;
            builder.UserID = "csl";
            builder.Password = "csl";
            builder.Database = "csl";
            this.ConnectionStringBuilder = builder;
        }

        public MySqlConnectionStringBuilder ConnectionStringBuilder { get; }

        public void AddHelp(HelpData data)
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
    }
}
