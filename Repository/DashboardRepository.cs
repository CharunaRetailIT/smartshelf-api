using log4net.Repository.Hierarchy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using static TERMS_LOYALTY_API.DTOs.shelf.DashboardDto;

namespace TERMS_LOYALTY_API.Repository
{
    public class DashboardRepository : IDashboard
    {
        private readonly SmartShelfDbContext _context;
        private readonly ILogger<DashboardRepository> _logger;

        public DashboardRepository(SmartShelfDbContext context, ILogger<DashboardRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardDto.DashboardDataDto> GetDashboardDataAsync(int? userId = null)
        {
            try
            {
                var dashboardData = new DashboardDataDto();

                // Call stored procedure using raw SQL
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    // FIX: Removed EXEC and parameter from command text
                    command.CommandText = "sp_GetDashboardData";
                    command.CommandType = CommandType.StoredProcedure;

                    var userIdParam = command.CreateParameter();
                    userIdParam.ParameterName = "@UserId";
                    userIdParam.Value = userId ?? (object)DBNull.Value;
                    userIdParam.DbType = DbType.Int32;
                    command.Parameters.Add(userIdParam);

                    await _context.Database.OpenConnectionAsync();

                    using (var result = await command.ExecuteReaderAsync())
                    {
                        // 1. Read recent shelves
                        if (await result.ReadAsync())
                        {
                            dashboardData.RecentShelves = new List<RecentShelfDto>();
                            do
                            {
                                dashboardData.RecentShelves.Add(new RecentShelfDto
                                {
                                    ShelfId = result["ShelfId"] as string,
                                    Name = result["Name"] as string,
                                    Location = result["Location"] as string,
                                    Status = result["Status"] as string,
                                    CreatedDate = Convert.ToDateTime(result["CreatedDate"])
                                });
                            } while (await result.ReadAsync());
                        }

                        // 2. Read recent activities
                        if (await result.NextResultAsync() && await result.ReadAsync())
                        {
                            dashboardData.RecentActivities = new List<RecentActivityDto>();
                            do
                            {
                                dashboardData.RecentActivities.Add(new RecentActivityDto
                                {
                                    ActivityType = result["ActivityType"] as string,
                                    Description = result["Description"] as string,
                                    PerformedBy = result["PerformedBy"]?.ToString(),
                                    UserName = result["UserName"] as string,
                                    ActivityDate = Convert.ToDateTime(result["ActivityDate"])
                                });
                            } while (await result.ReadAsync());
                        }

                        // 3. Read summary counts
                        if (await result.NextResultAsync() && await result.ReadAsync())
                        {
                            dashboardData.Summary = new DashboardSummaryDto
                            {
                                TotalUsers = Convert.ToInt32(result["TotalUsers"]),
                                ActiveShelves = Convert.ToInt32(result["ActiveShelves"]),
                                TotalProducts = Convert.ToInt32(result["TotalProducts"]),
                                ActiveDisplays = Convert.ToInt32(result["ActiveDisplays"])
                            };
                        }
                    }
                }

                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                throw;
            }
        }
    }
}
