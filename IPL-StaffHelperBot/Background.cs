using Discord.WebSocket;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace IPL_StaffHelperBot
{
    public class Background
    {
        private DiscordSocketClient client;
        int storedDay = 0;

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
                if (storedDay != DateTime.Now.Day)
                {
                    await CalendarHelper.UpdateCalendarMessage(client);
                    CalendarHelper.RemoveOldEvents();
                    storedDay = DateTime.Now.Day;
                }

                await Task.Delay(60000); //every minute

            } while (true);
        }
    }
}
