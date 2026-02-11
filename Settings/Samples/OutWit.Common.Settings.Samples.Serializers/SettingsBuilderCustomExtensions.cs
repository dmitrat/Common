using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Samples.Serializers.Serialization;

namespace OutWit.Common.Settings.Samples.Serializers
{
    /// <summary>
    /// Extension methods for registering custom domain type serializers.
    /// </summary>
    public static class SettingsBuilderCustomExtensions
    {
        /// <summary>
        /// Registers custom serializers for <c>BoundedInt</c> and <c>ColorRgb</c> types.
        /// </summary>
        /// <param name="builder">The settings builder.</param>
        /// <returns>The builder for chaining.</returns>
        public static SettingsBuilder AddCustomSerializers(this SettingsBuilder builder)
        {
            return builder
                .AddSerializer(new SettingsSerializerBoundedInt())
                .AddSerializer(new SettingsSerializerColorRgb());
        }
    }
}
