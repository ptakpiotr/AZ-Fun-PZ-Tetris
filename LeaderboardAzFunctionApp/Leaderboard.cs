using LeaderboardAzFunctionApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderboardAzFunctionApp;

public static class Leaderboard
{
    [FunctionName("AddScore")]
    public static async Task<IActionResult> AddScore(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger logger)
    {
        MongoClientSettings settings = MongoClientSettings
                                           .FromConnectionString(Environment
                                                                 .GetEnvironmentVariable("MONGO_CONN_STRING"));

        settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        MongoClient client = new(settings);

        IMongoDatabase db = client.GetDatabase("score_data");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        ScoreModel data = JsonConvert.DeserializeObject<ScoreModel>(requestBody);

        try
        {
            await db.GetCollection<ScoreModel>("scores").InsertOneAsync(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);

            return new ObjectResult(new ResultModel(ex.Message))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        return new ObjectResult(new ResultModel("Succesfully added new score"))
        {
            StatusCode = StatusCodes.Status201Created
        };
    }

    [FunctionName("GetScores")]
    public static async Task<IActionResult> GetScores(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
    {
        string skipQuery = req.Query["start"];
        string limitQuery = req.Query["limit"];

        if (string.IsNullOrEmpty(skipQuery) || string.IsNullOrEmpty(limitQuery))
        {
            return new ObjectResult(new ResultModel("Invalid limit boundaries supplied"))
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        int.TryParse(skipQuery, out int skip);
        int.TryParse(limitQuery, out int limit);


        MongoClientSettings settings = MongoClientSettings
                                           .FromConnectionString(Environment
                                                                 .GetEnvironmentVariable("MONGO_CONN_STRING"));

        settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        MongoClient client = new(settings);

        IMongoDatabase db = client.GetDatabase("score_data");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        ScoreModel data = JsonConvert.DeserializeObject<ScoreModel>(requestBody);

        IAsyncCursor<ScoreModel> scores = await db.GetCollection<ScoreModel>("scores").FindAsync(_ => true);

        IList<ScoreModel> res = await scores.ToListAsync();

        return new OkObjectResult(new ResultModel()
        {
            Message = (JsonConvert.SerializeObject(res.Skip(skip).Take(limit).ToList()))
        });
    }
}
