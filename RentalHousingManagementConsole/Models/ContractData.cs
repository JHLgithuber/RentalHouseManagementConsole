using System;

namespace RentalHousingManagementConsole.Models;

/// <summary>
/// 계약정보 (Contract_data)
/// </summary>
public class ContractData
{
    public string ContractId { get; set; } = string.Empty; // 계약관리번호 (UUID, PK)
    public string? UnitId { get; set; } // 세대번호 (FK - HouseinfoData.UnitId)
    public string? TenantName { get; set; } // 임차인 성명
    public string? PersonalId { get; set; } // 주민번호
    public string? Address { get; set; } // 주소
    public string? PhoneNumber { get; set; } // 전화번호
    public string? AccountNumber { get; set; } // 계좌번호
    public string? Language { get; set; } // 언어
    public DateTime? ContractStartDate { get; set; } // 계약 시작일
    public DateTime? MoveInDate { get; set; } // 입실일
    public DateTime? ContractEndDate { get; set; } // 계약 종료일
    public DateTime? MoveOutDate { get; set; } // 퇴실일
    public int? ContractRent { get; set; } // 계약 임대료
    public int? ContractManagementFee { get; set; } // 계약 관리비
    public int? ContractDeposit { get; set; } // 계약 보증금
    public int? DownPayment { get; set; } // 계약금
    public int? BalancePayment { get; set; } // 잔금
    public string? SpecialTerms { get; set; } // 특약사항
    public string? ContractFile { get; set; } // 계약서 사본 (파일참조식별자)
    public string? ContractRemarks { get; set; } // 계약비고
    public string? MoveOutReturnAccount { get; set; } // 퇴실 반환 계좌번호
    public int? MoveOutDeductionAmount { get; set; } // 퇴실 반환 공제액
    public string? MoveOutDeductionDetails { get; set; } // 퇴실 반환 공제내역 (CSV - 내역, 금액)
    public string? MoveOutConfirmationFile { get; set; } // 퇴실확인서 사본 (파일참조식별자)
    public string? MoveOutRemarks { get; set; } // 퇴실비고
}
