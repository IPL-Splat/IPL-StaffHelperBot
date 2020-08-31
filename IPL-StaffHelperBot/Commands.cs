using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;

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
                await ReplyAsync("Could not find a reminder under that name! Make sure it matches exactly.");
                return;
            }

            await ReplyAsync("Removed reminder.");
        }

        #endregion


    }
}
