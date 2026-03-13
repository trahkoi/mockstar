namespace Mockstar.Web.Persistence.Entities;

public class EventEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<HeatEntity> Heats { get; set; } = new List<HeatEntity>();
}
