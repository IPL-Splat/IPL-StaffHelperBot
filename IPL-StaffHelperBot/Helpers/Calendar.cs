using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IPL_StaffHelperBot
{
    public static class CalendarHelper
    {
        const string CAL_PATH = "calendar.xml";

        public static void AddToCalendar(int month, int day, int year, string name)
        {
            XmlDocument doc = GetDoc();
            XmlElement root = doc.DocumentElement;

            XmlElement eventElement = doc.CreateElement("event");
            eventElement.SetAttribute("name", name);
            eventElement.SetAttribute("month", month.ToString());
            eventElement.SetAttribute("day", day.ToString());
            eventElement.SetAttribute("year", year.ToString());

            root.AppendChild(eventElement);
            doc.Save(CAL_PATH);
        }

        public static void EditCalendarChannel(SocketGuildChannel channel)
        {
            XmlDocument doc = GetDoc();
            XmlElement channelElement = doc.SelectSingleNode("/root/channel") as XmlElement;

            channelElement.SetAttribute("id", channel.Id.ToString());
            channelElement.SetAttribute("guild", channel.Guild.Id.ToString());

            doc.Save(CAL_PATH);
        }

        public static async Task UpdateCalendarMessage(DiscordSocketClient client)
        {
            XmlDocument doc = GetDoc();
            XmlElement channelElement = doc.SelectSingleNode("/root/channel") as XmlElement;

            ulong guildId = ulong.Parse(channelElement.GetAttribute("guild"));
            ulong textId = ulong.Parse(channelElement.GetAttribute("id"));

            if (client.GetGuild(guildId) == null || client.GetChannel(textId) == null) return;

            SocketGuildChannel channel = client.GetGuild(guildId).GetTextChannel(textId);

            DateTime dateTime = DateTime.UtcNow;
            EmbedBuilder builder = new EmbedBuilder() 
            { 
                Title = "📆 Calendar (Next 2 weeks)" 
            };

            for (int i = 0; i < 14; i++) 
            {
                string title = $"{IntToMonth(dateTime.Month)} {dateTime.Day}";

                if (i == 0)
                    title += " (Today)";
                else if (i == 1)
                    title += " (Tomorrow)";
                else if (i < 7)
                    title += $" (This {dateTime.DayOfWeek})";
                else
                    title += $" (Next {dateTime.DayOfWeek})";

                string day = dateTime.Day.ToString();
                string month = dateTime.Month.ToString();
                string year = dateTime.Year.ToString();

                string events = "";

                foreach (XmlElement element in doc.SelectNodes($"/root/event"))
                {
                    if (element.GetAttribute("day") == day && element.GetAttribute("month") == month && element.GetAttribute("year") == year)
                    {
                        if (events != "")
                            events += "\n";
                        events += "• " + element.GetAttribute("name");
                    }
                }

                if (events != "")
                    builder.AddField(title, events);

                dateTime = dateTime.AddDays(1);
            }

            var messages = await (channel as IMessageChannel).GetMessagesAsync().FlattenAsync();
            var postedMessage = messages.FirstOrDefault(a => a.Author.Id == client.CurrentUser.Id);

            if (postedMessage != null)
            {
                await (postedMessage as IUserMessage).ModifyAsync(c => c.Embed = builder.Build());
            } 
            else
            {
                await (channel as IMessageChannel).SendMessageAsync(embed: builder.Build());
            }
        }

        public static void RemoveOldEvents()
        {
            XmlDocument doc = GetDoc();
            DateTime dateTime = DateTime.UtcNow;

            bool changeMade = false;

            foreach (XmlElement child in doc.SelectNodes("/root/event"))
            {
                int day = int.Parse(child.GetAttribute("month"));
                int month = int.Parse(child.GetAttribute("day"));
                int year = int.Parse(child.GetAttribute("year"));

                if ((dateTime.Day > day && dateTime.Month == month && dateTime.Year == year) 
                    || (dateTime.Month > month && dateTime.Year == year) 
                    || dateTime.Year > year)
                {
                    doc.DocumentElement.RemoveChild(child);
                    changeMade = true;
                }
            }

            if (changeMade) doc.Save(CAL_PATH);
        }

        public static void EnsureCompatibility() //temp code, remove in the future
        {
            XmlDocument doc = GetDoc();
            DateTime dateTime = DateTime.UtcNow;

            foreach (XmlElement child in doc.SelectNodes("/root/event"))
            {
                if (!child.HasAttribute("year"))
                    child.SetAttribute("year", dateTime.Year.ToString());
            }

            doc.Save(CAL_PATH);
        }

        public static bool CalendarEventExists(int month, int day, int year, string name)
        {
            XmlDocument doc = GetDoc();

            foreach(XmlElement child in doc.SelectNodes("/root/event"))
            {
                if (int.Parse(child.GetAttribute("month")) == month
                    && int.Parse(child.GetAttribute("day")) == day
                    && int.Parse(child.GetAttribute("year")) == year
                    && child.GetAttribute("name") == name)
                {
                    return true;
                }
            }

            return false;
        }

        public static void RemoveCalendarEvent(int month, int day, int year, string name)
        {
            XmlDocument doc = GetDoc();

            XmlElement element = doc.SelectSingleNode($"/root/event[@name='{name}' and @day='{day}' and @month='{month}' and @year='{year}']") as XmlElement;
            doc.DocumentElement.RemoveChild(element);

            doc.Save(CAL_PATH);
        }

        private static string IntToMonth(int month)
        {
            string[] monthStr =
            {
                "January",
                "February",
                "March",
                "April",
                "May",
                "June",
                "July",
                "August",
                "September",
                "October",
                "November",
                "December"
            };
            return monthStr[month - 1];
        }

        private static XmlDocument GetDoc()
        {
            if (!File.Exists(CAL_PATH))
            {
                using (StreamWriter sw = File.CreateText(CAL_PATH))
                    sw.Write("<root><channel id=\"0\" guild=\"0\" /></root>");
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(CAL_PATH);

            return doc;
        }
    }
}
