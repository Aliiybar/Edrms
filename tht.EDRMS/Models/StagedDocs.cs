using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tht.EDRMS.Models
{

        public class BuildingSafetyCertificateDocFields
        {
            [JsonProperty("Id")]
            public string Id { get; set; }

            [JsonProperty("FileLeafRef")]
            public string FileName { get; set; }

            [JsonProperty("ContentTypeId")]
            public string ContentTypeId { get; set; }

            [JsonProperty("Title")]
            public string Title { get; set; }

            [JsonProperty("BusinessArea")]
            public BusinessArea BusinessArea { get; set; }

            [JsonProperty("DocumentType")]
            public DocumentType DocumentType { get; set; }

            [JsonProperty("PlaceRef")]
            public string PlaceRef { get; set; }

            [JsonProperty("InspectionCompletionDate")]
            public DateTime? InspectionCompletionDate { get; set; }

            [JsonProperty("ValidToDate")]
            public DateTime? ValidToDate { get; set; }

            [JsonProperty("DocumentStatus")]
            public string DocumentStatus { get; set; }

            [JsonProperty("Contractor")]
            public Contractor Contractor { get; set; }

            [JsonProperty("FileRef")]
            public string FilePath { get; set; }


        }

        public class BuildingSafetyCertificateDocResults
        {
            [JsonProperty("results")]
            public List<BuildingSafetyCertificateDocFields> results { get; set; }
        }

        public class BuildingSafetyCertificateDoc
        {
            [JsonProperty("d")]
            public BuildingSafetyCertificateDocResults data { get; set; }
        }
  
}
