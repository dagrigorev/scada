using System.Threading.Tasks;
using ScadaUserService.Contracts;

namespace ScadaUserService
{
    /// <summary>
    /// Сервис для работы с пользователями.
    /// </summary>
    public interface IUserService
    {
        void Create(UserRegistrationInContract registrationInContract);
        Task<UserAuthenticateOutContract> Authenticate(UserAuthenticateInContract authenticateInContract);
        Task Update(UserUpdateInContract updateInContract);
    }
}