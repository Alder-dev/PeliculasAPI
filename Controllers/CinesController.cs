using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;

namespace PeliculasAPI.Controllers
{
    [Route("api/cines")]
    [ApiController]
    public class CinesController : CustomBaseController
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private const string cacheKey = "cinesCache";

        public CinesController(
            ApplicationDbContext context,
            IMapper mapper,
            IOutputCacheStore outputCacheStore)
            : base(context, mapper, outputCacheStore, cacheKey)
        {
            this.context = context;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
        }

        [HttpGet]
        [OutputCache(Tags = [cacheKey])]
        public async Task<List<CineDTO>> Get([FromQuery] PaginacionDTO paginacion)
        {
            return await Get<Cine, CineDTO>(paginacion, c => c.Nombre);
        }

        [HttpGet("{id:int}", Name = "ObtenerCinePorID")]
        [OutputCache(Tags = [cacheKey])]
        public async Task<ActionResult<CineDTO>> Get(int id)
        {
            return await Get<Cine, CineDTO>(id);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CineCreacionDTO cineCreacionDTO)
        {
            return await Post<CineCreacionDTO, Cine, CineDTO>(cineCreacionDTO, "ObtenerCinePorID");
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] CineCreacionDTO cineCreacionDTO)
        {
            return await Put<CineCreacionDTO, Cine>(id, cineCreacionDTO);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await Delete<Cine>(id);
        }
    }
}
