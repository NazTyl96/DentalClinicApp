using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Patient
{
    public class IndexViewModel
    {
        public List<string> FullNames { get; set; }
        public List<DateTime> DatesOfBirth { get; set; }
        public List<int> Sexes { get; set; }
        public int Status { get; set; }
    }
}