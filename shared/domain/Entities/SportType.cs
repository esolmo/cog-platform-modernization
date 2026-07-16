namespace Cog.Domain.Entities;

public class SportType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<Game> Games { get; set; } = new List<Game>();
}
