using MongoDbAccess.Data;
using MongoDbAccess.DbAccess;

namespace HighScoreApi
{
    public static class Api
    {
        public static void ConfigureApi(this WebApplication app)
        {
            app.MapGet("/HighScores", GetHighScores);
            app.MapPost("/HighScores", CreateHighScore);
        }

        private static async Task<IResult> GetHighScores(IDataAccess data)
        {
            try
            {
                return Results.Ok(await data.GetHighScoresAsync());
            }
            catch(Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        private static async Task<IResult> CreateHighScore(HighScoreModel highScore, IDataAccess data)
        {
            try
            {
                await data.CreateHighScoreAsync(highScore);
                return Results.Ok();
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}
