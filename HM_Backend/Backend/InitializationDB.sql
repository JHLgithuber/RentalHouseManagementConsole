-- Database 생성
CREATE DATABASE HouseManager;
USE HouseManager;

-- 1. 주택정보 테이블
CREATE TABLE Houseinfo_data (
    UnitId VARCHAR(36) PRIMARY KEY, -- 세대번호 (PK)
    Furnishing TEXT, -- 비품정보 (CSV - 비품명, 수량, 상태)
    Location VARCHAR(255), -- 소재지
    RoomNumber INT, -- 호실
    RentalArea FLOAT, -- 임대면적
    HousingType VARCHAR(50), -- 주택유형
    StandardRent INT, -- 표준 임대료
    StandardManagementFee INT, -- 표준 관리비
    StandardDeposit INT, -- 표준 보증금
    Remarks TEXT, -- 비고
    ListingStatus BOOLEAN -- 매물여부
);

-- 2. 계약관리 테이블
CREATE TABLE Contract_data (
    ContractId VARCHAR(36) PRIMARY KEY, -- 계약관리번호 (UUID, PK)
    UnitId VARCHAR(36), -- 세대번호 (FK - Houseinfo_data.UnitId)
    TenantName VARCHAR(100), -- 임차인 성명
    PersonalId VARCHAR(20), -- 주민번호
    Address TEXT, -- 주소
    PhoneNumber VARCHAR(20), -- 전화번호
    AccountNumber VARCHAR(50), -- 계좌번호
    Language VARCHAR(20), -- 언어
    ContractStartDate DATE, -- 계약 시작일
    MoveInDate DATE, -- 입실일
    ContractEndDate DATE, -- 계약 종료일
    MoveOutDate DATE, -- 퇴실일
    ContractRent INT, -- 계약 임대료
    ContractManagementFee INT, -- 계약 관리비
    ContractDeposit INT, -- 계약 보증금
    DownPayment INT, -- 계약금
    BalancePayment INT, -- 잔금
    SpecialTerms TEXT, -- 특약사항
    ContractFile VARCHAR(255), -- 계약서 사본 (파일참조식별자)
    ContractRemarks TEXT, -- 계약비고
    MoveOutReturnAccount VARCHAR(50), -- 퇴실 반환 계좌번호
    MoveOutDeductionAmount INT, -- 퇴실 반환 공제액
    MoveOutDeductionDetails TEXT, -- 퇴실 반환 공제내역 (CSV - 내역, 금액)
    MoveOutConfirmationFile VARCHAR(255), -- 퇴실확인서 사본 (파일참조식별자)
    MoveOutRemarks TEXT, -- 퇴실비고
    FOREIGN KEY (UnitId) REFERENCES Houseinfo_data(UnitId)
);

-- 3. 청구서 테이블
CREATE TABLE Bill_data (
    BillId VARCHAR(36) PRIMARY KEY, -- 청구관리번호 (UUID, PK)
    ContractId VARCHAR(36), -- 계약관리번호 (FK - Contract_data.ContractId)
    BillDate DATE, -- 청구일
    PeriodStartDate DATE, -- 청구기간 시작일
    PeriodEndDate DATE, -- 청구기간 종료일
    Rent INT, -- 임대료
    ManagementFee INT, -- 관리비
    UnpaidAmount INT, -- 미납금
    WaterBill INT, -- 수도청구액
    ElectricityBill INT, -- 전기청구액
    GasBill INT, -- 가스청구액
    HeatingBill INT, -- 난방청구액
    CommunicationBill INT, -- 통신청구액
    Adjustment TEXT, -- 가감액 (CSV - 내역, 금액)
    BillRemarks TEXT, -- 청구비고
    PaymentMethod VARCHAR(50), -- 납입 방식
    PaymentDueDate DATE, -- 납입 기한
    LastPaymentDate DATE, -- 마지막 납입 날짜
    PaidAmount INT, -- 납입액
    PaymentRemarks TEXT, -- 납입비고
    AIComment TEXT, -- AI 코멘트
    FOREIGN KEY (ContractId) REFERENCES Contract_data(ContractId)
);

-- 4. 공과금 사용량정보 테이블
CREATE TABLE UtilUsage_data (
    MeasurementTime TIMESTAMP, -- 계량시각 (PK)
    UnitId VARCHAR(36), -- 세대번호 (PK, FK - Houseinfo_data.UnitId)
    UtilityType VARCHAR(50), -- 계량대상 (PK)
    MeasurementValue FLOAT, -- 계량값
    PRIMARY KEY (MeasurementTime, UnitId, UtilityType),
    FOREIGN KEY (UnitId) REFERENCES Houseinfo_data(UnitId)
);

-- 5. 주민정보 테이블
CREATE TABLE Resident_data (
    ResidentId VARCHAR(36) PRIMARY KEY, -- 주민관리번호 (UUID, PK)
    ContractId VARCHAR(36), -- 계약관리번호 (FK - Contract_data.ContractId)
    Name VARCHAR(100), -- 성명
    FamilyRelationship VARCHAR(50), -- 관계
    PhoneNumber VARCHAR(20), -- 전화번호
    Language VARCHAR(20), -- 언어
    ResidencyStatus BOOLEAN, -- 거주여부
    ApprovalStatus BOOLEAN, -- 승인상태
    FOREIGN KEY (ContractId) REFERENCES Contract_data(ContractId)
);

-- 6. 차량정보 테이블
CREATE TABLE Vehicle_data (
    VehicleNumber VARCHAR(20) PRIMARY KEY, -- 차량번호 (PK)
    ContractId VARCHAR(36), -- 계약관리번호 (FK - Contract_data.ContractId)
    ResidentId VARCHAR(36), -- 주민관리번호 (FK - Resident_data.ResidentId)
    AdditionalPhoneNumber VARCHAR(20), -- 추가전화번호
    VehicleType VARCHAR(50), -- 차종
    ParkingType VARCHAR(20), -- 주차구분 (상시 or 수시)
    FOREIGN KEY (ContractId) REFERENCES Contract_data(ContractId),
    FOREIGN KEY (ResidentId) REFERENCES Resident_data(ResidentId)
);

-- 7. 회원정보 테이블
CREATE TABLE Membership_data (
    ID VARCHAR(50) PRIMARY KEY, -- ID (PK)
    PasswordHash CHAR(64), -- PW (SHA-256)
    ResidentId VARCHAR(36), -- 주민관리번호 (FK - Resident_data.ResidentId)
    Authority CHAR(20), -- 권한
    Note TEXT, -- 비고
    FOREIGN KEY (ResidentId) REFERENCES Resident_data(ResidentId)
);

-- 8. 공지문 테이블
CREATE TABLE Notice_data (
    NoticeId VARCHAR(36) PRIMARY KEY, -- 공지번호 (UUID, PK)
    AuthorId VARCHAR(50), -- 작성자 (FK - Membership_data.ID)
    Content TEXT, -- 내용
    NoticeTargets TEXT, -- 공지대상 (다중값 - Resident_data.ResidentId, Vehicle_data.VehicleNumber, Contract_data.ContractId, 수동입력전화번호)
    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP, -- 최초작성일
    LastModifiedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, -- 마지막수정일
    DeliveryStatus BOOLEAN, -- 발송상태
    FOREIGN KEY (AuthorId) REFERENCES Membership_data(ID)
);

-- ========================================
-- 더미 데이터 삽입
-- ========================================

-- 1. 주택정보 더미 데이터
INSERT INTO Houseinfo_data (UnitId, Furnishing, Location, RoomNumber, RentalArea, HousingType, StandardRent, StandardManagementFee, StandardDeposit, Remarks, ListingStatus) VALUES
('unit-001', '냉장고,1,양호|세탁기,1,양호|에어컨,2,양호|가스레인지,1,양호', '서울특별시 강남구 테헤란로 123', 101, 84.5, '아파트', 1200000, 150000, 50000000, '남향, 최상층', FALSE),
('unit-002', '냉장고,1,양호|세탁기,1,보통|에어컨,1,양호', '서울특별시 강남구 테헤란로 123', 102, 59.8, '아파트', 900000, 120000, 30000000, '동향', FALSE),
('unit-003', '냉장고,1,양호|세탁기,1,양호|에어컨,1,양호|책상,2,양호', '서울특별시 강남구 테헤란로 123', 201, 74.3, '아파트', 1100000, 140000, 40000000, '남향', FALSE),
('unit-004', '냉장고,1,보통|세탁기,1,양호', '서울특별시 강남구 테헤란로 123', 202, 59.8, '아파트', 850000, 110000, 28000000, '북향', TRUE),
('unit-005', '냉장고,1,양호|세탁기,1,양호|에어컨,2,양호|식탁,1,양호', '서울특별시 송파구 올림픽로 456', 301, 105.2, '아파트', 1500000, 180000, 70000000, '남동향, 넓은 거실', FALSE),
('unit-006', '냉장고,1,양호|세탁기,1,양호|에어컨,1,양호', '서울특별시 송파구 올림픽로 456', 302, 84.5, '아파트', 1250000, 150000, 55000000, '동향', TRUE),
('unit-007', '냉장고,1,양호|세탁기,1,양호|에어컨,1,보통', '서울특별시 마포구 와우산로 789', 101, 45.2, '오피스텔', 700000, 80000, 20000000, '역세권, 교통편리', FALSE),
('unit-008', '냉장고,1,양호|세탁기,1,양호|에어컨,1,양호|침대,1,양호', '서울특별시 마포구 와우산로 789', 205, 35.8, '오피스텔', 600000, 70000, 15000000, '고층 전망좋음', TRUE),
('unit-009', '냉장고,1,보통|세탁기,1,보통|에어컨,1,양호', '경기도 성남시 분당구 판교역로 321', 501, 95.7, '아파트', 1400000, 160000, 60000000, '판교 신도시', FALSE),
('unit-010', '냉장고,1,양호|세탁기,1,양호|에어컨,2,양호|김치냉장고,1,양호', '경기도 성남시 분당구 판교역로 321', 502, 114.8, '아파트', 1700000, 200000, 80000000, '펜트하우스형', TRUE);

-- 2. 계약관리 더미 데이터
INSERT INTO Contract_data (ContractId, UnitId, TenantName, PersonalId, Address, PhoneNumber, AccountNumber, Language, ContractStartDate, MoveInDate, ContractEndDate, MoveOutDate, ContractRent, ContractManagementFee, ContractDeposit, DownPayment, BalancePayment, SpecialTerms, ContractFile, ContractRemarks, MoveOutReturnAccount, MoveOutDeductionAmount, MoveOutDeductionDetails, MoveOutConfirmationFile, MoveOutRemarks) VALUES
('contract-001', 'unit-001', '김철수', '800101-1234567', '서울특별시 서초구 반포대로 100', '010-1234-5678', '110-123-456789', '한국어', '2024-01-01', '2024-01-05', '2026-01-01', NULL, 1200000, 150000, 50000000, 5000000, 45000000, '반려동물 불가, 전대 금지', 'contract_001.pdf', '장기계약 희망', NULL, NULL, NULL, NULL, NULL),
('contract-002', 'unit-002', '이영희', '850315-2345678', '서울특별시 강동구 천호대로 200', '010-2345-6789', '020-234-567890', '한국어', '2023-06-01', '2023-06-01', '2025-06-01', NULL, 900000, 120000, 30000000, 3000000, 27000000, '소음 주의', 'contract_002.pdf', NULL, NULL, NULL, NULL, NULL, NULL),
('contract-003', 'unit-003', '박민수', '900520-1456789', '경기도 고양시 일산동구 정발산로 300', '010-3456-7890', '110-345-678901', '한국어', '2024-03-01', '2024-03-01', '2026-03-01', NULL, 1100000, 140000, 40000000, 4000000, 36000000, '주차 1대 포함', 'contract_003.pdf', '신혼부부', NULL, NULL, NULL, NULL, NULL),
('contract-004', 'unit-005', 'John Smith', '920815-5678901', '서울특별시 용산구 이태원로 400', '010-4567-8901', '020-456-789012', 'English', '2023-09-01', '2023-09-05', '2025-09-01', NULL, 1500000, 180000, 70000000, 7000000, 63000000, '외국인 거주자, 영문 계약서 별도', 'contract_004.pdf', '주재원', NULL, NULL, NULL, NULL, NULL),
('contract-005', 'unit-007', '최지혜', '950210-2567890', '서울특별시 노원구 상계로 500', '010-5678-9012', '110-567-890123', '한국어', '2024-07-01', '2024-07-01', '2025-07-01', NULL, 700000, 80000, 20000000, 2000000, 18000000, '1년 단기계약', 'contract_005.pdf', '대학생', NULL, NULL, NULL, NULL, NULL),
('contract-006', 'unit-009', '정대호', '880925-1678902', '경기도 용인시 수지구 광교로 600', '010-6789-0123', '020-678-901234', '한국어', '2024-02-01', '2024-02-01', '2026-02-01', NULL, 1400000, 160000, 60000000, 6000000, 54000000, '주차 2대 포함', 'contract_006.pdf', '4인 가족', NULL, NULL, NULL, NULL, NULL),
('contract-007', 'unit-001', '강민지', '970605-2789013', '서울특별시 관악구 신림로 700', '010-7890-1234', '110-789-012345', '한국어', '2022-01-01', '2022-01-01', '2024-01-01', '2024-01-05', 1150000, 140000, 45000000, 4500000, 40500000, NULL, 'contract_007.pdf', '만기 정상 퇴실', '110-789-012345', 500000, '벽지 파손 보수비,300000|청소비,200000', 'moveout_007.pdf', '양호한 상태로 반환'),
('contract-008', 'unit-003', '윤서준', '890420-1890124', '경기도 광명시 오리로 800', '010-8901-2345', '020-890-123456', '한국어', '2021-06-01', '2021-06-01', '2023-06-01', '2023-06-05', 1000000, 130000, 35000000, 3500000, 31500000, NULL, 'contract_008.pdf', '중도 해지', '020-890-123456', 1000000, '중도해지 위약금,800000|청소비,200000', 'moveout_008.pdf', '조기 퇴실로 위약금 발생');

-- 3. 청구서 더미 데이터
INSERT INTO Bill_data (BillId, ContractId, BillDate, PeriodStartDate, PeriodEndDate, Rent, ManagementFee, UnpaidAmount, WaterBill, ElectricityBill, GasBill, HeatingBill, CommunicationBill, Adjustment, BillRemarks, PaymentMethod, PaymentDueDate, LastPaymentDate, PaidAmount, PaymentRemarks, AIComment) VALUES
-- contract-001 청구내역 (2024년)
('bill-001', 'contract-001', '2024-01-25', '2024-02-01', '2024-02-29', 1200000, 150000, 0, 25000, 85000, 45000, 120000, 0, NULL, NULL, '자동이체', '2024-02-05', '2024-02-03', 1625000, '정상납부', '정상 납부 이력'),
('bill-002', 'contract-001', '2024-02-25', '2024-03-01', '2024-03-31', 1200000, 150000, 0, 28000, 90000, 50000, 100000, 0, NULL, NULL, '자동이체', '2024-03-05', '2024-03-04', 1618000, '정상납부', '정상 납부 이력'),
('bill-003', 'contract-001', '2024-03-25', '2024-04-01', '2024-04-30', 1200000, 150000, 0, 30000, 75000, 40000, 60000, 0, NULL, NULL, '자동이체', '2024-04-05', '2024-04-03', 1555000, '정상납부', '정상 납부 이력'),
('bill-004', 'contract-001', '2024-04-25', '2024-05-01', '2024-05-31', 1200000, 150000, 0, 32000, 95000, 35000, 0, 0, NULL, NULL, '자동이체', '2024-05-05', '2024-05-04', 1512000, '정상납부', '정상 납부 이력'),
('bill-005', 'contract-001', '2024-05-25', '2024-06-01', '2024-06-30', 1200000, 150000, 0, 35000, 120000, 30000, 0, 0, NULL, NULL, '자동이체', '2024-06-05', '2024-06-03', 1535000, '정상납부', '정상 납부 이력'),

-- contract-002 청구내역 (일부 미납)
('bill-006', 'contract-002', '2024-01-25', '2024-02-01', '2024-02-29', 900000, 120000, 0, 20000, 65000, 35000, 90000, 0, NULL, NULL, '수동입금', '2024-02-05', '2024-02-04', 1230000, '정상납부', '정상 납부'),
('bill-007', 'contract-002', '2024-02-25', '2024-03-01', '2024-03-31', 900000, 120000, 0, 22000, 70000, 38000, 80000, 0, NULL, NULL, '수동입금', '2024-03-05', '2024-03-08', 1230000, '연체 3일', '경미한 연체'),
('bill-008', 'contract-002', '2024-03-25', '2024-04-01', '2024-04-30', 900000, 120000, 50000, 25000, 60000, 30000, 50000, 0, NULL, NULL, '수동입금', '2024-04-05', '2024-04-15', 1185000, '연체 10일, 일부미납', '미납액 발생, 독촉 필요'),
('bill-009', 'contract-002', '2024-04-25', '2024-05-01', '2024-05-31', 900000, 120000, 50000, 27000, 75000, 28000, 0, 0, NULL, NULL, '수동입금', '2024-05-05', NULL, 0, '미납', '2개월 연속 미납, 긴급 독촉 필요'),

-- contract-003 청구내역
('bill-010', 'contract-003', '2024-03-25', '2024-04-01', '2024-04-30', 1100000, 140000, 0, 28000, 85000, 42000, 70000, 0, NULL, NULL, '자동이체', '2024-04-05', '2024-04-02', 1465000, '정상납부', '정상 납부 이력'),
('bill-011', 'contract-003', '2024-04-25', '2024-05-01', '2024-05-31', 1100000, 140000, 0, 30000, 95000, 38000, 0, 0, NULL, NULL, '자동이체', '2024-05-05', '2024-05-03', 1403000, '정상납부', '정상 납부 이력'),
('bill-012', 'contract-003', '2024-05-25', '2024-06-01', '2024-06-30', 1100000, 140000, 0, 33000, 110000, 35000, 0, 0, NULL, NULL, '자동이체', '2024-06-05', '2024-06-04', 1418000, '정상납부', '정상 납부 이력'),

-- contract-004 청구내역 (외국인)
('bill-013', 'contract-004', '2024-01-25', '2024-02-01', '2024-02-29', 1500000, 180000, 0, 35000, 110000, 55000, 150000, 0, NULL, '영문 청구서 발송', '자동이체', '2024-02-05', '2024-02-03', 2030000, '정상납부', '정상 납부 이력'),
('bill-014', 'contract-004', '2024-02-25', '2024-03-01', '2024-03-31', 1500000, 180000, 0, 38000, 120000, 60000, 130000, 0, NULL, '영문 청구서 발송', '자동이체', '2024-03-05', '2024-03-04', 2028000, '정상납부', '정상 납부 이력'),

-- contract-005 청구내역 (학생)
('bill-015', 'contract-005', '2024-07-25', '2024-08-01', '2024-08-31', 700000, 80000, 0, 15000, 45000, 20000, 0, 30000, NULL, NULL, '수동입금', '2024-08-05', '2024-08-04', 890000, '정상납부', '정상 납부'),
('bill-016', 'contract-005', '2024-08-25', '2024-09-01', '2024-09-30', 700000, 80000, 0, 16000, 50000, 22000, 0, 30000, NULL, NULL, '수동입금', '2024-09-05', '2024-09-03', 898000, '정상납부', '정상 납부'),

-- contract-006 청구내역
('bill-017', 'contract-006', '2024-02-25', '2024-03-01', '2024-03-31', 1400000, 160000, 0, 32000, 105000, 48000, 110000, 0, NULL, NULL, '자동이체', '2024-03-05', '2024-03-03', 1855000, '정상납부', '정상 납부 이력'),
('bill-018', 'contract-006', '2024-03-25', '2024-04-01', '2024-04-30', 1400000, 160000, 0, 35000, 100000, 45000, 80000, 0, NULL, NULL, '자동이체', '2024-04-05', '2024-04-04', 1820000, '정상납부', '정상 납부 이력'),
('bill-019', 'contract-006', '2024-04-25', '2024-05-01', '2024-05-31', 1400000, 160000, 0, 38000, 115000, 40000, 0, 0, '주차비추가,50000', NULL, '자동이체', '2024-05-05', '2024-05-02', 1803000, '정상납부, 주차비 추가', '추가 주차비 정상 납부');

-- 4. 공과금 사용량정보 더미 데이터
INSERT INTO UtilUsage_data (MeasurementTime, UnitId, UtilityType, MeasurementValue) VALUES
-- unit-001 사용량 (2024년 1~6월)
('2024-01-31 23:59:00', 'unit-001', '수도', 12.5),
('2024-01-31 23:59:00', 'unit-001', '전기', 340.0),
('2024-01-31 23:59:00', 'unit-001', '가스', 45.0),
('2024-02-29 23:59:00', 'unit-001', '수도', 14.0),
('2024-02-29 23:59:00', 'unit-001', '전기', 360.0),
('2024-02-29 23:59:00', 'unit-001', '가스', 50.0),
('2024-03-31 23:59:00', 'unit-001', '수도', 15.0),
('2024-03-31 23:59:00', 'unit-001', '전기', 300.0),
('2024-03-31 23:59:00', 'unit-001', '가스', 40.0),
('2024-04-30 23:59:00', 'unit-001', '수도', 16.0),
('2024-04-30 23:59:00', 'unit-001', '전기', 380.0),
('2024-04-30 23:59:00', 'unit-001', '가스', 35.0),
('2024-05-31 23:59:00', 'unit-001', '수도', 17.5),
('2024-05-31 23:59:00', 'unit-001', '전기', 480.0),
('2024-05-31 23:59:00', 'unit-001', '가스', 30.0),

-- unit-002 사용량
('2024-01-31 23:59:00', 'unit-002', '수도', 10.0),
('2024-01-31 23:59:00', 'unit-002', '전기', 260.0),
('2024-01-31 23:59:00', 'unit-002', '가스', 35.0),
('2024-02-29 23:59:00', 'unit-002', '수도', 11.0),
('2024-02-29 23:59:00', 'unit-002', '전기', 280.0),
('2024-02-29 23:59:00', 'unit-002', '가스', 38.0),
('2024-03-31 23:59:00', 'unit-002', '수도', 12.5),
('2024-03-31 23:59:00', 'unit-002', '전기', 240.0),
('2024-03-31 23:59:00', 'unit-002', '가스', 30.0),
('2024-04-30 23:59:00', 'unit-002', '수도', 13.5),
('2024-04-30 23:59:00', 'unit-002', '전기', 300.0),
('2024-04-30 23:59:00', 'unit-002', '가스', 28.0),

-- unit-003 사용량
('2024-03-31 23:59:00', 'unit-003', '수도', 14.0),
('2024-03-31 23:59:00', 'unit-003', '전기', 340.0),
('2024-03-31 23:59:00', 'unit-003', '가스', 42.0),
('2024-04-30 23:59:00', 'unit-003', '수도', 15.0),
('2024-04-30 23:59:00', 'unit-003', '전기', 380.0),
('2024-04-30 23:59:00', 'unit-003', '가스', 38.0),
('2024-05-31 23:59:00', 'unit-003', '수도', 16.5),
('2024-05-31 23:59:00', 'unit-003', '전기', 440.0),
('2024-05-31 23:59:00', 'unit-003', '가스', 35.0),

-- unit-005 사용량
('2024-01-31 23:59:00', 'unit-005', '수도', 17.5),
('2024-01-31 23:59:00', 'unit-005', '전기', 440.0),
('2024-01-31 23:59:00', 'unit-005', '가스', 55.0),
('2024-02-29 23:59:00', 'unit-005', '수도', 19.0),
('2024-02-29 23:59:00', 'unit-005', '전기', 480.0),
('2024-02-29 23:59:00', 'unit-005', '가스', 60.0),

-- unit-007 사용량
('2024-07-31 23:59:00', 'unit-007', '수도', 7.5),
('2024-07-31 23:59:00', 'unit-007', '전기', 180.0),
('2024-07-31 23:59:00', 'unit-007', '가스', 20.0),
('2024-08-31 23:59:00', 'unit-007', '수도', 8.0),
('2024-08-31 23:59:00', 'unit-007', '전기', 200.0),
('2024-08-31 23:59:00', 'unit-007', '가스', 22.0),

-- unit-009 사용량
('2024-02-29 23:59:00', 'unit-009', '수도', 16.0),
('2024-02-29 23:59:00', 'unit-009', '전기', 420.0),
('2024-02-29 23:59:00', 'unit-009', '가스', 48.0),
('2024-03-31 23:59:00', 'unit-009', '수도', 17.5),
('2024-03-31 23:59:00', 'unit-009', '전기', 400.0),
('2024-03-31 23:59:00', 'unit-009', '가스', 45.0),
('2024-04-30 23:59:00', 'unit-009', '수도', 19.0),
('2024-04-30 23:59:00', 'unit-009', '전기', 460.0),
('2024-04-30 23:59:00', 'unit-009', '가스', 40.0);

-- 5. 주민정보 더미 데이터
INSERT INTO Resident_data (ResidentId, ContractId, Name, FamilyRelationship, PhoneNumber, Language, ResidencyStatus, ApprovalStatus) VALUES
-- contract-001 가족 (김철수 가족)
('resident-001', 'contract-001', '김철수', '본인', '010-1234-5678', '한국어', TRUE, TRUE),
('resident-002', 'contract-001', '이미영', '배우자', '010-1234-5679', '한국어', TRUE, TRUE),
('resident-003', 'contract-001', '김지훈', '자녀', '010-1234-5680', '한국어', TRUE, TRUE),

-- contract-002 (이영희 단독)
('resident-004', 'contract-002', '이영희', '본인', '010-2345-6789', '한국어', TRUE, TRUE),

-- contract-003 가족 (박민수 신혼부부)
('resident-005', 'contract-003', '박민수', '본인', '010-3456-7890', '한국어', TRUE, TRUE),
('resident-006', 'contract-003', '최수정', '배우자', '010-3456-7891', '한국어', TRUE, TRUE),

-- contract-004 가족 (John Smith 외국인 가족)
('resident-007', 'contract-004', 'John Smith', '본인', '010-4567-8901', 'English', TRUE, TRUE),
('resident-008', 'contract-004', 'Emily Smith', '배우자', '010-4567-8902', 'English', TRUE, TRUE),
('resident-009', 'contract-004', 'Michael Smith', '자녀', '010-4567-8903', 'English', TRUE, TRUE),
('resident-010', 'contract-004', 'Sarah Smith', '자녀', '010-4567-8904', 'English', TRUE, TRUE),

-- contract-005 (최지혜 학생 + 룸메이트)
('resident-011', 'contract-005', '최지혜', '본인', '010-5678-9012', '한국어', TRUE, TRUE),
('resident-012', 'contract-005', '한소희', '친구', '010-5678-9013', '한국어', TRUE, TRUE),

-- contract-006 가족 (정대호 4인 가족)
('resident-013', 'contract-006', '정대호', '본인', '010-6789-0123', '한국어', TRUE, TRUE),
('resident-014', 'contract-006', '김은주', '배우자', '010-6789-0124', '한국어', TRUE, TRUE),
('resident-015', 'contract-006', '정민서', '자녀', '010-6789-0125', '한국어', TRUE, TRUE),
('resident-016', 'contract-006', '정민준', '자녀', '010-6789-0126', '한국어', TRUE, TRUE),

-- contract-007 (과거 계약 - 퇴실)
('resident-017', 'contract-007', '강민지', '본인', '010-7890-1234', '한국어', FALSE, TRUE),

-- contract-008 (과거 계약 - 퇴실)
('resident-018', 'contract-008', '윤서준', '본인', '010-8901-2345', '한국어', FALSE, TRUE),
('resident-019', 'contract-008', '박소연', '배우자', '010-8901-2346', '한국어', FALSE, TRUE);

-- 6. 차량정보 더미 데이터
INSERT INTO Vehicle_data (VehicleNumber, ContractId, ResidentId, AdditionalPhoneNumber, VehicleType, ParkingType) VALUES
-- contract-001 차량
('12가3456', 'contract-001', 'resident-001', NULL, '현대 그랜저', '상시'),

-- contract-002 차량
('34나5678', 'contract-002', 'resident-004', NULL, '기아 K5', '상시'),

-- contract-003 차량
('56다7890', 'contract-003', 'resident-005', NULL, '현대 아반떼', '상시'),

-- contract-004 차량 (2대)
('78라1234', 'contract-004', 'resident-007', '010-4567-8905', 'BMW 520d', '상시'),
('90마5678', 'contract-004', 'resident-008', NULL, 'Mercedes C-Class', '상시'),

-- contract-006 차량 (2대)
('11바9012', 'contract-006', 'resident-013', NULL, '현대 팰리세이드', '상시'),
('22사3456', 'contract-006', 'resident-014', NULL, '기아 스포티지', '수시'),

-- 과거 계약 차량
('33아7890', 'contract-007', 'resident-017', NULL, '현대 소나타', '상시'),
('44자1234', 'contract-008', 'resident-018', NULL, '기아 쏘렌토', '상시');

-- 7. 회원정보 더미 데이터 (SHA-256 해시 사용)
INSERT INTO Membership_data (ID, PasswordHash, ResidentId, Authority, Note) VALUES
-- 관리자 계정
('admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', NULL, 'ADMIN', '시스템 관리자'), -- 비밀번호: admin
('manager1', '6cf615d5bcaac778352a8f1f3360d23f02f34ec182e259897fd6ce485d7870d4', NULL, 'MANAGER', '건물 관리인'), -- 비밀번호: manager1

-- 입주민 계정
('kim_cs', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', 'resident-001', 'RESIDENT', NULL), -- 비밀번호: 1234
('lee_yh', 'ef797c8118f02dfb649607dd5d3f8c7623048c9c063d532cc95c5ed7a898a64f', 'resident-004', 'RESIDENT', NULL), -- 비밀번호: 12345
('park_ms', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'resident-005', 'RESIDENT', NULL), -- 비밀번호: password
('john_smith', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'resident-007', 'RESIDENT', '외국인 입주민'), -- 비밀번호: 123
('choi_jh', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 'resident-011', 'RESIDENT', '학생'), -- 비밀번호: 123456
('jung_dh', '5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5', 'resident-013', 'RESIDENT', NULL); -- 비밀번호: 12345678

-- 8. 공지문 더미 데이터
INSERT INTO Notice_data (NoticeId, AuthorId, Content, NoticeTargets, CreatedDate, LastModifiedDate, DeliveryStatus) VALUES
('notice-001', 'admin', '2024년 설 연휴 관리사무소 운영 안내\n\n입주민 여러분께,\n\n설 연휴(2024.02.09~02.12) 기간 동안 관리사무소는 휴무입니다.\n긴급 상황 시 비상연락처(010-1234-9999)로 연락 주시기 바랍니다.\n\n감사합니다.', 'resident-001,resident-002,resident-003,resident-004,resident-005,resident-006,resident-007,resident-008,resident-009,resident-010', '2024-01-25 10:00:00', '2024-01-25 10:00:00', TRUE),

('notice-002', 'manager1', '정기 소독 실시 안내\n\n다음 주 수요일(2024.03.13) 오전 10시부터 전체 건물 정기 소독을 실시합니다.\n소독 시간 동안 외출을 권장드리며, 애완동물이 있으신 세대는 별도 연락 주시기 바랍니다.\n\n관리사무소 드림', 'contract-001,contract-002,contract-003,contract-004,contract-005,contract-006', '2024-03-06 14:30:00', '2024-03-06 14:30:00', TRUE),

('notice-003', 'admin', '주차 관련 안내 사항\n\n최근 무단 주차 차량이 증가하여 안내 말씀 드립니다.\n- 지정된 주차구역에만 주차 가능\n- 방문차량은 사전 신고 필수\n- 위반 시 견인 조치될 수 있습니다.\n\n협조 부탁드립니다.', '12가3456,34나5678,56다7890,78라1234,90마5678,11바9012,22사3456', '2024-04-15 09:00:00', '2024-04-15 09:00:00', TRUE),

('notice-004', 'manager1', '수도 점검 안내\n\n2024년 5월 20일(월) 오전 9시~오후 1시까지 수도 배관 점검이 있습니다.\n점검 시간 동안 단수될 예정이오니 미리 용수를 받아두시기 바랍니다.\n\n불편을 드려 죄송합니다.', 'resident-001,resident-004,resident-005,resident-006,resident-007,resident-008,resident-011,resident-012,resident-013,resident-014', '2024-05-13 16:00:00', '2024-05-13 16:00:00', TRUE),

('notice-005', 'admin', '하절기 전기 사용 안내\n\n여름철 전기 사용량 증가로 인한 안내입니다.\n과도한 냉방기 사용은 전기요금 증가의 원인이 됩니다.\n적정 온도(26도) 유지를 권장드립니다.\n\n감사합니다.', 'contract-001,contract-002,contract-003,contract-004,contract-005,contract-006', '2024-06-01 11:00:00', '2024-06-01 11:00:00', FALSE),

('notice-006', 'manager1', '층간 소음 관련 협조 요청\n\n최근 층간 소음 관련 민원이 접수되었습니다.\n특히 야간 시간대(22:00~06:00)에는 소음에 주의해 주시기 바랍니다.\n\n모두가 쾌적한 환경에서 생활할 수 있도록 협조 부탁드립니다.', 'resident-001,resident-002,resident-003,resident-004,resident-005,resident-006,resident-011,resident-012', '2024-07-10 15:30:00', '2024-07-10 15:30:00', FALSE),

('notice-007', 'admin', '추석 연휴 택배 보관 안내\n\n추석 연휴 기간 동안 택배 물량이 많을 것으로 예상됩니다.\n택배는 1층 택배 보관함에 보관되며, 최대 3일까지 보관 가능합니다.\n장기 부재 시 사전에 연락 주시기 바랍니다.', 'contract-001,contract-002,contract-003,contract-004,contract-005,contract-006,010-9999-8888', '2024-09-10 10:00:00', '2024-09-10 10:00:00', FALSE);

-- 더미 데이터 삽입 완료
SELECT '더미 데이터 삽입이 완료되었습니다.' AS Message;
