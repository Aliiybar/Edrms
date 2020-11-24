using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace tht.EDRMS.Models
{
    public class PostModel
    {
        public string BusinessArea { get; set; }
        public string DocumentType { get; set; }
        public string placeRef { get; set; }
        public string inspectionCompletionDate { get; set; }
        public string expiryDate { get; set; }
        public string Contractor { get; set; }
        public IFormFile DocFile { get; set; }

    }
     
}
