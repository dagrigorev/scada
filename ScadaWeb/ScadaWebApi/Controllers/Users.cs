using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ScadaUserService;
using ScadaUserService.Contracts;

namespace Scada.Web.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }
        
        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody]UserAuthenticateInContract model)
        {
            var user = await _userService.Authenticate(model);

            if (user == null)
                return BadRequest(new { message = "Username or password is incorrect" });
            
            // return basic user info and authentication token
            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody]UserRegistrationInContract registrationInContract)
        {
            try
            {
                // create user
                _userService.Create(registrationInContract);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            /*var user = _userService.GetById(id);
            var model = _mapper.Map<UserModel>(user);
            return Ok(model);*/
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody]UserUpdateInContract updateInContract)
        {
            try
            {
                // update user 
                await _userService.Update(updateInContract);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            //_userService.Delete(id);
            return Ok();
        }
    }
}