using MongoDB.Driver;
using MongoDbData.Data;

namespace MongoDbAccess.DbAccess;

public class DataAccess : IDataAccess
{
    private const string ConnectionString = "mongodb+srv://danderle86:bEI2egXHlO2ksBZ4@snakecluster.inqp1fa.mongodb.net/";
    private const string DatabaseName = "SnakeDb";
    private const string HighScoreCollection = "HighScores";

    private IMongoCollection<T> ConnectToMongo<T>(in string collection)
    {
        var client = new MongoClient(ConnectionString);
        var db = client.GetDatabase(DatabaseName);
        return db.GetCollection<T>(collection);
    }

    public async Task<List<HighScoreModel>> GetHighScoresAsync()
    {
        var scoreCollection = ConnectToMongo<HighScoreModel>(HighScoreCollection);
        var results = await scoreCollection.FindAsync(item => true);
        return results.ToList();
    }

    public Task CreateHighScoreAsync(HighScoreModel highScore)
    {
        var scoreCollection = ConnectToMongo<HighScoreModel>(HighScoreCollection);
        return scoreCollection.InsertOneAsync(highScore);
    }
}
