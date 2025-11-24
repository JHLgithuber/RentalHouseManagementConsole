using System;

namespace RentalHousingManagementConsole.Models;

/// <summary>
/// 공지정보 (Notice_data)
/// </summary>
public class NoticeData
{
    public string NoticeId { get; set; } = string.Empty; // 공지번호 (UUID, PK)
    public string? AuthorId { get; set; } // 작성자 (FK - MembershipData.ID)
    public string? Content { get; set; } // 내용
    public string? NoticeTargets { get; set; } // 공지대상 (다중값 - ResidentData.ResidentId, VehicleData.VehicleNumber, ContractData.ContractId, 수동입력전화번호)
    public DateTime? CreatedDate { get; set; } // 최초작성일
    public DateTime? LastModifiedDate { get; set; } // 마지막수정일
    public bool? DeliveryStatus { get; set; } // 발송상태
}
