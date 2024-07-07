namespace Casino.Model.Dto
{
    public class GameAttributeDto
    {
        public int GameId { get; set; }
        public int Provider { get; set; }
        public int Category { get; set; }
        public List<int> Feature = new List<int> { };
    }
}
