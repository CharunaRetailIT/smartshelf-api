using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Repository;
using TERMS_LOYALTY_API.Shared.Enum;

namespace TERMS_LOYALTY_API.Services
{
    public class QueueProcessorService : BackgroundService
    {
        private readonly ILogger<QueueProcessorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30); // Check every 30 seconds

        public QueueProcessorService(ILogger<QueueProcessorService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue Processor Service started.");

            // Initial delay to let application start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<SmartShelfDbContext>();
                    var queueService = scope.ServiceProvider.GetRequiredService<IQueue>();

                    await ProcessQueues(context, queueService, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in queue processor");
                }

                // Run every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task ProcessQueues(SmartShelfDbContext context, IQueue queueService, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            try
            {
                // 1. Process pending queues that should start
                await ProcessPendingQueues(context, queueService, now, cancellationToken);

                // 2. Process active queues that should end
                await ProcessEndingQueues(context, queueService, now, cancellationToken);

                // 3. Process recurring queues
                await ProcessRecurringQueues(context, queueService, now, cancellationToken);

                // 4. Clean up old completed queues (older than 30 days)
                await CleanupOldQueues(context, now, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queues");
            }
        }

        private async Task ProcessPendingQueues(SmartShelfDbContext context, IQueue queueService, DateTime now, CancellationToken cancellationToken)
        {
            var queuesToStart = await context.QueueMaster
                .Include(q => q.Device)
                .Where(q => q.IsActive &&
                           q.StatusId == (int)QueueStatus.Pending &&
                           q.StartDate <= now &&
                           (q.EndDate == null || q.EndDate > now))
                .Take(20) // Process max 20 at a time
                .ToListAsync(cancellationToken);

            foreach (var queue in queuesToStart)
            {
                try
                {
                    _logger.LogInformation($"Activating queue {queue.Id} (scheduled for {queue.StartDate})");

                    // Use the service to activate
                    await queueService.ActivateQueueAsync(queue.Id);

                    _logger.LogInformation($"Successfully activated queue {queue.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to activate queue {queue.Id}");

                    // Update queue as failed
                    queue.StatusId = (int)QueueStatus.Failed;
                    queue.ErrorMessage = ex.Message;
                    queue.RetryCount = (queue?.RetryCount ?? 0) + 1;
                    queue.LastAttempt = now;

                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        private async Task ProcessEndingQueues(SmartShelfDbContext context, IQueue queueService, DateTime now, CancellationToken cancellationToken)
        {
            var queuesToEnd = await context.QueueMaster
                .Include(q => q.Device)
                .Where(q => q.IsActive &&
                           q.StatusId == (int)QueueStatus.Completed &&
                           q.EndDate.HasValue &&
                           q.EndDate <= now)
                .Take(20) // Process max 20 at a time
                .ToListAsync(cancellationToken);

            foreach (var queue in queuesToEnd)
            {
                try
                {
                    _logger.LogInformation($"Ending queue {queue.Id} (ended at {queue.EndDate})");

                    // Deactivate the queue
                    await queueService.DeactivateQueueAsync(queue.Id);

                    // If recurring, create next occurrence
                    if (queue?.IsRecurring ?? false)
                    {
                        await CreateNextRecurrence(context, queue, now, cancellationToken);
                    }

                    _logger.LogInformation($"Successfully ended queue {queue.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to end queue {queue.Id}");
                }
            }
        }

        private async Task ProcessRecurringQueues(SmartShelfDbContext context, IQueue queueService, DateTime now, CancellationToken cancellationToken)
        {
            // Process queues that have completed but need to be rescheduled
            var completedRecurringQueues = await context.QueueMaster
                .Where(q => q.IsActive &&
                           q.StatusId == (int)QueueStatus.Completed &&
                           (q.IsRecurring) &&
                           q.EndDate.HasValue &&
                           q.EndDate <= now.AddMinutes(-5)) // Give 5 minute buffer
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var queue in completedRecurringQueues)
            {
                await CreateNextRecurrence(context, queue, now, cancellationToken);
            }
        }

        private async Task CreateNextRecurrence(SmartShelfDbContext context, QueueMaster queue, DateTime now, CancellationToken cancellationToken)
        {
            try
            {
                var nextStartDate = CalculateNextRecurrence(queue, now);

                if (!nextStartDate.HasValue || nextStartDate <= now)
                    return;

                // Create new queue for next occurrence
                var nextQueue = new QueueMaster
                {
                    DeviceId = queue.DeviceId,
                    TemplateId = queue.TemplateId,
                    MessageId = queue.MessageId,
                    LocationType = queue.LocationType,
                    LocationId = queue.LocationId,
                    ProductId = queue.ProductId,
                    ShelfId = queue.ShelfId,
                    StartDate = nextStartDate.Value,
                    EndDate = queue.EndDate.HasValue ?
                        nextStartDate.Value.Add(queue.EndDate.Value - queue.StartDate) : null,
                    IsActive = true,
                    StatusId = (int)QueueStatus.Pending,
                    PriorityId = queue.PriorityId,
                    QueueType = queue.QueueType,
                    DisplayOrder = queue.DisplayOrder,
                    IsRecurring = queue.IsRecurring,
                    RecurrencePattern = queue.RecurrencePattern,
                    CreatedDate = now,
                    CreatedUser = queue.CreatedUser,
                    StoreId = queue.StoreId
                };

                context.QueueMaster.Add(nextQueue);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"Created next recurrence for queue {queue.Id} at {nextStartDate}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create next recurrence for queue {queue.Id}");
            }
        }

        private DateTime? CalculateNextRecurrence(QueueMaster queue, DateTime now)
        {
            if (!(queue?.IsRecurring ?? false) || string.IsNullOrEmpty(queue.RecurrencePattern))
                return null;

            var lastStart = queue.StartDate;

            return queue.RecurrencePattern.ToUpper() switch
            {
                "DAILY" => lastStart.AddDays(1),
                "WEEKLY" => lastStart.AddDays(7),
                "MONTHLY" => lastStart.AddMonths(1),
                "YEARLY" => lastStart.AddYears(1),
                _ => null
            };
        }

        private async Task CleanupOldQueues(SmartShelfDbContext context, DateTime now, CancellationToken cancellationToken)
        {
            // Clean up completed queues older than 30 days
            var cutoffDate = now.AddDays(-30);

            var oldQueues = await context.QueueMaster
                .Where(q => q.StatusId == (int)QueueStatus.Completed &&
                           q.EndDate.HasValue &&
                           q.EndDate < cutoffDate &&
                           !(q.IsRecurring))
                .Take(50) // Clean up 50 at a time
                .ToListAsync(cancellationToken);

            if (oldQueues.Any())
            {
                context.QueueMaster.RemoveRange(oldQueues);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"Cleaned up {oldQueues.Count} old queues");
            }
        }

    }
}
