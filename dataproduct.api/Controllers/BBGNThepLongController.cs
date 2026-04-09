using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Services;
using dataproduct.api.ResponseModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BBGNThepLongController : ControllerBase
    {
        private readonly BBGN_ThepLongService _service;

        public BBGNThepLongController(
            BBGN_ThepLongService service)
        {
            _service = service;
        }

        
        // [HttpGet("{id}")]
        // public async Task<IActionResult> GetById(int id)
        // {
        //     try
        //     {
        //         var result = await _service.GetByIdAsync(id);
        //         return result == null ? NotFound() : Ok(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //     }
        // }

        

        // [HttpGet("search")]
        // public async Task<IActionResult> Search(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, string? searchText, int page = 1, int pageSize = 10)
        // {
        //     if (page < 1) page = 1;
        //     if (pageSize < 1) pageSize = 10;
        //     if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 records mỗi trang

        //     try
        //     {
        //         var result = await _service.SearchWithPagingAsync(
        //             NgaySX,
        //             Ca,
        //             LoaiBM,
        //             Scope,
        //             searchText,
        //             page,
        //             pageSize
        //         );

        //         return Ok(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //     }
        // }

        [HttpPost("fetch")]
        public async Task<IActionResult> Fetch([FromBody] FetchMeThoiRequest request)
        {
            var result = await _service.FetchMeThoiAsync(request);
            return Ok(result);
        }

        [HttpPost("load")]
        public async Task<IActionResult> Load([FromBody] LoadBBGNThepLongRequest request)
        {
            var result = await _service.LoadAsync(request);
            return Ok(result);
        }
    }
}
