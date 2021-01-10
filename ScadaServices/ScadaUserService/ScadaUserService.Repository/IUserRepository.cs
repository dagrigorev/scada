using System;
using System.Threading.Tasks;
using Scada.Models;

namespace ScadaUserService.Repository
{
    /// <summary>
    /// Репозиторий для хранения пользователей.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Создает нового пользователя.
        /// </summary>
        /// <param name="user"></param>
        Task CreateUser(User user);
        
        /// <summary>
        /// Удаляет пользователя.
        /// </summary>
        /// <param name="user">Экземпляр пользоваетля.</param>
        /// <returns></returns>
        Task DeleteUser(User user);
        
        /// <summary>
        /// Ищет пользоваетля по имени для входа.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        Task<User> FindUserByUsername(string username);

        /// <summary>
        /// Обновляет данные о пользователе.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task UpdateUser(User user);
    }
}