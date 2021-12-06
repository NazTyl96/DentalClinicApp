using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Document
{
    public class IndexViewModel
    {
        public List<string> PatientFilter { get; set; }
        public List<Models.Document> DocumentFilter { get; set; }
        public List<Models.DocType> DocTypeFilter { get; set; }
        public int Status { get; set; }
    }
}