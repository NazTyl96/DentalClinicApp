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
    
    public partial class Notification
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public bool IsShown { get; set; }
        public bool IsAccepted { get; set; }
        public int Type { get; set; }
        public System.DateTime DateOfShow { get; set; }
    
        public virtual Appointment Appointment { get; set; }
    }
}
