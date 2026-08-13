using Microsoft.EntityFrameworkCore;
using TERMS_LOYALTY_API.Context;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Models.user;

namespace TERMS_LOYALTY_API.Data
{
    public class SmartShelfDbContext : DbContext
    {
        public SmartShelfDbContext(DbContextOptions<SmartShelfDbContext> options)
        : base(options) { }

        public DbSet<AisleMaster> AisleMaster { get; set; }
        public DbSet<ShelfMaster> ShelfMaster { get; set; }
        public DbSet<ProductMaster> ProductMaster { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductSubCategory> ProductSubCategories { get; set; }
        public DbSet<ProductAssignment> ProductAssignments{ get; set; }

        public DbSet<MessageMaster> MessageMaster { get; set; }
        public DbSet<QueueMaster> QueueMaster { get; set; }
        public DbSet<QueueExecutionLog> QueueExecutionLog { get; set; }
        public DbSet<ContentType> ContentType { get; set; }

        public DbSet<StoreMaster> StoreMaster { get; set; }

        public DbSet<AppSetting> AppSettings { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        public DbSet<Department> Departments { get; set; }

        public DbSet<DisplayType> DisplayType { get; set; }
        public DbSet<DeviceMaster> DeviceMaster { get; set; }
        public DbSet<MinewDevices> MinewDevices { get; set; }
        public DbSet<MinewTemplates> MinewTemplates { get; set; }

        public DbSet<DeviceTemplateCombos> DeviceTemplateCombos { get; set; }
        public DbSet<DeviceAssignment> DeviceAssignment { get; set; }
        public DbSet<Status> Status { get; set; }
        public DbSet<DeviceMessageCombos> DeviceMessageCombos { get; set; }
        public DbSet<DeviceScreen> DeviceScreens { get; set; }
        public DbSet<DeviceScreenType> DeviceScreenTypes { get; set; }
        public DbSet<GatewayMaster> GatewayMaster { get; set; }
        public DbSet<PriorityMaster> PriorityMaster { get; set; }
        public DbSet<EslBrandMaster> EslBrandMaster { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<StoreDetailsRow>().HasNoKey();

        }
    }
}
