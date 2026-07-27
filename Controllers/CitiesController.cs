using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NomadCN.Api.Models;
using NomadCN.Api.Services;

namespace NomadCN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;

    public CitiesController(ICityService cityService)
    {
        _cityService = cityService;
    }

    /// <summary>获取所有城市（Redis 缓存）</summary>
    [HttpGet]
    public async Task<ApiResponse<List<City>>> GetAll()
    {
        var cities = await _cityService.GetAllAsync();
        return ApiResponse<List<City>>.Success(cities);
    }

    /// <summary>按城市名获取详情</summary>
    [HttpGet("{name}")]
    public async Task<ApiResponse<City>> GetByName(string name)
    {
        var city = await _cityService.GetByNameAsync(name);
        if (city == null) return ApiResponse<City>.Fail("城市不存在");
        return ApiResponse<City>.Success(city);
    }

    /// <summary>搜索城市</summary>
    [HttpGet("search/{keyword}")]
    public async Task<ApiResponse<List<City>>> Search(string keyword)
    {
        var cities = await _cityService.SearchAsync(keyword);
        return ApiResponse<List<City>>.Success(cities);
    }
}
