using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Service
{
    public class ServicesListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Nullable<decimal> Cost { get; set; }
        public int Status { get; set; }
        public string Type { get; set; }
    }
}