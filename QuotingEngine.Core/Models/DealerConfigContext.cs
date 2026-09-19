using System.Text.Json.Serialization;

namespace QuotingEngine.Core.Models;

[JsonSerializable(typeof(DealerConfig))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class DealerConfigContext : JsonSerializerContext
{
}
