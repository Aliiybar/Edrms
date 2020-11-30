using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using tht.EDRMS.Business.SharePoint.Contracts;
using tht.EDRMS.Models;

namespace tht.EDRMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDashBoardService _dashBoardService;
        private readonly ISharePointSettings _sharePointSettings;
        private readonly ISharePointManager _sharePointManager;
        private string token;
        public HomeController(ILogger<HomeController> logger, 
                               IHostingEnvironment hostingEnvironment,
                              IHttpContextAccessor httpContextAccessor,
                              IDashBoardService dashBoardService,
                              ISharePointSettings sharePointSettings,
                              ISharePointManager sharePointManager)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _dashBoardService = dashBoardService;
            _sharePointSettings = sharePointSettings;
            _sharePointManager = sharePointManager;
        }

        public async Task<IActionResult> Index()
        {
            await PrepareIndexPage();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(PostModel model)
        {
            string filePath = "";
            filePath = await Upload(model);
            //-------------------
            token = _httpContextAccessor.HttpContext.Request.Cookies["token"];
            var docTypes = _sharePointManager.GetDocumentTypeMetaData("BuildingSafetyCertificate", token);

            var documentData = new DocumentData();
            documentData.BusinessAreaId = model.BusinessArea;
            // documentData. = model.DocumentType;
            documentData.ContentTypeId = docTypes.Result.First().ContentTypeId;
            documentData.FilePath = filePath;
            documentData.MetaDatas = new List<MetaData>();
            if (model.DocumentType != null)
                documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "DocumentType", Value = model.DocumentType });

            if (model.placeRef != null)
               documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "PlaceRef", Value = model.placeRef });
         
            if (model.expiryDate != null)
                documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "ExpiryDate", Value = model.expiryDate });

            if (model.inspectionCompletionDate != null)
                documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "InspectionCompletionDate", Value = model.inspectionCompletionDate });

            if (model.Contractor != null)
                documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "Contractor", Value = model.Contractor });


            documentData.Token = token;
            var result = _sharePointManager.Upload(documentData);

            await PrepareIndexPage();
            // Delete the the upload folder 
            // Redirect page to update with the document id
            return RedirectToAction("ListDocuments");
        }

        [HttpGet("DownloadFile")]
        public IActionResult DownloadFile(string fileName, string fileType)
        {
            try
            {
                token = _httpContextAccessor.HttpContext.Request.Cookies["token"];
                byte[] res;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var requestUrl = String.Format("{0}{1}_api/web/GetFileByServerRelativeUrl('{2}/{3}')/$value", _sharePointSettings.SharePointBaseUrl, _sharePointSettings.Library, _sharePointSettings.DownloadFromPath, fileName);
                    res = client.GetByteArrayAsync(requestUrl).Result;

                }
                var fType = (fileType == "docx") ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : "application/pdf";
                return new FileContentResult(res, fType);
            }
            catch (Exception)
            {

                return null;
            }
           
        }

        [Route("Update/{Id}")]
        public async Task<IActionResult> Update(int Id)
        {
            
            return View(await PrepareUpdatePage(Id));
        }
     
        [HttpPost]
        [Route("Update/{Id}")]
        public async Task<IActionResult> Update(int Id, PostModel model)
        {
            var m = model;
            try
            {
                 token = _httpContextAccessor.HttpContext.Request.Cookies["token"];
                var documentData = new DocumentData();
                documentData.DocumentId = model.DocumentId;
                documentData.BusinessAreaId = model.BusinessArea;
                documentData.Token = token;
                documentData.MetaDatas = new List<MetaData>();
                if(model.expiryDate != null )
                    documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "ValidToDate", Value = model.expiryDate });

                if (model.DocumentType != null)
                    documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "DocumentType", Value = model.DocumentType });

                if (model.Contractor != null)
                    documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "Contractor", Value = model.Contractor });

                if (model.inspectionCompletionDate != null)
                    documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "InspectionCompletionDate", Value = model.inspectionCompletionDate });

                if (model.placeRef != null)
                    documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "PlaceRef", Value = model.placeRef });


                if (model.Final != null && model.Final == true)
                {
                    documentData.MetaDatas.Add(new MetaData() { EntityPropertyName = "DocumentStatus", Value = "Complete" });
                }
                _sharePointManager.UpdateDocument(documentData);

                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                
            }
            return RedirectToAction("ListDocuments");  //View(await PrepareUpdatePage((int) model.DocumentId));
        }
        private async Task PrepareIndexPage()
        {
            token = _httpContextAccessor.HttpContext.Request.Cookies["token"];
            if (token == null)
            {
                token = await _sharePointManager.GenerateToken();
                Set("token", token, 240);
            }

            var result = await _sharePointManager.GetBusinessAreas(token);
            ViewBag.BusinessAreas = GetBusinessArea(result);
            ViewBag.DocumentTypes = GetDocumentTypes(result);
            ViewBag.Contractors = await GetContractors();
        }
        private async Task<PostModel> PrepareUpdatePage(int Id)
        {
            token = _httpContextAccessor.HttpContext.Request.Cookies["token"];
            await PrepareIndexPage();
            var model = await _sharePointManager.GetDocument(token, Id);
            var postModel = new PostModel();
            postModel.Token = token;
            if (model != null)
            {
                postModel.BusinessArea = model.BusinessAreaId ?? "";
      //          postModel.Contractor = model.MetaDatas.FirstOrDefault(t => t.EntityPropertyName == "Contractor").Value;
                postModel.DocumentId = model.DocumentId;
                postModel.FilePath =  model.FilePath;
                postModel.FileName = model.FileName;
                // LoadPdf(_sharePointSettings.SharePointRoot + model.FilePath);
              //  _sharePointManager.DownloadPdf(_sharePointSettings.SharePointRoot + model.FilePath);

                if (model.MetaDatas.First(k => k.EntityPropertyName == "InspectionCompletionDate").Value != null)
                    postModel.inspectionCompletionDate = model.MetaDatas.First(k => k.EntityPropertyName == "InspectionCompletionDate").Value;

                if (model.MetaDatas.First(k => k.EntityPropertyName == "ValidToDate").Value != null)
                    postModel.expiryDate = model.MetaDatas.First(k => k.EntityPropertyName == "ValidToDate").Value;

                if (model.MetaDatas.First(k => k.EntityPropertyName == "DocumentType").Value != null)
                    postModel.DocumentType = model.MetaDatas.First(k => k.EntityPropertyName == "DocumentType").Value;

                if (model.MetaDatas.First(k => k.EntityPropertyName == "Contractor").Value != null)
                    postModel.Contractor = model.MetaDatas.First(k => k.EntityPropertyName == "Contractor").Value;
                if (model.MetaDatas.First(k => k.EntityPropertyName == "PlaceRef").Value != null)
                    postModel.placeRef = model.MetaDatas.First(k => k.EntityPropertyName == "PlaceRef").Value;


            }
            return postModel;
        }



        [HttpPost]
        public async Task<List<PropertyDetail>> PropertyLookup(string postCode)
        {
            return await _dashBoardService.PropertyLookup(postCode);
        }
        private IEnumerable<SelectListItem> GetBusinessArea(List<BusinessAreaDTO> data)
        {
            var selectList = new List<SelectListItem>();
            selectList.Add(new SelectListItem { Disabled = true, Text = "Select a Business Group", Value = "" });
            foreach (var element in data)
            {
                selectList.Add(new SelectListItem
                {
                    Value = element.Guid,
                    Text = element.Name
                });
            }

            return selectList;
        }
        private  async Task<IEnumerable<SelectListItem>> GetContractors()
        {
            var selectList = new List<SelectListItem>();
            token = _httpContextAccessor.HttpContext.Request.Cookies["token"];
            var contractorList = await _sharePointManager.GetContractorsList(token);
            selectList.Add(new SelectListItem { Disabled = true, Text = "Select a Contractor", Value = "" });
            foreach (var element in contractorList)
            {
                selectList.Add(new SelectListItem
                {
                    Value = element.ContractorId,
                    Text = element.ContractorName
                });
            }

            return selectList;
        }
        private IEnumerable<SelectListItem> GetDocumentTypes(List<BusinessAreaDTO> data)
        {
            var selectList = new List<SelectListItem>();
            selectList.Add(new SelectListItem { Disabled = true, Text = "Select a Document Type", Value = "" });

            foreach (var element in data.First().DocumentTypes)
            {
                selectList.Add(new SelectListItem
                {
                    Value = element.DocumentTypeGuid,
                    Text = element.DocumentType
                });
            }

            return selectList;
        }
       

        private async Task<string> Upload(PostModel model)
        {
            string filePath = "";
            if (model.DocFile != null)
            {
                string uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "Uploads");
                filePath = Path.Combine(uploadFolder, Path.GetFileName(model.DocFile.FileName));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.DocFile.CopyToAsync(stream);
                }
            }
            return filePath;
        }

 
        public void Set(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();

            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(10);

            Response.Cookies.Append(key, value, option);
        }

        public async Task<IActionResult> ListDocuments()
        {
            token = _httpContextAccessor.HttpContext.Request.Cookies["token"];
           // var mm = await _dashBoardService.PropertyLookup("M33 3HY");
 

            var result = await _sharePointManager.StagedDocList(token);

            return View(result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
