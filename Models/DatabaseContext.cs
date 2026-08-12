using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Models;

namespace TERMS_MOBILE_WEB_API.Models
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext()
        {
        }

        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public virtual DbSet<UserMaster>? UserMaster { get; set; }
        
        public virtual DbSet<AutoGenerateInfo>? AutoGenerateInfo { get; set; }
        public virtual DbSet<UserPrivileges>? UserPrivileges { get; set; }
       

        public virtual DbSet<DocumentNumber>? DocumentNumber { get; set; }

      


        public virtual DbSet<Location>? Location { get; set; }

        public virtual DbSet<Company>? Company { get; set; }


        public virtual DbSet<CustomerGroup>? CustomerGroup { get; set; }

        public virtual DbSet<AppConfig>? AppConfig { get; set; }

        public virtual DbSet<Customer>? Customer { get; set; }
        public virtual DbSet<Employee>? Employee { get; set; }
        public virtual DbSet<LoyaltyCustomer>? LoyaltyCustomer { get; set; }

        public virtual DbSet<ExternalLoyaltyTransaction>? ExternalLoyaltyTransaction { get; set; }

        public virtual DbSet<TransactionDet>? TransactionDet { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
        }

       
    }
}
