using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AlphaStomPlusMVC.Models;
using AlphaStomPlusMVC.Models.ViewModels.Doctor;

namespace AlphaStomPlusMVC.Controllers
{
    public class DoctorController : Controller
    {
        AlphaStomPlusEntities db = new AlphaStomPlusEntities();
        public ActionResult Index(int status = 1)
        {
            IndexViewModel model = new IndexViewModel();

            model.Status = status;

            var filtersInfo = (from doc in db.Doctor
                               where doc.Status == status
                               select new { doc.Id, doc.FullName }).ToList();

            model.DoctorsFilter = filtersInfo.Select(x => new Doctor() { Id = x.Id, FullName = x.FullName}).Distinct().OrderBy(x => x.FullName).ToList();

            ViewBag.Title = "Врачи";

            return View(model);
        }

        [HttpGet]
        public PartialViewResult LoadDoctorsList(string doctorIds, int status)
        {
            List<Doctor> model = new List<Doctor>();

            var doctorsList = (from doc in db.Doctor
                                  where doc.Status == status
                                  select new { doc.Id, doc.FullName, doc.Position, doc.Status }).ToList();

            model = doctorsList.Select(x => new Doctor() { Id = x.Id, FullName = x.FullName, Position = x.Position, Status = x.Status }).OrderBy(x => x.FullName).ToList();

            if (!String.IsNullOrEmpty(doctorIds))
            {
                List<int> intDocIds = new List<int>();
                List<string> stringsDocIds = doctorIds.Split(',').ToList();
                int idInt = 0;
                foreach (var str in stringsDocIds)
                {
                    if (Int32.TryParse(str, out idInt))
                    {
                        intDocIds.Add(idInt);
                    }
                }
                model = model.Where(x => intDocIds.Contains(x.Id)).ToList();
            }

            return PartialView("DoctorsListTable", model);

        }

        [HttpGet]
        public PartialViewResult ViewDoctor(int id)
        {
            DateTime today = DateTime.Today.Date;

            ViewDoctorViewModel model = new ViewDoctorViewModel();

            model.Doctor = db.Doctor.Find(id);
            model.Appointments = (from app in db.Appointment
                                  where app.DoctorId == id && app.Date >= today
                                  select new { app.Id, app.Date, app.PatientId, app.ServiceId }).Select(x => new ViewDoctorViewModel.Appointment() { Id = x.Id, Date = x.Date, PatientId = x.PatientId, ServiceId = x.ServiceId }).ToList();

            List<int> appPatientIds = model.Appointments.Select(x => x.PatientId).ToList();

            var appPatients = (from pat in db.Patient
                               where appPatientIds.Contains(pat.Id)
                               select new { pat.Id, pat.FullName }).ToList();

            List<int> appServicesIds = model.Appointments.Select(x => x.ServiceId).ToList();

            var appServices = (from service in db.Service
                               where appServicesIds.Contains(service.Id)
                               select new { service.Id, service.Name }).ToList();

            foreach (var app in model.Appointments)
            {
                app.PatientName = appPatients.FirstOrDefault(x => x.Id == app.PatientId).FullName;
                app.ServiceName = appServices.FirstOrDefault(x => x.Id == app.ServiceId).Name;
            }

            return PartialView("ViewForm", model);
        }

        [HttpGet]
        public PartialViewResult AddDoctor()
        {
            Doctor model = new Doctor();

            return PartialView("AddEditForm", model);
        }

        [HttpGet]
        public PartialViewResult EditDoctor(int id)
        {
            Doctor model = db.Doctor.Find(id);
            return PartialView("AddEditForm", model);
        }

        [HttpPost]
        public JsonResult SaveDoctor(Doctor newDoctor)
        {
            if (String.IsNullOrEmpty(newDoctor.FullName))
            {
                ModelState.AddModelError("FullName", "Поле 'ФИО' обязательно для ввода");
            }

            if (ModelState.IsValid)
            {
                if (newDoctor.Id > 0)
                {
                    Doctor curDoctor = db.Doctor.Find(newDoctor.Id);
                    foreach (var property in typeof(Doctor).GetProperties())
                    {
                        if (property.GetValue(newDoctor) != null)
                        {
                            property.SetValue(curDoctor, property.GetValue(newDoctor));
                        }

                    }

                    db.SaveChanges();

                    return Json(new { success = true, data = 0 });
                }
                else
                {
                    newDoctor.Status = 1;
                    db.Doctor.Add(newDoctor);
                    db.SaveChanges();

                    return Json(new { success = true, data = newDoctor.Id });
                }

            }

            List<string> validationMessages = new List<string>();
            foreach (var item in ModelState.Values)
            {
                foreach (var msg in item.Errors)
                {
                    validationMessages.Add(msg.ErrorMessage);

                }
            }

            return Json(new { success = false, data = validationMessages });
        }

        [HttpPost]
        public JsonResult DeleteDoctor(int doctorId)
        {
            Doctor curDoctor = db.Doctor.Find(doctorId);
            curDoctor.Status = 0;

            List<Appointment> curDoctorAppointments = db.Appointment.Where(x => x.DoctorId == doctorId).ToList();

            List<int> appIds = curDoctorAppointments.Select(x => x.Id).ToList();
            List<Notification> curAppNotifications = db.Notification.Where(x => appIds.Contains(x.AppointmentId)).ToList();
            foreach (var notification in curAppNotifications)
            {
                db.Notification.Remove(notification);
            }

            foreach (var app in curDoctorAppointments)
            {
                db.Appointment.Remove(app);
            }

            db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult RestoreDoctor(int doctorId)
        {
            Doctor curDoctor = db.Doctor.Find(doctorId);
            curDoctor.Status = 1;
            db.SaveChanges();

            return Json(new { success = true });
        }

    }
}