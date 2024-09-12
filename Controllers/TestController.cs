using Microsoft.AspNetCore.Mvc;
using TodoList_back.Data;

namespace TodoList_back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

  
        public TestController(ApplicationDbContext context, ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("testRoute")]
        public string TestRoute()
        {
            return "ok";
        }
    }
}