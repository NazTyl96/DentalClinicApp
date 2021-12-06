using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Appointment
{
    public class AppointmentsListViewModel
    {
        public class Appointment
        {
            public int AppointmentId { get; set; }
            public string PatientName { get; set; }
            public string DoctorName { get; set; }
            public string Time { get; set; }
            public string ServiceName { get; set; }
            public decimal? ServiceCost { get; set; }
            public string Comment { get; set; }

        }
        public List<Appointment> Appointments { get; set; }
        public string DateString { get; set; }
    }
}