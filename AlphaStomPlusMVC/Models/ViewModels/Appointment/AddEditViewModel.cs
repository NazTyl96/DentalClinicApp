using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Appointment
{
    public class AddEditViewModel
    {
        public Models.Appointment Appointment { get; set; }
        public List<Models.Doctor> DoctorSelect { get; set; }
        public List<Models.Patient> PatientSelect { get; set; }
        public List<Models.Service> ServiceSelect { get; set; }
    }
}