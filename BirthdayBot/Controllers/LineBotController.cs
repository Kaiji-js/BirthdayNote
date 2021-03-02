using BirthdayBot.Models;
using BirthdayBot.Services;
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
        private readonly ICosmosDbService cosmosDbService;
        AppSettings appSettings;

        public LineBotController(IOptions<AppSettings> options, ICosmosDbService cosmosDbService)
        {
            appSettings = options.Value;
            lineMessaingClient = new LineMessagingClient(appSettings.LineSettings.ChannelAccessToken);
            this.cosmosDbService = cosmosDbService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]JsonElement req)
        {
            var events = WebhookEventParser.Parse(req.ToString());
            var app = new LineBotApp(lineMessaingClient, cosmosDbService);
            await app.RunAsync(events);
            return new OkResult();

        }
    }
}
