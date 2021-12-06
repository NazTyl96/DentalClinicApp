using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AlphaStomPlusMVC.Models;

namespace AlphaStomPlusMVC.Models.ViewModels.Patient
{
    public class AddEditViewModel
    {
        public Models.Patient Patient { get; set; }
        public Models.Doctor Doctor { get; set; }

        public DateTime Today { get; set; }

        public List<Models.Doctor> Doctors { get; set; }
        public List<Models.Service> Services { get; set; }
    }
}