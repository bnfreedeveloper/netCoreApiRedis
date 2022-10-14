using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using netCoreApiRedis.Data;
using netCoreApiRedis.Models;
using netCoreApiRedis.Services;

namespace netCoreApiRedis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly ILogger<DriverController> _logger;
        private readonly ICacheService _cacheService;
        private readonly AppDbContext _context;
        public DriverController(ILogger<DriverController> logger, ICacheService cacheService,
        AppDbContext dbContext)
        {
            _logger = logger;
            _cacheService = cacheService;
            _context = dbContext;
        }

        [HttpGet("drivers")]
        public async Task<ActionResult<IEnumerable<Driver>>> GetAll()
        {

            //WE CHECK CACHE DATA
            var cacheData = await _cacheService.GetDataAsync<IEnumerable<Driver>>("drivers");
            if (cacheData != null && cacheData.Count() > 0)
            {
                return Ok(cacheData);
            }
            cacheData = await _context.Drivers.ToListAsync();

            //WE SET THE EXPIRY TIME FOR THE CACHE
            var expiryTime = DateTimeOffset.Now.AddSeconds(40);
            await _cacheService.SetDataAsync("drivers", cacheData, expiryTime);
            return Ok(cacheData);
        }
        [HttpPost("AddDriver")]
        public async Task<IActionResult> Add(Driver driver)
        {
            var added = await _context.AddAsync(driver);
            var result = await _context.SaveChangesAsync();
            if (result != 0)
            {
                var expireTime = DateTimeOffset.Now.AddSeconds(40);
                await _cacheService.SetDataAsync("driver" + driver.Id, driver, expireTime);
                return Ok(driver);
            }
            return BadRequest(new
            {
                error = "could save the driver"
            });

        }

        [HttpDelete("deleteDriver/{id:int}")]

        public async Task<IActionResult> Delete(int id)
        {
            var toDelete = await _context.Drivers.FindAsync(id);
            if (toDelete != null)
            {
                _context.Drivers.Remove(toDelete);
                await _context.SaveChangesAsync();
                await _cacheService.RemoveDataAsync("driver" + toDelete.Id);
                return NoContent();
            }
            return BadRequest(new
            {
                error = "couldn't delete the driver for this id"
            });
        }
        [HttpGet("driver/{id:int}")]
        public async Task<ActionResult<Driver>> GetById(int id)
        {
            var fromCache = await _cacheService.GetDataAsync<Driver>("driver" + id);
            if (fromCache != null)
            {
                return Ok(fromCache);
            }
            var fromDatabase = await _context.Drivers.FindAsync(id);
            if (fromDatabase != null)
            {
                var expirationTime = DateTimeOffset.Now.AddSeconds(45);
                await _cacheService.SetDataAsync("driver" + fromDatabase.Id, fromDatabase, expirationTime);
                return Ok(fromDatabase);
            }
            return BadRequest(new
            {
                error = "no driver at this id"
            });
        }
    }
}