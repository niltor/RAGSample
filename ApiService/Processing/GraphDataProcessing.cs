using ApiService.Models.ProcessingDtos;
using QuikGraph;

namespace ApiService.Processing;

/// <summary>
/// 图结构存储和处理关系数据
/// </summary>
public static class GraphDataProcessing
{
    private readonly static BidirectionalGraph<string, RelationEdge> Graph = new();

    public static void AddRelation(RelationDto relation)
    {
        if (!Graph.ContainsVertex(relation.Subject))
        {
            Graph.AddVertex(relation.Subject);
        }
        if (!Graph.ContainsVertex(relation.Target))
        {
            Graph.AddVertex(relation.Target);
        }
        Graph.AddEdge(new RelationEdge(relation.Subject, relation.Target, relation.Relation));
    }

    public static IEnumerable<RelationDto> QueryRelations(string subject, string target)
    {
        return Graph.Edges
            .Where(edge => edge.Source == subject || edge.Target == target)
            .Select(edge => new RelationDto
            {
                Subject = edge.Source,
                Relation = edge.Relation,
                Target = edge.Target
            });
    }
}
public class RelationEdge : Edge<string>
{
    public string Relation { get; }

    public RelationEdge(string source, string target, string relation)
        : base(source, target)
    {
        Relation = relation;
    }
}
