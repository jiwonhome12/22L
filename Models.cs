using System;
using System.Collections.Generic;

namespace SeatManagerApp
{
    public class AttendanceRecord
    {
        public string DateString { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "출석", "결석", "지각" 
    }

    public class StudentInfo
    {
        public string Department { get; set; } = string.Empty;  // 소속
        public string Name { get; set; } = string.Empty;        // 이름
        public string Advisor { get; set; } = string.Empty;     // 지도교수
        public string StudentId { get; set; } = string.Empty;   // 학번
        public string Email { get; set; } = string.Empty;       // 이메일
        public List<AttendanceRecord> Attendance { get; set; } = new List<AttendanceRecord>();

        public StudentInfo Clone()
        {
            var cloned = new StudentInfo
            {
                Department = this.Department,
                Name = this.Name,
                Advisor = this.Advisor,
                StudentId = this.StudentId,
                Email = this.Email,
                Attendance = new List<AttendanceRecord>()
            };
            foreach (var att in this.Attendance)
            {
                cloned.Attendance.Add(new AttendanceRecord
                {
                    DateString = att.DateString,
                    Status = att.Status
                });
            }
            return cloned;
        }
    }

    public class Seat
    {
        public int SeatNumber { get; set; }
        public StudentInfo? Student { get; set; }
        public bool IsFixed { get; set; }
        public bool IsSelected { get; set; }
        public bool IsPillar { get; set; }

        // Helper properties for UI binding
        public string DisplayId => Student != null ? Student.StudentId : string.Empty;
        public string DisplayName => Student != null ? Student.Name : string.Empty;
        public bool HasStudent => Student != null && !IsPillar;
    }

    public class MemoItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; } = string.Empty;
    }

    public class RentalItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StudentName { get; set; } = string.Empty;
        public string EquipmentType { get; set; } = string.Empty;
        public DateTime RentalDate { get; set; }
        public int RentalPeriodDays { get; set; } = 7; // Default 7 days rental
        public DateTime DueDate => RentalDate.AddDays(RentalPeriodDays);
        public bool IsReturned { get; set; } = false;
        public bool IsOverdue => DueDate < new DateTime(2026, 7, 16);
        public bool IsWarning => !IsReturned && !IsOverdue && (DueDate - new DateTime(2026, 7, 16)).TotalDays <= 7;
        public string StatusDisplay => IsReturned ? "반납 완료" : (IsOverdue ? "연체됨" : (IsWarning ? "반납 임박" : "대여중"));

        // Columns requested in screenshot
        public int Quantity { get; set; } = 1; // 대여수량
        public string ExtraItems { get; set; } = string.Empty; // 추가 대여물품(수량)
        public string Location { get; set; } = "DSU 창의공간"; // 사용장소
        public string Purpose { get; set; } = "개인 실습"; // 사용목적
        public string Remarks { get; set; } = ""; // 특이사항
        public string Department { get; set; } = "소프트웨어융합학과"; // 전공
        public string YearLevel { get; set; } = "3학년"; // 학년
        public string StudentId { get; set; } = "20261234"; // 학번
        public string Phone { get; set; } = "010-0000-0000"; // 연락처
        public string Advisor { get; set; } = "김동욱 교수"; // 지도교수
        public DateTime? ReturnDate { get; set; } // 반납일
        public string DisplayDeptYear => $"{Department} / {YearLevel}";
        public string DisplayPhoneAdvisor => $"{Phone} / {Advisor}";
    }

    public class ApprovalRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StudentName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Advisor { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TabType { get; set; } = string.Empty; // "상상Lab" or "캐비닛"
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "승인 대기"; // "승인 대기", "승인 완료", "반려"
    }
}
