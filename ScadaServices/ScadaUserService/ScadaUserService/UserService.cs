using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scada.Models;
using ScadaUserService.Contracts;
using ScadaUserService.Repository;

namespace ScadaUserService
{
    /// <summary>
    /// Сервис для работы с пользователями SCADA.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ILogger _logger;
        private readonly UserServiceConfiguration _options;
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        
        public UserService(ILogger<UserService> logger, 
            IOptions<UserServiceConfiguration> options, 
            IUserRepository repository, IMapper mapper)
        {
            _logger = logger;
            _options = options.Value;
            _repository = repository;
            _mapper = mapper;
        }

        public void Create(UserRegistrationInContract registrationInContract)
        {
            try
            {
                var user = _mapper.Map<User>(registrationInContract);
                user.Id = Guid.NewGuid();
                user.Token = CreateJwtToken(user.Id);
                
                _repository.CreateUser(user);
            }
            catch (NotImplementedException ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
            }
        }

        public async Task<UserAuthenticateOutContract> Authenticate(UserAuthenticateInContract authenticateInContract)
        {
            try
            {
                var user = await _repository.FindUserByUsername(authenticateInContract.Username);
                if (user == null)
                    return null;

                user.Token = CreateJwtToken(user.Id);

                await _repository.UpdateUser(user);

                return _mapper.Map<UserAuthenticateOutContract>(user);
            }
            catch (NotImplementedException ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
            }

            return null;
        }

        public async Task Update(UserUpdateInContract updateInContract)
        {
            var user = await _repository.FindUserByUsername(updateInContract.Username);
            if (user == null)
                throw new InvalidOperationException("User not found");

            await _repository.UpdateUser(user);
        }

        private string CreateJwtToken(Guid userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_options.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, userId.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}