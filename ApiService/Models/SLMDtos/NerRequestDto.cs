using System.ComponentModel.DataAnnotations;

namespace ApiService.Models.SLMDtos;

public class NerRequestDto
{
    [MaxLength(512)]
    public required string Text { get; set; }
}
