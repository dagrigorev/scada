using System.Threading.Tasks;
using Scada.Models;

namespace ScadaUserService.Repository
{
    /// <summary>
    /// Реализация репозиторий пользователей.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        public Task CreateUser(User user)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteUser(User user)
        {
            throw new System.NotImplementedException();
        }

        public Task<User> FindUserByUsername(string username)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateUser(User user)
        {
            throw new System.NotImplementedException();
        }
    }
}