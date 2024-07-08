using CasinoML.Model;
using CasinoML.Model.Data;
using CasinoML.Model.Dto;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace CasinoML
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Data Load

            var roundsPlayedPerGameFilePath = Path.Combine(Environment.CurrentDirectory, "App_Data", "rounds_played_per_game.csv");
            var roundsPlayedPerGameFile = File.ReadAllLines(roundsPlayedPerGameFilePath);
            var roundsPlayedPerGame = new List<RoundsPlayedPerGame>();
            foreach (var row in roundsPlayedPerGameFile.Skip(1))
            {
                var columns = row.Split(',');
                var item = new RoundsPlayedPerGame
                {
                    GameId = int.Parse(columns[0]),
                    PlayerId = int.Parse(columns[1]),
                    Rounds = int.Parse(columns[2])
                };
                roundsPlayedPerGame.Add(item);
            }

            var gameFeatureFilePath = Path.Combine(Environment.CurrentDirectory, "App_Data", "game_features.csv");
            var gameFeaturesFile = File.ReadAllLines(gameFeatureFilePath);
            var gameFeature = new List<GameFeature>();
            foreach (var row in gameFeaturesFile.Skip(1))
            {
                var columns = row.Split(',');
                var item = new GameFeature
                {
                    Name = columns[0],
                    GameInternalId = int.Parse(columns[1])
                };
                gameFeature.Add(item);
            }

            var gameProviderCategoryFilePath = Path.Combine(Environment.CurrentDirectory, "App_Data", "game_provider_category.csv");
            var gameProviderCategoryFile = File.ReadAllLines(gameProviderCategoryFilePath);
            var gameProviderCategory = new List<GameProviderCategory>();
            foreach (var row in gameProviderCategoryFile.Skip(1))
            {
                var columns = row.Split(',');
                var item = new GameProviderCategory
                {
                    GameId = int.Parse(columns[0]),
                    Provider = columns[1],
                    Category = columns[2]
                };
                gameProviderCategory.Add(item);
            }

            var validationSetFilePath = Path.Combine(Environment.CurrentDirectory, "App_Data", "validation_set.csv");
            var validationSetFile = File.ReadAllLines(validationSetFilePath);
            var validationSet = new List<GameRecommendation>();
            foreach (var row in validationSetFile.Skip(1))
            {
                var columns = row.Split(',');
                var item = new GameRecommendation
                {
                    GameId = int.Parse(columns[0].Substring(0, columns[0].IndexOf('.'))),
                    PlayerId = int.Parse(columns[1])
                };
                validationSet.Add(item);
            }

            #endregion

            //get all attributes for all games
            var gameAttribute = new List<GameAttributeDto>();
            foreach (var r in gameProviderCategory)
            {
                if (!gameAttribute.Any(x => x.GameId == r.GameId))
                {
                    gameAttribute.Add(new GameAttributeDto
                    {
                        GameId = r.GameId,
                        Category = int.Parse(r.Category.Replace("Category ", "")),
                        Provider = int.Parse(r.Provider.Replace("Provider ", "")),
                        Feature = string.Join(",", gameFeature.Where(x => x.GameInternalId == r.GameId)
                                    .Select(x => int.Parse(x.Name.Replace("Feature ", ""))).ToList())
                    });
                }
            }


            //collaborative filtering uses players with similar taste to recommend new game to player 
            var mlContext = new MLContext();
            var roundsPlayedData = mlContext.Data.LoadFromEnumerable(roundsPlayedPerGame);

            var dataSplit = mlContext.Data.TrainTestSplit(roundsPlayedData, testFraction: 0.2);
            var trainingData = dataSplit.TrainSet;
            var testData = dataSplit.TestSet;

            IEstimator<ITransformer> estimator = mlContext.Transforms.Conversion
                .MapValueToKey("playerIdEncoded", nameof(RoundsPlayedPerGame.PlayerId))
                .Append(mlContext.Transforms.Conversion
                .MapValueToKey("gameIdEncoded", nameof(RoundsPlayedPerGame.GameId)));

            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "playerIdEncoded",
                MatrixRowIndexColumnName = "gameIdEncoded",
                LabelColumnName = nameof(RoundsPlayedPerGame.Rounds),
                NumberOfIterations = 20,
                ApproximationRank = 100
            };

            var trainerEstimator = estimator.Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));
            var model = trainerEstimator.Fit(trainingData);

            //var predictions = model.Transform(testData);
            //var metrics = mlContext.Regression.Evaluate(predictions, labelColumnName: nameof(RoundsPlayedPerGame.Rounds), scoreColumnName: "Score");
            //Console.WriteLine("Root Mean Squared Error : " + metrics.RootMeanSquaredError.ToString());
            //Console.WriteLine("RSquared: " + metrics.RSquared.ToString());

            var predictionEngine = mlContext.Model.CreatePredictionEngine<RoundsPlayedPerGame, GameRatingPrediction>(model);


            //content based filtering finds games with similar characteristics to reccomend a new game
            var gameFeaturesData = mlContext.Data.LoadFromEnumerable(gameAttribute);
            var gameFeaturesPipeline = mlContext.Transforms.Conversion.MapValueToKey("GameId")
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("ProviderEncoded", "Provider"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("CategoryEncoded", "Category"))
                .Append(mlContext.Transforms.Text.FeaturizeText("FeatureEncoded", "Feature"))
                .Append(mlContext.Transforms.Concatenate("Feature", "ProviderEncoded", "CategoryEncoded", "FeatureEncoded"));

            var contentBasedModel = gameFeaturesPipeline.Fit(gameFeaturesData);
            var contentBasedPredictionEngine = mlContext.Model.CreatePredictionEngine<GameAttributeDto, GameRatingPrediction>(contentBasedModel);


            var gameIds = gameAttribute.Select(r => r.GameId).Distinct().ToList();
            var userIds = roundsPlayedPerGame.Select(r => r.PlayerId).Distinct().ToList();

            foreach (var userId in userIds)
            {
                var collaborativeRecommendations = new List<Tuple<int, float>>();
                foreach (var gameId in gameIds)
                {
                    var predict = predictionEngine.Predict(new RoundsPlayedPerGame { PlayerId = userId, GameId = gameId });
                    collaborativeRecommendations.Add(new Tuple<int, float>(gameId, predict.Score));
                }
                collaborativeRecommendations = collaborativeRecommendations.OrderByDescending(r => r.Item2).ToList();


                var contentBasedRecommendations = new List<Tuple<int, float>>();
                foreach (var game in gameAttribute)
                {
                    var prediction = contentBasedPredictionEngine.Predict(new GameAttributeDto { GameId = game.GameId });
                    contentBasedRecommendations.Add(new Tuple<int, float>(game.GameId, prediction.Score));
                }

                var combinedRecommendations = collaborativeRecommendations.Concat(contentBasedRecommendations)
                        .GroupBy(r => r.Item1)
                        .Select(g => new Tuple<int, float>(g.Key, g.Average(r => r.Item2)))
                        .OrderByDescending(r => r.Item2)
                        .ToList();
            }
        }
    }
}