using System;

namespace ScadaUserService.Contracts
{
    /// <summary>
    /// Выходной контракт для выдачи результата аутентификации пользователя.
    /// </summary>
    public class UserAuthenticateOutContract
    {
        public Guid Id { get; set; }
        public string Token { get; set; }
    }
}