using System.Collections.Generic;

namespace tht.EDRMS.Models
{
    public class DocumentTypeDTO
    {
        public string Guid { get; set; }
        public string Title { get; set; }
        public string DocumentContentType { get; set; }
        public string DocumentType { get; set; }
        public string DocumentTypeGuid { get; set; }
        public string BusinessArea { get; set; }

        public string SiteUrl { get; set; }
        public string StagePathUrl { get; set; }
        public int? ExpiryPeriod { get; set; }

        public string BusinessAreaGuid { get; set; }
    }

   public class DocumentData
    {
        public int? DocumentId { get; set; }
        public string ContentTypeId { get; set; }
        public string ContentTypeName { get; set; }
        public string BusinessAreaId { get; set; }
        public string BusinessAreaName { get; set; } // if this class is used on it's own this can be handy
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Token { get; set; }
        public string DocumentStatus { get; set; }
        public List<MetaData>  MetaDatas { get; set; }

    }

    public class MetaData
    {
        public string EntityPropertyName { get; set; }
        public string Value { get; set; }
    }
}
 