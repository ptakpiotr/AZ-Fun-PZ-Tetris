using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LeaderboardAzFunctionApp.Models;

public class ScoreModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("name")]
    public string Nick { get; set; }

    public int Score { get; set; }
}
