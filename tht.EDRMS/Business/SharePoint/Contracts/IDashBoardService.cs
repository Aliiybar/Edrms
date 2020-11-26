using System.Collections.Generic;
using System.Threading.Tasks;
using tht.EDRMS.Models;

namespace tht.EDRMS.Business.SharePoint.Contracts
{
    public interface IDashBoardService
    {
        Task<List<PropertyDetail>> PropertyLookup(string SearchText);
    }
}