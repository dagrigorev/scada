using System;

namespace ScadaUserService.Contracts
{
    /// <summary>
    /// Входной контракт для регистрации пользователей.
    /// </summary>
    public class UserRegistrationInContract
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}