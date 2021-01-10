using System;

namespace Scada.Models
{
    /// <summary>
    /// Модель пользователя.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Имя.
        /// </summary>
        public string FirstName { get; set; }
        
        /// <summary>
        /// Фамилия.
        /// </summary>
        public string SecondName { get; set; }
        
        /// <summary>
        /// Имя для входа.
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Пароль.
        /// </summary>
        public string Password { get; set; }
        
        /// <summary>
        /// Токен для работы в авторизованной среде.
        /// </summary>
        public string Token { get; set; }
    }
}