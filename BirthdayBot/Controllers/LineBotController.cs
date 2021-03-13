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
    /// <summary>
    /// LineBotのコントローラー
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LineBotController : Controller
    {
        private static LineMessagingClient lineMessaingClient;
        private readonly ICosmosDbService cosmosDbService;
        private AppSettings appSettings;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LineBotController(IOptions<AppSettings> options, ICosmosDbService cosmosDbService)
        {
            this.appSettings = options.Value;
            lineMessaingClient = new LineMessagingClient(this.appSettings.LineSettings.ChannelAccessToken);
            this.cosmosDbService = cosmosDbService;
        }

        /// <summary>
        /// アプリ本体の実行
        /// </summary>
        /// <param name="req">LINEから届いたJson形式のリクエスト本体.</param>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]JsonElement req)
        {
            // 受け取ったリクエストをLINEのSDK上で扱えるイベントに変換
            var events = WebhookEventParser.Parse(req.ToString());

            // アプリ実行
            var app = new LineBotApp(lineMessaingClient, this.cosmosDbService);
            await app.RunAsync(events);
            return new OkResult();
        }
    }
}
