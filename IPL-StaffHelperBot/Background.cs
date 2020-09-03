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
                await ReminderHelper.ScanForNewReminders(client);

                if (storedDay != DateTime.Now.ToUniversalTime().Day) //if it is a new day
                {
                    PollHelper.ScanForOldPolls();

                    await CalendarHelper.UpdateCalendarMessage(client);
                    CalendarHelper.RemoveOldEvents();
                    storedDay = DateTime.Now.ToUniversalTime().Day;
                }

                await Task.Delay(60000); //every minute

            } while (true);
        }
    }
}
