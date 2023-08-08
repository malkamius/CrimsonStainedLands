using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{

    public class Quest
    {
        public static Dictionary<int, Quest> Quests = new Dictionary<int, Quest>();

        public static Quest GetQuest(int vnum) => Quests.TryGetValue(vnum, out var result) ? result : null;
        public static Quest GetQuest(string name) => (from quest in Quests.Values where quest.Name.StringCmp(name) select quest).FirstOrDefault();

        public static QuestProgressData GetQuestProgress(Character ch, int vnum) => ch is Player? ((Player)ch).Quests.FirstOrDefault(q => q.Quest.Vnum == vnum) : null;   

        public static QuestProgressData GetQuestProgress(Character ch, Quest quest) => GetQuestProgress(ch, quest.Vnum);
        
        public static void LoadQuests(AreaData area, XElement QuestsElement)
        {
            foreach (var questElement in QuestsElement.Elements("Quest"))
            {
                var Quest = new Quest(area, questElement);
                if (!Quests.ContainsKey(Quest.Vnum))
                {
                    Quests.Add(Quest.Vnum, Quest);
                }
                else
                    Game.bug("Duplicate Quest Vnum {0} in area {1}", Quest.Vnum, area != null ? area.Name : "unknown");

            }
        }

        public enum QuestStatus
        {
            None = 0,
            InProgress = 1,
            Failed,
            Complete,
            Disabled
        }

        public Quest(AreaData area, XElement questdata)
        {
            this.Area = area;
            Vnum = questdata.GetAttributeValueInt("Vnum");
            Name = questdata.GetElementValue("Name");
            Display = questdata.GetElementValue("Display");
            ShortDescription = questdata.GetElementValue("ShortDescription");
            Description = questdata.GetElementValue("Description");

            StartLevel = questdata.GetElementValueInt("StartLevel");
            EndLevel = questdata.GetElementValueInt("EndLevel");

            RewardXp = questdata.GetElementValueInt("RewardXp");
            RewardGold = questdata.GetElementValueInt("RewardGold");
            ShowInQuests = bool.TryParse(questdata.GetElementValue("ShowInQuests"), out var show) && show;
            RewardItems.Clear();

            if (questdata.HasElement("RewardItems"))
                RewardItems.AddRange(from rewarditem in questdata.GetElement("RewardItems").Elements() select rewarditem.GetAttributeValueInt("Vnum"));

            if (questdata.HasElement("Prerequisites"))
                RewardItems.AddRange(from prereq in questdata.GetElement("Prerequisites").Elements() select prereq.GetAttributeValueInt("Vnum"));

            area.Quests.Add(Vnum, this);
        }

        public XElement Element
        {
            get
            {
                return new XElement("Quest", new XAttribute("Vnum", Vnum),
                    new XElement("Name", Name),
                    new XElement("Display", Display),
                    new XElement("ShortDescription", ShortDescription),
                    new XElement("Description", Description),
                    new XElement("StartLevel", StartLevel),
                    new XElement("EndLevel", EndLevel),
                    new XElement("RewardXp", RewardXp),
                    new XElement("RewardGold", RewardGold),
                    RewardItems.Any() ?
                        new XElement("RewardItems", from itemvnum in RewardItems select new XElement("Item", new XAttribute("Vnum", itemvnum))) : null,
                    new XElement("ShowInQuests", ShowInQuests),
                    QuestPrerequisites.Any() ?
                        new XElement("Prerequisites", from questvnum in QuestPrerequisites select new XElement("Quest", new XAttribute("Vnum", questvnum))) : null
                    );
            }
        }

        public AreaData Area { get; set; }

        public int Vnum { get; set; } = 0;
        public string Name { get; set; } = "";

        public string Display { get; set; } = "";

        public string ShortDescription { get; set; } = "";

        public string Description { get; set; } = "";

        public int StartLevel { get; set; } = 0;

        public int EndLevel { get; set; } = 0;


        public int RewardXp { get; set; } = 0;

        public int RewardGold { get; set; } = 0;
        public List<int> RewardItems { get; set; } = new List<int>();

        public bool ShowInQuests { get; set; } = true;

        public List<int> QuestPrerequisites { get; set; } = new List<int>();

    }

    public class QuestProgressData
    {

        public QuestProgressData(XElement progress)
        {
            var questVnum = progress.GetAttributeValueInt("QuestVnum");
            var questName = progress.GetAttributeValue("QuestName");

            this.Quest = Quest.GetQuest(questVnum) ?? Quest.GetQuest(questName);
            QuestGiver = progress.GetAttributeValue("QuestGiver");
            Quest.QuestStatus status = Quest.QuestStatus.None;
            if (Utility.GetEnumValue<Quest.QuestStatus>(progress.GetAttributeValue("Status"), ref status))
                Status = status;
            LevelStarted = progress.GetAttributeValueInt("LevelStarted");
            LevelCompleted = progress.GetAttributeValueInt("LevelCompleted");
            Progress = progress.GetAttributeValueInt("Progress");
            ExtraState = progress.GetElement("ExtraState") ?? new XElement("ExtraState");
        }

        private QuestProgressData(Quest quest, string giver)
        {
            this.Quest = quest;
            this.QuestGiver = giver;
            this.ExtraState = new XElement("ExtraState");
        }

        public static bool StartQuest(Character ch, string giver, Quest quest)
        {
            if (ch is Player && quest != null)
            {
                var player = (Player)ch;

                if (!player.Quests.Any(q => q.Quest == quest))
                {
                    var questProgress = new QuestProgressData(quest, giver);
                    questProgress.LevelStarted = player.Level;
                    questProgress.Status = Quest.QuestStatus.InProgress;
                    if (questProgress.Quest.ShowInQuests)
                    {
                        ch.send("You have started the quest '{0}'.\n\r", questProgress.Quest.Display);
                    }
                    player.Quests.Add(questProgress);
                    
                    return true;
                }
                else
                {
                    var questProgress = player.Quests.FirstOrDefault(q => q.Quest == quest && q.Status == Quest.QuestStatus.None);
                    if (questProgress != null)
                    {
                        questProgress.LevelStarted = player.Level;
                        questProgress.Status = Quest.QuestStatus.InProgress;
                        if (questProgress.Quest.ShowInQuests)
                        {
                            ch.send("You have started the quest '{0}'.\n\r", questProgress.Quest.Display);
                        }
                    }

                }
            }
            return false;
        }

        public static void CompleteQuest(Character ch, Quest quest)
        {
            if (ch is Player && quest != null)
            {
                var player = (Player)ch;
                QuestProgressData questProgress = null;
                if ((questProgress = player.Quests.FirstOrDefault(q => q.Quest == quest)) == null)
                {
                    questProgress = new QuestProgressData(quest, "");
                    questProgress.LevelStarted = player.Level;
                    player.Quests.Add(questProgress);
                }

                if (questProgress != null)
                {
                    questProgress.Status = Quest.QuestStatus.Complete;
                    questProgress.LevelCompleted = player.Level;

                    player.GainExperience(questProgress.Quest.RewardXp);

                    if (questProgress.Quest.RewardGold > 0)
                    {
                        player.Silver += questProgress.Quest.RewardGold % 1000;
                        player.Gold += questProgress.Quest.RewardGold / 1000;
                    }
                    if (questProgress.Quest.ShowInQuests)
                    {
                        ch.send("You have completed the quest '{0}'.\n\r", questProgress.Quest.Display);
                    }

                    if (questProgress.Quest.RewardXp > 0)
                        ch.send("You receive {0} experience points.\n\r", questProgress.Quest.RewardXp);

                    if (questProgress.Quest.RewardGold > 0)
                    {
                        ch.send("You receive {0} silver and {1} gold.\n\r", questProgress.Quest.RewardGold % 1000, questProgress.Quest.RewardGold / 1000);
                    }

                    foreach (var itemvnum in questProgress.Quest.RewardItems)
                    {
                        if (ItemTemplateData.Templates.TryGetValue(itemvnum, out var template))
                        {
                            var item = new ItemData(template, player);

                            ch.Act("You receive $p.", null, item);
                        }
                    }
                }
            }
        }

        public static void FailQuest(Character ch, Quest quest)
        {
            if (ch is Player && quest != null)
            {
                var player = (Player)ch;
                QuestProgressData questProgress = null;
                if ((questProgress = player.Quests.FirstOrDefault(q => q.Quest == quest)) == null)
                {
                    questProgress = new QuestProgressData(quest, "");
                    questProgress.LevelStarted = player.Level;
                    player.Quests.Add(questProgress);
                }

                if (questProgress != null)
                {
                    questProgress.Status = Quest.QuestStatus.Failed;

                    if (questProgress.Quest.ShowInQuests)
                    {

                        ch.send("You have failed the quest '{0}'.\n\r", questProgress.Quest.Display);
                    }

                }
            }
        }

        public static void DisableQuest(Character ch, Quest quest)
        {
            if (ch is Player && quest != null)
            {
                var player = (Player)ch;
                QuestProgressData questProgress = null;
                if ((questProgress = player.Quests.FirstOrDefault(q => q.Quest == quest)) == null)
                {
                    questProgress = new QuestProgressData(quest, "");
                    questProgress.LevelStarted = player.Level;
                    player.Quests.Add(questProgress);
                }

                if (questProgress != null)
                {
                    questProgress.Status = Quest.QuestStatus.Disabled;

                    if (questProgress.Quest.ShowInQuests)
                    {

                        ch.send("Your are no longer eligible for the quest '{0}'.\n\r", questProgress.Quest.Display);
                    }

                }
            }
        }

        public static void DropQuest(Character ch, Quest quest)
        {
            if (ch is Player && quest != null)
            {
                var player = (Player)ch;
                QuestProgressData questProgress = null;
                if ((questProgress = player.Quests.FirstOrDefault(q => q.Quest == quest)) == null)
                {
                    questProgress = new QuestProgressData(quest, "");
                    questProgress.LevelStarted = player.Level;
                    player.Quests.Add(questProgress);
                }

                if (questProgress != null)
                {
                    questProgress.Status = Quest.QuestStatus.None;
                    questProgress.ExtraState = new XElement("ExtraState");
                    if (questProgress.Quest.ShowInQuests)
                    {

                        ch.send("Quest '{0}' has been dropped.\n\r", questProgress.Quest.Display);
                    }

                }
            }
        }

        public static bool IsQuestComplete(Character ch, Quest quest) => (ch is Player) && quest != null ?
            ((Player)ch).Quests.Any(q => q.Quest == quest && q.Status == Quest.QuestStatus.Complete) : false;

        public static bool IsQuestFailed(Character ch, Quest quest) => (ch is Player) && quest != null ?
            ((Player)ch).Quests.Any(q => q.Quest == quest && q.Status == Quest.QuestStatus.Failed) : false;

        public static bool IsQuestInProgress(Character ch, Quest quest) => (ch is Player) && quest != null ?
            ((Player)ch).Quests.Any(q => q.Quest == quest && q.Status == Quest.QuestStatus.InProgress) : false;

        public static bool IsQuestAvailable(Character ch, Quest quest) => (ch is Player) && quest != null ?
            (!((Player)ch).Quests.Any(q => q.Quest == quest && q.Status != Quest.QuestStatus.None)) && ch.Level >= quest.StartLevel && ch.Level <= quest.EndLevel : false;

        public static bool HasQuestPrerequisites(Character ch, Quest quest) => (ch is Player) ?
            quest.QuestPrerequisites.All(vnum => IsQuestComplete(ch, Quest.GetQuest(vnum))) : false;

        public static void ResetQuest(Character ch, Quest quest)
        {
            if (ch is Player && quest != null)
            {
                var player = (Player)ch;
                QuestProgressData questProgress = null;
                if ((questProgress = player.Quests.FirstOrDefault(q => q.Quest == quest)) == null)
                {
                    questProgress = new QuestProgressData(quest, "");
                    questProgress.LevelStarted = player.Level;
                    player.Quests.Add(questProgress);
                }

                if (questProgress != null)
                {
                    questProgress.Status = Quest.QuestStatus.InProgress;
                    questProgress.Progress = 0;
                    questProgress.ExtraState = new XElement("ExtraState");

                    if (questProgress.Quest.ShowInQuests)
                    {
                        ch.send("Your progress for the quest '{0}' has been reset.\n\r", questProgress.Quest.Display);
                    }

                }
            }
        }

        public Quest Quest { get; set; }

        public string QuestGiver { get; set; } = "";

        public Quest.QuestStatus Status { get; set; } = Quest.QuestStatus.None;

        public int LevelStarted { get; set; } = 0;

        public int LevelCompleted { get; set; } = 0;

        public int Progress { get; set; } = 0;

        public XElement ExtraState { get; set; } = new XElement("ExtraState");

        public XElement Element => new XElement("QuestProgress",
            new XAttribute("QuestVnum", Quest.Vnum),
            new XAttribute("QuestName", Quest.Name),
            new XAttribute("QuestGiver", QuestGiver),
            new XAttribute("Status", Status.ToString()),
            new XAttribute("LevelStarted", LevelStarted),
            new XAttribute("LevelCompleted", LevelCompleted),
            new XAttribute("Progress", Progress),
            ExtraState);
    }
}
