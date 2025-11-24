using System;

namespace RentalHousingManagementConsole.Models;

/// <summary>
/// 차량정보 (Vehicle_data)
/// </summary>
public class VehicleData
{
    public string VehicleNumber { get; set; } = string.Empty; // 차량번호 (PK)
    public string? ContractId { get; set; } // 계약관리번호 (FK - ContractData.ContractId)
    public string? ResidentId { get; set; } // 주민관리번호 (FK - ResidentData.ResidentId)
    public string? AdditionalPhoneNumber { get; set; } // 추가전화번호
    public string? VehicleType { get; set; } // 차종
    public string? ParkingType { get; set; } // 주차구분 (상시 or 수시)
}
