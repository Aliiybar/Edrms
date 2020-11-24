namespace tht.EDRMS.Business.SharePoint.Contracts
{
    public interface ISharePointSettings
    {
        string AccountBaseUrl { get; }
        string SharePointBaseUrl { get; }
        string ClientId { get; }
        string ResourceId { get; }
        string Secret { get; }
        string TenantId { get; }
        string Library { get; }
    }
}