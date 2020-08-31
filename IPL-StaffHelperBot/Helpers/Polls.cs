using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace IPL_StaffHelperBot
{
    public static class PollHelper
    {
        const string POLL_PATH = "polls.xml";

        public static Dictionary<int, Emoji> numToEmote = new Dictionary<int, Emoji>()
        {
            {1, new Emoji("\u0031\u20E3")},
            {2, new Emoji("\u0032\u20E3")},
            {3, new Emoji("\u0033\u20E3")},
            {4, new Emoji("\u0034\u20E3")},
            {5, new Emoji("\u0035\u20E3")},
            {6, new Emoji("\u0036\u20E3")},
            {7, new Emoji("\u0037\u20E3")},
            {8, new Emoji("\u0038\u20E3")},
            {9, new Emoji("\u0039\u20E3")}
        };

        public static Dictionary<Emoji, int> emoteToNum = new Dictionary<Emoji, int>()
        {
            {new Emoji("\u0031\u20E3"), 1},
            {new Emoji("\u0032\u20E3"), 2},
            {new Emoji("\u0033\u20E3"), 3},
            {new Emoji("\u0034\u20E3"), 4},
            {new Emoji("\u0035\u20E3"), 5},
            {new Emoji("\u0036\u20E3"), 6},
            {new Emoji("\u0037\u20E3"), 7},
            {new Emoji("\u0038\u20E3"), 8},
            {new Emoji("\u0039\u20E3"), 8}
        };

        public static void CreatePoll(ulong messageID, string title, string[] options)
        {
            XmlDocument doc = GetDoc();

            if (PollExists(title))
                throw new PollNameTakenException();

            XmlElement root = doc.DocumentElement;
            XmlElement pollElement = doc.CreateElement("poll");

            long experation = DateTimeOffset.Now.ToUnixTimeSeconds() + 604800;

            pollElement.SetAttribute("messageID", messageID.ToString());
            pollElement.SetAttribute("title", title);
            pollElement.SetAttribute("experation", experation.ToString());

            int index = 1;
            foreach(string option in options)
            {
                XmlElement pollOption = doc.CreateElement("option");
                pollOption.SetAttribute("name", option.Trim());
                pollOption.SetAttribute("votes", "0");
                pollOption.SetAttribute("index", index.ToString());
                pollElement.AppendChild(pollOption);
                index++;
            }

            root.AppendChild(pollElement);
            doc.Save(POLL_PATH);
        }

        public static bool IsMessagePoll(ulong messageID)
        {
            XmlDocument doc = GetDoc();

            XmlElement element = doc.SelectSingleNode($"/root/poll[@messageID='{messageID}']") as XmlElement;
            return element != null;
        }

        public static async Task VoteOnPollAsync(SocketReaction reaction, DiscordSocketClient client)
        {
            XmlDocument doc = GetDoc();

            XmlElement element = doc.SelectSingleNode($"/root/poll[@messageID='{reaction.MessageId}']") as XmlElement;

            foreach(XmlElement child in element.SelectNodes("user"))
            {
                if (child.GetAttribute("userID") == reaction.UserId.ToString())
                    return;
            }

            XmlElement userElement = doc.CreateElement("user");
            userElement.SetAttribute("userID", reaction.UserId.ToString());
            
            element.AppendChild(userElement);

            foreach(XmlElement child in element.ChildNodes)
            {
                int index = int.Parse(child.GetAttribute("index"));

                if (emoteToNum[(Emoji)reaction.Emote] == index) 
                {
                    int voteCount = int.Parse(child.GetAttribute("votes")) + 1;
                    child.SetAttribute("votes", voteCount.ToString());
                    break;
                }
            }

            doc.Save(POLL_PATH);

            string pollTitle = element.GetAttribute("title");

            var message = await ((ISocketMessageChannel)client.GetChannel(reaction.Channel.Id)).GetMessageAsync(reaction.MessageId);
            await (message as RestUserMessage).ModifyAsync(e => e.Embed = GetPollEmbed(pollTitle).Build());
        }

        public static EmbedBuilder GetPollEmbed(string title)
        {
            XmlDocument doc = GetDoc();

            EmbedBuilder builder = new EmbedBuilder();

            XmlElement element = doc.SelectSingleNode($"/root/poll[@title='{title}']") as XmlElement;
            if (element == null)
                throw new PollDoesNotExistException();

            int max = 0;
            foreach (XmlElement option in element.ChildNodes)
            {
                if (option.Name == "option")
                    max += int.Parse(option.GetAttribute("votes"));
            }

            builder.WithFooter($"{max} total vote{ (max != 1 ? "s" : "") }");

            foreach(XmlElement option in element.ChildNodes)
            {
                if (option.Name == "option") 
                {
                    string optionName = option.GetAttribute("name");
                    int optionVotes = int.Parse(option.GetAttribute("votes"));
                    string plural = optionVotes != 1 ? "s" : "";

                    int barFill = max != 0 ? (int)(((double)optionVotes / max) * 100 / 3) : 0;
                    string bar = string.Concat(Enumerable.Repeat("▓", barFill)) + string.Concat(Enumerable.Repeat("░", 33 - barFill));

                    int index = int.Parse(option.GetAttribute("index"));

                    builder.AddField($"{numToEmote[index]} {optionName} : {optionVotes} vote{plural}.", $"`{bar}`");
                }
            }

            return builder;
        }

        public static void ScanForOldPolls()
        {
            XmlDocument doc = GetDoc();

            XmlElement root = doc.DocumentElement;
            long time = DateTimeOffset.Now.ToUnixTimeSeconds();

            foreach (XmlElement child in root)
            {
                long childExperationDate = long.Parse(child.GetAttribute("experation")); //don't worry about it
                if (childExperationDate < time)
                {
                    root.RemoveChild(child); //yeet the child
                }
            }

            doc.Save(POLL_PATH);
        }

        private static XmlDocument GetDoc()
        {
            if (!File.Exists(POLL_PATH))
            {
                using (StreamWriter sw = File.CreateText(POLL_PATH))
                    sw.Write("<root></root>");
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(POLL_PATH);

            return doc;
        }

        private static bool PollExists(string title)
        {
            XmlDocument doc = GetDoc();
            XmlElement root = doc.DocumentElement;

            foreach(XmlElement element in root.ChildNodes)
            {
                if (element.GetAttribute("title") == title)
                    return true;
            }

            return false;
        }
    }


    public class PollNameTakenException : Exception
    {
        public PollNameTakenException() { }
    }

    public class PollDoesNotExistException : Exception
    {
        public PollDoesNotExistException() { }
    }
}