using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tht.EDRMS.Models
{
    public class BusinessAreaDTO
    {
        public BusinessAreaDTO()
        {
            DocumentTypes = new List<DocumentTypeDTO>();
        }
        public string Guid { get; set; }
        public string Name { get; set; }
        public List<DocumentTypeDTO> DocumentTypes { get; set; }
    }
}
