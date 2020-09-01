using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace IPL_StaffHelperBot
{
    public static class ReminderHelper
    {
        const string REMINDER_PATH = "reminders.xml";
        const int MINUTE_SECONDS = 60;
        const int HOUR_SECONDS = 3600;
        const int DAY_SECONDS = 86400;

        public static void CreateReminder(int days, int hours, int minutes, string name, string taggable, ulong channelID, ulong guildID)
        {
            XmlDocument doc = GetDoc();

            if (ReminderExists(name))
                throw new ReminderNameTakenException();

            XmlElement root = doc.DocumentElement;
            XmlElement reminderElement = doc.CreateElement("reminder");

            long time = DateTimeOffset.Now.ToUnixTimeSeconds()
                + (days * DAY_SECONDS)
                + (hours * HOUR_SECONDS)
                + (minutes * MINUTE_SECONDS);

            reminderElement.SetAttribute("name", name);
            reminderElement.SetAttribute("time", time.ToString());
            reminderElement.SetAttribute("taggable", taggable);
            reminderElement.SetAttribute("channel", channelID.ToString());
            reminderElement.SetAttribute("guild", guildID.ToString());

            root.AppendChild(reminderElement);
            doc.Save(REMINDER_PATH);
        }

        public static async Task ScanForNewReminders(DiscordSocketClient client)
        {
            long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            XmlDocument doc = GetDoc();
            
            foreach(XmlElement child in doc.DocumentElement.ChildNodes)
            {
                long childTime = long.Parse(child.GetAttribute("time"));

                if (currentTime >= childTime)
                {
                    ulong channelID = ulong.Parse(child.GetAttribute("channel"));
                    ulong guildID = ulong.Parse(child.GetAttribute("guild"));
                    string taggable = child.GetAttribute("taggable");
                    string name = child.GetAttribute("name");

                    try
                    {
                        IMessageChannel channel = client.GetGuild(guildID).GetChannel(channelID) as IMessageChannel;

                        await channel.SendMessageAsync($"{taggable} **New reminder**!\n`{name}`");
                    } 
                    catch (Exception e) //not great design but in case a channel gets deleted
                    {
                        Console.WriteLine(e);
                    }

                    RemoveReminder(name);
                }
            }
        }

        public static void RemoveReminder(string name)
        {
            if (!ReminderExists(name))
                throw new ReminderDoesNotExistException();

            XmlDocument doc = GetDoc();

            XmlElement reminderElement = doc.SelectSingleNode($"/root/reminder[@name='{name}']") as XmlElement;
            doc.DocumentElement.RemoveChild(reminderElement);

            doc.Save(REMINDER_PATH);
        }

        private static bool ReminderExists(string name)
        {
            XmlDocument doc = GetDoc();
            XmlElement root = doc.DocumentElement;

            foreach(XmlElement child in root.ChildNodes)
            {
                if (child.GetAttribute("name") == name)
                    return true;
            }

            return false;
        }

        private static XmlDocument GetDoc()
        {
            if (!File.Exists(REMINDER_PATH))
            {
                using (StreamWriter sw = File.CreateText(REMINDER_PATH))
                    sw.Write("<root></root>");
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(REMINDER_PATH);

            return doc;
        }
    }
    
    public class ReminderNameTakenException : Exception
    {
        public ReminderNameTakenException() { }
    }

    public class ReminderDoesNotExistException : Exception
    {
        public ReminderDoesNotExistException() { }
    }
}
