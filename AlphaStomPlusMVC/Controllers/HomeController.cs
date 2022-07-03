using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AlphaStomPlusMVC.Models;
using AlphaStomPlusMVC.Models.ViewModels.Home;


namespace AlphaStomPlusMVC.Controllers
{
    public class HomeController : Controller
    {
        AlphaStomPlusEntities db = new AlphaStomPlusEntities();
        public ActionResult Index()
        {
            IndexViewModel model = new IndexViewModel();

            model.NotificationsCount = GetNotificationsCount();

            ViewBag.Title = "Home Page";

            return View(model);
        }

        [HttpGet]
        public int GetNotificationsCount()
        {
            DateTime today = DateTime.Today.Date;
            var curNotificationList = (from not in db.Notification
                                       where (not.DateOfShow <= today && !not.IsAccepted && not.Type == 2) || (not.DateOfShow == today && !not.IsAccepted && not.Type == 1)
                                       select not).ToList();

            return curNotificationList.Any() ? curNotificationList.Count() : 0;
        }

        [HttpGet]
        public PartialViewResult CheckForNewNotifications()
        {
            DateTime today = DateTime.Today.Date;

            List<Notification> dayBeforeNotifications = db.Notification.Where(x => x.DateOfShow == today && !x.IsShown && x.Type == 1).ToList();
            List<Notification> halfYearNotifications = db.Notification.Where(x => x.DateOfShow == today && !x.IsShown && x.Type == 2).ToList();
            if (dayBeforeNotifications.Any() || halfYearNotifications.Any())
            {
                NotificationsViewModel model = new NotificationsViewModel();

                if (today.DayOfWeek.ToString() == "Friday")
                {
                    model.NextWorkDay = today.AddDays(3);
                }
                else
                {
                    model.NextWorkDay = today.AddDays(1);
                }

                List<int> tomorrowAppsIds = dayBeforeNotifications.Select(x => x.AppointmentId).ToList();
                List<int> halfYearAppsIds = halfYearNotifications.Select(x => x.AppointmentId).ToList();

                List<Appointment> curAppointments = db.Appointment.Where(x => tomorrowAppsIds.Contains(x.Id) || halfYearAppsIds.Contains(x.Id)).ToList();

                List<int> curPatientIds = curAppointments.Select(x => x.PatientId).Distinct().ToList();
                List<int> curDoctorIds = curAppointments.Select(x => x.DoctorId).Distinct().ToList();
                List<int> curServiceIds = curAppointments.Select(x => x.ServiceId).Distinct().ToList();

                var curPatients = (from pat in db.Patient
                                   where curPatientIds.Contains(pat.Id)
                                   select new { PatientId = pat.Id, PatientName = pat.FullName, pat.Phone }).ToList();

                List<Doctor> curDocs = db.Doctor.Where(x => curDoctorIds.Contains(x.Id)).ToList();
                List<Service> curServices = db.Service.Where(x => curServiceIds.Contains(x.Id)).ToList();


                model.DayBeforeAppointments = (from not in dayBeforeNotifications
                                               join app in curAppointments on not.AppointmentId equals app.Id
                                               join pat in curPatients on app.PatientId equals pat.PatientId
                                               join doc in curDocs on app.DoctorId equals doc.Id
                                               join service in curServices on app.ServiceId equals service.Id
                                               select new NotificationsViewModel.Appointment()
                                               {
                                                   NotificationId = not.Id,
                                                   PatientName = pat.PatientName,
                                                   Phone = pat.Phone,
                                                   DoctorName = doc.FullName,
                                                   DateTime = app.Date,
                                                   ServiceName = service.Name,
                                                   Comment = app.Comment
                                               }).ToList();

                model.HalfYearAppointments = (from not in halfYearNotifications
                                              join app in curAppointments on not.AppointmentId equals app.Id
                                              join pat in curPatients on app.PatientId equals pat.PatientId
                                              join doc in curDocs on app.DoctorId equals doc.Id
                                              join service in curServices on app.ServiceId equals service.Id
                                              select new NotificationsViewModel.Appointment()
                                              {
                                                  NotificationId = not.Id,
                                                  PatientName = pat.PatientName,
                                                  Phone = pat.Phone,
                                                  DoctorName = doc.FullName,
                                                  DateTime = app.Date,
                                                  ServiceName = service.Name,
                                                  Comment = app.Comment
                                              }).ToList();

                foreach (var notification in dayBeforeNotifications)
                {
                    notification.IsShown = true;
                }

                foreach (var notification in halfYearNotifications)
                {
                    notification.IsShown = true;
                }

                db.SaveChanges();

                return PartialView("NotificationList", model);
            }


            return null;
        }

        [HttpGet]
        public PartialViewResult GetAllNotifications()
        {
            DateTime today = DateTime.Today.Date;

            List<Notification> dayBeforeNotifications = db.Notification.Where(x => x.DateOfShow == today && !x.IsAccepted && x.Type == 1).ToList();
            List<Notification> halfYearNotifications = db.Notification.Where(x => x.DateOfShow <= today && !x.IsAccepted && x.Type == 2).ToList();
            
            NotificationsViewModel model = new NotificationsViewModel();
            model.DayBeforeAppointments = new List<NotificationsViewModel.Appointment>();
            model.HalfYearAppointments = new List<NotificationsViewModel.Appointment>();

            if (today.DayOfWeek.ToString() == "Friday")
            {
                model.NextWorkDay = today.AddDays(3);
            }
            else
            {
                model.NextWorkDay = today.AddDays(1);
            }

            if (dayBeforeNotifications.Any() || halfYearNotifications.Any())
            {

                List<int> tomorrowAppsIds = dayBeforeNotifications.Select(x => x.AppointmentId).ToList();
                List<int> halfYearAppsIds = halfYearNotifications.Select(x => x.AppointmentId).ToList();

                List<Appointment> curAppointments = db.Appointment.Where(x => tomorrowAppsIds.Contains(x.Id) || halfYearAppsIds.Contains(x.Id)).ToList();

                List<int> curPatientIds = curAppointments.Select(x => x.PatientId).Distinct().ToList();
                List<int> curDoctorIds = curAppointments.Select(x => x.DoctorId).Distinct().ToList();
                List<int> curServiceIds = curAppointments.Select(x => x.ServiceId).Distinct().ToList();

                var curPatients = (from pat in db.Patient
                                   where curPatientIds.Contains(pat.Id)
                                   select new { PatientId = pat.Id, PatientName = pat.FullName, pat.Phone }).ToList();

                List<Doctor> curDocs = db.Doctor.Where(x => curDoctorIds.Contains(x.Id)).ToList();
                List<Service> curServices = db.Service.Where(x => curServiceIds.Contains(x.Id)).ToList();


                model.DayBeforeAppointments = (from not in dayBeforeNotifications
                                               join app in curAppointments on not.AppointmentId equals app.Id
                                               join pat in curPatients on app.PatientId equals pat.PatientId
                                               join doc in curDocs on app.DoctorId equals doc.Id
                                               join service in curServices on app.ServiceId equals service.Id
                                               select new NotificationsViewModel.Appointment()
                                               {
                                                   NotificationId = not.Id,
                                                   PatientName = pat.PatientName,
                                                   Phone = pat.Phone,
                                                   DoctorName = doc.FullName,
                                                   DateTime = app.Date,
                                                   ServiceName = service.Name,
                                                   Comment = app.Comment
                                               }).ToList();

                model.HalfYearAppointments = (from not in halfYearNotifications
                                              join app in curAppointments on not.AppointmentId equals app.Id
                                              join pat in curPatients on app.PatientId equals pat.PatientId
                                              join doc in curDocs on app.DoctorId equals doc.Id
                                              join service in curServices on app.ServiceId equals service.Id
                                              select new NotificationsViewModel.Appointment()
                                              {
                                                  NotificationId = not.Id,
                                                  PatientName = pat.PatientName,
                                                  Phone = pat.Phone,
                                                  DoctorName = doc.FullName,
                                                  DateTime = app.Date,
                                                  ServiceName = service.Name,
                                                  Comment = app.Comment
                                              }).ToList();

                foreach (var notification in dayBeforeNotifications.Where(x => !x.IsShown))
                {
                    notification.IsShown = true;
                }

                foreach (var notification in halfYearNotifications.Where(x => !x.IsShown))
                {
                    notification.IsShown = true;
                }

                db.SaveChanges();

            }

            return PartialView("NotificationList", model);
        }

        [HttpPost]
        public JsonResult AcceptNotification(int id)
        {
            Notification curNotification = db.Notification.Find(id);
            curNotification.IsAccepted = true;

            db.SaveChanges();

            return Json(new { success = true });
        }
    }
}