using System;

namespace RentalHousingManagementConsole.Models;

/// <summary>
/// 계량정보 (UtilUsage_data)
/// </summary>
public class UtilUsageData
{
    public DateTime MeasurementTime { get; set; } // 계량시각 (PK)
    public string UnitId { get; set; } = string.Empty; // 세대번호 (PK, FK - HouseinfoData.UnitId)
    public string UtilityType { get; set; } = string.Empty; // 계량대상 (PK)
    public double? MeasurementValue { get; set; } // 계량값
}
