using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Document
{
    public class DocumentsListViewModel
    {
        public class Document
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string PatientName { get; set; }
            public int DocTypeId { get; set; }
            public string DocTypeName { get; set; }
            public int Status { get; set; }
        }

        public List<Document> Documents { get; set;}
    }
}