using System;
using System.Collections.Generic;

namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class DashboardDto
    {
        // DashboardModels.cs
        public class DashboardSummaryDto
        {
            public int TotalUsers { get; set; }
            public int ActiveShelves { get; set; }
            public int TotalProducts { get; set; }
            public int ActiveDisplays { get; set; }
        }

        public class RecentShelfDto
        {
            public string ShelfId { get; set; }
            public string Name { get; set; }
            public string Location { get; set; }
            public string Status { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        public class RecentActivityDto
        {
            public string ActivityType { get; set; }
            public string Description { get; set; }
            public string PerformedBy { get; set; }
            public string UserName { get; set; }
            public DateTime ActivityDate { get; set; }
        }

        public class DashboardDataDto
        {
            public DashboardSummaryDto Summary { get; set; }
            public List<RecentShelfDto> RecentShelves { get; set; }
            public List<RecentActivityDto> RecentActivities { get; set; }
        }
    }
}
