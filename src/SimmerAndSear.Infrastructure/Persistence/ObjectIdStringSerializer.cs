using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace SimmerAndSear.Infrastructure.Persistence;

/// <summary>
/// Maps a domain <see cref="string"/> identifier to a real BSON ObjectId.
///
/// <para>
/// The driver's own StringSerializer with an ObjectId representation throws a
/// FormatException on an unassigned identifier, because a new entity's Id is an
/// empty string until it is inserted. That is fine on the insert path - the id
/// generator fills it in first - but it turns any other serialisation of a fresh
/// entity into a confusing failure several frames deep in the driver.
/// </para>
///
/// <para>
/// This tolerates the unassigned case by writing a fresh ObjectId instead, so an
/// entity is always serialisable. Inserts still get their identifier written back
/// onto the object by the id generator, which is what lets a repository return it
/// to the caller.
/// </para>
/// </summary>
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

            // Tolerated so a document written by hand, or by an earlier build,
            // still loads rather than taking the whole collection down.
            BsonType.String => context.Reader.ReadString(),

            _ => throw new FormatException(
                $"Cannot read an identifier from BSON type {context.Reader.CurrentBsonType}.")
        };
}
