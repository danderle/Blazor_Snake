using MongoDbAccess.Data;

namespace MongoDbAccess.DbAccess
{
    public interface IDataAccess
    {
        Task CreateHighScoreAsync(HighScoreModel highScore);
        Task<List<HighScoreModel>> GetHighScoresAsync();
    }
}