using AutoMapper;
using AutoMapper.QueryableExtensions;
using PeliculasAPI.DTOs;
using PeliculasAPI.Utilidades;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using PeliculasAPI.Entidades;
using Microsoft.AspNetCore.OutputCaching;

namespace PeliculasAPI.Controllers
{
    public class CustomBaseController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private readonly string cacheKey;

        public CustomBaseController(
            ApplicationDbContext context,
            IMapper mapper,
            IOutputCacheStore outputCacheStore,
            string cacheKey)
        {
            this.context = context;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
            this.cacheKey = cacheKey;
        }

        protected async Task<List<TDTO>> Get<TEntidad, TDTO>(PaginacionDTO paginacion,
            Expression<Func<TEntidad, object>> ordenarpor) where TEntidad : class
        {
            var queryable = context.Set<TEntidad>().AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);
            return await queryable
                .OrderBy(ordenarpor)
                .Paginar(paginacion)
                .ProjectTo<TDTO>(mapper.ConfigurationProvider).ToListAsync();
        }
        protected async Task<List<TDTO>> Get<TEntidad, TDTO>(
            Expression<Func<TEntidad, object>> ordenarpor) where TEntidad : class
        {
            return await context.Set<TEntidad>()
                .OrderBy(ordenarpor)
                .ProjectTo<TDTO>(mapper.ConfigurationProvider).ToListAsync();
        }

        protected async Task<ActionResult<TDTO>> Get<TEntidad, TDTO>(int id) where TEntidad : class, IId where TDTO : IId
        {
            var entidad = await context.Set<TEntidad>()
                .ProjectTo<TDTO>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entidad is null)
            {
                return NotFound();
            }

            return entidad;
        }

        protected async Task<IActionResult> Post<TCreacionDTO, TEntidad, TDTO>(
            TCreacionDTO creacionDTO, string nombreRuta)
            where TEntidad : class, IId
            where TDTO : IId
        {
            var entidad = mapper.Map<TEntidad>(creacionDTO);
            context.Add(entidad);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cacheKey, default);

            var entidadDTO = mapper.Map<TDTO>(entidad);
            return CreatedAtRoute(nombreRuta, new { id = entidad.Id }, entidadDTO);
        }

        protected async Task<IActionResult> Put<TCreacionDTO, TEntidad>(
            int id, TCreacionDTO creacionDTO)
            where TEntidad : class, IId
        {
            var entidadExiste = await context.Set<TEntidad>().AnyAsync(g => g.Id == id);

            if (!entidadExiste)
            {
                return NotFound();
            }

            var entidad = mapper.Map<TEntidad>(creacionDTO);
            entidad.Id = id;

            context.Update(entidad);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cacheKey, default);

            return NoContent();
        }

        protected async Task<IActionResult> Delete<TEntidad>(int id)
            where TEntidad : class, IId
        {
            var registrosEliminados = await context.Set<TEntidad>()
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync();

            if (registrosEliminados == 0)
            {
                return NotFound();
            }

            await outputCacheStore.EvictByTagAsync(cacheKey, default);
            return NoContent();
        }
    }
}
