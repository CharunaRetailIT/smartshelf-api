using System.Collections.Generic;
using System.Linq;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Models.shelf;

namespace TERMS_LOYALTY_API.Shared.Helpers
{
    public static class QueryableExtensions
    {
        //Device & Template Combo return sorting helper method
        public static IQueryable<DeviceTemplateComboViewModel> ApplyComboSorting( this IQueryable<DeviceTemplateComboViewModel> query,string sortBy, bool sortDescending)
        {
            var sortField = string.IsNullOrWhiteSpace(sortBy) ? "Priority" : sortBy;

            // Since this is called after ToList(), we're working with in-memory data
            var enumerable = query.AsEnumerable();

            IEnumerable<DeviceTemplateComboViewModel> sorted = sortField.ToLower() switch
            {
                "priority" => sortDescending
                    ? enumerable.OrderByDescending(x => x.Priority)
                    : enumerable.OrderBy(x => x.Priority),

                "isdefault" => sortDescending
                    ? enumerable.OrderByDescending(x => x.IsDefault)
                    : enumerable.OrderBy(x => x.IsDefault),

                "devicename" => sortDescending
                    ? enumerable.OrderByDescending(x => x.DeviceName)
                    : enumerable.OrderBy(x => x.DeviceName),

                "templatename" => sortDescending
                    ? enumerable.OrderByDescending(x => x.TemplateName)
                    : enumerable.OrderBy(x => x.TemplateName),

                "createddate" => sortDescending
                    ? enumerable.OrderByDescending(x => x.CreatedDate)
                    : enumerable.OrderBy(x => x.CreatedDate),

                "screeninch" => sortDescending
                    ? enumerable.OrderByDescending(x => x.ScreenInch)
                    : enumerable.OrderBy(x => x.ScreenInch),

                _ => sortDescending
                    ? enumerable.OrderByDescending(x => x.Priority)
                    : enumerable.OrderBy(x => x.Priority)
            };

            return sorted.AsQueryable();
        }

        //Device assignment (to location - Shelf or Product) return sorting helper method 
        public static IQueryable<AssignmentViewModel> ApplyAssignmentSorting(
            this IQueryable<AssignmentViewModel> query,
            string sortBy,
            bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                sortBy = "DisplayOrder";
            }

            // Since this is called after ToList(), we're working with in-memory data
            var enumerable = query.AsEnumerable();

            IEnumerable<AssignmentViewModel> sorted = sortBy.ToLower() switch
            {
                // Sort by assignment properties
                "displayorder" => sortDescending
                    ? enumerable.OrderByDescending(x => x.DisplayOrder)
                    : enumerable.OrderBy(x => x.DisplayOrder),

                "createddate" => sortDescending
                    ? enumerable.OrderByDescending(x => x.CreatedDate)
                    : enumerable.OrderBy(x => x.CreatedDate),

                "locationtype" => sortDescending
                    ? enumerable.OrderByDescending(x => x.LocationType)
                    : enumerable.OrderBy(x => x.LocationType),

                "locationid" => sortDescending
                    ? enumerable.OrderByDescending(x => x.LocationId)
                    : enumerable.OrderBy(x => x.LocationId),

                "isactive" => sortDescending
                    ? enumerable.OrderByDescending(x => x.IsActive)
                    : enumerable.OrderBy(x => x.IsActive),

                "createduser" => sortDescending
                    ? enumerable.OrderByDescending(x => x.CreatedUser)
                    : enumerable.OrderBy(x => x.CreatedUser),

                "createdusername" => sortDescending
                    ? enumerable.OrderByDescending(x => x.CreatedUser)
                    : enumerable.OrderBy(x => x.CreatedUser),

                "createduserid" => sortDescending
                    ? enumerable.OrderByDescending(x => x.CreatedUserId)
                    : enumerable.OrderBy(x => x.CreatedUserId),

                // Sort by device properties
                "devicename" => sortDescending
                    ? enumerable.OrderByDescending(x => x.DeviceName)
                    : enumerable.OrderBy(x => x.DeviceName),

                "devicemac" => sortDescending
                    ? enumerable.OrderByDescending(x => x.DeviceMac)
                    : enumerable.OrderBy(x => x.DeviceMac),

                "deviceheight" => sortDescending
                    ? enumerable.OrderByDescending(x => x.DeviceHeight)
                    : enumerable.OrderBy(x => x.DeviceHeight),

                "devicewidth" => sortDescending
                    ? enumerable.OrderByDescending(x => x.DeviceWidth)
                    : enumerable.OrderBy(x => x.DeviceWidth),

                // Sort by template properties
                "templatename" => sortDescending
                    ? enumerable.OrderByDescending(x => x.TemplateName)
                    : enumerable.OrderBy(x => x.TemplateName),

                // Default sorting
                _ => sortDescending
                    ? enumerable.OrderByDescending(x => x.DisplayOrder)
                    : enumerable.OrderBy(x => x.DisplayOrder)
            };

            return sorted.AsQueryable();
        }

        public static QueueDto ToDto(this QueueMaster queue)
        {
            if (queue == null) return null;

            return new QueueDto
            {
                Id = queue.Id,
                QueueType = queue.QueueType,
                DeviceName = queue.Device?.Name,
                DeviceType = queue.Device?.DeviceType,
                LocationType = queue.LocationType,
                LocationName = queue.LocationType switch
                {
                    "PRODUCT" => queue.Product?.ProductName,
                    "SHELF" => queue.Shelf?.Name,
                    _ => null
                },
                ProductId = queue.ProductId,
                ProductName = queue.Product?.ProductName,
                ShelfId = queue.ShelfId,
                ShelfName = queue.Shelf?.Name,
                TemplateName = queue.Template?.Name,
                MessageTitle = queue.Message?.Title,
                StartDate = queue.StartDate,
                EndDate = queue.EndDate,
                Status = queue.Status?.Name,
                Priority = queue.Priority?.PriorityName,
                IsActive = queue.IsActive,
                IsRecurring = queue.IsRecurring,
                RecurrencePattern = queue.RecurrencePattern,
                DisplayOrder = queue.DisplayOrder,
                CreatedDate = queue.CreatedDate,
                StoreId = queue.StoreId,
                StoreName = queue.Store?.StoreName
            };
        }
    }
}
