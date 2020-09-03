using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using System.Security.Cryptography.X509Certificates;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;

namespace IPL_StaffHelperBot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        /*
         * How to add a command:
         * 
         * [Command("Command name")]
         * public async Task MethodName(int firstParam, [Remainder]string restOfParam)
         * {
         *     call the Context object to get all sorts of contextual stuff.
         *     for example, Context.User gets the object of the user that sent the command.
         *     ReplyAsync() will reply to the user in the channel the command was sent in.
         * }
         */

        [Command("help")]
        public async Task Help()
        {
            string commands =
                "`[] = Optional   () = Required`" + "\n\n" +
                "**Roll the dice:**" + "\n" +
                "`rtd [dice size]`" + "\n" +
                "**Create a poll:**" + "\n" +
                "`poll (title), (option 1), (option 2)` Up to 9 options seperated by commas." + "\n" +
                "**Interact with reminders:**" + "\n" +
                "`reminder create [num days] [num hours] (num minutes) (name)`" + "\n" +
                "`reminder remove (name)`" + "\n" +
                "**Use the Calendar**" + "\n" +
                "`calendar add [num month] (num day) [num year] (name)`" + "\n" +
                "`calendar remove (month) (day) (num year) (name)`";

            await ReplyAsync(commands);
        }

        #region RTD

        [Command("rtd")]
        public async Task Rtd() =>
            await ReplyAsync($"The dice rolled a **{GetDiceRoll(6)}**.");

        [Command("rtd")]
        public async Task Rtd(int max)
        {
            if (max <= 0)
            {
                await ReplyAsync("Number must be a nonzero positive value!");
                return;
            }
            await ReplyAsync($"The dice rolled a **{GetDiceRoll(max)}**.");
        }

        private int GetDiceRoll(int max)
        {
            int roll = new Random().Next(max) + 1;
            return roll;
        }

        #endregion

        #region POLLS

        [Command("poll")]
        public async Task Poll() =>
            await ReplyAsync("To create a poll, use the following command:\n`poll (title), (option 1), (option 2) - Up to 9 options seperated by commas.`");

        [Command("poll")]
        public async Task PollCreate([Remainder]string argsParam) //args will be seperated by commas
        {
            string[] args = argsParam.Split(',');
            string title = args[0];
            string[] options = args.Skip(1).ToArray();

            if(options.Length <= 1)
            {
                await ReplyAsync("Polls must have 2 options or more!");
                return;
            }

            if (options.Length > 9)
            {
                await ReplyAsync("Polls are limited to 9 options! Reduce the number of options.");
                return;
            }

            foreach(string arg in args)
            {
                if (arg.Length >= 40)
                {
                    await ReplyAsync("Please limit the amount of text in your title/options!");
                    return;
                }
            }

            var message = await ReplyAsync("Creating poll...");

            try
            {
                PollHelper.CreatePoll(message.Id, title, options);
            }
            catch (PollNameTakenException)
            {
                await message.ModifyAsync(c => c.Content = "A poll with this name is already active! Choose a different name.");
                return;
            }

            await message.ModifyAsync(e => e.Embed = PollHelper.GetPollEmbed(title).Build());
            await message.ModifyAsync(c => c.Content = $"**{title}**");

            IEmote[] reactions = new IEmote[options.Length];
            for (int i = 0; i < reactions.Length; i++)
            {
                reactions[i] = PollHelper.numToEmote[i + 1];
            }

            await message.AddReactionsAsync(reactions);
        }


        #endregion

        #region REMINDERS

        [Command("reminder")]
        public async Task Reminder() =>
            await ReplyAsync("To interact with reminders, use the following commands:\n" +
                "`reminder create [num days] [num hours] (num minutes) (name)`\n" +
                "`reminder remove (name)`");

        [Command("reminder create")]
        public async Task ReminderCreateComm(int days, int hours, int minutes, [Remainder] string name) => 
            await ReminderCreation(days, hours, minutes, name);

        [Command("reminder create")]
        public async Task ReminderCreateComm(int hours, int minutes, [Remainder] string name) =>
            await ReminderCreation(0, hours, minutes, name);

        [Command("reminder create")]
        public async Task ReminderCreateComm(int minutes, [Remainder] string name) =>
            await ReminderCreation(0, 0, minutes, name);

        private async Task ReminderCreation(int days, int hours, int minutes, string name)
        {
            try
            {
                ReminderHelper.CreateReminder(days, hours, minutes, name, Context.User.Mention, Context.Channel.Id, Context.Guild.Id);
            } 
            catch (ReminderNameTakenException)
            {
                await ReplyAsync("A reminder under that name is already being used! Choose a different name.");
                return;
            }

            await ReplyAsync("Reminder created!");
        }

        [Command("reminder remove")]
        public async Task ReminderRemove([Remainder]string name)
        {
            try
            {
                ReminderHelper.RemoveReminder(name);
            }
            catch (ReminderDoesNotExistException)
            {
                await ReplyAsync("Could not find a reminder under that name! Make sure the name matches exactly.");
                return;
            }

            await ReplyAsync("Removed reminder.");
        }

        #endregion

        #region CALENDAR

        [Command("calendar")]
        public async Task Calendar() =>
            await ReplyAsync("To use the calendar, use the following commands: " + "\n" +
                "`calendar add [num month] (num day) [num year] (name)`" + "\n" +
                "`calendar remove (month) (day) (name)`");

        [Command("calendar add")]
        public async Task CalendarAdd(int month, int day, int year, [Remainder] string name) =>
            await BaseCalendarAdd(month, day, year, name);

        [Command("calendar add")]
        public async Task CalendarAdd(int month, int day, [Remainder]string name) =>
            await BaseCalendarAdd(month, day, DateTime.UtcNow.Year, name);

        [Command("calendar add")]
        public async Task CalendarAdd(int day, [Remainder] string name) =>
            await BaseCalendarAdd(DateTime.UtcNow.Month, day, DateTime.UtcNow.Year, name);

        public async Task BaseCalendarAdd(int month, int day, int year, string name)
        {
            if (name.Length > 100)
            {
                await ReplyAsync("Event message too long! Keep it under 100 characters.");
                return;
            }
            DateTime now = DateTime.UtcNow;

            if ((now.Day > day && now.Month == month && now.Year == year)
               || (now.Month > month && now.Year == year)
               || now.Year > year)
            {
                await ReplyAsync("You can't create an event at a date that's in the past!");
                return;
            }


            CalendarHelper.AddToCalendar(month, day, year, name);
            await CalendarHelper.UpdateCalendarMessage(Context.Client);
            await ReplyAsync("Added to calendar.");
        }

        [Command("calendar channel"), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CalendarChannel(string name)
        {
            var channel = Context.Guild.Channels.FirstOrDefault(n => n.Name == name);

            if (channel is null)
            {
                await ReplyAsync("Channel not found! Make sure you typed in the name correctly.");
                return;
            }

            CalendarHelper.EditCalendarChannel(channel);
            await ReplyAsync("Channel changed.");
        }

        [Command("calendar remove")]
        public async Task CalendarRemove(int month, int day, int year,[Remainder]string name)
        {
            if (!CalendarHelper.CalendarEventExists(month, day, year, name))
            {
                await ReplyAsync("Calendar event not found! Make sure the month, day, year, and name are all exactly the same.");
                return;
            }

            CalendarHelper.RemoveCalendarEvent(month, day, year, name);
            await CalendarHelper.UpdateCalendarMessage(Context.Client);

            await ReplyAsync("Event deleted.");
        }

        #endregion
    }
}
