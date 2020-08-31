/*
 * IPL Staff Helper Bot.
 * Created by .jpg.
 * Intended for use by IPL staff.
 */

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace IPL_StaffHelperBot
{
    class Program
    {
        static void Main(string[] args) => new Program().Init().GetAwaiter().GetResult();

        DiscordSocketClient client;
        CommandService commands;
        bool clientLoggedIn = false;

        const string TOKEN_PATH = "token.txt";

        async Task Init()
        {
            if (!File.Exists(TOKEN_PATH))
            {
                Console.WriteLine("Enter token: ");
                using (StreamWriter sw = File.CreateText(TOKEN_PATH))
                    sw.Write(Console.ReadLine());
            }

            string token = File.ReadAllText(TOKEN_PATH);

            client = new DiscordSocketClient();
            commands = new CommandService();

            client.Ready += ClientReady;

            client.Log += ClientLog;

            client.MessageReceived += HandleCommandAsync;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            client.ReactionAdded += HandleReactionAsync;

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            while (!clientLoggedIn) { }

            Background bg = new Background(client);

            await Task.Delay(-1);
        }

        private Task ClientReady()
        {
            clientLoggedIn = true;
            return Task.CompletedTask;
        }

        private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel messageChannel, SocketReaction reaction)
        {
            if (!reaction.User.Value.IsBot && PollHelper.IsMessagePoll(reaction.MessageId))
            {
                await PollHelper.VoteOnPollAsync(reaction, client);
            }
        }

        private Task ClientLog(LogMessage arg)
        {
            Console.WriteLine(arg.Message);
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null || message.Author.IsBot) return;

            int argPos = 0;
            if (message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(client, message);
                await commands.ExecuteAsync(context, argPos, null);
            }
        }
    }
}
