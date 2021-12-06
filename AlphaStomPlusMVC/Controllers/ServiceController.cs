using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AlphaStomPlusMVC.Models;
using AlphaStomPlusMVC.Models.ViewModels.Service;

namespace AlphaStomPlusMVC.Controllers
{
    public class ServiceController : Controller
    {
        AlphaStomPlusEntities db = new AlphaStomPlusEntities();
        public ActionResult Index(int status = 1)
        {
            IndexViewModel model = new IndexViewModel();

            model.Status = status;

            var filtersInfo = (from service in db.Service
                               where service.Status == status
                               select new { service.Id, service.Name, service.Cost }).ToList();

            model.ServicesFilter = filtersInfo.Select(x => new Service() { Id = x.Id, Name = x.Name, Cost = x.Cost }).Distinct().ToList();

            ViewBag.Title = "Услуги";

            return View(model);
        }

        [HttpGet]
        public PartialViewResult LoadServicesList(string serviceIds, int status)
        {
            List<ServicesListViewModel> model = new List<ServicesListViewModel>();

            var servicesList = (from service in db.Service
                               where service.Status == status && service.Id != 0
                               select new { service.Id, service.Name, service.Cost, service.Status, service.Type/*, service.IsDefault */}).ToList();

            foreach (var service in servicesList)
            {
                ServicesListViewModel curViewModel = new ServicesListViewModel()
                {
                    Id = service.Id,
                    Name = service.Name,
                    Cost = service.Cost,
                    Status = service.Status
                };

                switch (service.Type)
                {
                    case 0:
                        curViewModel.Type = "Терапия";
                        break;
                    case 1:
                        curViewModel.Type = "Хирургия";
                        break;
                    case 2:
                        curViewModel.Type = "Ортопедия";
                        break;
                    case 3:
                        curViewModel.Type = "Ортодонтия";
                        break;
                }

                model.Add(curViewModel);
            }
            //model = servicesList.Select(x => new ServicesListViewModel() { Id = x.Id, Name = x.Name, Cost = x.Cost, Status = x.Status/*, IsDefault = x.IsDefault*/ }).ToList();

            if (!String.IsNullOrEmpty(serviceIds))
            {
                List<int> intServiceIds = new List<int>();
                List<string> stringsServiceIds = serviceIds.Split(',').ToList();
                int idInt = 0;
                foreach (var str in stringsServiceIds)
                {
                    if (Int32.TryParse(str, out idInt))
                    {
                        intServiceIds.Add(idInt);
                    }
                }
                model = model.Where(x => intServiceIds.Contains(x.Id)).ToList();
            }

            return PartialView("ServicesListTable", model.OrderBy(x => x.Type).ToList());

        }

        [HttpGet]
        public PartialViewResult AddService()
        {
            Service model = new Service();

            return PartialView("AddEditForm", model);
        }

        [HttpGet]
        public PartialViewResult EditService(int id)
        {
            Service model = db.Service.Find(id);
            return PartialView("AddEditForm", model);
        }

        [HttpPost]
        public JsonResult SaveService(int id, string name, string cost, int type)
        {
            if (String.IsNullOrEmpty(name))
            {
                ModelState.AddModelError("Name", "Поле 'Название услуги' обязательно для ввода");
            }

            cost = cost.Replace('.', ',').Replace(" ", "").Trim();
            decimal serviceCost = 0.00m;
            if (!Decimal.TryParse(cost, out serviceCost))
            {
                ModelState.AddModelError("Cost", "Для поля 'Стоимость' необходим цифровой формат");
            }

            List<Service> allServices = db.Service.Where(x => x.Status == 1).ToList();
            //if (!allServices.Any(x => x.IsDefault) && isDefault)
            //{
            //    ModelState.AddModelError("IsDefault", "В системе отсутствует услуга по умолчанию. Выберите настоящую услугу как услугу по умолчанию");
            //}

            if (ModelState.IsValid)
            {
                if (id > 0)
                {
                    Service curService = allServices.FirstOrDefault(x => x.Id == id);
         
                    curService.Name = name;
                    curService.Cost = serviceCost;
                    curService.Type = type;

                    //if (isDefault && !curService.IsDefault)
                    //{
                    //    Service curDefault = db.Service.FirstOrDefault(x => x.IsDefault);
                    //    curDefault.IsDefault = false;
                    //}

                    //curService.IsDefault = isDefault;

                    db.SaveChanges();

                    return Json(new { success = true, data = 0 });

                }
                else
                {
                    Service newService = new Service();
                    newService.Status = 1;
                    newService.Name = name;
                    newService.Cost = serviceCost;
                    newService.Type = type;

                    db.Service.Add(newService);
                    db.SaveChanges();

                    return Json(new { success = true, data = newService.Id });
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
        public JsonResult DeleteService(int serviceId)
        {
            Appointment curServiceAppointment = db.Appointment.FirstOrDefault(x => x.ServiceId == serviceId && x.Date >= DateTime.Today.Date);
            if (curServiceAppointment != null)
            {
                return Json(new { success = false, data = "hasAppointment" });
            }

            Service curService = db.Service.Find(serviceId);

            //if (curService.IsDefault)
            //{
            //    return Json(new { success = false, data = "default" });
            //}
            curService.Status = 0;
            db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult RestoreService(int serviceId)
        {
            Service curService = db.Service.Find(serviceId);
            curService.Status = 1;
            db.SaveChanges();

            return Json(new { success = true });
        }

    }
}