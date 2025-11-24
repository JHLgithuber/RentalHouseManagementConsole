using System;

namespace RentalHousingManagementConsole.Models;

/// <summary>
/// 세대정보 (Houseinfo_data)
/// </summary>
public class HouseInfoData
{
    public string UnitId { get; set; } = string.Empty; // 세대번호 (PK)
    public string? Furnishing { get; set; } // 비품정보 (CSV - 비품명, 수량, 상태)
    public string? Location { get; set; } // 소재지
    public int? RoomNumber { get; set; } // 호실
    public double? RentalArea { get; set; } // 임대면적
    public string? HousingType { get; set; } // 주택유형
    public int? StandardRent { get; set; } // 표준 임대료
    public int? StandardManagementFee { get; set; } // 표준 관리비
    public int? StandardDeposit { get; set; } // 표준 보증금
    public string? Remarks { get; set; } // 비고
    public bool? ListingStatus { get; set; } = true; // 매물여부
}
