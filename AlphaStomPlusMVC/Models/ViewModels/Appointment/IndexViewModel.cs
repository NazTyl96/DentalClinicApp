using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Appointment
{
    public class IndexViewModel
    {
        public DateTime CurrentDate { get; set; }
        public List<string> PatientFilter { get; set; }
        public List<Models.Doctor> DoctorFilter { get; set; }
        public List<Models.Service> ServiceFilter { get; set; }
        public int Status { get; set; }

    }
}