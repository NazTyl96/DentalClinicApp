using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Home
{
    public class NotificationsViewModel
    {
        public class Appointment
        {
            public int NotificationId { get; set; }
            public string PatientName { get; set; }
            public string Phone { get; set; }
            public string DoctorName { get; set; }
            public DateTime DateTime { get; set; }
            public string ServiceName { get; set; }
            public decimal ServiceCost { get; set; }
            public string Comment { get; set; }
        }
        public List<Appointment> DayBeforeAppointments { get; set; }
        public List<Appointment> HalfYearAppointments { get; set; }
        public DateTime NextWorkDay { get; set; }
    }
}