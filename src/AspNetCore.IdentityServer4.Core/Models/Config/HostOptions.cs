namespace AspNetCore.IdentityServer4.Core.Models.Config
{
    /// <summary>
    /// Host options
    /// </summary>
    public class HostOptions
    {
        /// <summary>
        /// Auth server
        /// </summary>
        public string AuthServer { get; set; }

        /// <summary>
        /// Redis's host (For example, "jb.com:6379" or "35.123.45.123:6379")
        /// </summary>
        public string Redis { get; set; }
    }
}