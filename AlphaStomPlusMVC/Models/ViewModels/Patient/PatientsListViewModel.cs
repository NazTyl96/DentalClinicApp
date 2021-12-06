using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Patient
{
    public class PatientsListViewModel
    {
        public class PatientInfo
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public DateTime DateOfBirth { get; set; }
            public int Sex { get; set; }
            public int? CardNumber { get; set; }
            public int Status { get; set; }
        }

        public List<PatientInfo> PatientsList { get; set; }

        public enum Sexes
        {
            Female = 0,
            Male = 1
        }
    }
}