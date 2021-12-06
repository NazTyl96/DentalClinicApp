using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Doctor
{
    public class ViewDoctorViewModel
    {
        public Models.Doctor Doctor { get; set; }
        public class Appointment
        {
            public int Id { get; set; }
            public DateTime Date { get;set; }
            public int PatientId { get; set; }
            public string PatientName { get; set; }
            public int ServiceId { get; set; }
            public string ServiceName { get; set; }
        } 
        public List <Appointment> Appointments { get; set; }

    }
}