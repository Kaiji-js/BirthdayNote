using BirthdayBot.Middleware;
using BirthdayBot.Models;
using BirthdayBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace BirthdayBot
{
    /// <summary>
    /// アプリの動作定義
    /// </summary>
    public class Startup
    {
        public IConfiguration Configuration { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// DI定義
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.Configure<AppSettings>(this.Configuration);
            services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync(this.Configuration.GetSection("CosmosDbSettings")).GetAwaiter().GetResult());
        }

        /// <summary>
        /// HTTPのリクエストパイプライン作成
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            // リクエストがLINEプラットフォーム上から送信されたものか検証
            app.UseLineValidationMiddleware(this.Configuration.GetSection("LineSettings")["ChannelSecret"]);

            app.UseEndpoints(endpoints =>
            {
                // エンドポイントにLineBotControllerのPostメソッドを指定
                endpoints.MapControllerRoute(name: "defaulf", pattern: "{controller=LineBot}/{action=Post}/{id?}");
            });
        }

        /// <summary>
        /// Azure CosmosDBの初期化
        /// </summary>
        private static async Task<CosmosDbService> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
        {
            // 設定ファイルからDB名・接続文字列等を取得
            string databaseName = configurationSection.GetSection("DatabaseName").Value;
            string contairName = configurationSection.GetSection("ContainerName").Value;
            string account = configurationSection.GetSection("Account").Value;
            string key = configurationSection.GetSection("Key").Value;

            // 取得した設定を元にCosmosDBサービスを立ち上げ
            CosmosClient client = new CosmosClient(account, key);
            CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, contairName);

            // 指定のDB、コンテナがなければ作る
            DatabaseResponse databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await databaseResponse.Database.CreateContainerIfNotExistsAsync(contairName, "/id");

            return cosmosDbService;
        }
    }
}
