using MongoDbData.Data;

namespace HighScoresApiClient;

public class HighScoresRequests
{
    public const string Url = "/HighScores";

    public static async Task<List<HighScoreModel>> LoadHighScores()
    {
        using(HttpResponseMessage response = await ApiHelper.Client.GetAsync(Url))
        {
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadAsAsync<List<HighScoreModel>>();
                return list;
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
        }
    }

    public static async Task<bool> SaveHighScore(HighScoreModel highscore)
    {
        using (HttpResponseMessage response = await ApiHelper.Client.PostAsJsonAsync(Url, highscore))
        {
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
        }
    }
}
