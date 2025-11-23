using ERPAccounting.Common.Interfaces;

namespace ERPAccounting.Infrastructure.Services
{
    /// <summary>
    /// Implementacija servisa za dobijanje trenutnog korisnika.
    /// Za sada vraća default vrednosti dok ne implementiramo Auth sistem.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private const string DEFAULT_USERNAME = "API_DEFAULT_USER";

        public string Username => DEFAULT_USERNAME;

        public int? UserId => null;
    }
}