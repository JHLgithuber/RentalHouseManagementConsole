using System;

namespace RentalHousingManagementConsole.Models;

/// <summary>
/// 주민정보 (Resident_data)
/// </summary>
public class ResidentData
{
    public string ResidentId { get; set; } = string.Empty; // 주민관리번호 (UUID, PK)
    public string? ContractId { get; set; } // 계약관리번호 (FK - ContractData.ContractId)
    public string? Name { get; set; } // 성명
    public string? FamilyRelationship { get; set; } // 관계
    public string? PhoneNumber { get; set; } // 전화번호
    public string? Language { get; set; } // 언어
    public bool? ResidencyStatus { get; set; } // 거주여부
    public bool? ApprovalStatus { get; set; } // 승인상태
}
