using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using tht.EDRMS.Business.SharePoint.Contracts;
using tht.EDRMS.Models;

namespace tht.EDRMS.Business.SharePoint.Implementation
{
    public class DashBoardService : IDashBoardService
    {
        private readonly ISharePointSettings _sharePointSettings;

        public DashBoardService(ISharePointSettings sharePointSettings)
        {
            _sharePointSettings = sharePointSettings;
        }
        public async Task<List<PropertyDetail>> PropertyLookup(string SearchText)
        {
            string url = _sharePointSettings.DashboardBaseUrl + "/api/property/SearchProperty";
            var searchParameters = new PropertySearch() { Postcode = SearchText };
            string payload = JsonConvert.SerializeObject(searchParameters);
            var client = new HttpClient();
            //var listData = new List<PropertyDetail>();
            HttpContent param = new StringContent(payload, Encoding.UTF8, "application/json");
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var res = await client.PostAsync(url, param);
            if (res.IsSuccessStatusCode)
            {
                var data =  res.Content.ReadAsStringAsync().Result;
                var listData = JsonConvert.DeserializeObject<List<PropertyDetail>>(data); // .data.results

                return listData;
            }
            return null;
        }
    }
}
