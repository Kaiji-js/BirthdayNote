using BirthdayBot.Models;
using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.Tasks;

namespace BirthdayBot.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LineBotController : Controller
    {
        private static LineMessagingClient lineMessaingClient;
        AppSettings appSettings;

        public LineBotController(IOptions<AppSettings> options)
        {
            appSettings = options.Value;
            lineMessaingClient = new LineMessagingClient(appSettings.LineSettings.ChannelAccessToken);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]JsonElement req)
        {
            var events = WebhookEventParser.Parse(req.ToString());
            var app = new LineBotApp(lineMessaingClient);
            await app.RunAsync(events);
            return new OkResult();

        }
    }
}
