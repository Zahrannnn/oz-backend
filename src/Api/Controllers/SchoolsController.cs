using Microsoft.AspNetCore.Mvc;
using Oz.Api.DTOs;
using Oz.Domain.Entities;
using Oz.Domain.Repositories;

namespace Oz.Api.Controllers;

[ApiController]
[Route("api/v1/admin/[controller]")]
public class SchoolsController : ControllerBase
{
    private readonly IRepository<School> _repository;

    public SchoolsController(IRepository<School> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSchoolDto dto, CancellationToken ct)
    {
        var school = new School
        {
            Name = dto.Name,
            NameAr = dto.NameAr,
            Slug = dto.Slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(school, ct);

        var result = new SchoolDto(school.Id, school.Name, school.NameAr, school.Slug, school.IsActive, school.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = school.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _repository.ListAsync(page, page_size, ct);
        var dtos = result.Items.Select(s => new SchoolDto(s.Id, s.Name, s.NameAr, s.Slug, s.IsActive, s.CreatedAt)).ToList();
        return Ok(new { items = dtos, result.Total, result.Page, result.PageSize });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var school = await _repository.GetByIdAsync(id, ct);
        if (school is null) return NotFound();
        var dto = new SchoolDto(school.Id, school.Name, school.NameAr, school.Slug, school.IsActive, school.CreatedAt);
        return Ok(dto);
    }
}
