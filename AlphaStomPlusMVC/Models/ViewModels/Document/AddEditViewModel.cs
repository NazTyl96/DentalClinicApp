using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Document
{
    public class AddEditViewModel
    {
        public Models.Document Document { get; set; }
        public List<Models.Patient> PatientSelect { get; set; }
        public List<DocType> DocTypeSelect { get; set; }
    }
}