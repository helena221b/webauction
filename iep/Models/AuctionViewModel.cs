using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PagedList;

using System.ComponentModel.DataAnnotations;

using System.Web.Mvc;
using System.ComponentModel;

namespace iep.Models
{
    public class AuctionViewModel
    {
        public IPagedList<Auction> AuctionList { get; set; }

        [DefaultValue(null)]
        [Display(Name = "Name for search")]
        public string Name { get; set; }

        [Display(Name = "High Price")]
        [DefaultValue(-1)]
        public decimal HighPrice { get; set; }

        [DefaultValue(-1)]
        [Display(Name = "Low Price")]
        public decimal LowPrice { get; set; }

        [DefaultValue(null)]
        [Display(Name = "Status")]
        public string Status { get; set; }

        [DefaultValue(null)]
        public int? Page { get; set; }
    }
}