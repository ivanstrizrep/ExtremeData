namespace Casino.Model.Dto
{
    public class PlayerGameDto
    {
        public int PlayerId { get; set; }
        public List<AttributeRoundsCount> Provider = new List<AttributeRoundsCount> { };
        public List<AttributeRoundsCount> Category = new List<AttributeRoundsCount> { };
        public List<AttributeRoundsCount> Feature = new List<AttributeRoundsCount> { };
    }

    public class AttributeRoundsCount
    {
        public int Attribute { get; set; }
        public double Rounds { get; set; }
    }
}
