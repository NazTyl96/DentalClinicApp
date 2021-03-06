//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AlphaStomPlusMVC.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Appointment
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Appointment()
        {
            this.Notification = new HashSet<Notification>();
        }
    
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int ServiceId { get; set; }
        public System.DateTime Date { get; set; }
        public string Comment { get; set; }
    
        public virtual Doctor Doctor { get; set; }
        public virtual Patient Patient { get; set; }
        public virtual Service Service { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Notification> Notification { get; set; }
    }
}
