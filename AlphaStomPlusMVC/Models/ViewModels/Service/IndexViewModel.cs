using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Service
{
    public class IndexViewModel
    {
        public List<Models.Service> ServicesFilter { get; set; }
        public int Status { get; set; }
    }
}