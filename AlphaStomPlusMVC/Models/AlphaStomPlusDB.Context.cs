﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class AlphaStomPlusEntities : DbContext
    {
        public AlphaStomPlusEntities()
            : base("name=AlphaStomPlusEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Appointment> Appointment { get; set; }
        public virtual DbSet<Doctor> Doctor { get; set; }
        public virtual DbSet<DocType> DocType { get; set; }
        public virtual DbSet<Document> Document { get; set; }
        public virtual DbSet<Notification> Notification { get; set; }
        public virtual DbSet<Patient> Patient { get; set; }
        public virtual DbSet<Service> Service { get; set; }
    }
}
