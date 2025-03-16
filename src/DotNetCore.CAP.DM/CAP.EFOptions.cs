
namespace DotNetCore.CAP
{
    public  class EFOptions
    {
        public const string DefaultSchema = "cap";

        /// <summary>
        /// Gets or sets the schema to use when creating database objects.
        /// Default is <see cref="DefaultSchema" />.
        /// </summary>
        public string Schema { get; set; } = DefaultSchema;

        /// <summary>
        /// EF db context type.
        /// </summary>
        internal Type? DbContextType { get; set; }

        /// <summary>
        /// Data version
        /// </summary>
        internal string Version { get; set; } = default!;

    }
}
