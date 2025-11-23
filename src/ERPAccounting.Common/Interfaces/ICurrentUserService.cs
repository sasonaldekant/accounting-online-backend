namespace ERPAccounting.Common.Interfaces
{
    /// <summary>
    /// Servis za dobijanje informacija o trenutnom korisniku.
    /// Za sada vraća default korisnika, kasnije će biti povezan sa JWT/Auth sistemom.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Korisničko ime trenutno ulogovanog korisnika.
        /// Default: "API_DEFAULT_USER"
        /// </summary>
        string Username { get; }

        /// <summary>
        /// ID trenutno ulogovanog korisnika (optional).
        /// Default: null
        /// </summary>
        int? UserId { get; }
    }
}