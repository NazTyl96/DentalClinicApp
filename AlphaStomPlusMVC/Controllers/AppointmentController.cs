using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using AlphaStomPlusMVC.Models;
using AlphaStomPlusMVC.Models.ViewModels.Appointment;

namespace AlphaStomPlusMVC.Controllers
{
    public class AppointmentController : Controller
    {
        AlphaStomPlusEntities db = new AlphaStomPlusEntities();

        internal Dictionary<string, int> weekDays = new Dictionary<string, int>
        {
            ["Monday"] = 1,
            ["Tuesday"] = 2,
            ["Wednesday"] = 3,
            ["Thursday"] = 4,
            ["Friday"] = 5,
            ["Saturday"] = 6,
            ["Sunday"] = 7
        };
        internal class AppointmentsSearchParams
        {
            public List<string> PatientsNames { get; set; }
            public List<int> DoctorIds { get; set; }
            public List<int> ServiceIds { get; set; }
            public int Status { get; set; }
            public DateTime FirstOfMonth { get; set; }
        }
        internal class AppointmentsByDateSearchParams
        {
            public List<string> PatientsNames { get; set; }
            public List<int> DoctorIds { get; set; }
            public List<int> ServiceIds { get; set; }
            public DateTime Date { get; set; }
        }
        public ActionResult Index(int status = 1)
        {
            IndexViewModel model = new IndexViewModel();

            model.Status = status;

            model.CurrentDate = DateTime.Today;

            var filtersInfo = (from app in db.Appointment
                               join pat in db.Patient on app.PatientId equals pat.Id
                               select new { PatientName = pat.FullName, app.DoctorId, app.ServiceId, app.Date });

            if (status == 1)
            {
                filtersInfo = filtersInfo.Where(x => x.Date >= model.CurrentDate.Date);
            }
            else
            {
                filtersInfo = filtersInfo.Where(x => x.Date < model.CurrentDate.Date);
            }

            model.PatientFilter = filtersInfo.Select(x => x.PatientName).Distinct().ToList();

            List<int> doctorIds = filtersInfo.Select(x => x.DoctorId).Distinct().ToList();
            List<int> serviceIds = filtersInfo.Select(x => x.ServiceId).Distinct().ToList();

            model.DoctorFilter = db.Doctor.Where(x => doctorIds.Contains(x.Id)).ToList();
            model.ServiceFilter = db.Service.Where(x => serviceIds.Contains(x.Id)).ToList();

            ViewBag.Title = "Appointment calendar";

            return View(model);
        }

        [HttpGet]
        public PartialViewResult LoadCalendar(string queryString)
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var searchParams = serializer.Deserialize<AppointmentsSearchParams>(queryString);

            CalendarViewModel model = new CalendarViewModel();

            CultureInfo myCI = new CultureInfo("ru-RU");
            CalendarWeekRule myCWR = myCI.DateTimeFormat.CalendarWeekRule;
            DayOfWeek myFirstDOW = myCI.DateTimeFormat.FirstDayOfWeek;
            Calendar myCal = myCI.Calendar;

            int firstDayWeekNumber = myCal.GetWeekOfYear(searchParams.FirstOfMonth, myCWR, myFirstDOW);
            //last day of the month for db query
            DateTime lastOfMonth = searchParams.FirstOfMonth.AddDays(DateTime.DaysInMonth(searchParams.FirstOfMonth.Year, searchParams.FirstOfMonth.Month))
                                                            .AddDays(1).AddSeconds(-1);
            int lastDayWeekNumber = myCal.GetWeekOfYear(lastOfMonth, myCWR, myFirstDOW);

            model.NumberOfWeeks = lastDayWeekNumber - firstDayWeekNumber + 1;

            DateTime today = DateTime.Today;

            //IQueryable object to be filtered and later materialized
            IQueryable<Appointment> appointments = db.Appointment;

            if (searchParams.Status == 1)
            {
                //appointments of today and future days
                appointments = appointments.Where(x => x.Date >= searchParams.FirstOfMonth.Date 
                                                    && x.Date >= today.Date && x.Date <= lastOfMonth);
            }
            else
            {
                //appointments of yesterday and other past days
                appointments = appointments.Where(x => x.Date >= searchParams.FirstOfMonth.Date 
                                                    && x.Date < today.Date && x.Date <= lastOfMonth);
            }

            if (searchParams.PatientsNames.Any())
            {
                List<int> patientIds = db.Patient.Where(x => searchParams.PatientsNames.Contains(x.FullName)).Select(y => y.Id).ToList();
                appointments = appointments.Where(x => patientIds.Contains(x.PatientId));
            }

            if (searchParams.DoctorIds.Any())
            {
                appointments = appointments.Where(x => searchParams.DoctorIds.Contains(x.DoctorId));
            }

            if (searchParams.ServiceIds.Any())
            {
                appointments = appointments.Where(x => searchParams.ServiceIds.Contains(x.ServiceId));
            }

            List<Appointment> curAppointments = appointments.ToList();

            //finding the quantity of aapointments for each date in the month
            model.AppointmentDates = new Dictionary<DateTime, int>();
            foreach (var app in curAppointments)
            {
                if (model.AppointmentDates.ContainsKey(app.Date.Date))
                {
                    model.AppointmentDates[app.Date.Date] += 1;
                }
                else
                {
                    model.AppointmentDates[app.Date.Date] = 1;
                }
            }

            //creating an object for each calendar day containing various fields for more comfortable display of calendar in the view
            model.CalendarDays = new List<CalendarViewModel.CalendarDay>();

            int daysCount = 0;
            for (DateTime date = searchParams.FirstOfMonth; date <= lastOfMonth; date = date.AddDays(1))
            {
                CalendarViewModel.CalendarDay calendarDay = new CalendarViewModel.CalendarDay();

                calendarDay.DayOfWeek = weekDays[date.DayOfWeek.ToString()];
                if (calendarDay.DayOfWeek < 6)
                {
                    calendarDay.Date = date;
                    calendarDay.DateString = date.ToString("yyyy-MM-dd");
                    calendarDay.Day = date.Day;

                    model.CalendarDays.Add(calendarDay);
                    daysCount++;
                }
            }

            model.LastDayIndex = daysCount - 1;

            return PartialView("CalendarTable", model);
        }

        [HttpGet]
        public PartialViewResult ViewAppointmentsByDate(string queryString)
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var searchParams = serializer.Deserialize<AppointmentsByDateSearchParams>(queryString);

            AppointmentsListViewModel model = new AppointmentsListViewModel();

            model.DateString = searchParams.Date.ToString("yyyy-MM-dd");
            DateTime nextDate = searchParams.Date.AddDays(1);

            var appointments = db.Appointment.Where(x => x.Date >= searchParams.Date && x.Date < nextDate);

            if (searchParams.PatientsNames.Any())
            {
                List<int> patientIds = db.Patient.Where(x => searchParams.PatientsNames.Contains(x.FullName)).Select(y => y.Id).ToList();
                appointments = appointments.Where(x => patientIds.Contains(x.PatientId));
            }

            if (searchParams.DoctorIds.Any())
            {
                appointments = appointments.Where(x => searchParams.DoctorIds.Contains(x.DoctorId));
            }

            if (searchParams.ServiceIds.Any())
            {
                appointments = appointments.Where(x => searchParams.ServiceIds.Contains(x.ServiceId));
            }

            List<Appointment> curAppointments = appointments.OrderBy(x => x.Date).ToList();

            //getting all the patients, doctors, services and their attributes to display in the view
            List<int> curPatientIds = curAppointments.Select(x => x.PatientId).Distinct().ToList();
            List<int> curDoctorIds = curAppointments.Select(x => x.DoctorId).Distinct().ToList();
            List<int> curServiceIds = curAppointments.Select(x => x.ServiceId).Distinct().ToList();

            var curPatients = (from pat in db.Patient
                               where curPatientIds.Contains(pat.Id)
                               select new { PatientId = pat.Id, PatientName = pat.FullName }).ToList();

            List<Doctor> curDocs = db.Doctor.Where(x => curDoctorIds.Contains(x.Id)).ToList();
            List<Service> curServices = db.Service.Where(x => curServiceIds.Contains(x.Id)).ToList();

            model.Appointments = (from app in curAppointments
                                  join pat in curPatients on app.PatientId equals pat.PatientId
                                  join doc in curDocs on app.DoctorId equals doc.Id
                                  join service in curServices on app.ServiceId equals service.Id
                                  select new AppointmentsListViewModel.Appointment ()
                                  {
                                      AppointmentId = app.Id,
                                      PatientName = pat.PatientName,
                                      DoctorName = doc.FullName,
                                      Time = app.Date.ToShortTimeString(),
                                      ServiceName = service.Name,
                                      ServiceCost = service.Cost,
                                      Comment = app.Comment
                                  }).ToList();

            return PartialView("ViewForm", model);
        }

        [HttpGet]
        public PartialViewResult AddAppointment(string date, int patientId = 0, int doctorId = 0)
        {
            AddEditViewModel model = new AddEditViewModel();
            model.Appointment = new Appointment () { PatientId = patientId, DoctorId = doctorId };
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();

            if (String.IsNullOrEmpty(serializer.Deserialize<string>(date)))
            {
                model.Appointment.Date = DateTime.Now;
            }
            else
            {
                model.Appointment.Date = serializer.Deserialize<DateTime>(date);
            }

            model.PatientSelect = db.Patient.Where(x => x.Status == 1).OrderBy(x => x.FullName).ToList();
            model.DoctorSelect = db.Doctor.Where(x => x.Status == 1).OrderBy(x => x.FullName).ToList();
            model.ServiceSelect = db.Service.Where(x => x.Status == 1).ToList();

            return PartialView("AddEditForm", model);
        }

        [HttpGet]
        public PartialViewResult EditAppointment(int id)
        {
            AddEditViewModel model = new AddEditViewModel();

            model.Appointment = db.Appointment.Find(id);

            model.PatientSelect = db.Patient.Where(x => x.Status == 1).ToList();
            model.DoctorSelect = db.Doctor.Where(x => x.Status == 1).ToList();
            model.ServiceSelect = db.Service.Where(x => x.Status == 1).ToList();

            return PartialView("AddEditForm", model);
        }

        [HttpPost]
        public JsonResult SaveAppointment(Appointment appointment)
        {

            List<string> errorList = new List<string>();

            if (appointment.Date == null)
            {
                errorList.Add("Please specify the date for an appointment");
            }
            if (appointment.PatientId == 0)
            {
                errorList.Add("Please specify a patient");
            }
            if (appointment.DoctorId == 0)
            {
                errorList.Add("Please specify a doctor");
            }

            if (ModelState.IsValid && !errorList.Any())
            {
                if (appointment.Id > 0)
                {
                    Appointment curAppointment = db.Appointment.Find(appointment.Id);

                    if (curAppointment.Date != appointment.Date)
                    {   //if date is changed, we need to change the date of each notification of the appointment (both "day before" type and "half a year after")
                        List<Notification> curAppNotifications = db.Notification.Where(x => x.AppointmentId == curAppointment.Id).ToList();
                        foreach (var notification in curAppNotifications)
                        {
                            if (notification.Type == 1)
                            {
                                DateTime dateOfShow = appointment.Date.Date.AddDays(-1);
                                if (dateOfShow.DayOfWeek.ToString() == "Sunday")
                                {
                                    dateOfShow = dateOfShow.AddDays(-2);
                                }
                                notification.DateOfShow = dateOfShow;
                            }
                            else
                            {
                                notification.DateOfShow = appointment.Date.Date.AddMonths(6);
                            }
                        }

                    }

                    curAppointment.PatientId = appointment.PatientId;
                    curAppointment.DoctorId = appointment.DoctorId;
                    curAppointment.ServiceId = appointment.ServiceId;
                    curAppointment.Date = appointment.Date;
                    curAppointment.Comment = appointment.Comment;
                }
                else
                {
                    db.Appointment.Add(appointment);

                    db.SaveChanges();

                    Notification dayBeforeNotification = new Notification()
                    {
                        AppointmentId = appointment.Id,
                        Type = 1
                    };
                    DateTime typeOneDateOfShow = appointment.Date.Date.AddDays(-1);
                    if (typeOneDateOfShow.DayOfWeek.ToString() == "Sunday")
                    {
                        typeOneDateOfShow = typeOneDateOfShow.AddDays(-2);
                    }
                    dayBeforeNotification.DateOfShow = typeOneDateOfShow;

                    if (appointment.Date.Date == DateTime.Today.Date.AddDays(1))
                    {
                        dayBeforeNotification.IsShown = true;
                    }

                    db.Notification.Add(dayBeforeNotification);

                    //deleting all "half a year later" type notifications to create the latest one
                    List<Notification> allCurPatientNotifications = (from not in db.Notification
                                                                     join app in db.Appointment on not.AppointmentId equals app.Id
                                                                     where not.Type == 2 && app.PatientId == appointment.PatientId && app.Date < appointment.Date
                                                                     select not).ToList();

                    foreach (var notification in allCurPatientNotifications)
                    {
                        db.Notification.Remove(notification);
                    }

                    Notification halfYearNotification = new Notification()
                    {
                        AppointmentId = appointment.Id,
                        Type = 2
                    };
                    DateTime typeTwoDateOfShow = appointment.Date.Date.AddMonths(6);
                    if (typeTwoDateOfShow.DayOfWeek.ToString() == "Saturday")
                    {
                        typeTwoDateOfShow = typeTwoDateOfShow.AddDays(-1);
                    }
                    if (typeTwoDateOfShow.DayOfWeek.ToString() == "Sunday")
                    {
                        typeTwoDateOfShow = typeTwoDateOfShow.AddDays(1);
                    }
                    halfYearNotification.DateOfShow = typeTwoDateOfShow;

                    db.Notification.Add(halfYearNotification);
                }

                db.SaveChanges();

                return Json(new { success = true, data = appointment.Id });

            }

            return Json(new { success = false, data = errorList });

        }

        [HttpPost]
        public JsonResult DeleteAppointment(int id)
        {

            Appointment curApp = db.Appointment.Find(id);
            db.Appointment.Remove(curApp);

            List<Notification> curAppNotifications = db.Notification.Where(x => x.AppointmentId == id).ToList();
            foreach (var notification in curAppNotifications)
            {
                db.Notification.Remove(notification);
            }

            db.SaveChanges();

            return Json(new { success = true });
        }
    }
}