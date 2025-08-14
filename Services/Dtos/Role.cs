using System.Runtime.Serialization;

namespace Financial_management_backend.Services.Dtos
{
    [DataContract]
    public enum ERole
    {
        [EnumMember(Value = "Admin")]
        Admin,
        [EnumMember(Value = "Accountant")]
        Accountant,
        [EnumMember(Value = "StockManager")]
        StockManager,        
        [EnumMember(Value = "ExpenseManager")]
        ExpenseManager        


    }
}