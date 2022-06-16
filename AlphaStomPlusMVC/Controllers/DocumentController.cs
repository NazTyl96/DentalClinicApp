using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AlphaStomPlusMVC.Models;
using AlphaStomPlusMVC.Models.ViewModels.Document;

namespace AlphaStomPlusMVC.Controllers
{
    public class DocumentController : Controller
    {
        AlphaStomPlusEntities db = new AlphaStomPlusEntities();
        public ActionResult Index(int status = 1)
        {
            IndexViewModel model = new IndexViewModel();

            model.Status = status;

            //var filtersInfo = (from doc in db.Document
            //                   join type in db.DocType on doc.TypeId equals type.Id
            //                   join pat in db.Patient on doc.PatientId equals pat.Id
            //                   where doc.Status == status && pat.Status == status
            //                   select new { pat.FullName, docId = doc.Id, doc.Name, docTypeId = type.Id, type.TypeName }).ToList();

            var filtersInfo = (from doc in db.Document
                               join type in db.DocType on doc.TypeId equals type.Id
                               join pat in db.Patient on doc.PatientId equals pat.Id into gj
                               from subpat in gj.DefaultIfEmpty()
                               select new { docId = doc.Id, doc.Name, doc.Status, PatientName = subpat != null ? subpat.FullName : "", docTypeId = type.Id }).ToList();

            model.DocumentFilter = filtersInfo.Select(x => new Document() { Id = x.docId, Name = x.Name }).Distinct().ToList();
            model.PatientFilter = filtersInfo.Select(x => x.PatientName).Distinct().ToList();

            List<int> typeIds = filtersInfo.Select(x => x.docTypeId).Distinct().ToList();

            var docTypeInfo = (from type in db.DocType
                               where typeIds.Contains(type.Id)
                               select new { type.Id, type.TypeName }).ToList();

            model.DocTypeFilter = docTypeInfo.Select(x => new DocType() { Id = x.Id, TypeName = x.TypeName }).ToList();
            ViewBag.Title = "Документы";

            return View(model);
        }

        [HttpGet]
        public PartialViewResult LoadDocumentsList(string docIds, string patientNames, string docTypeIds)
        {
            DocumentsListViewModel model = new DocumentsListViewModel();

            model.Documents = (from doc in db.Document
                               join type in db.DocType on doc.TypeId equals type.Id
                               join pat in db.Patient on doc.PatientId equals pat.Id into gj
                               from subpat in gj.DefaultIfEmpty()
                               select new { docId = doc.Id, doc.Name, doc.Status, PatientName = subpat != null ? subpat.FullName : "", docTypeId = type.Id, type.TypeName}).Select(x => new DocumentsListViewModel.Document() { Id = x.docId, Name = x.Name, PatientName = x.PatientName, DocTypeId = x.docTypeId, DocTypeName = x.TypeName, Status = x.Status }).ToList();

            if (!String.IsNullOrEmpty(docIds))
            {
                List<int> allIds = new List<int>();
                List<string> idsStrings = docIds.Split(',').ToList();
                int idInt = 0;
                foreach (var str in idsStrings)
                {
                    if (Int32.TryParse(str, out idInt))
                    {
                        allIds.Add(idInt);
                    }
                }
                model.Documents = model.Documents.Where(x => allIds.Contains(x.Id)).ToList();
            }

            if (!String.IsNullOrEmpty(patientNames))
            {
                List<string> namesList = patientNames.Replace("0", "").Split(',').ToList();
                model.Documents = model.Documents.Where(x => namesList.Contains(x.PatientName)).ToList();
            }

            if (!String.IsNullOrEmpty(docTypeIds))
            {
                List<int> allIds = new List<int>();
                List<string> idsStrings = docTypeIds.Split(',').ToList();
                int idInt = 0;
                foreach (var str in idsStrings)
                {
                    if (Int32.TryParse(str, out idInt))
                    {
                        allIds.Add(idInt);
                    }
                }
                model.Documents = model.Documents.Where(x => allIds.Contains(x.DocTypeId)).ToList();
            }

            return PartialView("DocumentsListTable", model);

        }

        [HttpGet]
        public PartialViewResult AddEditDocType()
        {
            List<DocType> model = db.DocType.ToList();

            return PartialView("AddEditTypeForm", model);
        }

        public PartialViewResult SaveDocType(int id, string name)
        {
            if (id > 0)
            {
                DocType curType = db.DocType.Find(id);
                curType.TypeName = name;
            }
            else
            {
                DocType newType = new DocType() { TypeName = name };
                db.DocType.Add(newType);
            }

            db.SaveChanges();

            List<DocType> model = db.DocType.ToList();

            return PartialView("AddEditTypeForm", model);
        }

        [HttpGet]
        public FileResult ViewDocument(int docId)
        {
            Document model = db.Document.Find(docId);

            if (model != null)
            {
                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "utf-8";
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.ContentType = MimeMapping.GetMimeMapping(model.Name);
                Response.AppendHeader("Content-Disposition", "attachment; filename=" + model.Name + "");
                Response.BinaryWrite(model.Content);
                Response.Flush();
                Response.End();
            }

            return File(model.Content, MimeMapping.GetMimeMapping(model.Name), model.Name);
        }

        [HttpGet]
        public PartialViewResult AddDocument(int patientId = 0)
        {
            AddEditViewModel model = new AddEditViewModel();
            model.Document = new Document() { PatientId = patientId };

            var patientsList = (from pat in db.Patient
                                where pat.Status == 1
                                select new { pat.Id, pat.FullName }).ToList();

            model.PatientSelect = patientsList.Select(x => new Patient() { Id = x.Id, FullName = x.FullName }).ToList();

            var typesList = db.DocType.ToList();

            model.DocTypeSelect = typesList.Select(x => new DocType() { Id = x.Id, TypeName = x.TypeName }).ToList();

            return PartialView("AddEditForm", model);
        }

        [HttpGet]
        public PartialViewResult EditDocument(int docId)
        {
            AddEditViewModel model = new AddEditViewModel();
            model.Document = db.Document.Find(docId);

            var patientsList = (from pat in db.Patient
                                where pat.Status == 1
                                select new { pat.Id, pat.FullName }).ToList();

            model.PatientSelect = patientsList.Select(x => new Patient() { Id = x.Id, FullName = x.FullName }).ToList();

            var typesList = db.DocType.ToList();

            model.DocTypeSelect = typesList.Select(x => new DocType() { Id = x.Id, TypeName = x.TypeName }).ToList();

            return PartialView("AddEditForm", model);
        }

        [HttpPost]
        public JsonResult SaveDocument(int id, int? patientId, int typeId, string name, HttpPostedFileBase content)
        {
            if (id == 0)
            {
                if (String.IsNullOrEmpty(name))
                {
                    ModelState.AddModelError("Name", "Поле 'Название' обязательно для ввода");
                }

                string contentType = content.ContentType;

                if (content == null || (contentType != "application/pdf" && contentType != "image/jpeg" && contentType != "image/png"))
                {
                    ModelState.AddModelError("Content", "Пожалуйста, загрузите файл в формате .pdf, .jpeg, .jpg или .png");
                }

                if (ModelState.IsValid)
                {
                    Document newDocument = new Document();
                    newDocument.Status = 1;
                    newDocument.PatientId = patientId;
                    newDocument.TypeId = typeId;
                    newDocument.Name = name;
                    System.IO.BinaryReader reader = new System.IO.BinaryReader(content.InputStream);
                    newDocument.Content = reader.ReadBytes((int)content.ContentLength); ;
                    db.Document.Add(newDocument);
                    db.SaveChanges();

                    return Json(new { success = true, data = newDocument.Id });
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
            else
            {
                Document curDocument = db.Document.Find(id);
                curDocument.PatientId = patientId;
                curDocument.TypeId = typeId;
                db.SaveChanges();

                return Json(new { success = true, data = 0 });
            }
        }

        [HttpPost]
        public JsonResult DeleteDocument(int docId)
        {
            Document curDocument = db.Document.Find(docId);
            db.Document.Remove(curDocument);
            db.SaveChanges();

            return Json(new { success = true });
        }

    }
}