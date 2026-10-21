using AccessRequestSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ResourcesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists all resources available to request access for.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetResources()
    {
        var resources = await _context.Resources
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        return Ok(resources);
    }
}
