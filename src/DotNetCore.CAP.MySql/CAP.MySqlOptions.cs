// ReSharper disable once CheckNamespace

namespace DotNetCore.CAP
{
    public class MySqlOptions : EFOptions
    {
        /// <summary>
        /// Gets or sets the database's connection string that will be used to store database entities.
        /// </summary>
        public string ConnectionString { get; set; }

        public string TableNamePrefix { get; set; } = "cap";
    }
}