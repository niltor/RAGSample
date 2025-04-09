namespace ApiService.Models.ProcessingDtos;

public class NerResultDto
{
    /// <summary>
    /// 技术名词
    /// </summary>
    public string[]? TechNoun { get; set; }
    /// <summary>
    /// 专有名词
    /// </summary>
    public string[]? ProperNoun { get; set; }

    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; }
}
