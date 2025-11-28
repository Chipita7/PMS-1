using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Requests;

namespace ProjectManagementSystem1.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class RequestConfigController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfigurationService _configService;

        public RequestConfigController(AppDbContext context, IConfigurationService configService)
        {
            _context = context;
            _configService = configService;
        }

        [HttpGet("{type}")]
        public async Task<IActionResult> GetOptions(string type)
        {
            try
            {
                object result = type.ToLower() switch
                {
                    "requesttypes" => await _context.RequestTypeConfigs.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(),
                    "requestcategories" => await _context.RequestCategoryConfigs.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(),
                    "servicecategories" => await _context.ServiceCategoryConfigs.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(),
                    "productcategories" => await _context.ProductCategoryConfigs.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(),
                    "priorities" => await _context.PriorityConfigs.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(),
                    "impacturgencies" => await _context.ImpactUrgencyConfigs.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(),
                    "strategicalignments" => await _context.StrategicAlignmentConfigs.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(),
                    _ => null
                };

                if (result == null) return BadRequest("Invalid configuration type");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("{type}")]
        public async Task<IActionResult> AddOption(string type, [FromBody] ConfigOptionCreateDto dto)
        {
            try
            {
                switch (type.ToLower())
                {
                    case "requesttypes":
                        _context.RequestTypeConfigs.Add(new RequestTypeConfig { Name = dto.Name, Code = dto.Code, Description = dto.Description, SortOrder = dto.SortOrder, IsActive = true });
                        break;
                    case "requestcategories":
                        _context.RequestCategoryConfigs.Add(new RequestCategoryConfig { Name = dto.Name, Code = dto.Code, Description = dto.Description, SortOrder = dto.SortOrder, IsActive = true });
                        break;
                    case "servicecategories":
                        _context.ServiceCategoryConfigs.Add(new ServiceCategoryConfig { Name = dto.Name, Code = dto.Code, Description = dto.Description, SortOrder = dto.SortOrder, IsActive = true });
                        break;
                    case "productcategories":
                        _context.ProductCategoryConfigs.Add(new ProductCategoryConfig { Name = dto.Name, Code = dto.Code, Description = dto.Description, SortOrder = dto.SortOrder, IsActive = true });
                        break;
                    case "priorities":
                        _context.PriorityConfigs.Add(new PriorityConfig { Name = dto.Name, Code = dto.Code, Description = dto.Description, Color = dto.Color, SortOrder = dto.SortOrder, IsActive = true });
                        break;
                    case "impacturgencies":
                        _context.ImpactUrgencyConfigs.Add(new ImpactUrgencyConfig { Name = dto.Name, Code = dto.Code, Description = dto.Description, Color = dto.Color, SortOrder = dto.SortOrder, IsActive = true });
                        break;
                    case "strategicalignments":
                        _context.StrategicAlignmentConfigs.Add(new StrategicAlignmentConfig { Name = dto.Name, Code = dto.Code, Description = dto.Description, SortOrder = dto.SortOrder, IsActive = true });
                        break;
                    default:
                        return BadRequest("Invalid configuration type");
                }

                await _context.SaveChangesAsync();
                await _configService.RefreshCacheAsync();
                return Ok(new { message = "Option added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{type}/{id}")]
        public async Task<IActionResult> DeleteOption(string type, int id)
        {
            try
            {
                switch (type.ToLower())
                {
                    case "requesttypes":
                        var rt = await _context.RequestTypeConfigs.FindAsync(id);
                        if (rt != null) _context.RequestTypeConfigs.Remove(rt);
                        break;
                    case "requestcategories":
                        var rc = await _context.RequestCategoryConfigs.FindAsync(id);
                        if (rc != null) _context.RequestCategoryConfigs.Remove(rc);
                        break;
                    case "servicecategories":
                        var sc = await _context.ServiceCategoryConfigs.FindAsync(id);
                        if (sc != null) _context.ServiceCategoryConfigs.Remove(sc);
                        break;
                    case "productcategories":
                        var pc = await _context.ProductCategoryConfigs.FindAsync(id);
                        if (pc != null) _context.ProductCategoryConfigs.Remove(pc);
                        break;
                    case "priorities":
                        var p = await _context.PriorityConfigs.FindAsync(id);
                        if (p != null) _context.PriorityConfigs.Remove(p);
                        break;
                    case "impacturgencies":
                        var iu = await _context.ImpactUrgencyConfigs.FindAsync(id);
                        if (iu != null) _context.ImpactUrgencyConfigs.Remove(iu);
                        break;
                    case "strategicalignments":
                        var sa = await _context.StrategicAlignmentConfigs.FindAsync(id);
                        if (sa != null) _context.StrategicAlignmentConfigs.Remove(sa);
                        break;
                    default:
                        return BadRequest("Invalid configuration type");
                }

                await _context.SaveChangesAsync();
                await _configService.RefreshCacheAsync();
                return Ok(new { message = "Option deleted successfully" });
            }
            catch (Exception ex)
            {
                 // Check for foreign key constraint violation
                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                     return BadRequest("Cannot delete this option because it is being used by existing requests.");
                }
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
