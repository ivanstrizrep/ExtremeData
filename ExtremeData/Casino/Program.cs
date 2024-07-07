using Casino.Model;
using Casino.Model.Data;
using Casino.Model.Dto;

namespace Casino
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

            #region Data Transform

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
                        Feature = gameFeature.Where(x => x.GameInternalId == r.GameId)
                                    .Select(x => int.Parse(x.Name.Replace("Feature ", ""))).ToList()
                    });
                }
            }

            //get players with number of rounds played per each provider, category and feature
            var playersGame = new List<PlayerGameDto>();
            foreach (var r in roundsPlayedPerGame)
            {
                var game = gameAttribute.Single(x => x.GameId == r.GameId);

                if (!playersGame.Any(x => x.PlayerId == r.PlayerId))
                {
                    var item = new PlayerGameDto { PlayerId = r.PlayerId };
                    item.Provider.Add(new AttributeRoundsCount { Attribute = game.Provider, Rounds = r.Rounds });
                    item.Category.Add(new AttributeRoundsCount { Attribute = game.Category, Rounds = r.Rounds });
                    foreach (var f in game.Feature)
                    {
                        item.Feature.Add(new AttributeRoundsCount { Attribute = f, Rounds = r.Rounds });
                    }
                    playersGame.Add(item);
                }
                else
                {
                    var item = playersGame.Single(x => x.PlayerId == r.PlayerId);
                    if (!item.Provider.Any(x => x.Attribute == game.Provider))
                    {
                        item.Provider.Add(new AttributeRoundsCount { Attribute = game.Provider, Rounds = r.Rounds });
                    }
                    else
                    {
                        item.Provider.Single(x => x.Attribute == game.Provider).Rounds += r.Rounds;
                    }
                    if (!item.Category.Any(x => x.Attribute == game.Category))
                    {
                        item.Category.Add(new AttributeRoundsCount { Attribute = game.Category, Rounds = r.Rounds });
                    }
                    else
                    {
                        item.Category.Single(x => x.Attribute == game.Category).Rounds += r.Rounds;
                    }
                    foreach (var f in game.Feature)
                    {
                        if (!item.Feature.Any(x => x.Attribute == f))
                        {
                            item.Feature.Add(new AttributeRoundsCount { Attribute = f, Rounds = r.Rounds });
                        }
                        else
                        {
                            item.Feature.Single(x => x.Attribute == f).Rounds += r.Rounds;
                        }
                    }
                }
            }

            //give significance to each players provider, category, feature by dividing number of rounds played on each
            //with maximum number of rounds played per provider, category, feature (between 0 and 1)
            foreach (var r in playersGame)
            {
                var maxRoundsProvider = r.Provider.Select(x => x.Rounds).Max();
                r.Provider.ForEach(x => x.Rounds = Math.Round(x.Rounds / maxRoundsProvider, 2));
                var maxRoundsCategory = r.Category.Select(x => x.Rounds).Max();
                r.Category.ForEach(x => x.Rounds = Math.Round(x.Rounds / maxRoundsCategory, 2));
                var maxRoundsFeature = r.Feature.Select(x => x.Rounds).Max();
                r.Feature.ForEach(x => x.Rounds = Math.Round(x.Rounds / maxRoundsFeature, 2));
            }

            #endregion

            #region Calculation

            //similarity factors can be adjusted as we decide what are the norms for recommending game to a player
            //here we assume player will like games of the same category, same provider and most similar
            //features as games he already played (category considered most important)
            //similarities between providers, categories and features are not considered as nothing is stated about it
            var providerSimilarityFactor = 0.1;
            var categorySimilarityFactor = 0.6;
            var featureSimilarityFactor = 0.3;

            //calculate likeability of each game for each player by summing calculated significance of each games
            //attribute multiplied by its significance factor
            var gameSimilarity = new List<GameSimilarityDto>();
            foreach (var player in playersGame)
            {
                foreach (var game in gameAttribute)
                {
                    var providerSignificance = player.Provider.Select(x => x.Attribute).Contains(game.Provider) ?
                        player.Provider.Single(x => x.Attribute == game.Provider).Rounds : 0;
                    var providerSimilarity = Math.Round(providerSimilarityFactor * providerSignificance, 2);

                    var categorySignificance = player.Category.Select(x => x.Attribute).Contains(game.Category) ?
                        player.Category.Single(x => x.Attribute == game.Category).Rounds : 0;
                    var categorySimilarity = Math.Round(categorySimilarityFactor * categorySignificance, 2);

                    var featuresSignificanceSum = 0.0;
                    foreach (var feature in game.Feature)
                    {
                        var featureSignificanceItem = player.Feature.Select(x => x.Attribute).Contains(feature) ?
                            player.Feature.Single(x => x.Attribute == feature).Rounds : 0;
                        featuresSignificanceSum += featureSignificanceItem;
                    }
                    var featureSignificance = Math.Round(featuresSignificanceSum / game.Feature.Count * 1d, 2);
                    var featureSimilarity = Math.Round(featureSimilarityFactor * featureSignificance, 2);

                    gameSimilarity.Add(new GameSimilarityDto
                    {
                        PlayerId = player.PlayerId,
                        GameId = game.GameId,
                        Similarity = Math.Round(providerSimilarity + categorySimilarity + featureSimilarity, 2)
                    });
                }
            }

            //adjustable - take given percent of most similar player games
            var recommendationPercent = 0.05;
            int recommendationIndex = (int)(gameSimilarity.Count * recommendationPercent);
            var recommendation = gameSimilarity.OrderByDescending(x => x.Similarity)
                .Take(recommendationIndex).Select(x => new GameRecommendation
                {
                    GameId = x.GameId,
                    PlayerId = x.PlayerId
                }).OrderBy(x => x.PlayerId).ToList();

            #endregion

            #region Validation

            //find how many recommendations are the same as in validation set
            var result = validationSet
                .Join(recommendation, l1 => l1.PlayerId, l2 => l2.PlayerId, (l1, l2) => new { l1, l2 })
                .Where(x => x.l1.GameId == x.l2.GameId)
                .Select(x => x.l1)
                .Distinct()
                .Count();

            #endregion
        }
    }
}