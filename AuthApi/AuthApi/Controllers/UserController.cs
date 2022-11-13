using AuthApi.DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize("Member")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext Context;
        public UserController(AppDbContext appDbContext)
        {
            Context = appDbContext;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllAsync() => Ok(await Context.Users.ToListAsync());
    }
}
