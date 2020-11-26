using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tht.EDRMS.Models
{
    public class PropertySearch
    {
  
            public string Areas = null;
            public string Types = null;
            public string Postcode = null;
            public string Street = null;
            public string PlaceRef = null;
            public bool HasResidentialProp = false;
    }

    public class PropertyDetail
    {
        //public PropertyDetail()
        //{
        //}

        //public PropertyDetail(int id, string placeRef, string address1, string address2, string address3, string address4, string address5, string postcode, string estateCode, string buildDate, string numberOfBedrooms, string subAreaCode, string decentHomesStatus, string targetRent, string areaDescription)
        //{
        //    Id = id;
        //    PlaceRef = placeRef;
        //    Address1 = address1;
        //    Address2 = address2;
        //    Address3 = address3;
        //    Address4 = address4;
        //    Address5 = address5;
        //    Postcode = postcode;
        //    EstateCode = estateCode;
        //    BuildDate = buildDate;
        //    NumberOfBedrooms = numberOfBedrooms;
        //    SubAreaCode = subAreaCode;
        //    DecentHomesStatus = decentHomesStatus;
        //    TargetRent = targetRent;
        //    AreaDescription = areaDescription;
        //}

        public int Id { get; set; }
        public string PlaceRef { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Address4 { get; set; }
        public string Address5 { get; set; }
        public string Postcode { get; set; }
        public string AreaDescription { get; set; }
        public string EstateCode { get; set; }
        public string BuildDate { get; set; }
        public string NumberOfBedrooms { get; set; }
        public string SubAreaCode { get; set; }
        public string DecentHomesStatus { get; set; }
        public string TargetRent { get; set; }

        public string Address => $"{Address1} {Address2} {Address3} {Address4} {Address5}";

        public string BuildingAddress => $"{Address1} {Address3} {Address4} {Address5}";
    }
}
