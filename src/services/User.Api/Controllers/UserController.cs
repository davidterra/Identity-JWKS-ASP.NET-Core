using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using User.Api.Models;

namespace User.Api.Controllers
{
    using System;
    using User.Api.Services;

    [ApiController]
    public class UserController : Controller
    {
        private readonly IAuthenticationService _authenticationService;
        public UserController(IAuthenticationService authenticationService) => _authenticationService = authenticationService;

        [HttpPost("signup")]
        public async Task<IActionResult> Signup(Signup user)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authenticationService.CreateNewAsync(user);

            if (result.Succeeded)
                return Ok(await _authenticationService.CreateJwtAsync(user.Email));

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLogin login)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authenticationService.AscendAsync(login);

            if (result)
            {
                return Ok(await _authenticationService.CreateJwtAsync(login.Email));
            }

            return BadRequest("Usuário ou senha incorretos.");

        }
        
        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken([FromBody] string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest("Refresh Token inválido.");

            var renewToken = await _authenticationService.RenewTokenAsync(Guid.Parse(refreshToken));

            if (renewToken == null)
                return BadRequest("Refresh Token expirado.");


            return Ok(renewToken);
        }




    }

}