using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TERMS_LOYALTY_API.Context;
using TERMS_LOYALTY_API.Data;
using TERMS_LOYALTY_API.DTOs.shelf;
using TERMS_LOYALTY_API.Interface;
using TERMS_LOYALTY_API.Models;
using TERMS_LOYALTY_API.Models.shelf;
using TERMS_LOYALTY_API.Models.user;
using TERMS_LOYALTY_API.Shared.Enum;
using TERMS_LOYALTY_API.Shared.Helpers;
namespace TERMS_LOYALTY_API.Repository
{
    public class MessageRepository: IMessage
    {
        private readonly SmartShelfDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly SmartShelfDbContext _userContext;

        public MessageRepository(SmartShelfDbContext context, IWebHostEnvironment env, SmartShelfDbContext userContext)
        {
            _context = context;
            _env = env;
            _userContext = userContext;
        }

        public async Task<IEnumerable<MessageMaster>> GetMessagesAsync(long? storeId)
        {
            if(storeId == null)
            return await _context.MessageMaster.Include(x => x.ContentTypes).ToListAsync();
            else
                return await _context.MessageMaster.Where(x => x.StoreId  == storeId).Include(x => x.ContentTypes).ToListAsync();
        }

        public async Task<IEnumerable<MessageWithUserDto>> GetMessagesWithUsersAsync(HttpRequest request, long? storeId)
        {
            // 1️⃣ Get messages (store filter applied only if storeId != null)
            var messages = await _context.MessageMaster
                .Where(m => m.IsActive && (storeId == null || m.StoreId == storeId))
                .Include(m => m.ContentTypes)
                .ToListAsync();

            // 2️⃣ Get related users
            var userIds = messages
                .Select(m => m.CreatedUser)
                .Distinct()
                .ToList();

            var users = await _userContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            // 3️⃣ Map result
            var result =
                from m in messages
                join u in users on m.CreatedUser equals u.Id into gj
                from user in gj.DefaultIfEmpty()
                select new MessageWithUserDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    ContentType = m.ContentType,
                    ContentTypeName = m.ContentTypes?.Name ?? "",
                    ContentData = m.ContentData,
                    FabricJsData = m.FabricJsData,
                    FileUrl = request.ToAbsoluteUrl(m.FileUrl),
                    Duration = m.Duration,
                    StoreId = m.StoreId,
                    IsActive = m.IsActive,
                    ScreenSizeId = m.ScreenSizeId,
                    CreatedUser = m.CreatedUser,
                    CreatedByName = user?.UserName ?? "Unknown",
                    CreatedDate = m.CreatedDate
                };

            return result.OrderByDescending(x => x.CreatedDate);
        }


        public async Task<MessageMaster> GetMessageByIdAsync(long id, long? storeId)
        {
            return await _context.MessageMaster
                .FirstOrDefaultAsync(m =>
                    m.Id == id &&
                    m.IsActive &&
                    (storeId == null || m.StoreId == storeId));
        }


        public async Task<MessageMaster> CreateGeneralMessageAsync(MessageCreateDto dto)
        {
            try
            {
                var message = new MessageMaster
                {
                    Title = dto.Title,
                    ContentType = (int)UploadContentType.GeneralMessage,
                    ContentData = dto.ContentData,
                    Duration = dto.Duration ?? 5,
                    StoreId = dto.StoreId,
                    ScreenSizeId = dto.ScreenSizeId,
                    CreatedUser = dto.CreatedBy,
                    CreatedDate = DateTime.UtcNow
                };

                _context.MessageMaster.Add(message);
                await _context.SaveChangesAsync();
                return message;
            }
            catch (Exception ex)
            {

                throw new Exception("Error in creating message", ex);
            }
          
        }
        //public async Task<MessageMaster> CreateImageMessageAsync(IFormFile file, string title, int duration, int createdBy)
        //{
        //    try
        //    {
        //        var fileName = $"image-{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
        //        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
        //                                         "uploads", "images");

        //        Directory.CreateDirectory(uploadsFolder); // makes sure folder exists

        //        var path = Path.Combine(uploadsFolder, fileName);

        //        using var stream = new FileStream(path, FileMode.Create);
        //        await file.CopyToAsync(stream);

        //        var message = new MessageMaster
        //        {
        //            Title = title,
        //            ContentType = (int)UploadContentType.Image,
        //            FileUrl = $"/uploads/images/{fileName}", // ✅ relative path for client
        //            Duration = duration,
        //            CreatedUser = createdBy,
        //            CreatedDate = DateTime.UtcNow
        //        };

        //        _context.MessageMaster.Add(message);
        //        await _context.SaveChangesAsync();
        //        return message;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error uploading image", ex);
        //    }
        //}

        public async Task<MessageMaster> CreateImageMessageAsync(IFormFile file, string title, int duration, int createdBy, long? storeId, long? screenSizeId)
        {
            try
            {
                var fileName = $"image-{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                                                 "uploads", "images");

                Directory.CreateDirectory(uploadsFolder); // makes sure folder exists

                var path = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                // Convert file to base64
                string base64String;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    base64String = Convert.ToBase64String(fileBytes);
                }

                // Optional: Create data URI format (if you want to include MIME type)
                var mimeType = file.ContentType ?? "image/jpeg"; // fallback to jpeg if null
                var base64DataUri = $"data:{mimeType};base64,{base64String}";

                var message = new MessageMaster
                {
                    Title = title,
                    ContentType = (int)UploadContentType.Image,
                    ContentData = base64DataUri, // Save base64 version here
                    FileUrl = $"/uploads/images/{fileName}", // Original file path
                    Duration = duration,
                    StoreId = storeId,
                    ScreenSizeId  = screenSizeId,
                    CreatedUser = createdBy,
                    CreatedDate = DateTime.UtcNow
                };

                _context.MessageMaster.Add(message);
                await _context.SaveChangesAsync();
                return message;
            }
            catch (Exception ex)
            {
                throw new Exception("Error uploading image", ex);
            }
        }

        public async Task<MessageMaster> CreateVideoMessageAsync(IFormFile file, string title, int duration, int createdBy, long? storeId, long? screenSizeId)
        {
            var fileName = $"video-{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(_env.WebRootPath, "uploads/videos", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            var message = new MessageMaster
            {
                Title = title,
                ContentType = (int)UploadContentType.Video,
                FileUrl = $"/uploads/videos/{fileName}",
                Duration = duration,
                StoreId= storeId,
                ScreenSizeId= screenSizeId,
                CreatedUser = createdBy,
                CreatedDate = DateTime.UtcNow
            };

            _context.MessageMaster.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<MessageMaster> CreateCustomImageMessageAsync(string title, string fabricJsData, string imageData, int duration, int createdBy,long? storeId, long? screenSizeId)
        {
            var fileName = $"custom-{DateTime.Now.Ticks}.png";
            var path = Path.Combine(_env.WebRootPath, "uploads/images", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var base64Data = Regex.Replace(imageData, @"^data:image\/\w+;base64,", "");
            await File.WriteAllBytesAsync(path, Convert.FromBase64String(base64Data));

            var message = new MessageMaster
            {
                Title = title,
                ContentType = (int)UploadContentType.Custom,
                FileUrl = $"/uploads/images/{fileName}",
                FabricJsData = fabricJsData,
                Duration = duration,
                StoreId  =  storeId,
                ScreenSizeId = screenSizeId,
                CreatedUser = createdBy,
                CreatedDate = DateTime.UtcNow,
            };

            _context.MessageMaster.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<MessageMaster> UpdateMessageAsync(long id, MessageUpdateDto dto)
        {
            var message = await _context.MessageMaster.FirstOrDefaultAsync(m => m.Id == id && m.IsActive && (dto.StoreId == null || m.StoreId == dto.StoreId));
            if (message == null) return null;

            message.Title = dto.Title;
            message.ContentData = dto.ContentData;
            message.Duration = dto.Duration ?? message.Duration;
            message.ScreenSizeId = dto.ScreenSizeId;
            message.UpdatedUser = dto.UpdatedUser;
            message.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<bool> DeleteMessageAsync(long id, int userId, long? storeId)
        {
            var message = await _context.MessageMaster.FirstOrDefaultAsync(m => m.Id == id && m.IsActive && (storeId == null || m.StoreId == storeId));
            if (message == null) return false;
            message.IsActive = false;
            message.UpdatedUser = userId;
            message.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<MessageMaster> UpdateGeneralMessage(UpdateGeneralMessageDto dto)
        {
            var message =  _context.MessageMaster.Where(x=> x.Id == dto.Id && (dto.StoreId == null || x.StoreId == dto.StoreId)).FirstOrDefault()
                             ?? throw new KeyNotFoundException("Message not found");

            message.Title = dto.Title;
            message.ContentData = dto.ContentData;
            message.Duration = dto.Duration;
            message.ScreenSizeId = dto.ScreenSizeId;
            message.IsActive = dto.IsActive;
            message.UpdatedUser= dto.UpdatedBy;
            message.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<MessageMaster> UpdateImageMessage(long id, IFormFile image, string title, int duration, bool isActive, int updatedBy,long? storeId, long? screenSizeId)
        {
            try
            {
                var message = _context.MessageMaster.Where(x => x.Id == id && (storeId == null || x.StoreId == storeId)).FirstOrDefault()
                       ?? throw new KeyNotFoundException("Message not found");

                // Always update NON-file fields
                message.Title = title;
                message.Duration = duration;
                message.ScreenSizeId = screenSizeId;
                message.IsActive = isActive;
                message.UpdatedUser = updatedBy;
                message.UpdatedDate = DateTime.UtcNow;

                // Update IMAGE only if provided
                if (image != null && image.Length > 0)
                {
                    var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "images");

                    if (!Directory.Exists(uploadsPath))
                        Directory.CreateDirectory(uploadsPath);

                    var fileName = $"image-{DateTime.UtcNow.Ticks}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    message.FileUrl = $"/uploads/images/{fileName}";
                }

                await _context.SaveChangesAsync();
                return message;
            }
            catch (Exception ex)
            {
                throw;
            }      
        }

        public async Task<MessageMaster> UpdateVideoMessage(long id, IFormFile video, string title, int duration, bool isActive, int updatedBy, long? storeId, long? screenSizeId)
        {
            try
            {
                var message = _context.MessageMaster.Where(x => x.Id == id && (storeId == null || x.StoreId == storeId)).FirstOrDefault()
                                 ?? throw new KeyNotFoundException("Message not found");

                // Update NON-file fields always
                message.Title = title;
                message.Duration = duration;
                message.ScreenSizeId = screenSizeId;
                message.IsActive = isActive;
                message.UpdatedUser = updatedBy;
                message.UpdatedDate = DateTime.UtcNow;

                // Update FILE only if video is provided
                if (video != null && video.Length > 0)
                {
                    var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "videos");

                    if (!Directory.Exists(uploadsPath))
                        Directory.CreateDirectory(uploadsPath);

                    var fileName = $"video-{DateTime.UtcNow.Ticks}{Path.GetExtension(video.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await video.CopyToAsync(stream);
                    }

                    message.FileUrl = $"/uploads/videos/{fileName}";
                }

                await _context.SaveChangesAsync();
                return message;
            }
            catch (Exception ex)
            {
                throw;
            }
            
        }

        public async Task<MessageMaster> UpdateCustomImageMessage(UpdateCustomImageMessageDto dto)
        {
            try
            {
                var message = _context.MessageMaster.Where(x => x.Id == dto.Id && (dto.storeId == null || x.StoreId == dto.storeId)).FirstOrDefault()
                                              ?? throw new KeyNotFoundException("Message not found");

                if (!string.IsNullOrEmpty(dto.ImageData))
                {
                    var base64 = dto.ImageData.Replace("data:image/png;base64,", "");
                    var fileName = $"custom-{DateTime.UtcNow.Ticks}.png";
                    var filePath = Path.Combine(_env.WebRootPath, "uploads/images", fileName);
                    await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(base64));

                    message.FileUrl = $"/uploads/images/{fileName}";
                }

                message.Title = dto.Title;
                message.FabricJsData = dto.FabricJsData;
                message.Duration = dto.Duration;
                message.ScreenSizeId = dto.ScreenSizeId;
                message.IsActive = dto.IsActive;
                message.UpdatedUser = dto.UpdatedBy;
                message.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return message;
            }
            catch (Exception ex)
            {
                throw;
            }
           
        }

        public async Task<PagedResult<MessageWithUserPaged>> GetPagedMessagesAsync(MessagePagedRequest request, HttpRequest httpRequest)
        {
            try
            {
                // Base query with includes
                var query = _context.MessageMaster
                    .Include(x => x.ContentTypes)
                    .AsQueryable();

                // Apply filters

                if (request.StoreId.HasValue)
                {
                    query = query.Where(m => m.StoreId == request.StoreId);
                }

                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    query = query.Where(m => m.Title.Contains(request.Title));
                }

                if (request.ContentType.HasValue)
                {
                    query = query.Where(m => m.ContentType == request.ContentType.Value);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(m => m.IsActive == request.IsActive.Value);
                }

                if (request.CreatedBy.HasValue)
                {
                    query = query.Where(m => m.CreatedUser == request.CreatedBy.Value);
                }

                if (request.CreatedFrom.HasValue)
                {
                    query = query.Where(m => m.CreatedDate >= request.CreatedFrom.Value);
                }

                if (request.CreatedTo.HasValue)
                {
                    query = query.Where(m => m.CreatedDate <= request.CreatedTo.Value);
                }

                if (request.UpdatedFrom.HasValue)
                {
                    query = query.Where(m => m.UpdatedDate >= request.UpdatedFrom.Value);
                }

                if (request.UpdatedTo.HasValue)
                {
                    query = query.Where(m => m.UpdatedDate <= request.UpdatedTo.Value);
                }

                // Apply search term if provided
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = query.Where(m =>
                        m.Title.Contains(request.SearchTerm) ||
                        (m.ContentData != null && m.ContentData.Contains(request.SearchTerm)) ||
                        m.Id.ToString().Contains(request.SearchTerm));
                }

                // Apply sorting
                if (!string.IsNullOrWhiteSpace(request.SortBy))
                {
                    query = request.SortBy.ToLower() switch
                    {
                        "title" => request.SortDescending
                            ? query.OrderByDescending(m => m.Title)
                            : query.OrderBy(m => m.Title),
                        "contenttype" => request.SortDescending
                            ? query.OrderByDescending(m => m.ContentType)
                            : query.OrderBy(m => m.ContentType),
                        "duration" => request.SortDescending
                            ? query.OrderByDescending(m => m.Duration)
                            : query.OrderBy(m => m.Duration),
                        "createddate" => request.SortDescending
                            ? query.OrderByDescending(m => m.CreatedDate)
                            : query.OrderBy(m => m.CreatedDate),
                        "updateddate" => request.SortDescending
                            ? query.OrderByDescending(m => m.UpdatedDate)
                            : query.OrderBy(m => m.UpdatedDate),
                        _ => request.SortDescending
                            ? query.OrderByDescending(m => m.Id)
                            : query.OrderBy(m => m.Id)
                    };
                }
                else
                {
                    query = query.OrderByDescending(m => m.CreatedDate);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                var allMessages = await query.ToListAsync();

                // Apply pagination
                //var messages = await query
                //    .Skip((request.PageNumber - 1) * request.PageSize)
                //    .Take(request.PageSize)
                //    .AsNoTracking()
                //    .ToListAsync();

                var messages = allMessages
                               .Skip((request.PageNumber - 1) * request.PageSize)
                               .Take(request.PageSize)
                               .ToList();

                // Get user information for the messages
                var userIds = messages.Select(m => m.CreatedUser).Distinct().ToList();
                var updatedUserIds = messages.Where(m => m.UpdatedUser.HasValue)
                                            .Select(m => m.UpdatedUser.Value)
                                            .Distinct()
                                            .ToList();
                var allUserIds = userIds.Union(updatedUserIds).ToList();

                var users = await _userContext.Users
                    .Where(u => allUserIds.Contains(u.Id))
                    .ToListAsync();

                // Map to DTO with user information
                var items = messages.Select(m => new MessageWithUserPaged
                {
                    Id = m.Id,
                    Title = m.Title,
                    ContentType = m.ContentType,
                    ContentTypeName = m.ContentTypes?.Name ?? "",
                    ContentData = m.ContentData,
                    FabricJsData = m.FabricJsData,
                    FileUrl = string.IsNullOrEmpty(m.FileUrl) ? null : httpRequest.ToAbsoluteUrl(m.FileUrl),
                    Duration = m.Duration,
                    ScreenSizeId = m.ScreenSizeId,
                    IsActive = m.IsActive,
                    CreatedUser = m.CreatedUser,
                    CreatedByName = users.FirstOrDefault(u => u.Id == m.CreatedUser)?.UserName ?? "Unknown",
                    CreatedDate = m.CreatedDate,
                    UpdatedUser = m.UpdatedUser,
                    UpdatedByName = m.UpdatedUser.HasValue ?
                        users.FirstOrDefault(u => u.Id == m.UpdatedUser.Value)?.UserName ?? "Unknown" : null,
                    UpdatedDate = m.UpdatedDate
                }).ToList();

                return new PagedResult<MessageWithUserPaged>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    SearchTerm = request.SearchTerm
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting paginated messages", ex);
            }
        }

        public async Task<List<MessageWithUserDto>> GetDevicesByIdsAsync(List<long> ids, long? storeId)
        {
            return await _context.MessageMaster
                                      .Where(m => ids.Contains(m.Id) && (storeId == null || m.StoreId == storeId))
                      .Select(m => new MessageWithUserDto
                      {
                          Id = m.Id,
                          Title = m.Title,
                          ContentType = m.ContentType,
                          ContentData = m.ContentData,
                          FabricJsData = m.FabricJsData,
                          Duration = m.Duration,
                          IsActive = m.IsActive,
                      })
                      .ToListAsync();
        }
    }
}
