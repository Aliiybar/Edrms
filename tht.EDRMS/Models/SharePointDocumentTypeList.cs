using System.Collections.Generic;
using Newtonsoft.Json;

namespace tht.EDRMS.Models
{
    public class BusinessArea
    {
        [JsonProperty("TermGuid")]
        public string TermGuid { get; set; }
        public string Name { get; set; }
    }

    public class DocumentType
    {
        [JsonProperty("TermGuid")]
        public string TermGuid { get; set; }
        public string Name { get; set; }
    }

    public class ConfigSettingsListFields
    {
        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("BusinessArea")]
        public BusinessArea BusinessArea { get; set; }

        [JsonProperty("DocumentType")]
        public DocumentType DocumentType { get; set; }

        [JsonProperty("DocumentContentType")]
        public string DocumentContentType { get; set; }

        [JsonProperty("SiteUrl")]
        public string SiteUrl { get; set; }

        [JsonProperty("StagePathUrl")]
        public string StagePathUrl { get; set; }

        [JsonProperty("PathUrl")]
        public string PathUrl { get; set; }

        [JsonProperty("ExpiryPeriod")]
        public int ExpiryPeriod { get; set; }

        //POCO
        public List<ContentTypeFields> ContentTypeProperties { get; set; }
    }

    public class ConfigSettingsListResults
    {
        [JsonProperty("results")]
        public List<ConfigSettingsListFields> results { get; set; }
    }

    public class ConfigSettingsList
    {
        [JsonProperty("d")]
        public ConfigSettingsListResults data { get; set; }
    }
}
