using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// DependentCostLineItem entity - represents the distribution of costs to document line items.
/// Maps cost line items to specific document line items.
/// </summary>
public class DependentCostLineItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int DocumentCostLineItemId { get; set; }
    
    [Required]
    public int DocumentLineItemId { get; set; }
    
    [Column(TypeName = "decimal(19, 4)")]
    public decimal Amount { get; set; }
    
    // Navigation properties
    public virtual DocumentCostLineItem? DocumentCostLineItem { get; set; }
    public virtual DocumentLineItem? DocumentLineItem { get; set; }
}
