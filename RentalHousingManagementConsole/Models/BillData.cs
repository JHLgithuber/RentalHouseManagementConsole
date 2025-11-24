using System;

namespace RentalHousingManagementConsole.Models;

/// <summary>
/// 청구정보 (Bill_data)
/// </summary>
public class BillData
{
    public string BillId { get; set; } = string.Empty; // 청구관리번호 (UUID, PK)
    public string? ContractId { get; set; } // 계약관리번호 (FK - ContractData.ContractId)
    public DateTime? BillDate { get; set; } // 청구일
    public DateTime? PeriodStartDate { get; set; } // 청구기간 시작일
    public DateTime? PeriodEndDate { get; set; } // 청구기간 종료일
    public int? Rent { get; set; } // 임대료
    public int? ManagementFee { get; set; } // 관리비
    public int? UnpaidAmount { get; set; } // 미납금
    public int? WaterBill { get; set; } // 수도청구액
    public int? ElectricityBill { get; set; } // 전기청구액
    public int? GasBill { get; set; } // 가스청구액
    public int? HeatingBill { get; set; } // 난방청구액
    public int? CommunicationBill { get; set; } // 통신청구액
    public string? Adjustment { get; set; } // 가감액 (CSV - 내역, 금액)
    public string? BillRemarks { get; set; } // 청구비고
    public string? PaymentMethod { get; set; } // 납입 방식
    public DateTime? PaymentDueDate { get; set; } // 납입 기한
    public DateTime? LastPaymentDate { get; set; } // 마지막 납입 날짜
    public int? PaidAmount { get; set; } // 납입액
    public string? PaymentRemarks { get; set; } // 납입비고
    public string? AIComment { get; set; } // AI 코멘트
}
