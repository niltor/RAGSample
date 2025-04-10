using System.ComponentModel.DataAnnotations;

namespace ApiService.Models.SLMDtos;

public class TextRequestDto
{
    [MaxLength(512)]
    public required string Text { get; set; }
}
