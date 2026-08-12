using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.SignalRHubs;

namespace TERMS_LOYALTY_API.Services
{
    public class TableChangeMonitorService : BackgroundService
    {
        private readonly ILogger<TableChangeMonitorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<DeviceAssignmentHub> _hubContext;
        private readonly Dictionary<string, DateTime> _lastCheckTimes = new Dictionary<string, DateTime>();
        public TableChangeMonitorService(ILogger<TableChangeMonitorService> logger, IServiceProvider serviceProvider, IHubContext<DeviceAssignmentHub> hubContext)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Table Change Monitor Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<SmartShelfDbContext>();

                        // Monitor DeviceAssignment table changes
                        await MonitorTableChanges(context, "DeviceAssignment", stoppingToken);

                        // Monitor DeviceMessageCombos table changes
                        await MonitorTableChanges(context, "DeviceMessageCombos", stoppingToken);

                        // Monitor MessageMaster table changes
                        await MonitorTableChanges(context, "MessageMaster", stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring table changes");
                }

                // Check every 2 seconds for real-time updates
                await Task.Delay(2000, stoppingToken);
            }
        }

        private async Task MonitorTableChanges(SmartShelfDbContext context, string tableName, CancellationToken cancellationToken)
        {
            var key = $"{tableName}-lastUpdate";
            var lastCheckTime = _lastCheckTimes.ContainsKey(key)
                ? _lastCheckTimes[key]
                : DateTime.UtcNow.AddMinutes(-5);

            var newLastUpdate = DateTime.UtcNow;

            try
            {
                switch (tableName)
                {
                    case "DeviceAssignment":
                        var changedAssignments = await context.DeviceAssignment
                            .Where(da => da.UpdatedDate > lastCheckTime || da.CreatedDate > lastCheckTime)
                            .Include(da => da.DeviceMessageCombo)
                                .ThenInclude(dmc => dmc.Message)
                            .Include(da => da.DeviceMessageCombo)
                                .ThenInclude(dmc => dmc.Device)
                            .ToListAsync(cancellationToken);

                        foreach (var assignment in changedAssignments)
                        {
                            await BroadcastAssignmentChange(assignment);
                        }
                        break;

                    case "DeviceMessageCombos":
                        var changedCombos = await context.DeviceMessageCombos
                            .Where(dmc => dmc.UpdatedDate > lastCheckTime || dmc.CreatedDate > lastCheckTime)
                            .Include(dmc => dmc.Message)
                            .Include(dmc => dmc.Device)
                            .ToListAsync(cancellationToken);

                        foreach (var combo in changedCombos)
                        {
                            await BroadcastComboChange(combo);
                        }
                        break;

                    case "MessageMaster":
                        var changedMessages = await context.MessageMaster
                            .Where(m => m.UpdatedDate > lastCheckTime || m.CreatedDate > lastCheckTime)
                            .ToListAsync(cancellationToken);

                        foreach (var message in changedMessages)
                        {
                            await BroadcastMessageChange(message);
                        }
                        break;
                }
            }
            finally
            {
                _lastCheckTimes[key] = newLastUpdate;
            }
        }

        private async Task BroadcastAssignmentChange(DeviceAssignment assignment)
        {
            var groupName = DeviceAssignmentHub.GetGroupName(
                assignment.DeviceMessageCombo?.Device?.DeviceType ?? "unknown",
                assignment.StoreId);

            await _hubContext.Clients.Group(groupName).SendAsync("AssignmentChanged", new
            {
                Type = "assignment",
                Action = assignment.IsActive ? "updated" : "deleted",
                Data = new
                {
                    assignment.Id,
                    DeviceId = assignment.DeviceMessageCombo?.DeviceId,
                    MessageId = assignment.DeviceMessageCombo?.MessageId,
                    assignment.LocationType,
                    assignment.LocationId,
                    assignment.IsActive,
                    assignment.DisplayOrder,
                    assignment.CreatedDate,
                    assignment.UpdatedDate
                }
            });
        }

        private async Task BroadcastComboChange(DeviceMessageCombos combo)
        {
            // Find related assignments
            var assignments = await GetRelatedAssignments(combo.Id);

            foreach (var assignment in assignments)
            {
                await BroadcastAssignmentChange(assignment);
            }
        }

        private async Task<List<DeviceAssignment>> GetRelatedAssignments(long comboId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SmartShelfDbContext>();
                return await context.DeviceAssignment
                    .Where(da => da.DeviceMessageComboId == comboId)
                    .Include(da => da.DeviceMessageCombo)
                        .ThenInclude(dmc => dmc.Message)
                    .Include(da => da.DeviceMessageCombo)
                        .ThenInclude(dmc => dmc.Device)
                    .ToListAsync();
            }
        }

        private async Task BroadcastMessageChange(MessageMaster message)
        {
            // Get all combos using this message
            var combos = await GetCombosByMessageId(message.Id);

            foreach (var combo in combos)
            {
                await BroadcastComboChange(combo);
            }
        }

        private async Task<List<DeviceMessageCombos>> GetCombosByMessageId(long messageId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SmartShelfDbContext>();
                return await context.DeviceMessageCombos
                    .Where(dmc => dmc.MessageId == messageId)
                    .Include(dmc => dmc.Message)
                    .Include(dmc => dmc.Device)
                    .ToListAsync();
            }
        }
    }
}
