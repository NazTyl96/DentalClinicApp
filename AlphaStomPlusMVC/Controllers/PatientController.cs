using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Web.Mvc;
using AlphaStomPlusMVC.Models;
using AlphaStomPlusMVC.Models.ViewModels.Patient;
using Spire.Doc;
using Spire.Doc.Documents;

namespace AlphaStomPlusMVC.Controllers
{
    public class PatientController : Controller
    {
        AlphaStomPlusEntities db = new AlphaStomPlusEntities();

        //an object for deserialization of filters values from the view
        internal class PatientSearchParams
        {
            public List<string> FullNames { get; set; }
            public List<DateTime> DatesOfBirth { get; set; }
            public List<int> Sexes { get; set; }
            public int Status { get; set; }
        }

        public ActionResult Index(int status = 1)
        {
            IndexViewModel model = new IndexViewModel();

            model.Status = status;

            var filtersInfo = (from pat in db.Patient
                               where pat.Status == status
                               select new { pat.FullName, pat.Sex, pat.DateOfBirth }).ToList();

            model.FullNames = filtersInfo.Select(x => x.FullName).Distinct().OrderBy(x => x).ToList();
            model.Sexes = filtersInfo.Select(x => x.Sex).Distinct().ToList();
            model.DatesOfBirth = filtersInfo.Select(x => x.DateOfBirth).Distinct().OrderBy(x => x).ToList();

            ViewBag.Title = "Patients";

            return View(model);
        }

        [HttpGet]
        public PartialViewResult LoadPatientsList(string queryString)
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            PatientSearchParams searchParams = serializer.Deserialize<PatientSearchParams>(queryString);

            PatientsListViewModel model = new PatientsListViewModel();

            //creating an IQueryable object to materialize later
            var dbQuery = (from pat in db.Patient
                           where pat.Status == searchParams.Status
                           select new
                           {
                               pat.Id,
                               pat.FullName,
                               pat.CardNumber,
                               pat.Sex,
                               pat.DateOfBirth,
                               pat.Status
                           });
            

            if (searchParams.FullNames.Any())
            {
                dbQuery = dbQuery.Where(x => searchParams.FullNames.Contains(x.FullName));
            }

            if (searchParams.DatesOfBirth.Any())
            {
                dbQuery = dbQuery.Where(x => searchParams.DatesOfBirth.Contains(x.DateOfBirth));
            }

            if (searchParams.Sexes.Any())
            {
                dbQuery = dbQuery.Where(x => searchParams.Sexes.Contains(x.Sex));
            }

            //materializing the query after having it filtrated
            model.PatientsList = dbQuery.Select(x => new PatientsListViewModel.PatientInfo() {
                                                                                                Id = x.Id,
                                                                                                CardNumber = x.CardNumber,
                                                                                                DateOfBirth = x.DateOfBirth,
                                                                                                FullName = x.FullName,
                                                                                                Sex = x.Sex,
                                                                                                Status = x.Status
                                                                                            }).OrderBy(x => x.FullName).ToList();

            return PartialView("PatientsListTable", model);

        }

        [HttpGet]
        public PartialViewResult ViewPatient(int id)
        {
            //today's date for finding patient's upcoming appointments
            DateTime today = DateTime.Today.Date;

            ViewPatientViewModel model = new ViewPatientViewModel();

            model.Patient = db.Patient.Find(id);
            model.Appointments = (from app in db.Appointment
                                  where app.PatientId == id && app.Date >= today
                                  select new { app.Id, app.Date, app.DoctorId, app.ServiceId }).Select(x => new ViewPatientViewModel.Appointment() { Id = x.Id, Date = x.Date, DoctorId = x.DoctorId, ServiceId = x.ServiceId }).ToList();

            model.Documents = (from doc in db.Document
                               join type in db.DocType on doc.TypeId equals type.Id
                               where doc.PatientId == id && doc.Status == 1
                               select new { doc.Id, doc.Name, type.TypeName, doc.Content }).Select(x => new ViewPatientViewModel.Document() { Id = x.Id, Name = x.Name, TypeName = x.TypeName, Content = x.Content }).ToList();

            List<int> appDoctorIds = model.Appointments.Select(x => x.DoctorId).ToList();

            var appDoctors = (from doc in db.Doctor
                              where appDoctorIds.Contains(doc.Id)
                              select new { doc.Id, doc.FullName }).ToList();

            List<int> appServicesIds = model.Appointments.Select(x => x.ServiceId).ToList();

            var appServices = (from service in db.Service
                               where appServicesIds.Contains(service.Id)
                               select new { service.Id, service.Name }).ToList();

            foreach (var app in model.Appointments)
            {
                app.DoctorName = appDoctors.FirstOrDefault(x => x.Id == app.DoctorId).FullName;
                app.ServiceName = appServices.FirstOrDefault(x => x.Id == app.ServiceId).Name;
            }

            return PartialView("ViewForm", model);
        }

        [HttpGet]
        public PartialViewResult AddPatient()
        {
            AddEditViewModel model = new AddEditViewModel();
            model.Patient = new Patient() { DateOfBirth = new DateTime(2000, 1, 1), Sex = 1 };

            var doctorsList = (from doc in db.Doctor
                               where doc.Status == 1
                               select new { doc.Id, doc.FullName }).OrderBy(x => x.FullName).ToList();

            model.Doctors = doctorsList.Select(x => new Doctor() { Id = x.Id, FullName = x.FullName }).ToList();

            var servicesList = (from service in db.Service
                              where service.Status == 1
                              select new { service.Id, service.Name }).ToList();

            model.Services = servicesList.Select(x => new Service() { Id = x.Id, Name = x.Name }).ToList();

            model.Today = DateTime.Today;

            return PartialView("AddEditForm", model);
        }

        [HttpGet]
        public PartialViewResult EditPatient(int id)
        {
            AddEditViewModel model = new AddEditViewModel();
            model.Patient = db.Patient.Find(id);

            return PartialView("AddEditForm", model);
        }

        [HttpPost]
        public JsonResult SavePatient(Patient patient, Appointment newAppointment)
        {
            if (String.IsNullOrEmpty(patient.FullName))
            {
                ModelState.AddModelError("FullName", "'Full name' is required");
            }

            if (patient.DateOfBirth.Year < 1900)
            {
                ModelState.AddModelError("DateOfBirth", "Please enter an actual date of birth");
            }

            if (ModelState.IsValid)
            {
                if (patient.Id > 0)
                {
                    Patient curPatient = db.Patient.Find(patient.Id);
                    foreach (var property in typeof(Patient).GetProperties())
                    {
                        if (property.GetValue(patient) != null)
                        {
                            property.SetValue(curPatient, property.GetValue(patient));
                        }

                    }

                    db.SaveChanges();

                    return Json(new { success = true, data = 0 });
                }
                else
                {
                    patient.Status = 1;
                    db.Patient.Add(patient);
                    db.SaveChanges();

                    newAppointment.Id = 0;
                    newAppointment.PatientId = patient.Id;
                    var appointmentController = DependencyResolver.Current.GetService<AppointmentController>();
                    appointmentController.ControllerContext = new ControllerContext(this.Request.RequestContext, appointmentController);
                    appointmentController.SaveAppointment(newAppointment);

                    return Json(new { success = true, data = patient.Id });
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
        public JsonResult DeletePatient(int patientId)
        {
            Patient curPatient = db.Patient.Find(patientId);
            curPatient.Status = 0;

            List<Appointment> curPatientAppointments = db.Appointment.Where(x => x.PatientId == patientId).ToList();

            List<int> appIds = curPatientAppointments.Select(x => x.Id).ToList();
            List<Notification> curAppNotifications = db.Notification.Where(x => appIds.Contains(x.AppointmentId)).ToList();
            foreach (var notification in curAppNotifications)
            {
                db.Notification.Remove(notification);
            }

            foreach (var app in curPatientAppointments)
            {
                db.Appointment.Remove(app);
            }

            List<AlphaStomPlusMVC.Models.Document> curPatientDocuments = db.Document.Where(x => x.PatientId == patientId).ToList();
            foreach (var doc in curPatientDocuments)
            {
                db.Document.Remove(doc);
            }

            db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult RestorePatient(int patientId)
        {
            Patient curPatient = db.Patient.Find(patientId);
            curPatient.Status = 1;
            db.SaveChanges();

            return Json(new { success = true });
        }

        #region print forms (patient's card, agreement, consent)
        [HttpGet]
        public ActionResult PatientCard(int id)
        {
            ViewPatientViewModel model = new ViewPatientViewModel();

            model.Patient = db.Patient.Find(id);

            ViewBag.Title = "Patient card";

            return View(model);
        }

        public ActionResult PatientDogovorWord(int id)
        {

            ViewPatientViewModel model = new ViewPatientViewModel();

            model.Patient = db.Patient.Find(id);

            var culture = CultureInfo.CreateSpecificCulture("ru-RU");
            var birthDate = model.Patient.DateOfBirth.ToString("d MMMM yyyy", culture);
            var today = DateTime.Now.ToString("d MMMM yyyy", culture);

            #region word
            Spire.Doc.Document doc = new Spire.Doc.Document();

            ParagraphStyle paragraphStyle = doc.AddParagraphStyle("paragraphStyle");
            paragraphStyle.CharacterFormat.FontSize = 8;

            ParagraphStyle headerStyle = doc.AddParagraphStyle("headerStyle");
            headerStyle.CharacterFormat.FontSize = 8;
            headerStyle.ParagraphFormat.BeforeSpacing = 10;
            headerStyle.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;

            ParagraphStyle underlineStyle = doc.AddParagraphStyle("underlineStyle");
            underlineStyle.CharacterFormat.FontSize = 8;
            underlineStyle.CharacterFormat.UnderlineStyle = UnderlineStyle.Single;


            //Add Section
            Section section = doc.AddSection();

            //первая страница
            Section sec1 = section;
            sec1.Body.ChildObjects.Clear();

            Paragraph par1 = sec1.AddParagraph();
            par1.ApplyStyle("headerStyle");
            par1.AppendText("Договор с пациентом №_____");

            Paragraph par2 = sec1.AddParagraph();
            par2.ApplyStyle("headerStyle");
            par2.Format.AfterSpacing = 12;
            par2.AppendText(String.Format("Санкт-Петербург {0}г.", today));

            Paragraph par3 = sec1.AddParagraph();
            par3.ApplyStyle("paragraphStyle");
            par3.AppendText("ООО “НАЗВАНИЕ_КЛИНИКИ” лицензия № ЛИЦЕНЗИЯ, именуемая в дальнейшем “Исполнитель”, в лице Генерального директора ФИО ГЕНЕРАЛЬНОГО ДИРЕКТОРА, действующего на основании Устава, с одной стороны и “Заказчика”, в лице гражданина:");


            Paragraph par32 = sec1.AddParagraph();
            par32.ApplyStyle("underlineStyle");
            par32.AppendText(String.Format(@"{0}, {1}, {2}                                                                                         (Ф.И.О.,дата рождения, телефон, адрес)", model.Patient.FullName, birthDate, model.Patient.Phone));


            Paragraph par33 = sec1.AddParagraph();
            par33.ApplyStyle("paragraphStyle");
            par33.AppendText("с другой стороны, заключили договор о нижеследующем: ");

            Paragraph par4 = sec1.AddParagraph();
            par4.ApplyStyle("headerStyle");
            par4.AppendText("1.ПРЕДМЕТ ДОГОВОРА");

            Paragraph par5 = sec1.AddParagraph();
            par5.ApplyStyle("paragraphStyle");
            par5.AppendText("Заказчик поручает, а Исполнитель принимает на себя обязанность по оказанию Заказчику платных стоматологических услуг в виде профилактической, лечебно-диагностической, зубопротезной помощи.");

            Paragraph par6 = sec1.AddParagraph();
            par6.ApplyStyle("headerStyle");
            par6.AppendText("2.ОБЯЗАННОСТИ СТОРОН");

            Paragraph par7 = sec1.AddParagraph();
            par7.ApplyStyle("paragraphStyle");
            par7.AppendText(
                String.Format(@"2.1 Исполнитель обязуется:{0}" +
                "- выполнять платные стоматологические услуги в согласованные сроки." +
                "2.2 Заказчик обязуется:{0}" +
                "- выполнять требования, обеспечивающие качественное предоставление платной стоматологической услуги, включая сообщение необходимых для этого сведений;{0}" +
                "- оплатить стоимость предоставляемых стоматологических услуг;{0}" +
                "- дать разрешение делать рентгеновские снимки и проводить другие диагностические мероприятия, необходимые для лечения.", Environment.NewLine)
            );

            Paragraph par8 = sec1.AddParagraph();
            par8.ApplyStyle("headerStyle");
            par8.AppendText("3.СТОИМОСТЬ УСЛУГ И ПОРЯДОК ОПЛАТЫ");

            Paragraph par9 = sec1.AddParagraph();
            par9.ApplyStyle("paragraphStyle");
            par9.AppendText("Расчеты за выполнение услуг производятся согласно действующего прейскуранта на платные услуги Исполнителя");

            Paragraph par10 = sec1.AddParagraph();
            par10.ApplyStyle("headerStyle");
            par10.AppendText("4.РАССМАТРИВАНИЕ СПОРА");

            Paragraph par11 = sec1.AddParagraph();
            par11.ApplyStyle("paragraphStyle");
            par11.AppendText("Стороны обязуются в случае возникновения спора урегулировать его путем переговоров");

            Paragraph par12 = sec1.AddParagraph();
            par12.ApplyStyle("headerStyle");
            par12.AppendText("4.РАССМАТРИВАНИЕ СПОРА");

            Paragraph par13 = sec1.AddParagraph();
            par13.ApplyStyle("paragraphStyle");
            par13.AppendText("Расчеты за выполнение услуг производятся согласно действующего прейскуранта на платные услуги Исполнителя.");

            Paragraph par14 = sec1.AddParagraph();
            par14.ApplyStyle("headerStyle");
            par14.AppendText("5.ГАРАНТИЙНЫЕ ОБЯЗАТЕЛЬСТВА");

            Paragraph par15 = sec1.AddParagraph();
            par15.ApplyStyle("paragraphStyle");
            par15.AppendText("Исполнитель несет ответственность сроком 12 месяцев в пределах стоимости оказанных услуг.");

            Paragraph par16 = sec1.AddParagraph();
            par16.ApplyStyle("headerStyle");
            par16.AppendText("6.ДОПОЛНИТЕЛЬНЫЕ УСЛОВИЯ");

            Paragraph par17 = sec1.AddParagraph();
            par17.ApplyStyle("paragraphStyle");
            par17.AppendText("Заказчик соглашается, что несмотря на стремление врача к успешному результату лечения, могут иметь место непредвиденные осложнения, связанные с проведением стоматологических манипуляций и обязуется выполнить все рекомендации врача, направленные на профилактику их возникновения и их лечения.");

            Paragraph par18 = sec1.AddParagraph();
            par18.ApplyStyle("headerStyle");
            par18.AppendText("7.СВЕДЕНИЯ О ЗАКАЗЧИКЕ");

            Paragraph par19 = sec1.AddParagraph();
            par19.ApplyStyle("paragraphStyle");
            par19.AppendText("Последующая информация является крайне важной для того, чтобы Мы могли обеспечить Вас стоматологическим лечением в соответствии с Вашим состоянием здоровья. Неправильная информация может повредить Вашему здоровью. Все изменения в Вашем общем состоянии здоровья должны быть сообщены нам при последующих посещениях.");

            Paragraph par20 = sec1.AddParagraph();
            par20.ApplyStyle("paragraphStyle");
            par20.AppendText(
                String.Format(@"Аллергическая реакция на антибиотики, анестетики и другие лекарства (НЕТ, ДА, то какие)________________________________________________________________________________{0}
Заболевания ЦНС______________________________________________________________________
Повышенное, пониженное АД____________________________________________________________
Повышенная кровоточивость, анемия_____________________________________________________
Гепатит(год). Заболевания ЖКТ, печени, почек_____________________________________________
Диабет___________________________Бронхиальная астма__________________________________
Эпилепсия, потеря сознания, обмороки___________________________________________________
Заболевания нижнечелюстного сустава___________________________________________________
Туберкулез___________________________________________________________________________
Добавьте то, что Вы считатете важным___________________________________________________
", Environment.NewLine)
            );

            Paragraph par21 = sec1.AddParagraph();
            par21.ApplyStyle("headerStyle");
            par21.AppendText("8.ПОДПИСИ СТОРОН");

            Paragraph par22 = sec1.AddParagraph();
            par22.ApplyStyle("paragraphStyle");
            par22.AppendText("Последующая информация является крайне важной для того, чтобы Мы могли обеспечить Вас стоматологическим лечением в соответствии с Вашим состоянием здоровья. Неправильная информация может повредить Вашему здоровью. Все изменения в Вашем общем состоянии здоровья должны быть сообщены нам при последующих посещениях.");

            Paragraph par23 = sec1.AddParagraph();
            par23.ApplyStyle("paragraphStyle");
            par23.Format.BeforeSpacing = 10;
            var par23TR = par23.AppendText(
                String.Format(@"Исполнитель                                                                                                                         Заказчик
________________ФИО ГЕНЕРАЛЬНОГО ДИРЕКТОРА                                                           _______________________________
ИНН
АДРЕС"
, Environment.NewLine)
            );

            using (var memoryStream = new System.IO.MemoryStream())
            {
                //DateTime createDate = DateTime.Now;

                //Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                //Response.AddHeader("content-disposition", "attachment;  filename=Договор " + model.Patient.FullName + " бланк.docx");

                doc.SaveToStream(memoryStream, FileFormat.Docx2013);
                Response.Clear();
                //Response.ContentType = "application/msword";
                //Response.AppendHeader("Content-Length", memoryStream.Length.ToString());
                Response.ContentType = "application/download";
                Response.AddHeader("content-disposition", "attachment;  filename=Договор " + model.Patient.FullName + " бланк.docx");
                Response.BinaryWrite(memoryStream.ToArray());
                Response.Flush();
                Response.End();
            }
            #endregion

            return null;
        }

        public ActionResult PatientConsentWord(int id)
        {
            ViewPatientViewModel model = new ViewPatientViewModel();

            model.Patient = db.Patient.Find(id);

            var culture = CultureInfo.CreateSpecificCulture("ru-RU");
            var birthDate = model.Patient.DateOfBirth.ToString("d MMMM yyyy", culture);

            #region word
            Spire.Doc.Document doc = new Spire.Doc.Document();

            ParagraphStyle paragraphStyle = doc.AddParagraphStyle("paragraphStyle");
            paragraphStyle.CharacterFormat.FontSize = 12;
            paragraphStyle.ParagraphFormat.LineSpacing = 13;


            ParagraphStyle headerStyle = doc.AddParagraphStyle("headerStyle");
            headerStyle.CharacterFormat.FontSize = 12;
            headerStyle.CharacterFormat.Bold = true;
            headerStyle.ParagraphFormat.BeforeSpacing = 20;
            headerStyle.ParagraphFormat.AfterSpacing = 10;
            headerStyle.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;

            //Add Section
            Section section = doc.AddSection();
            Section sec1 = section;
            sec1.Body.ChildObjects.Clear();

            Paragraph par1 = sec1.AddParagraph();
            par1.ApplyStyle("headerStyle");
            par1.AppendText("ИНФОРМИРОВАННОЕ ДОБРОВОЛЬНОЕ СОГЛАСИЕ ПАЦИЕНТА НА МЕДИЦИНСКОЕ ВМЕШАТЕЛЬСТВО");

            Paragraph par2 = sec1.AddParagraph();
            par2.ApplyStyle("paragraphStyle");
            par2.AppendText(
                String.Format(@"Я, {1}_____________________________________________________
                                                            (Ф.И.О. гражданина)
{2}г. рождения,
зарегистрированный по адресу: _______________________________________________________
                                (адрес места жительства гражданина либо законного представителя)
даю информированное добровольное согласие на проведение медицинского обследования, в том числе выявление жалоб, сбор анамнеза, осмотр______________________________________, обследование_____________________ и медицинские вмешательства_____________________ в
__________________________________________________________________________.
                        (полное наименование медицинской организации)
Медицинским работником ____________________________________________________
                                                        (должность, Ф.И.О. медицинского работника)
в доступной для меня форме мне разъяснены:
- цели, методы оказания медицинской помощи, связанный с ними риск, возможные варианты медицинских вмешательств, их последствия, в том числе вероятность развития осложнений, а также предполагаемые результаты оказания медицинской помощи.Мне разъяснено, что я имею право отказаться от одного или нескольких видов медицинских
вмешательств или потребовать его(их) прекращения, за исключением случаев, предусмотренных частью 9 статьи 20 Федерального закона от 21 ноября 2011 г. № 323 - ФЗ «Об основах охраны здоровья граждан в Российской Федерации»;
‐ цели, характер, содержание и неблагоприятные эффекты указанных медицинских действий, возможности непреднамеренного причинения вреда здоровью, в том числе нетрудоспособности и смерти, а также о том, что предстоит мне делать во время их проведения;
В доступной для меня форме, что в ходе выполнения указанных выше медицинских действий может возникнуть необходимость выполнения другого вмешательства, исследования, операции, не указанных выше. 
Я доверяю врачам и иным медицинским работникам принять соответствующее решение, в соответствии с их профессиональными суждениями выполнять любые медицинские действия, которые они сочтут необходимыми для улучшения моего(представляемого) состояния.
Я извещен(а), о том, что мне необходимо строго и неукоснительно выполнять все указания и рекомендации медицинских работников, которые я буду получать при прохождении определенных видов медицинских вмешательств, немедленно сообщать врачу о любом ухудшении самочувствия.
Я предупрежден(а) и осознаю, что отказ от рекомендаций медицинских работников, может отрицательно сказаться на состоянии моего(представляемого) здоровья и привести к искажению результатов определенных видов медицинских вмешательств, что в свою очередь может привести к неверной интерпретации результатов определенных видов
медицинских вмешательств;
Я поставил(а) в известность врача обо всех проблемах, связанных с моим(лица, мной представляемого) здоровьем, в том числе об аллергических проявлениях или индивидуальной непереносимости лекарственных препаратов, обо всех перенесенных мною
(представляемым) и известных мне травмах, операциях, заболеваниях, об экологических и производственных факторах физической, химической или биологической природы, воздействующих на меня(представляемого) во время жизнедеятельности, о принимаемых
лекарственных средствах. 
Я сообщил(а) правдивые сведения о наследственности, а также об употреблении алкоголя, наркотических и токсических средств.
Разрешаю, в случае необходимости, предоставить информацию о моем диагнозе, степени и характере моего заболевания законным представителям.
Сведения о выбранных мною лицах, которым в соответствии с пунктом 5
части 3 статьи 19 Федерального закона от 21 ноября 2011 г.N 323 - ФЗ {0}Об
основах охраны здоровья граждан в Российской Федерации{0} может быть передана
информация о состоянии моего здоровья

{1} {3}_____________________________
                   (Ф.И.О. гражданина, контактный телефон)

Я ознакомлен(а) и согласен(а) со всеми пунктами настоящего документа, положения которого мне разъяснены, мною поняты и добровольно даю свое согласие на обследование и лечение в предложенном объеме.



Подпись пациента: __________________________


Расписался в моем присутствии:
Медицинский работник_________________________________/_________________
                                          (должность, фамилия, имя, отчество)           (подпись)
", "\"", model.Patient.FullName, birthDate, model.Patient.Phone)
            );

            using (var memoryStream = new System.IO.MemoryStream())
            {
                doc.SaveToStream(memoryStream, FileFormat.Docx2013);
                Response.Clear();
                Response.ContentType = "application/download";
                Response.AddHeader("content-disposition", "attachment;  filename=Информированное согласие " + model.Patient.FullName + " бланк.docx");
                Response.BinaryWrite(memoryStream.ToArray());
                Response.Flush();
                Response.End();
            }
            #endregion

            return null;
        }
        #endregion

    }
}