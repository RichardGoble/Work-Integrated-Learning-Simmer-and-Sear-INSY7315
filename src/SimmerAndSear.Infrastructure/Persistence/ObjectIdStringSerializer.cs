using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace SimmerAndSear.Infrastructure.Persistence;
internal sealed class ObjectIdStringSerializer : SerializerBase<string>
{
    public override void Serialize(
        BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            context.Writer.WriteObjectId(ObjectId.GenerateNewId());
            return;
        }

        if (!ObjectId.TryParse(value, out var parsed))
        {
            throw new FormatException(
                $"'{value}' is not a valid identifier. Entity identifiers are ObjectIds; " +
                "a value from another source must be converted before it is stored.");
        }

        context.Writer.WriteObjectId(parsed);
    }

    public override string Deserialize(
        BsonDeserializationContext context, BsonDeserializationArgs args) =>
        context.Reader.CurrentBsonType switch
        {
            BsonType.ObjectId => context.Reader.ReadObjectId().ToString(),
            BsonType.String => context.Reader.ReadString(),

            _ => throw new FormatException(
                $"Cannot read an identifier from BSON type {context.Reader.CurrentBsonType}.")
        };
}
