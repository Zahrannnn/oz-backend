using Microsoft.AspNetCore.Mvc;
using Oz.Api.DTOs;
using Oz.Domain.Entities;
using Oz.Domain.Repositories;

namespace Oz.Api.Controllers;

[ApiController]
[Route("api/v1/admin/[controller]")]
public class SchoolsController : ControllerBase
{
    private readonly IRepository<School> _schoolRepository;

    public SchoolsController(IRepository<School> schoolRepository)
    {
        _schoolRepository = schoolRepository;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SchoolDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSchoolDto dto, CancellationToken ct)
    {
        var entity = new School
        {
            Name = dto.Name,
            NameAr = dto.NameAr,
            Slug = dto.Slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var created = await _schoolRepository.AddAsync(entity, ct);

        var result = MapToDto(created);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SchoolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var school = await _schoolRepository.GetByIdAsync(id, ct);
        if (school == null)
            return NotFound();

        return Ok(MapToDto(school));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SchoolDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        page_size = Math.Clamp(page_size, 1, 100);

        var result = await _schoolRepository.ListAsync(page, page_size, ct);

        var dto = new PagedResult<SchoolDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            Total = result.Total,
            Page = result.Page,
            PageSize = result.PageSize,
        };

        return Ok(dto);
    }

    private static SchoolDto MapToDto(School school) => new()
    {
        Id = school.Id,
        Name = school.Name,
        NameAr = school.NameAr,
        Slug = school.Slug,
        IsActive = school.IsActive,
        CreatedAt = school.CreatedAt,
        UpdatedAt = school.UpdatedAt,
    };
}
