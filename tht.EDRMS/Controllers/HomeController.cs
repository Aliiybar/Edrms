using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private readonly ISharePointManager _sharePointManager;
        private string token;
        public HomeController(ILogger<HomeController> logger, 
                               IHostingEnvironment hostingEnvironment,
                              IHttpContextAccessor httpContextAccessor,
                              ISharePointManager sharePointManager)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _sharePointManager = sharePointManager;
        }

        public async Task<IActionResult> Index()
        {
            await PrepareIndexPage();
            return View();
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
        private IEnumerable<SelectListItem> GetDocumentTypes(List<BusinessAreaDTO> data)
        {
            var selectList = new List<SelectListItem>();
            selectList.Add(new SelectListItem { Disabled = true, Text = "Select a Document Type", Value = "" });
            foreach (var element in data.First().DocumentTypes)
            {
                selectList.Add(new SelectListItem
                {
                    Value = element.DocumentType,
                    Text = element.DocumentType
                });
            }

            return selectList;
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
            documentData.Token = token;
            var result = _sharePointManager.Upload(documentData);

            await PrepareIndexPage();
            return View();
        }

        private async Task<string> Upload(PostModel model)
        {
            string filePath = "";
            if (model.DocFile != null)
            {
                string uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "Uploads");
                filePath = Path.Combine(uploadFolder, Path.GetFileName(model.DocFile.FileName));
                // it doesn't release the file after copy happens
        //        await model.DocFile.CopyToAsync(new FileStream(filePath, FileMode.Create));
                
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
