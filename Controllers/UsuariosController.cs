using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PeliculasAPI.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PeliculasAPI.Controllers
{
    [Route("api/usuarios")]
    [ApiController]
    public class UsuariosController: ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IConfiguration configuration;

        public UsuariosController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
        }

        [HttpPost("registrar")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = new IdentityUser
            {
                Email = credencialesUsuarioDTO.Email,
                UserName = credencialesUsuarioDTO.Email
            };

            var resultado = await userManager.CreateAsync(usuario, credencialesUsuarioDTO.Password);

            if (resultado.Succeeded)
            {
                return await ConstriurToken(usuario);
            }
            else
            {
                return BadRequest(resultado.Errors);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);

            if (usuario is null)
            {
                var errores = ConstruirLoginIncorrecto();
                return BadRequest(errores);
            }

            var resultado = await signInManager.CheckPasswordSignInAsync(usuario, credencialesUsuarioDTO.Password, lockoutOnFailure: false);

            if (resultado.Succeeded)
            {
                return await ConstriurToken(usuario);
            }
            else
            {
                var errores = ConstruirLoginIncorrecto();
                return BadRequest(errores);
            }
        }

        private IEnumerable<IdentityError> ConstruirLoginIncorrecto()
        {
            var identityError = new IdentityError() { Description = "Login Incorrecto" };
            var errores = new List<IdentityError>();
            errores.Add(identityError);
            return errores;
        }

        private async Task<RespuestaAutenticacionDTO> ConstriurToken(IdentityUser identityUser)
        {
            var claims = new List<Claim>
            {
                new Claim("email", identityUser.Email!),
                new Claim("yo", "alder")
            };

            var claimsDB = await userManager.GetClaimsAsync(identityUser);

            claims.AddRange(claimsDB);

            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["llavejwt"]!));
            var creds = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

            var expiracion = DateTime.UtcNow.AddYears(1);

            var tokenDeSeguridad = new JwtSecurityToken(issuer: null, audience: null, claims: claims, expires: expiracion, signingCredentials: creds);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            return new RespuestaAutenticacionDTO { Token = token, Expiracion = expiracion };
        }
    }
}
