using Discord.WebSocket;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace IPL_StaffHelperBot
{
    public class Background
    {
        private DiscordSocketClient client;
        bool firstRun = true;

        public Background(DiscordSocketClient client)
        {
            this.client = client;
            Loop().GetAwaiter().GetResult();
        }

        async Task Loop()
        {
            do
            {
                await ReminderHelper.ScanForNewReminders(client);

                if ((DateTime.UtcNow.Hour == 0 && DateTime.UtcNow.Minute < 1) || firstRun) //if it is a new day
                {
                    PollHelper.ScanForOldPolls();

                    await CalendarHelper.UpdateCalendarMessage(client);
                    CalendarHelper.RemoveOldEvents();

                    firstRun = false;
                }

                await Task.Delay(60000); //every minute

            } while (true);
        }
    }
}
