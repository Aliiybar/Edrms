using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using tht.EDRMS.Business.SharePoint.Contracts;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.SharePoint.Client;
using Microsoft.AspNetCore.Hosting;
using tht.EDRMS.Models;
using Microsoft.SharePoint.Client.Taxonomy;

namespace tht.EDRMS.Business.SharePoint.Implementation
{
    public class SharepointManager : ISharePointManager
    {
        private readonly ISharePointSettings _sharePointSettings;
        private readonly IWebHostEnvironment _webHostEnvironment;
        const string libName = "StagedDocuments";

        public SharepointManager(ISharePointSettings sharePointSettings, IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _sharePointSettings = sharePointSettings;
        }


        /// <summary>
        /// Generates token for sharepoint based on ClientId, TenantId, ClientSecret etc. 
        /// </summary>
        /// <returns>JWT Token </returns>
        public async Task<string> GenerateToken()
        {
            string token = string.Empty;
            var client = new HttpClient();

            var nameValueCollection = new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id",  _sharePointSettings.ClientId + "@"+ _sharePointSettings.TenantId),
                new KeyValuePair<string, string>("client_secret", _sharePointSettings.Secret),
                new KeyValuePair<string, string>("resource", _sharePointSettings.ResourceId + "@" + _sharePointSettings.TenantId)
            };

            var url = _sharePointSettings.AccountBaseUrl  + _sharePointSettings.TenantId + "/tokens/OAuth/2";
            var result = client.PostAsync(url, new FormUrlEncodedContent(nameValueCollection)).Result;
            if (result.IsSuccessStatusCode)
            {
                var tokenObj = result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<SharePointToken>(tokenObj);
                token = data.access_token;
            }
            return token;
        }

        /// <summary>
        /// Gets the list of 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<List<BusinessAreaDTO>> GetBusinessAreas(string token)
        {
            string url = _sharePointSettings.SharePointBaseUrl + "/_api/web/lists/getbytitle('ConfigSettings')/items?$select *";
          
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var res = await client.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
                var data = res.Content.ReadAsStringAsync().Result;
                var listData = JsonConvert.DeserializeObject<ConfigSettingsList>(data).data.results;

                return await ConvertToDTO(listData, token);
            }
            return null;
        }

        /// <summary>
        /// Return meta data for a given Document Type Name
        /// </summary>
        /// <param name="documentTypeName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<List<ContentTypeFields>> GetDocumentTypeMetaData(string documentTypeName, string token)
        {
            List<ContentTypeFields> documentFields = null;
            string url = _sharePointSettings.SharePointBaseUrl + "/_api/web/AvailableContentTypes?$select=Name,StringId&$filter=Name eq '" + documentTypeName + "'";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var res = await client.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
                var data = res.Content.ReadAsStringAsync().Result;
                var contentData = JsonConvert.DeserializeObject<Models.ContentType>(data).data.results.First();

                //get content fields data

                string contentUrl = _sharePointSettings.SharePointBaseUrl + "/_api/web/AvailableContentTypes('" + contentData.ContentTypeId + "')/fields?$filter=Hidden eq false and Group ne '_Hidden'";
                var contentResult = await client.GetAsync(contentUrl);

                if (contentResult.IsSuccessStatusCode)
                {
                    var cData = contentResult.Content.ReadAsStringAsync().Result;
                    var cFields = JsonConvert.DeserializeObject<ContentTypeFieldsData>(cData).data.results;

                    if (cFields != null && cFields.Any())
                    {
                        foreach (var f in cFields)
                        {
                            f.ContentTypeId = contentData.ContentTypeId;
                            f.ContentTypeName = contentData.ContentTypeName;
                        }
                    }

                    documentFields = cFields;
                }
            }

            return documentFields;
        }

        public async Task<List<BuildingSafetyCertificateDocFields>> StagedDocList(string token)
        {
            string url = _sharePointSettings.SharePointBaseUrl + "/propertyservices-uat/_api/web/lists/getbytitle('StagedDocuments')/items?$select=FileLeafRef,FileRef,*";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var res = client.GetAsync(url).Result;
            var retVal = new List<BuildingSafetyCertificateDocFields>();
            if (res.IsSuccessStatusCode)
            {
                var data = res.Content.ReadAsStringAsync().Result;
                var listData = JsonConvert.DeserializeObject<BuildingSafetyCertificateDoc>(data).data.results;

                //get taxonomy data
                if (listData != null && listData.Any())
                {
                    foreach (var i in listData)
                    {
                        if (i.BusinessArea != null)
                        {
                            i.BusinessArea.Name = await GetTaxonomyTerm(i.BusinessArea.TermGuid, token);
                        }

                        if (i.DocumentType != null)
                        {
                            i.DocumentType.Name = await GetTaxonomyTerm(i.DocumentType.TermGuid, token);
                        }

                        if (i.Contractor != null)
                        {
                            i.Contractor.TermName = await GetTaxonomyTerm(i.Contractor.TermGuid, token);
                        }

                    }
                }
                retVal = listData;
            }
            return retVal;

        }
        public async Task<List<DocumentData>> GetAllDocuments(string token)
        {
            var rowLimit = 100;
            var retVal = new List<DocumentData>();
            using (ClientContext ctx = GetClientContext( token))
            {
                Web myWeb = ctx.Web;
                List myLib = myWeb.Lists.GetByTitle(libName);
                CamlQuery camlQuery = new CamlQuery();
                camlQuery.ViewXml = "<View><Query><Where><Geq><FieldRef Name='ID'/>" +
                    "<Value Type='Number'>0</Value></Geq></Where></Query><RowLimit>" + rowLimit + "</RowLimit></View>";

                var list = myLib.GetItems(camlQuery);

                ctx.Load(list);

                await ctx.ExecuteQueryAsync();

                foreach (ListItem listItem in list)
                {
                    var metaDatas = new List<MetaData>();
                    foreach(var meta in listItem.FieldValues)
                    {
                        if(meta.Value != null)
                        {
                            metaDatas.Add(new MetaData()
                            {
                                EntityPropertyName = meta.Key,
                                Value = meta.Value.ToString()
                            });
                        }
                    }
                 

                    var businessArea = "";
                    var businessAreaId = "";

                    if(listItem["BusinessArea"] != null)
                    {
                        var ba = (Dictionary<string, object>)listItem["BusinessArea"];
                        businessAreaId = ba["TermGuid"].ToString();
                        businessArea = ba["Label"].ToString();

                    }
                    //TODO: Find the correct  ContentTypeName 
                    retVal.Add(new DocumentData()
                    {
                        DocumentId = listItem.Id,
                        Token = token,
                        ContentTypeId = listItem["ContentTypeId"].ToString(),
                        //ContentTypeName = "",
                        BusinessAreaId = businessAreaId, 
                        MetaDatas = metaDatas
                    });
                    //TODO : Ask if I need to do anything with it 
                     
                               // "entityPropertyName": "DocumentStatus",
                               //"value": "Staging"
                    
                }
            }

            return retVal;
        }

        public bool UpdateDocument(DocumentData documentData)
        {
            if(documentData.DocumentId != null) { 
                try
                {
                    using (ClientContext ctx = GetClientContext(documentData.Token))
                    {
                        Web myWeb = ctx.Web;
                        List myLib = myWeb.Lists.GetByTitle(libName);
                        ListItem myListItem = myLib.GetItemById((int)documentData.DocumentId);

                        ctx.Load(myListItem);
                        ctx.ExecuteQuery();

                        foreach (var item in documentData.MetaDatas)
                        {


                            if (item.EntityPropertyName == "BusinessArea" || item.EntityPropertyName == "DocumentType" || item.EntityPropertyName == "Contractor")
                            {

                                UpdateTaxonomyField(ctx, myLib, myListItem, item.EntityPropertyName, item.Value, item.Label);
                            }
                            else
                            {
                                myListItem[item.EntityPropertyName] = item.Value;
                            }

                        }

                            myListItem.Update();
                        ctx.ExecuteQuery();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                 
                }
            }
            return false;
        }

 

        public async Task<DocumentData> GetDocument(string token, int documentId)
        {
    
            try
            {
                using (ClientContext ctx = GetClientContext(token))
                {
                    Web myWeb = ctx.Web;
                    List myLib = myWeb.Lists.GetByTitle(libName);
                    ListItem myListItem = myLib.GetItemById (documentId);

                    ctx.Load(myListItem);
                    await ctx.ExecuteQueryAsync ();
                    var documentData = new DocumentData();
                    if (myListItem["BusinessArea"] != null)
                    {
                        var businessArea = (TaxonomyFieldValue)myListItem["BusinessArea"];
                        //   var businessArea = (Dictionary<string, Dictionary<string, string>>)myListItem["BusinessArea"];
                        if (businessArea != null)
                        {
                            documentData.BusinessAreaId = businessArea.TermGuid.ToString();
                            documentData.BusinessAreaName = businessArea.Label.ToString();
                        }
                    }
                    documentData.ContentTypeId = myListItem["ContentTypeId"]?.ToString();
                    documentData.FilePath = myListItem["FileRef"]?.ToString();
                    documentData.FileName = myListItem["FileLeafRef"]?.ToString();
                    documentData.DocumentStatus = myListItem["DocumentStatus"]?.ToString();
                    documentData.DocumentId = documentId;
                    documentData.MetaDatas = new List<MetaData>();
                    documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "PlaceRef", Value = myListItem["PlaceRef"]?.ToString() });
                    if (myListItem["Contractor"] != null)
                    {
                        var contractor = (TaxonomyFieldValue)myListItem["Contractor"];
                        if (contractor != null)
                        {
                            documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "Contractor", Value = contractor.TermGuid.ToString() });

                        }
                    }
                    if (myListItem["DocumentType"] != null)
                    {
                        var documentType = (TaxonomyFieldValue)myListItem["DocumentType"];
                        if (documentType != null)
                        {
                            documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "DocumentType", Value = documentType.TermGuid.ToString() });
                        }
                    }
                  //  documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "DocumentType", Value = myListItem["DocumentType"]?.ToString() });
                    documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "InspectionCompletionDate", Value = myListItem["InspectionCompletionDate"]?.ToString() });
                    documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "ValidToDate", Value = myListItem["ValidToDate"]?.ToString() });

                    return documentData;
                         
               
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
          
        }

        public string Upload(DocumentData documentData)
        {
          //  filePath is temporarily overriden. It will be removed once the first part of upload completed
          //  string filePath = "C:\\temp\\Dummy.pdf";
            string filePath = documentData.FilePath;
                string fileName = Path.GetFileName(filePath);

                try
                {
                    using (ClientContext CContext = GetClientContext( documentData.Token))
                    {
                        /// <summary>
                        /// Method 1: use FileCreationInformation to handle uploaded documents data
                        /// Tip: use ContentStream of FileCreationInformation class to upload large files...
                        /// </summary>

                        System.IO.FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                        FileCreationInformation fcInfo = new FileCreationInformation();
                        fcInfo.ContentStream = fs;
                        fcInfo.Url = fileName;
                        fcInfo.Overwrite = true;

                        Web myWeb = CContext.Web;
                        List myLib = myWeb.Lists.GetByTitle(libName);
                        Microsoft.SharePoint.Client.File uploadedFileRef = myLib.RootFolder.Files.Add(fcInfo);

                        //load all contenttypes that are applicable for the doc-library
                        CContext.Load(myLib.ContentTypes);
                        CContext.ExecuteQuery();

                    //assign content type to the uploaded documents
                    //Microsoft.SharePoint.Client.ContentType myContentType = myLib.ContentTypes.Where(ctx => ctx.Name == documentData.ContentTypeName).First();

                        uploadedFileRef.ListItemAllFields["ContentTypeId"] = documentData.ContentTypeId;
                        uploadedFileRef.ListItemAllFields["BusinessArea"] =    documentData.BusinessAreaId;
                        if (documentData.MetaDatas != null)
                        {
                            foreach (var item in documentData.MetaDatas)
                            {
                                if((item.EntityPropertyName == "ExpiryDate" || item.EntityPropertyName == "InspectionCompletionDate")  && item.Value != null)
                                {
                                    if(item.EntityPropertyName == "ExpiryDate")
                                    {
                                        uploadedFileRef.ListItemAllFields["ValidToDate"] = Convert.ToDateTime(item.Value);
                                    }
                                    else
                                    {
                                        uploadedFileRef.ListItemAllFields[item.EntityPropertyName] = Convert.ToDateTime(item.Value);
                                    }
                               
                                }
                                else
                                {
                                    uploadedFileRef.ListItemAllFields[item.EntityPropertyName] = item.Value;
                                }
                             
                            }
                        }
                     
                        uploadedFileRef.ListItemAllFields.Update();
                        CContext.ExecuteQuery();
                    return "OK";
                    }
                }
                catch (Exception ex)
                {
                    
                     return ex.Message;
                }
            

        }

        public async Task<byte[]> DownloadFile(string token, string fileUrl)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await client.GetByteArrayAsync(fileUrl);

        }


        public async Task<List<ContractorFields>> GetContractorsList(string token)
        {
            string contractorTermSetId = "5e7f7b17-a35e-403f-a836-4de99216a492";
            var listData =  new List<ContractorFields>();
          
            string url = _sharePointSettings.SharePointBaseUrl + "/_api/web/lists/getbytitle('TaxonomyHiddenList')/items?$filter=IdForTermSet eq '" + contractorTermSetId + "'";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
           
            var res = await client.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
                var data = res.Content.ReadAsStringAsync().Result;
                 listData = JsonConvert.DeserializeObject<ContractorList>(data).data.results;
            }
            return listData;
        }


        /// <summary>
        /// Converts sharepoint ConfigSettingsListFields to BusinessArea object
        /// to provide simple data
        /// </summary>
        /// <param name="sharepointData"> data list created by  JSON data returned from Sharepoint</param>
        /// <returns></returns>
        private async Task<List<BusinessAreaDTO>> ConvertToDTO(List<ConfigSettingsListFields> sharepointData, string token)
        {
            var retVal = new List<BusinessAreaDTO>();
            var businessAreas = sharepointData.Select(o => new { o.BusinessArea.TermGuid, o.BusinessArea.Name })
                .Distinct()
                .OrderBy(k => k.Name);
            foreach (var businessArea in businessAreas)
            {
                var documentTypes = new List<DocumentTypeDTO>();
                var docTypes = sharepointData.Select(d => new DocumentTypeDTO
                {
                    Guid = d.DocumentType.TermGuid,
                    DocumentContentType = d.DocumentContentType,
                    DocumentType = GetTaxonomyTerm(d.DocumentType.TermGuid, token).Result,
                    DocumentTypeGuid = d.DocumentType.TermGuid,
                    Title = d.Title,
                    ExpiryPeriod = d.ExpiryPeriod,
                    SiteUrl = d.SiteUrl,
                    StagePathUrl = d.StagePathUrl,

                    BusinessAreaGuid = d.BusinessArea.TermGuid

                })
                    .Where(b => b.BusinessAreaGuid == businessArea.TermGuid)
                    .ToList();

                retVal.Add(new BusinessAreaDTO()
                {
                    Guid = businessArea.TermGuid,
                    Name = await GetTaxonomyTerm(businessArea.TermGuid, token),
                    DocumentTypes = docTypes
                });
            }

            //var sharepointDataOrderedByBusinessArea =  sharepointData.OrderBy(k => k.BusinessArea.Name);

            //foreach(var item in sharepointDataOrderedByBusinessArea)
            //{
            //    var businessArea =  retVal.FirstOrDefault(k => k.Guid == item.BusinessArea.TermGuid);

            //}

            return retVal;
        }
        private  ClientContext GetClientContext(string token)
        {
            var clientContext = new ClientContext(_sharePointSettings.SharePointBaseUrl  + _sharePointSettings.Library);
            clientContext.ExecutingWebRequest += (object sender, WebRequestEventArgs e) =>
            {
                e.WebRequestExecutor.RequestHeaders.Add("Authorization", $"Bearer {token}");
            };
            return clientContext;
        }


        public async Task<string> GetTaxonomyTerm(string termId, string token)
        {
            string term = string.Empty;
            string url = _sharePointSettings.SharePointBaseUrl + "/_api/web/lists/getbytitle('TaxonomyHiddenList')/items?$filter=IdForTerm eq '" + termId + "'";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var res = client.GetAsync(url).Result;
            if (res.IsSuccessStatusCode)
            {
                var data = res.Content.ReadAsStringAsync().Result;
                var taxonomyData = JsonConvert.DeserializeObject<TaxonomyData>(data).data.results.First();
                term = taxonomyData.TermName;
            }

            return term;
        }

        private void UpdateTaxonomyField(ClientContext ctx, List myLib, ListItem myListItem, string fieldName, string fieldValue, string fieldLabel ="")
        {
            var field = myLib.Fields.GetByInternalNameOrTitle(fieldName);
            var taxKeywordField = ctx.CastTo<TaxonomyField>(field);
            TaxonomyFieldValue termValue = new TaxonomyFieldValue();
            termValue.TermGuid = fieldValue;
            if(fieldLabel.Length > 1)
                termValue.Label = fieldLabel;
            taxKeywordField.SetFieldValueByValue(myListItem, termValue);

            taxKeywordField.Update();
        }
    }
}
