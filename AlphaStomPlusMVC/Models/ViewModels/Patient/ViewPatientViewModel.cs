using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlphaStomPlusMVC.Models.ViewModels.Patient
{
    public class ViewPatientViewModel
    {
        public Models.Patient Patient { get; set; }
        public partial class Document
        {
            public int Id { get; set; }
            public Nullable<int> PatientId { get; set; }
            public string Name { get; set; }
            public Nullable<int> TypeId { get; set; }
            public string TypeName { get; set; }
            public byte[] Content { get; set; }
        }
        public List<Document> Documents { get; set; }
        public class Appointment
        {
            public int Id { get; set; }
            public DateTime Date { get;set; }
            public int DoctorId { get; set; }
            public string DoctorName { get; set; }
            public int ServiceId { get; set; }
            public string ServiceName { get; set; }
        } 
        public List <Appointment> Appointments { get; set; }

    }
}