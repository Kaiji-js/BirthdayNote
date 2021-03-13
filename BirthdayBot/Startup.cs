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
    /// �A�v���̓����`
    /// </summary>
    public class Startup
    {
        public IConfiguration Configuration { get; }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// DI��`
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.Configure<AppSettings>(this.Configuration);
            services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync(this.Configuration.GetSection("CosmosDbSettings")).GetAwaiter().GetResult());
        }

        /// <summary>
        /// HTTP�̃��N�G�X�g�p�C�v���C���쐬
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

            // ���N�G�X�g��LINE�v���b�g�t�H�[���ォ�瑗�M���ꂽ���̂�����
            app.UseLineValidationMiddleware(this.Configuration.GetSection("LineSettings")["ChannelSecret"]);

            app.UseEndpoints(endpoints =>
            {
                // �G���h�|�C���g��LineBotController��Post���\�b�h���w��
                endpoints.MapControllerRoute(name: "defaulf", pattern: "{controller=LineBot}/{action=Post}/{id?}");
            });
        }

        /// <summary>
        /// Azure CosmosDB�̏�����
        /// </summary>
        private static async Task<CosmosDbService> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
        {
            // �ݒ�t�@�C������DB���E�ڑ������񓙂��擾
            string databaseName = configurationSection.GetSection("DatabaseName").Value;
            string contairName = configurationSection.GetSection("ContainerName").Value;
            string account = configurationSection.GetSection("Account").Value;
            string key = configurationSection.GetSection("Key").Value;

            // �擾�����ݒ������CosmosDB�T�[�r�X�𗧂��グ
            CosmosClient client = new CosmosClient(account, key);
            CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, contairName);

            // �w���DB�A�R���e�i���Ȃ���΍��
            DatabaseResponse databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await databaseResponse.Database.CreateContainerIfNotExistsAsync(contairName, "/id");

            return cosmosDbService;
        }
    }
}
