using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeliculasAPI.DTOs;

namespace PeliculasAPI.Controllers
{
    [ApiController]
    [Route("api/rating")]
    public class RatingsController: ControllerBase
    {
        private readonly ApplicationDbContext context;

        public RatingsController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Post([FromBody] RatingCreacionDTO ratingCreacionDTO)
        {

        }
    }
}
