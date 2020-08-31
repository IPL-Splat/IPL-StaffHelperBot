using Discord.WebSocket;
using System.Threading.Tasks;

namespace IPL_StaffHelperBot
{
    public class Background
    {
        private DiscordSocketClient client;

        public Background(DiscordSocketClient client)
        {
            this.client = client;
            Task.Run(Loop);
        }

        async Task Loop()
        {
            do
            {
                PollHelper.ScanForOldPolls();
                await ReminderHelper.ScanForNewReminders(client);

                await Task.Delay(60000); //every minute

            } while (true);
        }
    }
}
