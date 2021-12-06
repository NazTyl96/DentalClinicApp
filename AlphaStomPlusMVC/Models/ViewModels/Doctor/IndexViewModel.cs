using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Doctor
{
    public class IndexViewModel
    {
        public List<Models.Doctor> DoctorsFilter { get; set; }
        public int Status { get; set; }
    }
}