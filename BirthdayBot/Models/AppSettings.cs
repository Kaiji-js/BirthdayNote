namespace BirthdayBot.Models
{
    /// <summary>
    /// 設定ファイル定義
    /// </summary>
    public class AppSettings
    {
        public LineSettings LineSettings { get; set; }
    }

    /// <summary>
    /// 設定ファイル内 LINE Messageing APIの設定
    /// </summary>
    public class LineSettings
    {
        public string ChannelSecret { get; set; }

        public string ChannelAccessToken { get; set; }
    }
}
