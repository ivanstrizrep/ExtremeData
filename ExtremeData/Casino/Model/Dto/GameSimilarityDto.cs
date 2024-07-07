namespace Casino.Model.Dto
{
    public class GameSimilarityDto
    {
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public double Similarity { get; set; }
    }
}
