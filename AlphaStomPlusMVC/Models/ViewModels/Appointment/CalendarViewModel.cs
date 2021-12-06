using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Appointment
{
    public class CalendarViewModel
    {
        public Dictionary<DateTime, int> AppointmentDates { get; set; }
        public int NumberOfWeeks { get; set; }
        public int LastDayIndex { get; set; }
        public class CalendarDay
        {
            public DateTime Date { get; set; }
            public string DateString { get; set; }
            public int Day { get; set; }
            public int DayOfWeek { get; set; }
            public int WeekNumber { get; set; }
            public bool WithAppointment { get; set; }
            
        }
        public List<CalendarDay> CalendarDays { get; set; }
    }
}