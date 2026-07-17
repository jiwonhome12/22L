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

        // App Modes
        private bool _isSeatFixMode = false;
        private bool _isSeatDeleteMode = false;

        // Current simulated date (set according to user metadata to 2026-07-16)
        private DateTime _currentSimulatedDate = new DateTime(2026, 7, 16);

        public MainWindow()
        {
            InitializeComponent();
            
            // Set current date string
            TxtCurrentDate.Text = _currentSimulatedDate.ToString("yyyy-MM-dd (ddd)");

            // Populate Year dropdown dynamically
            InitializeYearDropdown();

            // Setup Demo Data
            SetupMasterDatabase();
            SetupCabinetAndSangsangLabDemoData();
            SetupRentalDemoData();
            SetupMemoDemoData();

            // Refresh UI Badges
            UpdateAlertBadges();

            // Load initial view
            LoadDashboardLayout();
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

            GridSangsangLabApprovals.ItemsSource = _approvals.Where(a => a.TabType == "상상Lab").ToList();
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
            // Emergency Overdue Alert calculation
            int overdueCount = _rentals.Count(r => !r.IsReturned && r.DueDate < _currentSimulatedDate);
            TxtEmergencyCount.Text = $"{overdueCount}건";

            // Pending approvals calculation
            int pendingCount = _approvals.Count(a => a.Status == "승인 대기");
            TxtPendingApprovalsCount.Text = $"{pendingCount}건";
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

            // Refresh data-grids if needed
            if (tabGrid == TabEquipment)
            {
                GridRentals.ItemsSource = null;
                GridRentals.ItemsSource = _rentals;
            }
            else if (tabGrid == TabCabinet)
            {
                GridCabinetApprovals.ItemsSource = null;
                GridCabinetApprovals.ItemsSource = _approvals.Where(a => a.TabType == "캐비닛").ToList();
            }
            else if (tabGrid == TabSangsangLab)
            {
                GridSangsangLabApprovals.ItemsSource = null;
                GridSangsangLabApprovals.ItemsSource = _approvals.Where(a => a.TabType == "상상Lab").ToList();
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

        private void AlertOverdue_MouseDown(object sender, MouseButtonEventArgs e) => SwitchTab(BtnEquipment, TabEquipment, "기자재 현황");
        private void AlertApprovals_MouseDown(object sender, MouseButtonEventArgs e) => SwitchTab(BtnSangsangLab, TabSangsangLab, "상상Lab 승인");

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
                        seats[seatNum - 1].Student = _masterStudents[studentIdx++].Clone();
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
                if (seat.IsSelected)
                {
                    seatCard.BorderBrush = Brushes.Yellow; // Select mode highlight
                    seatCard.Background = new SolidColorBrush(Color.FromRgb(254, 249, 195)); // Soft yellow
                }
                else if (seat.IsFixed)
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

                if (seat.IsFixed)
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
                    // Regular Mode: Show student details
                    if (seat.Student != null)
                    {
                        ShowStudentDetailsModal(seat);
                    }
                }
            }
        }

        private void ShowStudentDetailsModal(Seat seat)
        {
            if (seat.Student == null) return;

            TxtModalSeatNum.Text = $"좌석 {seat.SeatNumber} 상세 정보";
            TxtModalDept.Text = seat.Student.Department;
            TxtModalName.Text = seat.Student.Name;
            TxtModalId.Text = seat.Student.StudentId;
            TxtModalAdvisor.Text = seat.Student.Advisor;
            TxtModalEmail.Text = seat.Student.Email;

            ItemsModalAttendance.ItemsSource = seat.Student.Attendance;

            ModalSeatDetails.Visibility = Visibility.Visible;
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
                        s.IsFixed = true;
                        s.IsSelected = false;
                    }
                }
            }
            RenderSeatGrid();
        }

        // ================= RANDOM ALLOCATION =================
        private void BtnRandomAllocation_Click(object sender, RoutedEventArgs e)
        {
            // 1. Find all seats that are NOT fixed, NOT pillars, and have a student slot
            // To make sure positions can be maps, let's locate their Row and Col index
            // We can determine Row and Col from the layout positioning
            var positions = GetSeatLayoutCoordinates();

            // Filter un-fixed seats
            var unFixedSeats = _activeSeats.Where(s => !s.IsFixed && !s.IsPillar).ToList();
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

            // Shuffle with constraint
            Random rand = new Random();
            bool success = false;
            List<StudentInfo> bestAttempt = new List<StudentInfo>(studentPool);

            // Backtracking/Retry solver
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                // Shuffle pool
                var shuffledPool = studentPool.OrderBy(x => rand.Next()).ToList();
                bool violation = false;

                // Check constraints: for each seat index i, check if candidate student shuffledPool[i] violates rule
                // Rule: If new seat and original seat are in the same row, horizontal distance must be >= 2.
                for (int i = 0; i < shuffledPool.Count; i++)
                {
                    var student = shuffledPool[i];
                    var newSeatNum = unFixedSeats[i].SeatNumber;

                    // Find original seat of this student in the original list
                    var oldSeatNum = _activeSeats.FindIndex(s => s.Student?.StudentId == student.StudentId) + 1;
                    if (oldSeatNum > 0)
                    {
                        var oldCoord = positions.FirstOrDefault(p => p.SeatNum == oldSeatNum);
                        var newCoord = positions.FirstOrDefault(p => p.SeatNum == newSeatNum);

                        if (oldCoord.SeatNum != 0 && newCoord.SeatNum != 0)
                        {
                            // If same row, verify col diff
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
                    // Assign shuffled
                    for (int i = 0; i < unFixedSeats.Count; i++)
                    {
                        if (i < shuffledPool.Count)
                        {
                            unFixedSeats[i].Student = shuffledPool[i];
                        }
                    }
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                // If it fails due to high constraints, assign directly as fallback
                MessageBox.Show("수평 거리 제약조건(2칸 이상)을 맞출 수 없어 기본 랜덤 배정으로 진행합니다.", "참고", MessageBoxButton.OK, MessageBoxImage.Information);
                var shuffledPool = studentPool.OrderBy(x => rand.Next()).ToList();
                for (int i = 0; i < unFixedSeats.Count; i++)
                {
                    if (i < shuffledPool.Count)
                    {
                        unFixedSeats[i].Student = shuffledPool[i];
                    }
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
            if (!string.IsNullOrWhiteSpace(TxtNewMemo.Text))
            {
                _memos.Add(new MemoItem { Content = TxtNewMemo.Text.Trim() });
                TxtNewMemo.Clear();
            }
        }

        private void BtnDeleteMemo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is MemoItem memo)
            {
                _memos.Remove(memo);
            }
        }

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
                _selectedMasterStudent.Name = TxtMasterName.Text;
                _selectedMasterStudent.Department = TxtMasterDept.Text;
                _selectedMasterStudent.Advisor = TxtMasterAdvisor.Text;
                _selectedMasterStudent.Email = TxtMasterEmail.Text;

                GridMasterStudents.ItemsSource = null;
                GridMasterStudents.ItemsSource = _masterStudents;
                MessageBox.Show("마스터 데이터가 수정되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
    }
}