using System.ComponentModel.DataAnnotations;

namespace Domain.Common;

public class BaseEntity
{
    [Key]
    public Guid Id { get; set; }
}
