using Microsoft.Extensions.Configuration;
using tht.EDRMS.Business.SharePoint.Contracts;

namespace tht.EDRMS.Business.SharePoint.Implementation
{
    public class SharePointSettings : ISharePointSettings
    {
        private readonly IConfiguration _configuration;

        public SharePointSettings(IConfiguration configuration)
        {
            _configuration = configuration;
            ClientId = _configuration["Sharepoint:clientId"];
            TenantId = _configuration["Sharepoint:tenantId"];
            Secret = _configuration["Sharepoint:secret"];
            ResourceId = _configuration["Sharepoint:resourceId"];
            AccountBaseUrl = _configuration["Sharepoint:baseAddress"];
            SharePointBaseUrl = _configuration["Sharepoint:sharePointBaseUrl"];
            Library = _configuration["Sharepoint:library"];
            DashboardBaseUrl = _configuration["Dashboard:baseAddress"];
        }

        public string ClientId { get; }
        public string Secret { get; }
        public string TenantId { get; }
        public string ResourceId { get; }
        public string AccountBaseUrl { get; }
        public string SharePointBaseUrl { get; }
        public string Library { get; set; }
        public string DashboardBaseUrl { get;  }
    }
}
