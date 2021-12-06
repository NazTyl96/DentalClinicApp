using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AlphaStomPlusMVC.Models;
using AlphaStomPlusMVC.Models.ViewModels.Appointment;

namespace AlphaStomPlusMVC.Controllers
{
    public class AppointmentController : Controller
    {
        AlphaStomPlusEntities db = new AlphaStomPlusEntities();

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

            ViewBag.Title = "Календарь записей пациентов на приём";

            return View(model);
        }

        [HttpGet]
        public PartialViewResult LoadCalendar(string patientNames, string doctorIds, string serviceIds, string month, int status)
        {
            Dictionary<string, int> weekDays = new Dictionary<string, int>
            {
                ["Monday"] = 1,
                ["Tuesday"] = 2,
                ["Wednesday"] = 3,
                ["Thursday"] = 4,
                ["Friday"] = 5,
                ["Saturday"] = 6,
                ["Sunday"] = 7
            };

            CalendarViewModel model = new CalendarViewModel();

            CultureInfo myCI = new CultureInfo("ru-RU");
            CalendarWeekRule myCWR = myCI.DateTimeFormat.CalendarWeekRule;
            DayOfWeek myFirstDOW = myCI.DateTimeFormat.FirstDayOfWeek;
            Calendar myCal = myCI.Calendar;

            List<string> dateParts = month.Split('-').ToList();
            DateTime firstOfMonth = new DateTime(Int32.Parse(dateParts[0]), Int32.Parse(dateParts[1]), 1);
            int firstDayWeekNumber = myCal.GetWeekOfYear(firstOfMonth, myCWR, myFirstDOW);
            DateTime lastOfMonth = new DateTime(Int32.Parse(dateParts[0]), Int32.Parse(dateParts[1]), DateTime.DaysInMonth(Int32.Parse(dateParts[0]), Int32.Parse(dateParts[1]))).AddDays(1).AddSeconds(-1);
            int lastDayWeekNumber = myCal.GetWeekOfYear(lastOfMonth, myCWR, myFirstDOW);

            model.NumberOfWeeks = lastDayWeekNumber - firstDayWeekNumber + 1;

            DateTime today = DateTime.Today;

            var appointments = from app in db.Appointment
                               select app;

            if (status == 1)
            {
                appointments = appointments.Where(x => x.Date >= firstOfMonth.Date && x.Date >= today.Date && x.Date <= lastOfMonth);
            }
            else
            {
                appointments = appointments.Where(x => x.Date >= firstOfMonth.Date && x.Date < today.Date && x.Date <= lastOfMonth);
            }

            if (!String.IsNullOrEmpty(patientNames))
            {
                List<string> patients = patientNames.Split(',').ToList();
                List<int> patientIds = db.Patient.Where(x => patients.Contains(x.FullName)).Select(y => y.Id).ToList();
                appointments = appointments.Where(x => patientIds.Contains(x.PatientId));
            }

            if (!String.IsNullOrEmpty(doctorIds))
            {
                List<int> allDoctorIds = new List<int>();
                List<string> idsStrings = doctorIds.Split(',').ToList();
                int docIdInt = 0;
                foreach (var str in idsStrings)
                {
                    if (Int32.TryParse(str, out docIdInt))
                    {
                        allDoctorIds.Add(docIdInt);
                    }
                }

                appointments = appointments.Where(x => allDoctorIds.Contains(x.DoctorId));
            }

            if (!String.IsNullOrEmpty(serviceIds))
            {
                List<int> allServiceIds = new List<int>();
                List<string> idsStrings = serviceIds.Split(',').ToList();
                int serviceIdInt = 0;
                foreach (var str in idsStrings)
                {
                    if (Int32.TryParse(str, out serviceIdInt))
                    {
                        allServiceIds.Add(serviceIdInt);
                    }
                }

                appointments = appointments.Where(x => allServiceIds.Contains(x.ServiceId));
            }

            List<Appointment> curAppointments = appointments.ToList();

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

            model.CalendarDays = new List<CalendarViewModel.CalendarDay>();

            int daysCount = 0;
            for (DateTime date = firstOfMonth; date <= lastOfMonth; date = date.AddDays(1))
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
        public PartialViewResult ViewAppointmentsByDate(string date, string patientNames, string doctorIds, string serviceIds)
        {
            AppointmentsListViewModel model = new AppointmentsListViewModel();

            model.DateString = date;

            List<string> dateParts = date.Split('-').ToList();
            DateTime curDate = new DateTime(Int32.Parse(dateParts[0]), Int32.Parse(dateParts[1]), Int32.Parse(dateParts[2]));
            DateTime nextDate = curDate.AddDays(1);

            var appointments = db.Appointment.Where(x => x.Date >= curDate && x.Date < nextDate);

            if (!String.IsNullOrEmpty(patientNames))
            {
                List<string> patients = patientNames.Split(',').ToList();
                List<int> patientIds = db.Patient.Where(x => patients.Contains(x.FullName)).Select(y => y.Id).ToList();
                appointments = appointments.Where(x => patientIds.Contains(x.PatientId));
            }

            if (!String.IsNullOrEmpty(doctorIds))
            {
                List<int> allDoctorIds = new List<int>();
                List<string> idsStrings = doctorIds.Split(',').ToList();
                int docIdInt = 0;
                foreach (var str in idsStrings)
                {
                    if (Int32.TryParse(str, out docIdInt))
                    {
                        allDoctorIds.Add(docIdInt);
                    }
                }

                appointments = appointments.Where(x => allDoctorIds.Contains(x.DoctorId));
            }

            if (!String.IsNullOrEmpty(serviceIds))
            {
                List<int> allServiceIds = new List<int>();
                List<string> idsStrings = serviceIds.Split(',').ToList();
                int serviceIdInt = 0;
                foreach (var str in idsStrings)
                {
                    if (Int32.TryParse(str, out serviceIdInt))
                    {
                        allServiceIds.Add(serviceIdInt);
                    }
                }

                appointments = appointments.Where(x => allServiceIds.Contains(x.ServiceId));
            }

            List<Appointment> curAppointments = appointments.OrderBy(x => x.Date).ToList();

            List<int> curPatientIds = curAppointments.Select(x => x.PatientId).Distinct().ToList();
            List<int> curDoctorIds = curAppointments.Select(x => x.DoctorId).Distinct().ToList();
            List<int> curServiceIds = curAppointments.Select(x => x.ServiceId).Distinct().ToList();

            var curPatients = (from pat in db.Patient
                               where curPatientIds.Contains(pat.Id)
                               select new { PatientId = pat.Id, PatientName = pat.FullName }).ToList();

            List<Doctor> curDocs = db.Doctor.Where(x => curDoctorIds.Contains(x.Id)).ToList();
            List<Service> curServices = db.Service.Where(x => curServiceIds.Contains(x.Id)).ToList();
            //curServices.Add(new Service() { Id = 0, Cost = null, Name = "Не выбрана", Status = 1 });

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

            if (String.IsNullOrEmpty(date) || date == "undefined")
            {
                model.Appointment.Date = DateTime.Now;
            }
            else
            {
                List<string> dateParts = date.Split('-').ToList();
                model.Appointment.Date = new DateTime(Int32.Parse(dateParts[0]), Int32.Parse(dateParts[1]), Int32.Parse(dateParts[2]));
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
        public JsonResult SaveAppointment(Appointment newAppointment)
        {

            List<string> errorList = new List<string>();

            if (newAppointment.Date == null)
            {
                errorList.Add("Укажите дату приёма");
            }
            if (newAppointment.PatientId == 0)
            {
                errorList.Add("Укажите пациента");
            }
            if (newAppointment.DoctorId == 0)
            {
                errorList.Add("Укажите врача");
            }
            //if (newAppointment.ServiceId == 0)
            //{
            //    errorList.Add("Укажите услугу");
            //}

            if (ModelState.IsValid && !errorList.Any())
            {
                if (newAppointment.Id > 0)
                {
                    Appointment curAppointment = db.Appointment.Find(newAppointment.Id);

                    if (curAppointment.Date != newAppointment.Date)
                    {
                        List<Notification> curAppNotifications = db.Notification.Where(x => x.AppointmentId == curAppointment.Id).ToList();
                        foreach (var notification in curAppNotifications)
                        {
                            if (notification.Type == 1)
                            {
                                DateTime dateOfShow = newAppointment.Date.Date.AddDays(-1);
                                if (dateOfShow.DayOfWeek.ToString() == "Sunday")
                                {
                                    dateOfShow = dateOfShow.AddDays(-2);
                                }
                                notification.DateOfShow = dateOfShow;
                            }
                            else
                            {
                                notification.DateOfShow = newAppointment.Date.Date.AddMonths(6);
                            }
                        }

                    }


                    //foreach (var property in typeof(Appointment).GetProperties())
                    //{
                    //    if (property.Name.ToString() != "Id" && property.GetValue(newAppointment) != null)
                    //    {
                    //        property.SetValue(curAppointment, property.GetValue(newAppointment));
                    //    }

                    //}

                    curAppointment.PatientId = newAppointment.PatientId;
                    curAppointment.DoctorId = newAppointment.DoctorId;
                    curAppointment.ServiceId = newAppointment.ServiceId;
                    curAppointment.Date = newAppointment.Date;
                    curAppointment.Comment = newAppointment.Comment;
                }
                else
                {
                    db.Appointment.Add(newAppointment);

                    db.SaveChanges();

                    Notification dayBeforeNotification = new Notification()
                    {
                        AppointmentId = newAppointment.Id,
                        Type = 1
                    };
                    DateTime typeOneDateOfShow = newAppointment.Date.Date.AddDays(-1);
                    if (typeOneDateOfShow.DayOfWeek.ToString() == "Sunday")
                    {
                        typeOneDateOfShow = typeOneDateOfShow.AddDays(-2);
                    }
                    dayBeforeNotification.DateOfShow = typeOneDateOfShow;

                    if (newAppointment.Date.Date == DateTime.Today.Date.AddDays(1))
                    {
                        dayBeforeNotification.IsShown = true;
                    }

                    db.Notification.Add(dayBeforeNotification);

                    List<Notification> allCurPatientNotifications = (from not in db.Notification
                                                                     join app in db.Appointment on not.AppointmentId equals app.Id
                                                                     where not.Type == 2 && app.PatientId == newAppointment.PatientId && app.Date < newAppointment.Date
                                                                     select not).ToList();

                    foreach (var notification in allCurPatientNotifications)
                    {
                        db.Notification.Remove(notification);
                    }

                    Notification halfYearNotification = new Notification()
                    {
                        AppointmentId = newAppointment.Id,
                        Type = 2
                    };
                    DateTime typeTwoDateOfShow = newAppointment.Date.Date.AddMonths(6);
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

                return Json(new { success = true, data = newAppointment.Id });

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