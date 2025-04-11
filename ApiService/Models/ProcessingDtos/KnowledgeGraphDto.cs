namespace ApiService.Models.ProcessingDtos;

public class KnowledgeGraphDto
{
    public HashSet<string> HashSet { get; set; } = [];
    public List<RelationDto> RelationDtos { get; set; } = [];
}
