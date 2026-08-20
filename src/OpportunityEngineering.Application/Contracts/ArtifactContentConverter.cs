using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace OpportunityEngineering.Application.Contracts;

// System.Text.Json resolves IArtifactContent polymorphism via [JsonPolymorphic]/[JsonDerivedType]
// on the API response path. Cosmos DB's default serializer is Newtonsoft.Json, which does not
// understand those attributes, so storage reads throw "Type is an interface... cannot be
// instantiated." This converter gives Newtonsoft the same discriminator mapping.
internal sealed class ArtifactContentConverter : JsonConverter<IArtifactContent>
{
    // Newtonsoft resolves [JsonConverter] attributes by walking implemented interfaces, so the
    // attribute on IArtifactContent also matches every concrete record. Serializing a concrete
    // value's own properties through the normal serializer therefore re-enters this converter and
    // Newtonsoft reports "Self referencing loop detected". This resolver skips the converter
    // lookup for IArtifactContent so the inner (de)serialization sees each record's real contract.
    private sealed class SkipArtifactContentConverterResolver : DefaultContractResolver
    {
        protected override JsonConverter? ResolveContractConverter(Type objectType) =>
            typeof(IArtifactContent).IsAssignableFrom(objectType)
                ? null
                : base.ResolveContractConverter(objectType);
    }

    private static readonly JsonSerializer InnerSerializer =
        JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new SkipArtifactContentConverterResolver(),
        });

    public override void WriteJson(JsonWriter writer, IArtifactContent? value, JsonSerializer serializer)
    {
        var contentType = value switch
        {
            ArchitectureHandoffContent => "architectureHandoff",
            PilotBriefContent => "pilotBrief",
            DecisionRecordContent => "decisionRecord",
            ExecutiveSummaryContent => "executiveSummary",
            ExperimentDefinitionContent => "experimentDefinition",
            null => throw new JsonSerializationException("Artifact content is required."),
            _ => throw new JsonSerializationException(
                $"Unsupported artifact content type '{value.GetType()}'.")
        };
        var token = JObject.FromObject(value, InnerSerializer);
        token.AddFirst(new JProperty("contentType", contentType));
        token.WriteTo(writer);
    }

    public override IArtifactContent? ReadJson(
        JsonReader reader,
        Type objectType,
        IArtifactContent? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }
        var token = JObject.Load(reader);
        var targetType = token.Value<string>("contentType") switch
        {
            "architectureHandoff" => typeof(ArchitectureHandoffContent),
            "pilotBrief" => typeof(PilotBriefContent),
            "decisionRecord" => typeof(DecisionRecordContent),
            "executiveSummary" => typeof(ExecutiveSummaryContent),
            "experimentDefinition" => typeof(ExperimentDefinitionContent),
            var other => throw new JsonSerializationException(
                $"Unknown artifact content type '{other}'.")
        };
        return (IArtifactContent?)token.ToObject(targetType, InnerSerializer);
    }
}
