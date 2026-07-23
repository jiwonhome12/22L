using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SeatManagerApp
{
    public partial class MainWindow : Window
    {
        // Master Database (simulating Google Forms inputs)
        private List<StudentInfo> _masterStudents = new List<StudentInfo>();
        
        // Year & Semester Seat Layout Cache
        private Dictionary<string, List<Seat>> _seatLayoutCache = new Dictionary<string, List<Seat>>();
        
        // Active display seats for current Year & Semester
        private List<Seat> _activeSeats = new List<Seat>();

        // Equipment Rentals
        private ObservableCollection<RentalItem> _rentals = new ObservableCollection<RentalItem>();
        private Stack<RentalItem> _rentalUndoStack = new Stack<RentalItem>();

        // Cabinet & SangsangLab Google Form Approvals
        private ObservableCollection<ApprovalRequest> _approvals = new ObservableCollection<ApprovalRequest>();

        // Memos
        private ObservableCollection<MemoItem> _memos = new ObservableCollection<MemoItem>();

        private Dictionary<int, (StudentInfo Student, string Period)> _cabinetAllocations = new Dictionary<int, (StudentInfo, string)>();

        // App Modes
        private bool _isSeatFixMode = false;
        private bool _isSeatDeleteMode = false;

        // Current simulated date
        private DateTime _currentSimulatedDate;

        // Current editing seat for modal
        private Seat? _currentEditingSeat;
         
        // Edit mode for memo
        private MemoItem? _editingMemo;
        private string? _originalMemoContent;

        private bool _isModalEditing = false;

        public MainWindow()
        {
            InitializeComponent();

            // Use current system time for date
            _currentSimulatedDate = DateTime.Now;

            // Update date display
            UpdateDateDisplay();

            // Start timer to update date every minute
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += (s, e) =>
            {
                _currentSimulatedDate = DateTime.Now;
                UpdateDateDisplay();
                UpdateAlertBadges();
            };
            timer.Start();

            // Populate Year dropdown dynamically
            InitializeYearDropdown();

            // Setup Demo Data (2026-여름방학만 유지)
            SetupMasterDatabase();
            SetupCabinetAndSangsangLabDemoData();
            SetupRentalDemoData();
            SetupMemoDemoData();

            // Refresh UI Badges
            UpdateAlertBadges();

            // Load initial view
            LoadDashboardLayout();
        }

        private void UpdateDateDisplay()
        {
            string dayOfWeek = _currentSimulatedDate.DayOfWeek switch
            {
                DayOfWeek.Monday => "월",
                DayOfWeek.Tuesday => "화",
                DayOfWeek.Wednesday => "수",
                DayOfWeek.Thursday => "목",
                DayOfWeek.Friday => "금",
                DayOfWeek.Saturday => "토",
                DayOfWeek.Sunday => "일",
                _ => ""
            };
            TxtCurrentDate.Text = $"{_currentSimulatedDate:yyyy-MM-dd} ({dayOfWeek})";
        }

        private void InitializeYearDropdown()
        {
            ComboSearchYear.Items.Clear();
            int currentYear = _currentSimulatedDate.Year;
            // Let's populate years from 2020 up to current year
            for (int y = 2020; y <= currentYear; y++)
            {
                ComboSearchYear.Items.Add(y.ToString());
            }
            ComboSearchYear.SelectedIndex = ComboSearchYear.Items.Count - 1; // Default to current year
        }

        private void SetupMasterDatabase()
        {
            // Preset names matching the screenshot
            var names = new[] {
                "임진섭", "김지원", "송유진", "문현수", "현길용", "박주은", "이인한",
                "이소빈", "김동욱", "김보경", "전상우", "김지원", "김지명", "허서현",
                "성지경", "장지환", "김지민", "김동욱", "김연준", "서채빈", "강호석",
                "민길홍", "임지규", "임시현", "이예원", "남지양", "권도균", "배지민"
            };

            var departments = new[] { "컴퓨터공학과", "소프트웨어융합학과", "정보보안학과", "게임공학과" };
            var professors = new[] { "김동욱 교수", "이지현 교수", "박상우 교수", "최준호 교수" };

            Random rand = new Random(101); // Fixed seed for reproducible student ids
            int idBase = 20210000;

            for (int i = 0; i < names.Length; i++)
            {
                var s = new StudentInfo
                {
                    StudentId = (idBase + rand.Next(10000, 99999)).ToString(),
                    Name = names[i],
                    Department = departments[rand.Next(departments.Length)],
                    Advisor = professors[rand.Next(professors.Length)],
                    Email = $"student{i}@dsu.ac.kr"
                };

                // Add 3 days of attendance
                s.Attendance.Add(new AttendanceRecord { DateString = "2026-07-13", Status = rand.Next(10) < 8 ? "출석" : "지각" });
                s.Attendance.Add(new AttendanceRecord { DateString = "2026-07-14", Status = rand.Next(10) < 8 ? "출석" : "결석" });
                s.Attendance.Add(new AttendanceRecord { DateString = "2026-07-15", Status = "출석" });

                _masterStudents.Add(s);
            }

            // Add graduate students to master database
            _masterStudents.Add(new StudentInfo { Name = "박지성", StudentId = "20205555", Department = "대학원 컴퓨터공학과", Advisor = "김동욱 교수", Email = "jisung@dsu.ac.kr" });
            _masterStudents.Add(new StudentInfo { Name = "손흥민", StudentId = "20206666", Department = "대학원 정보보안학과", Advisor = "이지현 교수", Email = "son@dsu.ac.kr" });

            GridMasterStudents.ItemsSource = _masterStudents;
        }

        private void SetupCabinetAndSangsangLabDemoData()
        {
            // Simulate 7 pending Google Forms submissions to match the screenshot "승인 대기 7건"
            _approvals.Add(new ApprovalRequest { StudentName = "홍길동", StudentId = "20231102", Department = "컴퓨터공학과", Advisor = "김동욱 교수", TabType = "상상Lab" });
            _approvals.Add(new ApprovalRequest { StudentName = "이영희", StudentId = "20220456", Department = "정보보안학과", Advisor = "이지현 교수", TabType = "상상Lab" });
            _approvals.Add(new ApprovalRequest { StudentName = "박철수", StudentId = "20240911", Department = "게임공학과", Advisor = "최준호 교수", TabType = "상상Lab" });
            _approvals.Add(new ApprovalRequest { StudentName = "최영선", StudentId = "20210872", Department = "소프트웨어융합학과", Advisor = "박상우 교수", TabType = "상상Lab" });

            _approvals.Add(new ApprovalRequest { StudentName = "강민호", StudentId = "20251142", Department = "컴퓨터공학과", Advisor = "김동욱 교수", TabType = "캐비닛" });
            _approvals.Add(new ApprovalRequest { StudentName = "윤하은", StudentId = "20231509", Department = "정보보안학과", Advisor = "이지현 교수", TabType = "캐비닛" });
            _approvals.Add(new ApprovalRequest { StudentName = "정승우", StudentId = "20221199", Department = "게임공학과", Advisor = "최준호 교수", TabType = "캐비닛" });

            // Initialize cabinet allocations dictionary
            _cabinetAllocations[4] = (new StudentInfo { Name = "홍길동", StudentId = "20261234", Department = "컴퓨터공학과" }, "07/16~08/16");
            _cabinetAllocations[13] = (new StudentInfo { Name = "이영희", StudentId = "20220456", Department = "정보보안학과" }, "07/16~08/16");
            _cabinetAllocations[20] = (new StudentInfo { Name = "박철수", StudentId = "20240911", Department = "게임공학과" }, "07/16~08/16");
            _cabinetAllocations[35] = (new StudentInfo { Name = "최영선", StudentId = "20210872", Department = "소프트웨어융합학과" }, "07/16~08/16");
            _cabinetAllocations[42] = (new StudentInfo { Name = "강민호", StudentId = "20251142", Department = "컴퓨터공학과" }, "07/16~08/16");

            LstSangsangLabCards.ItemsSource = _approvals.Where(a => a.TabType == "상상Lab").ToList();
            GridCabinetApprovals.ItemsSource = _approvals.Where(a => a.TabType == "캐비닛").ToList();
        }

        private void SetupRentalDemoData()
        {
            // 2 overdue rentals (Red light) to match screenshot "긴급 요청 2건"
            _rentals.Add(new RentalItem { StudentName = "임진섭", EquipmentType = "VR 헤드셋 Oculus Quest 2", RentalDate = _currentSimulatedDate.AddDays(-10), RentalPeriodDays = 7 });
            _rentals.Add(new RentalItem { StudentName = "김지원", EquipmentType = "iPad Pro 12.9 + Apple Pencil", RentalDate = _currentSimulatedDate.AddDays(-8), RentalPeriodDays = 7 });

            // In-period rentals (Green light)
            _rentals.Add(new RentalItem { StudentName = "송유진", EquipmentType = "Arduino IoT Starter Kit", RentalDate = _currentSimulatedDate.AddDays(-2), RentalPeriodDays = 7 });
            _rentals.Add(new RentalItem { StudentName = "문현수", EquipmentType = "Raspberry Pi 4 Model B", RentalDate = _currentSimulatedDate.AddDays(-1), RentalPeriodDays = 7 });

            GridRentals.ItemsSource = _rentals;
        }

        private void SetupMemoDemoData()
        {
            _memos.Add(new MemoItem { Content = "3층 강의실 컴퓨터 포멧" });
            _memos.Add(new MemoItem { Content = "지문 고장/ 수리기사님 7/22 방문" });
            _memos.Add(new MemoItem { Content = "3~4층 에어컨 고장" });

            LstMemos.ItemsSource = _memos;
        }

        private void UpdateAlertBadges()
        {
            // SangsangLab approval count
            int sangsangLabCount = _approvals.Count(a => a.Status == "승인 대기" && a.TabType == "상상Lab");
            TxtSangsangLabCount.Text = $"{sangsangLabCount}건";

            // Cabinet approval count
            int cabinetCount = _approvals.Count(a => a.Status == "승인 대기" && a.TabType == "캐비닛");
            TxtCabinetCount.Text = $"{cabinetCount}건";

            // Equipment overdue count
            int overdueCount = _rentals.Count(r => !r.IsReturned && r.DueDate < _currentSimulatedDate);
            TxtEquipmentCount.Text = $"{overdueCount}건";

            // Sync other counts in tabs
            if (TxtEquipmentPendingCount != null)
                TxtEquipmentPendingCount.Text = $"{_approvals.Count(a => a.TabType == "기자재")}건";
            
            if (TxtRentedEquipmentCount != null)
                TxtRentedEquipmentCount.Text = $"{_rentals.Count}개";
            
            if (TxtAvailableEquipmentCount != null)
                TxtAvailableEquipmentCount.Text = $"{24 - _rentals.Count}개";

            if (TxtCabinetPendingCount != null)
                TxtCabinetPendingCount.Text = $"{_approvals.Count(a => a.TabType == "캐비닛")}건";

            if (TxtRentedCabinetCount != null)
                TxtRentedCabinetCount.Text = "5개";

            if (TxtAvailableCabinetCount != null)
                TxtAvailableCabinetCount.Text = "43개";

            if (TxtSangsangLabPendingCount != null)
                TxtSangsangLabPendingCount.Text = $"{_approvals.Count(a => a.TabType == "상상Lab")}건";
        }

        // ================= NAVIGATION =================
        private void ResetSidebarButtons()
        {
            BtnDashboard.Style = (Style)FindResource("SidebarBtn");
            BtnEquipment.Style = (Style)FindResource("SidebarBtn");
            BtnCabinet.Style = (Style)FindResource("SidebarBtn");
            BtnSangsangLab.Style = (Style)FindResource("SidebarBtn");
            BtnDataManage.Style = (Style)FindResource("SidebarBtn");
            BtnSettings.Style = (Style)FindResource("SidebarBtn");

            TabDashboard.Visibility = Visibility.Collapsed;
            TabEquipment.Visibility = Visibility.Collapsed;
            TabCabinet.Visibility = Visibility.Collapsed;
            TabSangsangLab.Visibility = Visibility.Collapsed;
            TabDataManage.Visibility = Visibility.Collapsed;
            TabSettings.Visibility = Visibility.Collapsed;
        }

        private void SwitchTab(Button btn, Grid tabGrid, string title)
        {
            ResetSidebarButtons();
            btn.Style = (Style)FindResource("SidebarBtnActive");
            tabGrid.Visibility = Visibility.Visible;
            TxtHeaderTitle.Text = title;

            // Only show Header alerts on Dashboard tab
            HeaderAlertArea.Visibility = (tabGrid == TabDashboard) ? Visibility.Visible : Visibility.Collapsed;

            // Refresh data-grids if needed
            if (tabGrid == TabEquipment)
            {
                GridEquipmentApprovals.ItemsSource = null;
                GridEquipmentApprovals.ItemsSource = _approvals.Where(a => a.TabType == "기자재" || a.TabType == "기자재 대여").ToList(); // Empty skeleton list or simulated approvals
                if (!_approvals.Any(a => a.TabType == "기자재"))
                {
                    // Seed some dummy equipment approvals matching Screenshot 2 (3 pending requests)
                    var equipReqs = new List<ApprovalRequest>
                    {
                        new ApprovalRequest { StudentName = "강민호", StudentId = "20251142", Department = "컴퓨터공학과", Advisor = "김동욱 교수", TabType = "기자재", RequestDate = _currentSimulatedDate.AddHours(-2) },
                        new ApprovalRequest { StudentName = "윤하은", StudentId = "20231509", Department = "정보보안학과", Advisor = "이지현 교수", TabType = "기자재", RequestDate = _currentSimulatedDate.AddHours(-2) },
                        new ApprovalRequest { StudentName = "정승우", StudentId = "20221199", Department = "게임공학과", Advisor = "최준호 교수", TabType = "기자재", RequestDate = _currentSimulatedDate.AddHours(-2) }
                    };
                    foreach (var req in equipReqs)
                    {
                        if (!_approvals.Any(a => a.StudentId == req.StudentId && a.TabType == "기자재"))
                            _approvals.Add(req);
                    }
                }
                GridEquipmentApprovals.ItemsSource = _approvals.Where(a => a.TabType == "기자재").ToList();

                GridRentals.ItemsSource = null;
                GridRentals.ItemsSource = _rentals;

                TxtAvailableEquipmentCount.Text = "24개";
                TxtRentedEquipmentCount.Text = $"{_rentals.Count}개";
                TxtEquipmentPendingCount.Text = $"{_approvals.Count(a => a.TabType == "기자재")}건";
            }
            else if (tabGrid == TabCabinet)
            {
                GridCabinetApprovals.ItemsSource = null;
                GridCabinetApprovals.ItemsSource = _approvals.Where(a => a.TabType == "캐비닛").ToList();
                TxtCabinetPendingCount.Text = $"{_approvals.Count(a => a.TabType == "캐비닛")}건";
                
                RenderCabinetGrid();
                
                // Set mock counts
                TxtAvailableCabinetCount.Text = "43개";
                TxtRentedCabinetCount.Text = "5개";
            }
            else if (tabGrid == TabSangsangLab)
            {
                LstSangsangLabCards.ItemsSource = null;
                LstSangsangLabCards.ItemsSource = _approvals.Where(a => a.TabType == "상상Lab").ToList();
                TxtSangsangLabPendingCount.Text = $"{_approvals.Count(a => a.TabType == "상상Lab")}건";
            }
            else if (tabGrid == TabDataManage)
            {
                GridMasterStudents.ItemsSource = null;
                GridMasterStudents.ItemsSource = _masterStudents;
            }

            UpdateAlertBadges();
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e) => SwitchTab(BtnDashboard, TabDashboard, "대시보드");
        private void BtnEquipment_Click(object sender, RoutedEventArgs e) => SwitchTab(BtnEquipment, TabEquipment, "기자재 현황");
        private void BtnCabinet_Click(object sender, RoutedEventArgs e) => SwitchTab(BtnCabinet, TabCabinet, "캐비닛 현황");
        private void BtnSangsangLab_Click(object sender, RoutedEventArgs e) => SwitchTab(BtnSangsangLab, TabSangsangLab, "상상Lab 승인");
        private void BtnDataManage_Click(object sender, RoutedEventArgs e) => SwitchTab(BtnDataManage, TabDataManage, "데이터 관리");
        private void BtnSettings_Click(object sender, RoutedEventArgs e) => SwitchTab(BtnSettings, TabSettings, "설정");

        private void AlertSangsangLab_MouseDown(object sender, MouseButtonEventArgs e) => SwitchTab(BtnSangsangLab, TabSangsangLab, "상상Lab 승인");
        private void AlertCabinet_MouseDown(object sender, MouseButtonEventArgs e) => SwitchTab(BtnCabinet, TabCabinet, "캐비닛 현황");
        private void AlertEquipment_MouseDown(object sender, MouseButtonEventArgs e) => SwitchTab(BtnEquipment, TabEquipment, "기자재 현황");

        // ================= SEAT GRID CONSTRUCTOR =================
        private void LoadDashboardLayout()
        {
            string year = ComboSearchYear.SelectedItem as string ?? _currentSimulatedDate.Year.ToString();
            string semester = (ComboSearchSemester.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1학기";
            string key = $"{year}_{semester}";

            if (!_seatLayoutCache.ContainsKey(key))
            {
                // Initialize default seating layout
                var seats = new List<Seat>();
                for (int i = 1; i <= 52; i++)
                {
                    seats.Add(new Seat { SeatNumber = i });
                }

                // Setup specific pillars (Row 3, Col 6, ColSpan 2 -> represents Pillar)
                seats[21].IsPillar = true; // Spot where pillar is located instead of Seat 22

                // Pre-populate students in specific seats using copies of master database
                int studentIdx = 0;
                // Specific seats mapped in screenshot
                int[] assignedSeatNumbers = {
                    13, 14, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 36, 37, 38,
                    43, 44, 45, 46, 47, 48, 49, 50, 51, 52
                };

                foreach (int seatNum in assignedSeatNumbers)
                {
                    if (studentIdx < _masterStudents.Count)
                    {
                        // Copy student (Deep copy) so deleting from dashboard won't affect master
                        var seat = seats[seatNum - 1];
                        seat.Student = _masterStudents[studentIdx++].Clone();
                        
                        // Graduate student's seat is always fixed
                        if (seat.Student != null && seat.Student.Department.Contains("대학원"))
                        {
                            seat.IsFixed = true;
                        }
                    }
                }

                _seatLayoutCache[key] = seats;
            }

            _activeSeats = _seatLayoutCache[key];
            RenderSeatGrid();
        }

        private void RenderSeatGrid()
        {
            SeatGridContainer.Children.Clear();
            SeatGridContainer.RowDefinitions.Clear();
            SeatGridContainer.ColumnDefinitions.Clear();

            // Columns definition
            // Col 0, 1, 2 (Group 1), Col 3 (Aisle), Col 4, 5, 6, 7 (Group 2)
            double[] colWidths = { 1.0, 1.0, 1.0, 0.4, 1.0, 1.0, 1.0, 1.0 };
            for (int i = 0; i < colWidths.Length; i++)
            {
                SeatGridContainer.ColumnDefinitions.Add(new ColumnDefinition 
                { 
                    Width = new GridLength(colWidths[i], GridUnitType.Star) 
                });
            }

            // Rows definition: 7 main rows + 2 bottom rows (gray background)
            for (int i = 0; i < 9; i++)
            {
                SeatGridContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
            }

            // Add seat UI elements
            // Let's create an explicit layout positioning array
            var positions = new List<(int SeatNum, int Row, int Col, int ColSpan, bool IsGray)>();

            // Row 0
            positions.Add((1, 0, 0, 1, false));
            positions.Add((2, 0, 1, 1, false));
            positions.Add((3, 0, 2, 1, false));
            positions.Add((4, 0, 4, 1, false));
            positions.Add((5, 0, 5, 1, false));
            positions.Add((6, 0, 6, 1, false));

            // Row 1
            positions.Add((7, 1, 0, 1, false));
            positions.Add((8, 1, 1, 1, false));
            positions.Add((9, 1, 2, 1, false));
            positions.Add((10, 1, 4, 1, false));
            positions.Add((11, 1, 5, 1, false));
            positions.Add((12, 1, 6, 1, false));

            // Row 2
            positions.Add((13, 2, 0, 1, false));
            positions.Add((14, 2, 1, 1, false));
            positions.Add((15, 2, 2, 1, false));
            positions.Add((16, 2, 4, 1, false));
            positions.Add((17, 2, 5, 1, false));
            positions.Add((18, 2, 6, 1, false));

            // Row 3
            positions.Add((19, 3, 0, 1, false));
            positions.Add((20, 3, 1, 1, false));
            positions.Add((21, 3, 2, 1, false));
            positions.Add((22, 3, 4, 1, false));
            positions.Add((23, 3, 5, 1, false));
            positions.Add((-1, 3, 6, 2, false)); // Pillar (기둥)

            // Row 4
            positions.Add((24, 4, 0, 1, false));
            positions.Add((25, 4, 1, 1, false));
            positions.Add((26, 4, 2, 1, false));
            positions.Add((27, 4, 4, 1, false));
            positions.Add((28, 4, 5, 1, false));

            // Row 5
            positions.Add((29, 5, 0, 1, false));
            positions.Add((30, 5, 1, 1, false));
            positions.Add((31, 5, 2, 1, false));
            positions.Add((32, 5, 4, 1, false));
            positions.Add((33, 5, 5, 1, false));
            positions.Add((34, 5, 6, 1, false));
            positions.Add((35, 5, 7, 1, false));

            // Row 6
            positions.Add((36, 6, 0, 1, false));
            positions.Add((37, 6, 1, 1, false));
            positions.Add((38, 6, 2, 1, false));
            positions.Add((39, 6, 4, 1, false));
            positions.Add((40, 6, 5, 1, false));
            positions.Add((41, 6, 6, 1, false));
            positions.Add((42, 6, 7, 1, false));

            // Row 7 (Bottom Row 1, Gray Background)
            positions.Add((43, 7, 0, 1, true));
            positions.Add((44, 7, 1, 1, true));
            positions.Add((45, 7, 4, 1, true));
            positions.Add((46, 7, 5, 1, true));
            positions.Add((47, 7, 6, 1, true));

            // Row 8 (Bottom Row 2, Gray Background)
            positions.Add((48, 8, 0, 1, true));
            positions.Add((49, 8, 1, 1, true));
            positions.Add((50, 8, 4, 1, true));
            positions.Add((51, 8, 5, 1, true));
            positions.Add((52, 8, 6, 1, true));

            foreach (var pos in positions)
            {
                if (pos.SeatNum == -1) // Pillar
                {
                    Border pillarBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(229, 230, 235)),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(3),
                        CornerRadius = new CornerRadius(4)
                    };
                    TextBlock pText = new TextBlock
                    {
                        Text = "기둥",
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    pillarBorder.Child = pText;
                    Grid.SetRow(pillarBorder, pos.Row);
                    Grid.SetColumn(pillarBorder, pos.Col);
                    Grid.SetColumnSpan(pillarBorder, pos.ColSpan);
                    SeatGridContainer.Children.Add(pillarBorder);
                    continue;
                }

                Seat seat = _activeSeats[pos.SeatNum - 1];

                // Outer Card Border
                Border seatCard = new Border
                {
                    Background = pos.IsGray ? new SolidColorBrush(Color.FromRgb(209, 213, 219)) : Brushes.White,
                    BorderThickness = new Thickness(1.5),
                    Margin = new Thickness(3),
                    CornerRadius = new CornerRadius(4),
                    Cursor = Cursors.Hand,
                    Tag = seat
                };

                // Border Color styling depending on state
                bool isFixed = seat.IsFixed || (seat.Student != null && seat.Student.Department.Contains("대학원"));
                if (seat.IsSelected)
                {
                    seatCard.BorderBrush = Brushes.Yellow; // Select mode highlight
                    seatCard.Background = new SolidColorBrush(Color.FromRgb(254, 249, 195)); // Soft yellow
                }
                else if (isFixed)
                {
                    seatCard.BorderBrush = new SolidColorBrush(Color.FromRgb(249, 115, 22)); // Orange border for fixed
                }
                else
                {
                    seatCard.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
                }

                // Grid inside Card
                Grid cardGrid = new Grid();
                cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
                cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

                // Seat Number & Lock Status Label
                StackPanel topStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 2, 0, 0) };
                TextBlock numTxt = new TextBlock
                {
                    Text = pos.SeatNum.ToString(),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
                };
                topStack.Children.Add(numTxt);

                if (isFixed)
                {
                    TextBlock lockTxt = new TextBlock
                    {
                        Text = " 🔒",
                        FontSize = 9,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    topStack.Children.Add(lockTxt);
                }
                Grid.SetRow(topStack, 0);
                cardGrid.Children.Add(topStack);

                // Student Details (StudentID & Name)
                if (seat.Student != null)
                {
                    StackPanel studentStack = new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    TextBlock idTxt = new TextBlock
                    {
                        Text = seat.Student.StudentId,
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    TextBlock nameTxt = new TextBlock
                    {
                        Text = seat.Student.Name,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    studentStack.Children.Add(idTxt);
                    studentStack.Children.Add(nameTxt);
                    Grid.SetRow(studentStack, 1);
                    cardGrid.Children.Add(studentStack);
                }

                seatCard.Child = cardGrid;
                seatCard.MouseDown += SeatCard_MouseDown;

                Grid.SetRow(seatCard, pos.Row);
                Grid.SetColumn(seatCard, pos.Col);
                Grid.SetColumnSpan(seatCard, pos.ColSpan);
                SeatGridContainer.Children.Add(seatCard);
            }
        }

        private void SeatCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Seat seat)
            {
                if (_isSeatFixMode)
                {
                    // Toggle selection for fixing
                    seat.IsSelected = !seat.IsSelected;
                    RenderSeatGrid();
                }
                else if (_isSeatDeleteMode)
                {
                    // Toggle selection for deletion
                    seat.IsSelected = !seat.IsSelected;
                    RenderSeatGrid();

                    // Show Delete Selected button if any are selected
                    bool anySelected = _activeSeats.Any(s => s.IsSelected);
                    BtnDeleteSelected.Visibility = anySelected ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    // Regular Mode: Show student details or add student
                    ShowStudentDetailsModal(seat);
                }
            }
        }

        private void ShowStudentDetailsModal(Seat seat)
        {
            _currentEditingSeat = seat;
            TxtModalSeatNum.Text = $"좌석 {seat.SeatNumber}";

            if (seat.Student != null)
            {
                TxtModalSeatNum.Text += " 상세 정보";
                TxtModalDept.Text = seat.Student.Department;
                TxtModalName.Text = seat.Student.Name;
                TxtModalId.Text = seat.Student.StudentId;
                TxtModalAdvisor.Text = seat.Student.Advisor;
                TxtModalEmail.Text = seat.Student.Email;
                ItemsModalAttendance.ItemsSource = seat.Student.Attendance;
                BtnEditStudentModal.Visibility = Visibility.Visible;
                BtnEditStudentInfo.Visibility = Visibility.Visible;
            }
            else
            {
                TxtModalSeatNum.Text += " (학생 정보 추가)";
                TxtModalDept.Text = "";
                TxtModalName.Text = "";
                TxtModalId.Text = "";
                TxtModalAdvisor.Text = "";
                TxtModalEmail.Text = "";
                ItemsModalAttendance.ItemsSource = new List<AttendanceRecord>();
                BtnEditStudentModal.Visibility = Visibility.Collapsed;
                BtnEditStudentInfo.Visibility = Visibility.Collapsed;
            }

            SetModalEditMode(false);
            ModalSeatDetails.Visibility = Visibility.Visible;
        }

        private void SetModalEditMode(bool enable)
        {
            _isModalEditing = enable;
            TxtModalName.IsReadOnly = !enable;
            TxtModalDept.IsReadOnly = !enable;
            TxtModalAdvisor.IsReadOnly = !enable;
            TxtModalEmail.IsReadOnly = !enable;

            if (enable)
            {
                TxtModalName.Background = Brushes.White; TxtModalName.BorderThickness = new Thickness(1);
                TxtModalDept.Background = Brushes.White; TxtModalDept.BorderThickness = new Thickness(1);
                TxtModalAdvisor.Background = Brushes.White; TxtModalAdvisor.BorderThickness = new Thickness(1);
                TxtModalEmail.Background = Brushes.White; TxtModalEmail.BorderThickness = new Thickness(1);
                BtnEditStudentModal.Content = "저장";
                BtnEditStudentInfo.Content = "저장";
            }
            else
            {
                TxtModalName.Background = Brushes.Transparent; TxtModalName.BorderThickness = new Thickness(0);
                TxtModalDept.Background = Brushes.Transparent; TxtModalDept.BorderThickness = new Thickness(0);
                TxtModalAdvisor.Background = Brushes.Transparent; TxtModalAdvisor.BorderThickness = new Thickness(0);
                TxtModalEmail.Background = Brushes.Transparent; TxtModalEmail.BorderThickness = new Thickness(0);
                BtnEditStudentModal.Content = "수정";
                BtnEditStudentInfo.Content = "정보 수정";
            }
        }

        private void BtnCloseModal_Click(object sender, RoutedEventArgs e)
        {
            ModalSeatDetails.Visibility = Visibility.Collapsed;
        }

        // ================= SEARCH BY YEAR & SEMESTER =================
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboardLayout();
        }

        // ================= SEAT FIX MODE =================
        private void BtnSeatFixMode_Click(object sender, RoutedEventArgs e)
        {
            if (_isSeatDeleteMode)
            {
                // Deactivate delete mode
                _isSeatDeleteMode = false;
                BtnDeleteSelected.Visibility = Visibility.Collapsed;
                BtnSeatDeleteMode.Content = "좌석 데이터 삭제";
            }

            if (!_isSeatFixMode)
            {
                // Enter Fix Mode
                _isSeatFixMode = true;
                BtnSeatFixMode.Content = "💾 고정 저장/종료";
                BtnSeatFixMode.Background = new SolidColorBrush(Color.FromRgb(254, 240, 138)); // Yellow indicator
                // Clear any selection state
                foreach (var s in _activeSeats) s.IsSelected = false;
            }
            else
            {
                // Exit Fix Mode and Save Fixed Seats
                _isSeatFixMode = false;
                BtnSeatFixMode.Content = "좌석 고정 모드";
                BtnSeatFixMode.Background = Brushes.White;

                foreach (var s in _activeSeats)
                {
                    if (s.IsSelected)
                    {
                        if (IsGraduateSeat(s))
                        {
                            s.IsSelected = false;
                            continue;
                        }
                        s.IsFixed = !s.IsFixed;
                        s.IsSelected = false;
                    }
                }
            }
            RenderSeatGrid();
        }

        private bool IsGraduateSeat(Seat seat)
        {
            return seat.Student != null && seat.Student.Department.Contains("대학원");
        }

        // ================= RANDOM ALLOCATION =================
        private void BtnRandomAllocation_Click(object sender, RoutedEventArgs e)
        {
            var positions = GetSeatLayoutCoordinates();

            // Find existing students currently assigned to seats
            var currentStudentIds = _activeSeats.Where(s => s.Student != null).Select(s => s.Student.StudentId).ToHashSet();
            // Find newly added students not in the active seats list
            var newStudents = _masterStudents.Where(m => !currentStudentIds.Contains(m.StudentId)).ToList();

            if (newStudents.Count > 0)
            {
                // Only place new students randomly into empty seats (filling from back)
                var emptySeats = _activeSeats.Where(s => s.Student == null && !s.IsPillar).OrderBy(s => s.SeatNumber).ToList();
                if (emptySeats.Count < newStudents.Count)
                {
                    MessageBox.Show("빈 좌석이 부족하여 추가 학생을 배치할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get target empty seats starting from the back
                var targetSeats = emptySeats.Skip(Math.Max(0, emptySeats.Count - newStudents.Count)).ToList();
                Random r = new Random();
                var shuffledNew = newStudents.OrderBy(x => r.Next()).ToList();

                for (int i = 0; i < shuffledNew.Count && i < targetSeats.Count; i++)
                {
                    targetSeats[i].Student = shuffledNew[i].Clone();
                    
                    // Graduate student check for new assignments
                    if (targetSeats[i].Student.Department.Contains("대학원"))
                    {
                        targetSeats[i].IsFixed = true;
                    }
                }

                RenderSeatGrid();
                UpdateAlertBadges();
                MessageBox.Show($"신규 등록 학생 {shuffledNew.Count}명이 빈 자리(뒷자리 우선)에 랜덤 배치되었습니다.", "신규 배치 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Otherwise, shuffle all un-fixed, non-pillar, and non-graduate seats, filling from the back
            var unFixedSeats = _activeSeats.Where(s => !s.IsFixed && !s.IsPillar && !IsGraduateSeat(s)).OrderBy(s => s.SeatNumber).ToList();
            if (unFixedSeats.Count <= 1)
            {
                MessageBox.Show("고정되지 않은 좌석이 부족하여 배정할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Extract student objects
            var studentPool = unFixedSeats.Where(s => s.Student != null).Select(s => s.Student!).ToList();

            // Clear students from un-fixed seats temporarily
            foreach (var s in unFixedSeats)
            {
                s.Student = null;
            }

            // Select target seats starting from back (fill from back)
            var targetSeatsToFill = unFixedSeats.Skip(Math.Max(0, unFixedSeats.Count - studentPool.Count)).ToList();

            // Shuffle with constraint
            Random rand = new Random();
            bool success = false;
            List<StudentInfo> shuffledPool = new List<StudentInfo>(studentPool);

            // Backtracking/Retry solver
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                // Shuffle pool
                shuffledPool = studentPool.OrderBy(x => rand.Next()).ToList();
                bool violation = false;

                // Check constraints
                for (int i = 0; i < shuffledPool.Count; i++)
                {
                    var student = shuffledPool[i];
                    var newSeatNum = targetSeatsToFill[i].SeatNumber;

                    // Find original seat of this student
                    var oldSeatNum = _activeSeats.FindIndex(s => s.Student?.StudentId == student.StudentId) + 1;
                    if (oldSeatNum > 0)
                    {
                        var oldCoord = positions.FirstOrDefault(p => p.SeatNum == oldSeatNum);
                        var newCoord = positions.FirstOrDefault(p => p.SeatNum == newSeatNum);

                        if (oldCoord.SeatNum != 0 && newCoord.SeatNum != 0)
                        {
                            if (oldCoord.Row == newCoord.Row)
                            {
                                if (Math.Abs(oldCoord.Col - newCoord.Col) < 2)
                                {
                                    violation = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!violation)
                {
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                MessageBox.Show("수평 거리 제약조건(2칸 이상)을 맞출 수 없어 기본 랜덤 배정으로 진행합니다.", "참고", MessageBoxButton.OK, MessageBoxImage.Information);
                shuffledPool = studentPool.OrderBy(x => rand.Next()).ToList();
            }

            // Assign shuffled to target seats
            for (int i = 0; i < targetSeatsToFill.Count; i++)
            {
                if (i < shuffledPool.Count)
                {
                    targetSeatsToFill[i].Student = shuffledPool[i];
                }
            }

            RenderSeatGrid();
        }

        private List<(int SeatNum, int Row, int Col)> GetSeatLayoutCoordinates()
        {
            var coords = new List<(int SeatNum, int Row, int Col)>();
            // Matches RenderSeatGrid mapping
            coords.Add((1, 0, 0)); coords.Add((2, 0, 1)); coords.Add((3, 0, 2)); coords.Add((4, 0, 4)); coords.Add((5, 0, 5)); coords.Add((6, 0, 6));
            coords.Add((7, 1, 0)); coords.Add((8, 1, 1)); coords.Add((9, 1, 2)); coords.Add((10, 1, 4)); coords.Add((11, 1, 5)); coords.Add((12, 1, 6));
            coords.Add((13, 2, 0)); coords.Add((14, 2, 1)); coords.Add((15, 2, 2)); coords.Add((16, 2, 4)); coords.Add((17, 2, 5)); coords.Add((18, 2, 6));
            coords.Add((19, 3, 0)); coords.Add((20, 3, 1)); coords.Add((21, 3, 2)); coords.Add((22, 3, 4)); coords.Add((23, 3, 5));
            coords.Add((24, 4, 0)); coords.Add((25, 4, 1)); coords.Add((26, 4, 2)); coords.Add((27, 4, 4)); coords.Add((28, 4, 5));
            coords.Add((29, 5, 0)); coords.Add((30, 5, 1)); coords.Add((31, 5, 2)); coords.Add((32, 5, 4)); coords.Add((33, 5, 5)); coords.Add((34, 5, 6)); coords.Add((35, 5, 7));
            coords.Add((36, 6, 0)); coords.Add((37, 6, 1)); coords.Add((38, 6, 2)); coords.Add((39, 6, 4)); coords.Add((40, 6, 5)); coords.Add((41, 6, 6)); coords.Add((42, 6, 7));
            
            // Bottom rows
            coords.Add((43, 7, 0)); coords.Add((44, 7, 1)); coords.Add((45, 7, 4)); coords.Add((46, 7, 5)); coords.Add((47, 7, 6));
            coords.Add((48, 8, 0)); coords.Add((49, 8, 1)); coords.Add((50, 8, 4)); coords.Add((51, 8, 5)); coords.Add((52, 8, 6));

            return coords;
        }

        // ================= DELETE SEATS =================
        private void BtnSeatDeleteMode_Click(object sender, RoutedEventArgs e)
        {
            if (_isSeatFixMode)
            {
                _isSeatFixMode = false;
                BtnSeatFixMode.Content = "좌석 고정 모드";
                BtnSeatFixMode.Background = Brushes.White;
            }

            // Create context menu to choose between selection delete and all delete
            ContextMenu menu = new ContextMenu();
            MenuItem m1 = new MenuItem { Header = "선택 삭제 모드 토글" };
            m1.Click += (s, ev) => ToggleSeatDeleteMode();
            
            MenuItem m2 = new MenuItem { Header = "전체 삭제" };
            m2.Click += (s, ev) => DeleteAllSeats();

            menu.Items.Add(m1);
            menu.Items.Add(m2);

            BtnSeatDeleteMode.ContextMenu = menu;
            BtnSeatDeleteMode.ContextMenu.IsOpen = true;
        }

        private void ToggleSeatDeleteMode()
        {
            if (!_isSeatDeleteMode)
            {
                _isSeatDeleteMode = true;
                BtnSeatDeleteMode.Content = "💾 선택 삭제 모드 활성 중";
                foreach (var s in _activeSeats) s.IsSelected = false;
            }
            else
            {
                _isSeatDeleteMode = false;
                BtnSeatDeleteMode.Content = "좌석 데이터 삭제";
                BtnDeleteSelected.Visibility = Visibility.Collapsed;
                foreach (var s in _activeSeats) s.IsSelected = false;
            }
            RenderSeatGrid();
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("선택한 좌석들의 인적사항과 출석 데이터를 삭제하시겠습니까?", "확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                foreach (var s in _activeSeats)
                {
                    if (s.IsSelected)
                    {
                        s.Student = null;
                        s.IsSelected = false;
                        s.IsFixed = false;
                    }
                }
                _isSeatDeleteMode = false;
                BtnSeatDeleteMode.Content = "좌석 데이터 삭제";
                BtnDeleteSelected.Visibility = Visibility.Collapsed;
                RenderSeatGrid();
            }
        }

        private void DeleteAllSeats()
        {
            MessageBoxResult result = MessageBox.Show("모든 좌석의 인적 사항과 출석 데이터를 삭제하시겠습니까? (좌석 번호와 고정상태는 남음)", "경고", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                foreach (var s in _activeSeats)
                {
                    s.Student = null;
                    s.IsSelected = false;
                }
                RenderSeatGrid();
            }
        }

        // ================= MEMO HANDLERS =================
        private void BtnAddMemo_Click(object sender, RoutedEventArgs e)
        {
            if (_editingMemo != null)
            {
                if (!string.IsNullOrWhiteSpace(TxtNewMemo.Text))
                {
                    _editingMemo.Content = TxtNewMemo.Text.Trim();
                }
                _editingMemo = null;
                _originalMemoContent = null;
                TxtNewMemo.Clear();
                BtnAddMemo.Content = "+ 메모 추가";
                LstMemos.ItemsSource = null;
                LstMemos.ItemsSource = _memos;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(TxtNewMemo.Text))
                {
                    _memos.Add(new MemoItem { Content = TxtNewMemo.Text.Trim() });
                    TxtNewMemo.Clear();
                }
            }
        }

        private void BtnDeleteMemo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is MemoItem memo)
            {
                _memos.Remove(memo);
            }
        }

        private void BtnEditMemo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is MemoItem memo)
            {
                _editingMemo = memo;
                _originalMemoContent = memo.Content;
                TxtNewMemo.Text = memo.Content;
                BtnAddMemo.Content = "편집 완료";
            }
        }

        // ================= REFRESH BUTTON =================
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("데이터 관리에서 학생 정보를 새로고침하시겠습니까?", "새로고침 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Clear current seats and reload from master database
                foreach (var seat in _activeSeats)
                {
                    if (seat.Student != null && !seat.IsFixed)
                    {
                        seat.Student = null;
                    }
                }

                // Re-populate from master database (copy, not reference)
                int studentIdx = 0;
                int[] assignedSeatNumbers = {
                    13, 14, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 36, 37, 38,
                    43, 44, 45, 46, 47, 48, 49, 50, 51, 52
                };

                foreach (int seatNum in assignedSeatNumbers)
                {
                    if (studentIdx < _masterStudents.Count && seatNum <= 42)
                    {
                        var seat = _activeSeats[seatNum - 1];
                        if (!seat.IsFixed || seat.Student == null)
                        {
                            seat.Student = _masterStudents[studentIdx++].Clone();
                        }
                    }
                }

                RenderSeatGrid();
                MessageBox.Show("데이터가 새로고침되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ================= STUDENT MODAL EDIT =================
        private void HandleModalEditSave()
        {
            if (_currentEditingSeat == null) return;

            if (!_isModalEditing)
            {
                SetModalEditMode(true);
            }
            else
            {
                if (_currentEditingSeat.Student == null)
                {
                    _currentEditingSeat.Student = new StudentInfo
                    {
                        StudentId = "2026" + new Random().Next(1000, 9999).ToString()
                    };
                }

                _currentEditingSeat.Student.Name = TxtModalName.Text;
                _currentEditingSeat.Student.Department = TxtModalDept.Text;
                _currentEditingSeat.Student.Advisor = TxtModalAdvisor.Text;
                _currentEditingSeat.Student.Email = TxtModalEmail.Text;

                // Graduate check
                if (_currentEditingSeat.Student.Department.Contains("대학원"))
                {
                    _currentEditingSeat.IsFixed = true;
                }

                var master = _masterStudents.FirstOrDefault(m => m.StudentId == _currentEditingSeat.Student.StudentId);
                if (master != null)
                {
                    master.Name = TxtModalName.Text;
                    master.Department = TxtModalDept.Text;
                    master.Advisor = TxtModalAdvisor.Text;
                    master.Email = TxtModalEmail.Text;
                }
                else
                {
                    _masterStudents.Add(_currentEditingSeat.Student.Clone());
                }

                SetModalEditMode(false);
                RenderSeatGrid();
                UpdateAlertBadges();
                MessageBox.Show("학생 정보가 수정되었으며 즉시 대시보드에 데이터가 업데이트되었습니다.", "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEditStudentModal_Click(object sender, RoutedEventArgs e) => HandleModalEditSave();
        private void BtnEditStudentInfo_Click(object sender, RoutedEventArgs e) => HandleModalEditSave();

        // ================= EQUIPMENT TAB (RENTALS / STATUS LIGHTS) =================
        private void BtnReturnEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RentalItem rental)
            {
                MessageBoxResult res = MessageBox.Show($"{rental.StudentName}의 '{rental.EquipmentType}' 반납 처리를 하시겠습니까?", "반납 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    rental.IsReturned = true;
                    _rentals.Remove(rental);
                    
                    // Push to undo LIFO stack
                    _rentalUndoStack.Push(rental);

                    UpdateAlertBadges();
                }
            }
        }

        private void BtnUndoReturn_Click(object sender, RoutedEventArgs e)
        {
            if (_rentalUndoStack.Count > 0)
            {
                var popped = _rentalUndoStack.Pop();
                popped.IsReturned = false;
                _rentals.Add(popped);
                UpdateAlertBadges();
            }
            else
            {
                MessageBox.Show("되돌릴 반납 내역이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ================= CABINET & SANGSANGLAB APPROVAL HANDLERS =================
        private void BtnApproveRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ApprovalRequest req)
            {
                req.Status = "승인 완료";
                _approvals.Remove(req);

                // Add student to dashboard if they are approved
                // Let's find an empty seat
                var emptySeat = _activeSeats.FirstOrDefault(s => s.Student == null && !s.IsPillar);
                if (emptySeat != null)
                {
                    emptySeat.Student = new StudentInfo
                    {
                        StudentId = req.StudentId,
                        Name = req.StudentName,
                        Department = req.Department,
                        Advisor = req.Advisor,
                        Email = req.Email
                    };
                    emptySeat.Student.Attendance.Add(new AttendanceRecord { DateString = _currentSimulatedDate.ToString("yyyy-MM-dd"), Status = "출석" });
                    
                    if (emptySeat.Student.Department.Contains("대학원"))
                    {
                        emptySeat.IsFixed = true;
                    }
                }

                // If cabinet request is approved, assign an empty cabinet block
                if (req.TabType == "캐비닛")
                {
                    int emptyCabinetNumber = Enumerable.Range(1, 48).FirstOrDefault(n => !_cabinetAllocations.ContainsKey(n));
                    if (emptyCabinetNumber > 0)
                    {
                        var student = new StudentInfo
                        {
                            StudentId = req.StudentId,
                            Name = req.StudentName,
                            Department = req.Department,
                            Advisor = req.Advisor,
                            Email = req.Email
                        };
                        _cabinetAllocations[emptyCabinetNumber] = (student, $"{_currentSimulatedDate:MM/dd}~{_currentSimulatedDate.AddMonths(1):MM/dd}");
                    }
                }

                // Refresh tab binding
                SwitchTab(req.TabType == "상상Lab" ? BtnSangsangLab : BtnCabinet, req.TabType == "상상Lab" ? TabSangsangLab : TabCabinet, req.TabType == "상상Lab" ? "상상Lab 승인" : "캐비닛 현황");
                RenderSeatGrid();
            }
        }

        private void BtnRejectRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ApprovalRequest req)
            {
                req.Status = "반려";
                _approvals.Remove(req);

                SwitchTab(req.TabType == "상상Lab" ? BtnSangsangLab : BtnCabinet, req.TabType == "상상Lab" ? TabSangsangLab : TabCabinet, req.TabType == "상상Lab" ? "상상Lab 승인" : "캐비닛 현황");
            }
        }

        // ================= MASTER DATABASE HANDLERS =================
        private StudentInfo? _selectedMasterStudent;

        private void GridMasterStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridMasterStudents.SelectedItem is StudentInfo student)
            {
                _selectedMasterStudent = student;
                TxtMasterId.Text = student.StudentId;
                TxtMasterName.Text = student.Name;
                TxtMasterDept.Text = student.Department;
                TxtMasterAdvisor.Text = student.Advisor;
                TxtMasterEmail.Text = student.Email;
                PanelMasterEdit.IsEnabled = true;
            }
            else
            {
                PanelMasterEdit.IsEnabled = false;
            }
        }

        private void BtnSaveMaster_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMasterStudent != null)
            {
                string oldId = _selectedMasterStudent.StudentId;
                string newId = TxtMasterId.Text.Trim();

                if (string.IsNullOrEmpty(newId))
                {
                    MessageBox.Show("학번은 비워둘 수 없습니다.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (oldId != newId && _masterStudents.Any(m => m.StudentId == newId))
                {
                    MessageBox.Show("이미 존재하는 학번입니다.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedMasterStudent.StudentId = newId;
                _selectedMasterStudent.Name = TxtMasterName.Text;
                _selectedMasterStudent.Department = TxtMasterDept.Text;
                _selectedMasterStudent.Advisor = TxtMasterAdvisor.Text;
                _selectedMasterStudent.Email = TxtMasterEmail.Text;

                // Sync with active seats
                var seat = _activeSeats.FirstOrDefault(s => s.Student?.StudentId == oldId);
                if (seat != null && seat.Student != null)
                {
                    seat.Student.StudentId = newId;
                    seat.Student.Name = _selectedMasterStudent.Name;
                    seat.Student.Department = _selectedMasterStudent.Department;
                    seat.Student.Advisor = _selectedMasterStudent.Advisor;
                    seat.Student.Email = _selectedMasterStudent.Email;
                    
                    if (seat.Student.Department.Contains("대학원"))
                    {
                        seat.IsFixed = true;
                    }
                }

                GridMasterStudents.ItemsSource = null;
                GridMasterStudents.ItemsSource = _masterStudents;
                RenderSeatGrid();
                MessageBox.Show("마스터 데이터가 수정되었으며 즉시 대시보드에 데이터가 업데이트되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSyncData_Click(object sender, RoutedEventArgs e)
        {
            int syncCount = 0;
            foreach (var seat in _activeSeats)
            {
                if (seat.Student != null)
                {
                    var master = _masterStudents.FirstOrDefault(m => m.StudentId == seat.Student.StudentId);
                    if (master != null)
                    {
                        seat.Student.Name = master.Name;
                        seat.Student.Department = master.Department;
                        seat.Student.Advisor = master.Advisor;
                        seat.Student.Email = master.Email;
                        if (seat.Student.Department.Contains("대학원"))
                        {
                            seat.IsFixed = true;
                        }
                        syncCount++;
                    }
                }
            }
            RenderSeatGrid();
            UpdateAlertBadges();
            MessageBox.Show($"총 {syncCount}명의 학생 데이터가 마스터 데이터베이스와 동기화되었습니다.", "동기화 완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDeleteMaster_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMasterStudent != null)
            {
                _masterStudents.Remove(_selectedMasterStudent);
                GridMasterStudents.ItemsSource = null;
                GridMasterStudents.ItemsSource = _masterStudents;
                PanelMasterEdit.IsEnabled = false;
                MessageBox.Show("마스터 데이터가 삭제되었습니다. (기존 배치된 좌석 데이터는 유지됩니다.)", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ================= SIMULATE GOOGLE FORM SUBMISSIONS =================
        private void BtnSimulateFormSubmission_Click(object sender, RoutedEventArgs e)
        {
            Random r = new Random();
            string[] names = { "서강준", "윤아라", "이지훈", "박소담", "최현우" };
            string[] depts = { "컴퓨터공학과", "정보보안학과", "디지털콘텐츠학과" };
            string name = names[r.Next(names.Length)];
            string id = "2026" + r.Next(1000, 9999).ToString();
            string type = r.Next(2) == 0 ? "상상Lab" : "캐비닛";

            var req = new ApprovalRequest
            {
                StudentName = name,
                StudentId = id,
                Department = depts[r.Next(depts.Length)],
                Advisor = "김동욱 교수",
                Email = $"{name}@dsu.ac.kr",
                TabType = type,
                Status = "승인 대기"
            };

            _approvals.Add(req);
            UpdateAlertBadges();

            MessageBox.Show($"[구글폼 신청 접수]\n이름: {name}\n학번: {id}\n신청구분: {type}\n\n알림 뱃지가 업데이트 되었습니다.", "구글폼 신청 도착", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ================= NEW UI STUB EVENT HANDLERS & HELPERS =================

        private void BtnEquipmentDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("기자재 데이터 삭제 기능 구현용 이벤트 핸들러입니다.", "기자재 삭제", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnEquipmentFix_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("기자재 데이터 고정 기능 구현용 이벤트 핸들러입니다.", "기자재 고정", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnEquipmentExport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("기자재 데이터 추출 기능 구현용 이벤트 핸들러입니다.", "기자재 추출", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCabinetDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("캐비닛 데이터 삭제 기능 구현용 이벤트 핸들러입니다.", "캐비닛 삭제", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCabinetFix_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("캐비닛 데이터 고정 기능 구현용 이벤트 핸들러입니다.", "캐비닛 고정", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCabinetExport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("캐비닛 데이터 추출 기능 구현용 이벤트 핸들러입니다.", "캐비닛 추출", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnViewSangsangLabDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ApprovalRequest req)
            {
                // Create a temporary Seat model to match ShowStudentDetailsModal signature
                var dummySeat = new Seat
                {
                    SeatNumber = 0,
                    Student = new StudentInfo
                    {
                        StudentId = req.StudentId,
                        Name = req.StudentName,
                        Department = req.Department,
                        Advisor = req.Advisor,
                        Email = req.Email
                    }
                };
                ShowStudentDetailsModal(dummySeat);
            }
        }

        private void RenderCabinetGrid()
        {
            GridCabinetBlock1.Children.Clear();
            GridCabinetBlock2.Children.Clear();

            // Row 0: 1, 4, 7...
            // Row 1: 2, 5, 8...
            // Row 2: 3, 6, 9...
            int[] block1Numbers = new int[24];
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    int cabinetNum = (c * 3) + r + 1;
                    block1Numbers[r * 8 + c] = cabinetNum;
                }
            }

            foreach (int num in block1Numbers)
            {
                GridCabinetBlock1.Children.Add(CreateCabinetCell(num));
            }

            // Row 0: 25, 28, 31...
            int[] block2Numbers = new int[24];
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    int cabinetNum = (c * 3) + r + 25;
                    block2Numbers[r * 8 + c] = cabinetNum;
                }
            }

            foreach (int num in block2Numbers)
            {
                GridCabinetBlock2.Children.Add(CreateCabinetCell(num));
            }
        }

        private Border CreateCabinetCell(int number)
        {
            Border border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                Padding = new Thickness(5),
                Background = Brushes.White
            };

            StackPanel stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            TextBlock numTxt = new TextBlock
            {
                Text = number.ToString(),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(numTxt);

            string studentText = "학번 이름";
            string periodText = "사용기간";

            if (_cabinetAllocations.ContainsKey(number))
            {
                var alloc = _cabinetAllocations[number];
                studentText = $"{alloc.Student.StudentId} {alloc.Student.Name}";
                periodText = alloc.Period;
                border.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255));
            }

            border.Tag = number;
            border.Cursor = Cursors.Hand;
            border.MouseDown += CabinetCell_MouseDown;

            TextBlock studentTxt = new TextBlock
            {
                Text = studentText,
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };
            stack.Children.Add(studentTxt);

            TextBlock periodTxt = new TextBlock
            {
                Text = periodText,
                FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0)
            };
            stack.Children.Add(periodTxt);

            border.Child = stack;
            return border;
        }

        private void CabinetCell_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int number)
            {
                if (_cabinetAllocations.ContainsKey(number))
                {
                    var alloc = _cabinetAllocations[number];
                    MessageBox.Show($"[캐비닛 {number}번 상세 정보]\n\n" +
                                    $"학생 이름: {alloc.Student.Name}\n" +
                                    $"학번: {alloc.Student.StudentId}\n" +
                                    $"소속: {alloc.Student.Department}\n" +
                                    $"대여 기간: {alloc.Period}\n" +
                                    $"상태: 사용 중", "캐비닛 정보", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"[캐비닛 {number}번 상세 정보]\n\n상태: 사용 가능 (미배정)", "캐비닛 정보", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void PerformStudentSearch()
        {
            string query = TxtSearchStudent.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(query))
            {
                GridMasterStudents.ItemsSource = _masterStudents;
            }
            else
            {
                var filtered = _masterStudents.Where(s => 
                    s.StudentId.ToLower().Contains(query) ||
                    s.Name.ToLower().Contains(query) ||
                    s.Advisor.ToLower().Contains(query) ||
                    s.Department.ToLower().Contains(query)
                ).ToList();
                GridMasterStudents.ItemsSource = filtered;
            }
        }

        private void TxtSearchStudent_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformStudentSearch();
            }
        }

        private void BtnSearchStudent_Click(object sender, RoutedEventArgs e) => PerformStudentSearch();

        private void BtnClearStudentSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtSearchStudent.Clear();
            GridMasterStudents.ItemsSource = _masterStudents;
        }

        private void ComboResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboResolution == null) return;
            var selected = (ComboResolution.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selected == "기본 (1200 x 780)")
            {
                this.Width = 1200;
                this.Height = 780;
            }
            else if (selected == "소형 (1024 x 768)")
            {
                this.Width = 1024;
                this.Height = 768;
            }
            else if (selected == "중형 (1440 x 900)")
            {
                this.Width = 1440;
                this.Height = 900;
            }
            else if (selected == "대형 (1920 x 1080)")
            {
                this.Width = 1920;
                this.Height = 1080;
            }
        }
    }
}