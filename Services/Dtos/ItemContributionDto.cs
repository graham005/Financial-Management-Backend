using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Financial_management_backend.Services.Dtos
{
    // Main DTO for recording a contribution
    public class ItemContributionDto
    {
        [Required]
        public Guid StudentId { get; set; }
        
        [Required]
        public DateTime DateReceived { get; set; }
        
        [Required]
        public List<ItemAllocationDto> ItemAllocations { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Term { get; set; }
        
        [Required]
        public int Year { get; set; }
    }
    
    // Individual item allocation 
    public class ItemAllocationDto
    {
        [Required]
        public Guid RequiredItemId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }
        
        [Required]
        public bool IsMonetaryContribution { get; set; }
    }
    
    // DTO for available item requirements
    public class AvailableItemRequirementDto
    {
        public Guid RequiredItemId { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public decimal ExpectedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal PendingQuantity { get; set; }
        public decimal ApproximateValue { get; set; }
        public decimal MonetaryEquivalent { get; set; }
        public string Term { get; set; }
        public int Year { get; set; }
        public bool IsPastDue { get; set; }
    }
    
    // Collection of available requirements
    public class AvailableItemRequirementsDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public List<AvailableItemRequirementDto> PendingItems { get; set; }
        public int TotalPendingItems { get; set; }
        public bool HasPendingRequirements { get; set; }
    }
}