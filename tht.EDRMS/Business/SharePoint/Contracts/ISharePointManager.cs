using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tht.EDRMS.Models;

namespace tht.EDRMS.Business.SharePoint.Contracts
{
    public interface ISharePointManager
    {
        Task<string> GenerateToken();
        Task<List<BusinessAreaDTO>> GetBusinessAreas(string token);
        Task<List<ContentTypeFields>> GetDocumentTypeMetaData(string documentTypeName, string token);
        Task<byte[]> DownloadFile(string token, string fileUrl);

        string Upload(DocumentData documentData);
        bool UpdateDocument(DocumentData documentData);
        Task<List<DocumentData>> GetAllDocuments(string token);
        Task<List<BuildingSafetyCertificateDocFields>> StagedDocList(string token);
        Task<List<ContractorFields>> GetContractorsList(string token);
        Task<DocumentData> GetDocument(string token, int documentId);
        
    }
}
