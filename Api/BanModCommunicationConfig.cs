//credits and licenses in the resources folder/
namespace BanMod
{
    public static class BanModCommunicationConfig
    {
        public const string ReportUrl = BanModApiConfig.ApiBaseUrl + "/api/communication/report";
        public const string MyReportsUrl = BanModApiConfig.ApiBaseUrl + "/api/communication/reports/mine";
        public const string MessagesUrl = BanModApiConfig.ApiBaseUrl + "/api/communication/messages";
        public const float MessagePollIntervalSeconds = 15f;

        public static string ReportItemUrl(int reportId)
        {
            return BanModApiConfig.ApiBaseUrl + "/api/communication/reports/" + reportId;
        }

        public static string ReportMessagesUrl(int reportId)
        {
            return BanModApiConfig.ApiBaseUrl + "/api/communication/reports/" + reportId + "/messages";
        }

        public static string ReportMessageUrl(int reportId)
        {
            return BanModApiConfig.ApiBaseUrl + "/api/communication/reports/" + reportId + "/message";
        }

        public static string ReportReadUrl(int reportId)
        {
            return BanModApiConfig.ApiBaseUrl + "/api/communication/reports/" + reportId + "/read";
        }

        public static string ReportDeleteUrl(int reportId)
        {
            return BanModApiConfig.ApiBaseUrl + "/api/communication/reports/" + reportId + "/delete";
        }

        public static string ReportCloseUrl(int reportId)
        {
            return BanModApiConfig.ApiBaseUrl + "/api/communication/reports/" + reportId + "/close";
        }

        public static string ReportResolveUrl(int reportId)
        {
            return BanModApiConfig.ApiBaseUrl + "/api/communication/reports/" + reportId + "/resolve";
        }

        public static string MessageReadUrl(int messageId)
        {
            return BanModApiConfig.ApiBaseUrl + "/api/communication/messages/" + messageId + "/read";
        }
    }
}
