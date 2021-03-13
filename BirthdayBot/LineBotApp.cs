using BirthdayBot.Models;
using BirthdayBot.Services;
using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BirthdayBot
{
    /// <summary>
    /// Bot本体
    /// 受け取ったイベントを元に各処理を実行
    /// </summary>
    public class LineBotApp : WebhookApplication
    {
        private LineMessagingClient messagingClient { get; }

        private readonly ICosmosDbService cosmosDbService;

        // Botの説明定型文
        private readonly string descriptionMessage =
            "------------------------------\n" +
            " ご利用ありがとうございます！\n\n" +
            "‣「X月X日 名前」で登録\n" +
            "‣「X月X日 名前 更新 X月X日\n" +
            " 　名前」で更新\n" +
            "‣「X月X日 名前 削除」で削除\n\n" +
            "  を行います！\n" +
            "   (項目間には空白が必要です)\n" +
            "   (姓名間に空白は不要です)\n" +
            "   (月日はX/Xの形式でも可です)\n\n" +
            " また「一覧」と入力いただくと\n" +
            " 登録内容をお知らせします！\n" +
            " もし誕生日の方が居れば\n" +
            " 当日0時ごろお知らせします！\n\n" +
            " 登録限度は50件となります！\n\n" +
            " なお友達登録を解除されますと\n" +
            " 登録データを全て削除致します\n" +
            " ので、ご留意ください！\n" +
            "------------------------------";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LineBotApp(LineMessagingClient lineMessagingClient, ICosmosDbService cosmosDbService)
        {
            this.messagingClient = lineMessagingClient;
            this.cosmosDbService = cosmosDbService;
        }

        /// <summary>
        /// 受信したメッセージタイプに応じた分岐処理
        /// </summary>
        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            // 利用者からテキストを受信した場合、処理開始
            switch (ev.Message.Type)
            {
                case EventMessageType.Text: await this.HandleTextAsync(ev.ReplyToken, ((TextEventMessage)ev.Message).Text, ev.Source.UserId);
                    break;
            }
        }

        /// <summary>
        /// テキスト受信時の処理
        /// </summary>
        private async Task HandleTextAsync(string replyToken, string userMessage, string userId)
        {
            userMessage = userMessage.ToLower().Replace("　", " ");

            // 半角スペースごとにユーザメッセージを区切った配列
            var userMessageArray = userMessage.Split(" ");

            // MM月dd日(or MM/dd) 何かしらの文字列 更新 MM月dd日(or MM/dd) 何かしらの文字列 の場合、更新
            // MM月dd日(or MM/dd) 何かしらの文字列 削除 の場合、削除
            // MM月dd日(or MM/dd) 何かしらの文字列 の場合、登録
            // （正規表現で数字部分は日付に適した値か、空白が連続していないかを判定）
            // 「一覧」と入力された場合、そのユーザの登録データをまとめてリプライ
            // 上記のどれにも該当しない場合、Botの使い方をリプライ
            if (Regex.IsMatch(userMessage, @"^([1-9１-９]|[1１][0-2０-２])月([1-9１-９]|[12１２][0-9０-９]|[3３][01０１])日\s\S*\s更新\s([1-9１-９]|[1１][0-2０-２])月([1-9１-９]|[12１２][0-9０-９]|[3３][01０１])日\s\S*$") ||
                Regex.IsMatch(userMessage, @"^([1-9１-９]|[1１][0-2０-２])/([1-9１-９]|[12１２][0-9０-９]|[3３][01０１])\s\S*\s更新\s([1-9１-９]|[1１][0-2０-２])/([1-9１-９]|[12１２][0-9０-９]|[3３][01０１])\s\S*$"))
            {
                // ユーザメッセージ配列から月日をMM/ddの形に変換(MM/ddの場合は空振る)
                var dateTimeStringForSelect = this.MMddParser(userMessageArray[0]);
                var dateTimeStringForUpdate = this.MMddParser(userMessageArray[3]);

                var updateItem = (List<Item>)await this.cosmosDbService.GetItemsAsync(
                    "SELECT * FROM Birthdays b WHERE b.userId = \'" + userId + "\' AND b.birthday = \'" + dateTimeStringForSelect + "\' AND b.name = \'" + userMessageArray[1] + "\'");

                // 更新対象のItemがあれば誕生日と名前を入力テキストの内容に書き換えて更新
                if (updateItem.Count > 0)
                {
                    updateItem[0].Birthday = dateTimeStringForUpdate;
                    updateItem[0].Name = userMessageArray[4];

                    // 更新
                    await this.cosmosDbService.UpdateItemAsync(updateItem[0].Id, updateItem[0]);
                    await this.messagingClient.ReplyMessageAsync(replyToken, "更新しました！");
                }
                else
                {
                    await this.messagingClient.ReplyMessageAsync(replyToken, "更新に失敗してしまいました！\n入力いただいた誕生日データはまだ登録されていないようです！");
                }
            }
            else if (Regex.IsMatch(userMessage, @"^([1-9１-９]|[1１][0-2０-２])月([1-9１-９]|[12１２][0-9０-９]|[3３][01０１])日\s\S*\s削除$") ||
                     Regex.IsMatch(userMessage, @"^([1-9１-９]|[1１][0-2０-２])/([1-9１-９]|[12１２][0-9０-９]|[3３][01０１])\s\S*\s削除$"))
            {
                // ユーザメッセージ配列から月日をMM/ddの形に変換(MM/ddの場合は空振る)
                var dateTimeStringForDelete = this.MMddParser(userMessageArray[0]);

                // 削除　結果のみ返す
                var deleteResult = await this.cosmosDbService.DeleteItemAsync("SELECT * FROM Birthdays b WHERE b.userId = \'" + userId + "\' AND b.birthday = \'" + dateTimeStringForDelete + "\' AND b.name = \'" + userMessageArray[1] + "\'");

                // 削除結果によりリプライを分岐
                if (deleteResult == true)
                {
                    await this.messagingClient.ReplyMessageAsync(replyToken, "削除しました！");
                }
                else
                {
                    await this.messagingClient.ReplyMessageAsync(replyToken, "削除に失敗してしまいました！\n入力いただいた誕生日データはまだ登録されていないようです！");
                }
            }
            else if (Regex.IsMatch(userMessage, @"^([1-9１-９]|[1１][0-2０-２])月([1-9１-９]|[12１２][0-9０-９]|[3３][01０１])日\s\S*$") ||
                     Regex.IsMatch(userMessage, @"^([1-9１-９]|[1１][0-2０-２])/([1-9１-９]|[12１２][0-9０-９]|[3３][01０１])\s\S*$"))
            {
                // 登録候補のItemを作成
                Item item = new Item
                {
                    // Idはその都度一意となるよう適当な文字列を生成
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Name = userMessageArray[1],
                    Birthday = this.MMddParser(userMessageArray[0]),
                };

                // userIdで全件SELECTした結果
                var selectAllItemResultList = (List<Item>)await this.cosmosDbService.GetItemsAsync("SELECT * FROM Birthdays b WHERE b.userId = \'" + item.UserId + "\'");

                // 登録予定のItemの内容でSELECTした結果。期待値0件
                var selectNewItemResultList = (List<Item>)await this.cosmosDbService.GetItemsAsync("SELECT * FROM Birthdays b WHERE b.userId = \'" + item.UserId + "\' AND b.birthday = \'" + item.Birthday + "\' AND b.name = \'" + item.Name + "\'");

                // SELECT結果に応じて登録可否の判定
                // 登録限度は暫定で50件。超える場合はメッセージをリプライ
                // 登録予定のItemがユーザの登録内容になければ登録
                // 登録予定のItemと同内容のデータが既にあればメッセージをリプライ
                if (selectAllItemResultList.Count >= 50)
                {
                    await this.messagingClient.ReplyMessageAsync(replyToken, "登録件数が限度の50件に達しているようです！");
                }
                else if (selectNewItemResultList.Count == 0)
                {
                    // 登録
                    await this.cosmosDbService.AddItemAsync(item);
                    await this.messagingClient.ReplyMessageAsync(replyToken, "登録しました！");
                }
                else
                {
                    await this.messagingClient.ReplyMessageAsync(replyToken, "入力いただいた誕生日データは既に登録されているようです！");
                }
            }
            else if (userMessage == "一覧")
            {
                // userIdで全件取得したリスト
                var selectAllItemResultList = (List<Item>)await this.cosmosDbService.GetItemsAsync("SELECT * FROM Birthdays b WHERE b.userId = \'" + userId + "\'");
                var userRegisteredItemList = new List<string>();

                // データがあれば整形してリプライ
                // なければないとリプライ
                if (selectAllItemResultList.Count != 0)
                {
                    // SELECT結果から誕生日と名前を取り出し、メッセージに整形してリストに詰める
                    foreach (var item in selectAllItemResultList)
                    {
                        userRegisteredItemList.Add(item.Birthday + " " + item.Name + "さん");
                    }

                    // 誕生日の早い順にソートして一覧を返す
                    userRegisteredItemList.Sort();
                    await this.messagingClient.ReplyMessageAsync(replyToken, string.Join("\n", userRegisteredItemList) + "\n計" + userRegisteredItemList.Count + "件が登録されています！");
                }
                else
                {
                    await this.messagingClient.ReplyMessageAsync(replyToken, "まだ誕生日データがないようです！");
                }
            }
            else
            {
                // Bot操作のキーワードに該当しなかった場合は説明定型文をリプライ
                await this.messagingClient.ReplyMessageAsync(replyToken, this.descriptionMessage);
            }
        }

        /// <summary>
        /// 友達追加時の処理
        /// </summary>
        protected override async Task OnFollowAsync(FollowEvent ev)
        {
            // Botの使い方をリプライ
            await this.messagingClient.ReplyMessageAsync(ev.ReplyToken, this.descriptionMessage);
        }

        /// <summary>
        /// 友達解除時の処理
        /// </summary>
        protected override async Task OnUnfollowAsync(UnfollowEvent ev)
        {
            // 当該UserIdを持つ登録データを全て削除する
            await this.cosmosDbService.DeleteItemAsync("SELECT * FROM Birthdays b WHERE b.userId = \'" + ev.Source.UserId + "\'");
        }

        /// <summary>
        /// M月d日やM/d→MM/ddへの変換処理
        /// </summary>
        private string MMddParser(string target)
        {
            // .Net Coreでは規定で2バイト文字に対応していない
            // 引数で与えられた日付の数字が全角の場合、半角に変換するため
            // エンコーディングプロバイダを登録しておく
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return DateTime.Parse(
                        Strings.StrConv(target, VbStrConv.Narrow, 0x411),
                        new System.Globalization.CultureInfo("ja-JP", true),
                        System.Globalization.DateTimeStyles.AssumeLocal).ToString(@"MM/dd");
        }
    }
}
