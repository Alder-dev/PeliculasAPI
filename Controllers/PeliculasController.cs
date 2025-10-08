using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Servicios;
using PeliculasAPI.Utilidades;

namespace PeliculasAPI.Controllers
{
    [Route("api/peliculas")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PeliculasController : CustomBaseController
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly string contenedor = "peliculas";
        private const string cacheKey = "PeliculasCache";

        public PeliculasController(
            ApplicationDbContext context,
            IMapper mapper,
            IOutputCacheStore outputCacheStore,
            IAlmacenadorArchivos almacenadorArchivos,
            IServicioUsuarios servicioUsuarios
            )
            : base(context, mapper, outputCacheStore, cacheKey)
        {
            this.context = context;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
            this.almacenadorArchivos = almacenadorArchivos;
            this.servicioUsuarios = servicioUsuarios;
        }

        [HttpGet("landing")]
        [OutputCache(Tags = [cacheKey])]
        [AllowAnonymous]
        public async Task<ActionResult<LandingPageDTO>> Get()
        {
            var top = 6;
            var hoy = DateTime.Today;

            var proximosEstrenos = await context.Peliculas
                .Where(x => x.FechaLanzamiento > hoy)
                .OrderBy(hoy => hoy.FechaLanzamiento)
                .Take(top)
                .ProjectTo<PeliculaDTO>(mapper.ConfigurationProvider)
                .ToListAsync();

            var enCines = await context.Peliculas
                .Where(p => p.PeliculasCines.Select(pc => pc.PeliculaId).Contains(p.Id))
                .OrderBy(p => p.FechaLanzamiento)
                .Take(top)
                .ProjectTo<PeliculaDTO>(mapper.ConfigurationProvider)
                .ToListAsync();

            var resultado = new LandingPageDTO();
            resultado.EnCines = enCines;
            resultado.ProximosEstrenos = proximosEstrenos;

            return resultado;
        }

        [HttpGet("{id:int}", Name = "ObtenerPeliculaPorID")]
        //[OutputCache(Tags = [cacheKey])]
        [AllowAnonymous]
        public async Task<ActionResult<PeliculaDetallesDTO>> Get (int id)
        {
            var pelicula = await context.Peliculas
                .ProjectTo<PeliculaDetallesDTO>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pelicula is null)
            {
                return NotFound();
            }

            var promedioVoto = 0.0;
            var usuarioVoto = 0;

            if (await context.RatingsPeliculas.AnyAsync(r => r.PeliculaId == id))
            {
                promedioVoto = await context.RatingsPeliculas.Where(r => r.PeliculaId == id)
                    .AverageAsync(r => r.Puntuacion);

                if (HttpContext.User.Identity!.IsAuthenticated)
                {
                    var usuarioId = await servicioUsuarios.ObtenerUsuarioId();

                    var ratingDB = await context.RatingsPeliculas
                        .FirstOrDefaultAsync(r => r.UsuarioId == usuarioId && r.PeliculaId == id);

                    if (ratingDB is not null)
                    {
                        usuarioVoto = ratingDB.Puntuacion;
                    }
                }
            }

            pelicula.PromedioVoto = promedioVoto;
            pelicula.VotoUsuario = usuarioVoto;

            return pelicula;
        }

        [HttpGet("filtrar")]
        [AllowAnonymous]
        public async Task<ActionResult<List<PeliculaDTO>>> Filtrar([FromQuery] PeliculasFiltrarDTO peliculasFiltrarDTO)
        {
            var peliculasQueryable = context.Peliculas.AsQueryable();
            if (!string.IsNullOrWhiteSpace(peliculasFiltrarDTO.Titulo))
            {
                peliculasQueryable = peliculasQueryable.Where(p => p.Titulo.Contains(peliculasFiltrarDTO.Titulo));
            }
            if (peliculasFiltrarDTO.EnCines)
            {
                peliculasQueryable = peliculasQueryable.Where(p => p.PeliculasCines.Select(pc => pc.PeliculaId).Contains(p.Id));
            }
            if (peliculasFiltrarDTO.ProximosEstrenos)
            {
                var hoy = DateTime.Today;
                peliculasQueryable = peliculasQueryable.Where(p => p.FechaLanzamiento > hoy);
            }
            if (peliculasFiltrarDTO.GeneroId != 0)
            {
                peliculasQueryable = peliculasQueryable.Where(p => p.PeliculasGeneros.Select(pg => pg.GeneroId).Contains(peliculasFiltrarDTO.GeneroId));
            }
            await HttpContext.InsertarParametrosPaginacionEnCabecera(peliculasQueryable);
            var peliculas = await peliculasQueryable
                .Paginar(peliculasFiltrarDTO.Paginacion)
                .ProjectTo<PeliculaDTO>(mapper.ConfigurationProvider)
                .ToListAsync();
            return peliculas;
        }

        [HttpGet("postget")]
        public async Task<ActionResult<PeliculasPostGetDTO>> PostGet()
        {
            var cines = await context.Cines.ProjectTo<CineDTO>(mapper.ConfigurationProvider).ToListAsync();
            var generos = await context.Generos.ProjectTo<GeneroDTO>(mapper.ConfigurationProvider).ToListAsync();

            return new PeliculasPostGetDTO
            {
                Cines = cines,
                Generos = generos
            };
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] PeliculaCreacionDTO peliculaCreacionDTO)
        {
            var pelicula = mapper.Map<Pelicula>(peliculaCreacionDTO);

            if (peliculaCreacionDTO.Poster is not null)
            {
                var url = await almacenadorArchivos.Almacenar(contenedor, peliculaCreacionDTO.Poster);
                pelicula.Poster = url;
            }

            AsignarOrdenActores(pelicula);
            context.Add(pelicula);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cacheKey, default);
            var peliculaDTO = mapper.Map<PeliculaDTO>(pelicula);
            return CreatedAtRoute("ObtenerPeliculaPorID", new { id = pelicula.Id }, peliculaDTO);
        }

        [HttpGet("PutGet/{id:int}")]
        public async Task<ActionResult<PeliculasPutGetDTO>> PutGet(int id)
        {
            var pelicula = context.Peliculas
                .ProjectTo<PeliculaDetallesDTO>(mapper.ConfigurationProvider)
                .FirstOrDefault(p => p.Id == id);

            if (pelicula is null)
            {
                return NotFound();
            }

            var generosSelecionadosIds = pelicula.Generos.Select(g => g.Id).ToList();
            var generosNoSelecionados = await context.Generos
                .Where(g => !generosSelecionadosIds.Contains(g.Id))
                .ProjectTo<GeneroDTO>(mapper.ConfigurationProvider)
                .ToListAsync();

            var cinesSelecionadosIds = pelicula.Cines.Select(c => c.Id).ToList();
            var cinesNoSelecionados = await context.Cines
                .Where(c => !cinesSelecionadosIds.Contains(c.Id))
                .ProjectTo<CineDTO>(mapper.ConfigurationProvider)
                .ToListAsync();
            
            var respuesta = new PeliculasPutGetDTO();
            respuesta.Pelicula = pelicula;
            respuesta.GenerosSeleccionados = pelicula.Generos;
            respuesta.GenerosNoSeleccionados = generosNoSelecionados;
            respuesta.CinesSeleccionados = pelicula.Cines;
            respuesta.CinesNoSeleccionados = cinesNoSelecionados;
            respuesta.Actores = pelicula.Actores;

            return respuesta;
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromForm] PeliculaCreacionDTO peliculaCreacionDTO)
        {
            var pelicula = await context.Peliculas
                .Include(p => p.PeliculasActores)
                .Include(p => p.PeliculasGeneros)
                .Include(p => p.PeliculasCines)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pelicula is null)
            {
                return NotFound();
            }
            pelicula = mapper.Map(peliculaCreacionDTO, pelicula);

            if (peliculaCreacionDTO.Poster is not null)
            {
                pelicula.Poster = await almacenadorArchivos.Editar(pelicula.Poster, contenedor, peliculaCreacionDTO.Poster);
            }

            AsignarOrdenActores(pelicula);

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cacheKey, default);
            return NoContent();
        }

        [HttpDelete("{id:int}")]   
        public async Task<IActionResult> Delete(int id)
        {
            return await Delete<Pelicula>(id);
        }

        private void AsignarOrdenActores(Pelicula pelicula)
        {
            if (pelicula.PeliculasActores is not null)
            {
                for (int i = 0; i < pelicula.PeliculasActores.Count; i++)
                {
                    pelicula.PeliculasActores[i].Orden = i;
                }
            }
        }
    }
}
