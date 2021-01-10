using System;

namespace ScadaUserService.Contracts
{
    /// <summary>
    /// Входной контракт для аутентификации пользователей.
    /// </summary>
    public class UserAuthenticateInContract 
    {
        public UserAuthenticateInContract(Guid id, string username)
        {
            Id = id;
            Username = username;
        }

        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}