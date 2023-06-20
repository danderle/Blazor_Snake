using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbData.Data;

public class HighScoreModel
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Name { get; set; }
    public int Score { get; set; }
}
