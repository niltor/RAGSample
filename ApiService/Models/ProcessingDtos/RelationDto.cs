namespace ApiService.Models.ProcessingDtos;

public record RelationDto
{
    public required string Subject { get; set; }
    public required string Relation { get; set; }
    public required string Target { get; set; }
}
