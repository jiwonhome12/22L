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
        private List<RentalItem> _rentalHistory = new List<RentalItem>();

        // Cabinet & SangsangLab Google Form Approvals
        private ObservableCollection<ApprovalRequest> _approvals = new ObservableCollection<ApprovalRequest>();
        private List<ApprovalRequest> _approvalHistory = new List<ApprovalRequest>();

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
        private int _currentEditingCabinetNum = 0;
        private bool _isCabinetModalEditing = false;

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

            // Setup Google Form default placeholders
            TxtSangsangLabFormUrl.Text = "https://docs.google.com/forms/d/e/1FAIpQLSfD_sangsang_lab_form/viewform";
            TxtCabinetFormUrl.Text = "https://docs.google.com/forms/d/e/1FAIpQLSdC_cabinet_rental_form/viewform";
            TxtEquipmentFormUrl.Text = "https://docs.google.com/forms/d/e/1FAIpQLScE_equipment_rental_form/viewform";
            TxtGoogleFormApiKey.Text = "AIzaSyA_example_google_forms_api_key_2026";

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
            // Start clean without regular student dummy data

            // Add 10 graduate students to master database for seats 43~52
            string[] gradNames = { "박지성", "손흥민", "김연아", "황희찬", "이강인", "안세영", "신유빈", "우상혁", "김우진", "임시현" };
            string[] gradDepts = { "대학원 컴퓨터공학과", "대학원 정보보안학과", "대학원 게임공학과", "대학원 소프트웨어융합학과" };
            for (int i = 0; i < gradNames.Length; i++)
            {
                var grad = new StudentInfo
                {
                    Name = gradNames[i],
                    StudentId = "2020" + (5555 + i).ToString(),
                    Department = gradDepts[i % gradDepts.Length],
                    Advisor = "김동욱 교수",
                    Email = $"grad{i}@dsu.ac.kr"
                };
                grad.Attendance.Add(new AttendanceRecord { DateString = "2026-07-13", Status = "출석" });
                grad.Attendance.Add(new AttendanceRecord { DateString = "2026-07-14", Status = "출석" });
                grad.Attendance.Add(new AttendanceRecord { DateString = "2026-07-15", Status = "출석" });
                _masterStudents.Add(grad);
            }

            GridMasterStudents.ItemsSource = _masterStudents;
        }

        private void SetupCabinetAndSangsangLabDemoData()
        {
            LstSangsangLabCards.ItemsSource = _approvals.Where(a => a.TabType == "상상Lab").ToList();
            GridCabinetApprovals.ItemsSource = _approvals.Where(a => a.TabType == "캐비닛").ToList();
        }

        private void SetupRentalDemoData()
        {
            BindEquipmentRentals();
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
            if (CardSangsangLabAlert != null)
                CardSangsangLabAlert.Visibility = sangsangLabCount > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Cabinet approval count
            int cabinetCount = _approvals.Count(a => a.Status == "승인 대기" && a.TabType == "캐비닛");
            TxtCabinetCount.Text = $"{cabinetCount}건";
            if (CardCabinetAlert != null)
                CardCabinetAlert.Visibility = cabinetCount > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Equipment pending approval count
            int equipmentCount = _approvals.Count(a => a.Status == "승인 대기" && a.TabType == "기자재");
            TxtEquipmentCount.Text = $"{equipmentCount}건";
            if (CardEquipmentAlert != null)
                CardEquipmentAlert.Visibility = equipmentCount > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Sync other counts in tabs
            if (TxtEquipmentPendingCount != null)
                TxtEquipmentPendingCount.Text = $"{_approvals.Count(a => a.TabType == "기자재")}건";
            
            int mainframeRented = _rentals.Count(r => r.EquipmentType.Contains("VR") || r.EquipmentType.Contains("Quest") || r.EquipmentType.Contains("본체"));
            int laptopRented = _rentals.Count(r => !r.EquipmentType.Contains("VR") && !r.EquipmentType.Contains("Quest") && !r.EquipmentType.Contains("본체"));

            if (TxtAvailableMainframeCount != null) TxtAvailableMainframeCount.Text = $"{Math.Max(0, 12 - mainframeRented)}개";
            if (TxtRentedMainframeCount != null) TxtRentedMainframeCount.Text = $"{mainframeRented}개";
            if (TxtAvailableLaptopCount != null) TxtAvailableLaptopCount.Text = $"{Math.Max(0, 12 - laptopRented)}개";
            if (TxtRentedLaptopCount != null) TxtRentedLaptopCount.Text = $"{laptopRented}개";

            if (TxtCabinetPendingCount != null)
                TxtCabinetPendingCount.Text = $"{_approvals.Count(a => a.TabType == "캐비닛")}건";

            if (TxtRentedCabinetCount != null)
                TxtRentedCabinetCount.Text = $"{_cabinetAllocations.Count}개";

            if (TxtAvailableCabinetCount != null)
                TxtAvailableCabinetCount.Text = $"{48 - _cabinetAllocations.Count}개";

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
                GridEquipmentApprovals.ItemsSource = _approvals.Where(a => a.TabType == "기자재").ToList();
 
                BindEquipmentRentals();
 
                int mainframeRented = _rentals.Count(r => r.EquipmentType.Contains("VR") || r.EquipmentType.Contains("Quest") || r.EquipmentType.Contains("본체"));
                int laptopRented = _rentals.Count(r => !r.EquipmentType.Contains("VR") && !r.EquipmentType.Contains("Quest") && !r.EquipmentType.Contains("본체"));

                if (TxtAvailableMainframeCount != null) TxtAvailableMainframeCount.Text = $"{Math.Max(0, 12 - mainframeRented)}개";
                if (TxtRentedMainframeCount != null) TxtRentedMainframeCount.Text = $"{mainframeRented}개";
                if (TxtAvailableLaptopCount != null) TxtAvailableLaptopCount.Text = $"{Math.Max(0, 12 - laptopRented)}개";
                if (TxtRentedLaptopCount != null) TxtRentedLaptopCount.Text = $"{laptopRented}개";
                TxtEquipmentPendingCount.Text = $"{_approvals.Count(a => a.TabType == "기자재")}건";
            }
            else if (tabGrid == TabCabinet)
            {
                GridCabinetApprovals.ItemsSource = null;
                GridCabinetApprovals.ItemsSource = _approvals.Where(a => a.TabType == "캐비닛").ToList();
                TxtCabinetPendingCount.Text = $"{_approvals.Count(a => a.TabType == "캐비닛")}건";
                
                RenderCabinetGrid();
                
                // Set counts
                TxtAvailableCabinetCount.Text = $"{48 - _cabinetAllocations.Count}개";
                TxtRentedCabinetCount.Text = $"{_cabinetAllocations.Count}개";
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
                var regStudents = _masterStudents.Where(s => !s.Department.Contains("대학원")).ToList();
                var gradStudents = _masterStudents.Where(s => s.Department.Contains("대학원")).ToList();

                int regIdx = 0;
                int gradIdx = 0;

                int[] assignedSeatNumbers = {
                    13, 14, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 36, 37, 38
                };

                foreach (int seatNum in assignedSeatNumbers)
                {
                    if (regIdx < regStudents.Count)
                    {
                        var seat = seats[seatNum - 1];
                        seat.Student = regStudents[regIdx++].Clone();
                    }
                }

                for (int seatNum = 43; seatNum <= 52; seatNum++)
                {
                    if (gradIdx < gradStudents.Count)
                    {
                        var seat = seats[seatNum - 1];
                        seat.Student = gradStudents[gradIdx++].Clone();
                        seat.IsFixed = true;
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
                BtnEditStudentModal.Visibility = Visibility.Visible;
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
            }
            else
            {
                TxtModalName.Background = Brushes.Transparent; TxtModalName.BorderThickness = new Thickness(0);
                TxtModalDept.Background = Brushes.Transparent; TxtModalDept.BorderThickness = new Thickness(0);
                TxtModalAdvisor.Background = Brushes.Transparent; TxtModalAdvisor.BorderThickness = new Thickness(0);
                TxtModalEmail.Background = Brushes.Transparent; TxtModalEmail.BorderThickness = new Thickness(0);
                BtnEditStudentModal.Content = "수정";
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
                var emptySeats = _activeSeats.Where(s => s.Student == null && !s.IsPillar && (s.SeatNumber < 43 || s.SeatNumber > 52)).OrderBy(s => s.SeatNumber).ToList();
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
            var unFixedSeats = _activeSeats.Where(s => !s.IsFixed && !s.IsPillar && !IsGraduateSeat(s) && (s.SeatNumber < 43 || s.SeatNumber > 52)).OrderBy(s => s.SeatNumber).ToList();
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

                var regStudents = _masterStudents.Where(s => !s.Department.Contains("대학원")).ToList();
                var gradStudents = _masterStudents.Where(s => s.Department.Contains("대학원")).ToList();

                int regIdx = 0;
                int gradIdx = 0;

                int[] assignedSeatNumbers = {
                    13, 14, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 36, 37, 38
                };

                foreach (int seatNum in assignedSeatNumbers)
                {
                    if (regIdx < regStudents.Count)
                    {
                        var seat = _activeSeats[seatNum - 1];
                        if (!seat.IsFixed || seat.Student == null)
                        {
                            seat.Student = regStudents[regIdx++].Clone();
                        }
                    }
                }

                for (int seatNum = 43; seatNum <= 52; seatNum++)
                {
                    if (gradIdx < gradStudents.Count)
                    {
                        var seat = _activeSeats[seatNum - 1];
                        seat.Student = gradStudents[gradIdx++].Clone();
                        seat.IsFixed = true;
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

        // ================= SEAT STUDENT SELECTOR HANDLERS =================
        private void BtnSelectStudentForSeat_Click(object sender, RoutedEventArgs e)
        {
            TxtSearchStudentForSeat.Clear();
            ListStudentsForSeat.ItemsSource = _masterStudents;
            ModalStudentSelection.Visibility = Visibility.Visible;
        }

        private void BtnCloseStudentSelect_Click(object sender, RoutedEventArgs e)
        {
            ModalStudentSelection.Visibility = Visibility.Collapsed;
        }

        private void TxtSearchStudentForSeat_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearchStudentForSeat();
            }
        }

        private void BtnSearchStudentForSeat_Click(object sender, RoutedEventArgs e)
        {
            PerformSearchStudentForSeat();
        }

        private void PerformSearchStudentForSeat()
        {
            string query = TxtSearchStudentForSeat.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(query))
            {
                ListStudentsForSeat.ItemsSource = _masterStudents;
            }
            else
            {
                var filtered = _masterStudents.Where(s => s.Name.ToLower().Contains(query) || s.StudentId.Contains(query)).ToList();
                ListStudentsForSeat.ItemsSource = filtered;
            }
        }

        private void BtnConfirmStudentSelect_Click(object sender, RoutedEventArgs e)
        {
            if (ListStudentsForSeat.SelectedItem is StudentInfo selected)
            {
                TxtModalName.Text = selected.Name;
                TxtModalId.Text = selected.StudentId;
                TxtModalDept.Text = selected.Department;
                TxtModalAdvisor.Text = selected.Advisor;
                TxtModalEmail.Text = selected.Email;
                ItemsModalAttendance.ItemsSource = selected.Attendance;

                if (_currentEditingSeat != null)
                {
                    _currentEditingSeat.Student = selected.Clone();
                    if (selected.Department.Contains("대학원"))
                    {
                        _currentEditingSeat.IsFixed = true;
                    }
                    RenderSeatGrid();
                    UpdateAlertBadges();
                }

                ModalStudentSelection.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("배정할 학생을 목록에서 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ================= APPROVAL HISTORY MODAL HANDLERS =================
        private void BtnCloseApprovalHistory_Click(object sender, RoutedEventArgs e)
        {
            ModalApprovalHistory.Visibility = Visibility.Collapsed;
        }

        private void BtnReAddApproval_Click(object sender, RoutedEventArgs e)
        {
            if (GridApprovalHistoryList.SelectedItem is ApprovalRequest req)
            {
                var result = MessageBox.Show($"정말 {req.StudentName} 학생의 신청을 대기목록으로 다시 추가하시겠습니까?", "대기목록 추가 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _approvalHistory.Remove(req);
                    req.Status = "승인 대기";
                    _approvals.Add(req);

                    // Refresh history grid and approvals grid
                    GridApprovalHistoryList.ItemsSource = null;
                    GridApprovalHistoryList.ItemsSource = _approvalHistory;

                    SwitchTab(req.TabType == "상상Lab" ? BtnSangsangLab : BtnCabinet, req.TabType == "상상Lab" ? TabSangsangLab : TabCabinet, req.TabType == "상상Lab" ? "상상Lab 승인" : "캐비닛 현황");
                    UpdateAlertBadges();
                    ModalApprovalHistory.Visibility = Visibility.Collapsed;
                    MessageBox.Show("대기목록에 다시 추가되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("대기목록에 추가할 내역을 먼저 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ================= RENTAL HISTORY MODAL HANDLERS =================
        private void BtnCloseRentalHistory_Click(object sender, RoutedEventArgs e)
        {
            ModalRentalHistory.Visibility = Visibility.Collapsed;
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
                    BindEquipmentRentals();
                }
            }
        }

        private void BtnUndoReturn_Click(object sender, RoutedEventArgs e)
        {
            GridApprovalHistoryList.ItemsSource = null;
            GridApprovalHistoryList.ItemsSource = _approvalHistory;
            ModalApprovalHistory.Visibility = Visibility.Visible;
        }

        // ================= CABINET & SANGSANGLAB APPROVAL HANDLERS =================
        private void BtnApproveRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ApprovalRequest req)
            {
                req.Status = "승인 완료";
                _approvals.Remove(req);
                _approvalHistory.Add(req);

                // Add student to dashboard if they are approved (Only SangsangLab)
                if (req.TabType == "상상Lab")
                {
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

                    // Add to Master Database if not present
                    var master = _masterStudents.FirstOrDefault(m => m.StudentId == req.StudentId);
                    if (master == null)
                    {
                        var newStudent = new StudentInfo
                        {
                            StudentId = req.StudentId,
                            Name = req.StudentName,
                            Department = req.Department,
                            Advisor = req.Advisor,
                            Email = req.Email
                        };
                        newStudent.Attendance.Add(new AttendanceRecord { DateString = _currentSimulatedDate.ToString("yyyy-MM-dd"), Status = "출석" });
                        _masterStudents.Add(newStudent);
                        GridMasterStudents.ItemsSource = null;
                        GridMasterStudents.ItemsSource = _masterStudents;
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

                // If equipment request is approved, add to active rentals and rental history
                if (req.TabType == "기자재")
                {
                    string equipType = new Random().Next(2) == 0 ? "VR 헤드셋 (Quest 2)" : "LG Gram 노트북";
                    var rental = new RentalItem
                    {
                        StudentName = req.StudentName,
                        EquipmentType = equipType,
                        RentalDate = _currentSimulatedDate,
                        RentalPeriodDays = 7,
                        IsReturned = false
                    };
                    _rentals.Add(rental);
                    _rentalHistory.Add(rental);
                }

                // Refresh tab binding without bug
                if (req.TabType == "기자재")
                {
                    SwitchTab(BtnEquipment, TabEquipment, "기자재 현황");
                }
                else
                {
                    SwitchTab(req.TabType == "상상Lab" ? BtnSangsangLab : BtnCabinet, req.TabType == "상상Lab" ? TabSangsangLab : TabCabinet, req.TabType == "상상Lab" ? "상상Lab 승인" : "캐비닛 현황");
                }
                RenderSeatGrid();
            }
        }

        private void BtnRejectRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ApprovalRequest req)
            {
                req.Status = "반려";
                _approvals.Remove(req);
                _approvalHistory.Add(req);

                if (req.TabType == "기자재")
                {
                    SwitchTab(BtnEquipment, TabEquipment, "기자재 현황");
                }
                else
                {
                    SwitchTab(req.TabType == "상상Lab" ? BtnSangsangLab : BtnCabinet, req.TabType == "상상Lab" ? TabSangsangLab : TabCabinet, req.TabType == "상상Lab" ? "상상Lab 승인" : "캐비닛 현황");
                }
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

                // Sync with active seats across all cache layouts
                foreach (var layout in _seatLayoutCache.Values)
                {
                    var seat = layout.FirstOrDefault(s => s.Student?.StudentId == oldId);
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
                var result = MessageBox.Show($"정말 {_selectedMasterStudent.Name} 학생을 마스터 데이터베이스에서 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _masterStudents.Remove(_selectedMasterStudent);
                    GridMasterStudents.ItemsSource = null;
                    GridMasterStudents.ItemsSource = _masterStudents;
                    PanelMasterEdit.IsEnabled = false;
                    MessageBox.Show("마스터 데이터가 삭제되었습니다. (기존 배치된 좌석 데이터는 유지됩니다.)", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // ================= SIMULATE GOOGLE FORM SUBMISSIONS =================
        private async void BtnSimulateFormSubmission_Click(object sender, RoutedEventArgs e)
        {
            ModalLoading.Visibility = Visibility.Visible;
            await System.Threading.Tasks.Task.Delay(1500);
            
            string apiKey = TxtGoogleFormApiKey.Text.Trim();
            
            var fetchedRequests = new List<ApprovalRequest>();
            bool apiAttempted = false;

            // Attempt fetching for SangsangLab
            if (!string.IsNullOrWhiteSpace(TxtSangsangLabFormUrl.Text) && !string.IsNullOrWhiteSpace(apiKey))
            {
                apiAttempted = true;
                var list = await FetchGoogleSheetsDataAsync(TxtSangsangLabFormUrl.Text, apiKey, "상상Lab");
                if (list != null && list.Count > 0) fetchedRequests.AddRange(list);
            }

            // Attempt fetching for Cabinet
            if (!string.IsNullOrWhiteSpace(TxtCabinetFormUrl.Text) && !string.IsNullOrWhiteSpace(apiKey))
            {
                apiAttempted = true;
                var list = await FetchGoogleSheetsDataAsync(TxtCabinetFormUrl.Text, apiKey, "캐비닛");
                if (list != null && list.Count > 0) fetchedRequests.AddRange(list);
            }

            // Attempt fetching for Equipment
            if (!string.IsNullOrWhiteSpace(TxtEquipmentFormUrl.Text) && !string.IsNullOrWhiteSpace(apiKey))
            {
                apiAttempted = true;
                var list = await FetchGoogleSheetsDataAsync(TxtEquipmentFormUrl.Text, apiKey, "기자재");
                if (list != null && list.Count > 0) fetchedRequests.AddRange(list);
            }

            ModalLoading.Visibility = Visibility.Collapsed;

            if (fetchedRequests.Count > 0)
            {
                int newCount = 0;
                foreach (var req in fetchedRequests)
                {
                    // Add only if not already exists in approvals or history
                    if (!_approvals.Any(a => a.StudentId == req.StudentId && a.TabType == req.TabType) &&
                        !_approvalHistory.Any(h => h.StudentId == req.StudentId && h.TabType == req.TabType))
                    {
                        _approvals.Add(req);
                        newCount++;
                    }
                }

                UpdateAlertBadges();
                MessageBox.Show($"[구글 스프레드시트 API 연동 완료]\n\n총 {newCount}건의 새로운 신청 내역이 연동되어 업데이트되었습니다.", "동기화 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (apiAttempted)
                {
                    MessageBox.Show("구글 스프레드시트 API 연동을 시도했으나 가져올 수 있는 새로운 데이터가 없거나,\nAPI 키/URL/공유 권한 설정이 잘못되었습니다.", "동기화 실패 또는 데이터 없음", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("구글 폼 연동 설정(URL 및 API 키)이 등록되지 않았습니다.\n설정 탭에서 먼저 연동을 구성해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private string ExtractSpreadsheetId(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;
            int dIndex = url.IndexOf("/d/");
            if (dIndex == -1) return string.Empty;
            string sub = url.Substring(dIndex + 3);
            int slashIndex = sub.IndexOf("/");
            if (slashIndex == -1) return sub;
            return sub.Substring(0, slashIndex);
        }

        private async System.Threading.Tasks.Task<List<ApprovalRequest>> FetchGoogleSheetsDataAsync(string url, string apiKey, string tabType)
        {
            string sheetId = ExtractSpreadsheetId(url);
            if (string.IsNullOrEmpty(sheetId) || string.IsNullOrEmpty(apiKey)) return null;

            using (var client = new System.Net.Http.HttpClient())
            {
                try
                {
                    string range = "A2:G50";
                    string apiUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{sheetId}/values/{range}?key={apiKey}";
                    
                    var response = await client.GetAsync(apiUrl);
                    if (!response.IsSuccessStatusCode) return null;

                    string json = await response.Content.ReadAsStringAsync();
                    var list = new List<ApprovalRequest>();

                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("values", out var valuesProp))
                        {
                            foreach (var row in valuesProp.EnumerateArray())
                            {
                                if (row.GetArrayLength() >= 3)
                                {
                                    string timestampStr = row[0].GetString() ?? "";
                                    string id = row[1].GetString() ?? "";
                                    string name = row[2].GetString() ?? "";
                                    string dept = row.GetArrayLength() > 3 ? (row[3].GetString() ?? "") : "소프트웨어융합학과";
                                    string advisor = row.GetArrayLength() > 4 ? (row[4].GetString() ?? "") : "김동욱 교수";
                                    string email = row.GetArrayLength() > 5 ? (row[5].GetString() ?? "") : $"{name}@dsu.ac.kr";

                                    DateTime reqDate = DateTime.Now;
                                    DateTime.TryParse(timestampStr, out reqDate);

                                    list.Add(new ApprovalRequest
                                    {
                                        StudentName = name,
                                        StudentId = id,
                                        Department = dept,
                                        Advisor = advisor,
                                        Email = email,
                                        TabType = tabType,
                                        RequestDate = reqDate,
                                        Status = "승인 대기"
                                    });
                                }
                            }
                        }
                    }
                    return list;
                }
                catch
                {
                    return null;
                }
            }
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

            bool isMatch = false;
            string query = TxtSearchCabinet != null ? TxtSearchCabinet.Text.Trim().ToLower() : "";
            bool hasQuery = !string.IsNullOrEmpty(query);

            if (_cabinetAllocations.ContainsKey(number))
            {
                var alloc = _cabinetAllocations[number];
                border.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255));

                isMatch = alloc.Student.Name.ToLower().Contains(query) || 
                          alloc.Student.StudentId.ToLower().Contains(query) || 
                          alloc.Student.Department.ToLower().Contains(query);

                TextBlock studentTxt = new TextBlock
                {
                    Text = $"{alloc.Student.StudentId} {alloc.Student.Name}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                stack.Children.Add(studentTxt);

                TextBlock periodTxt = new TextBlock
                {
                    Text = alloc.Period,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 1, 0, 0)
                };
                stack.Children.Add(periodTxt);
            }
            else
            {
                numTxt.FontSize = 13;
                numTxt.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
            }

            if (hasQuery)
            {
                if (isMatch)
                {
                    border.Opacity = 1.0;
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                    border.BorderThickness = new Thickness(2);
                }
                else
                {
                    border.Opacity = 0.2;
                }
            }

            border.Tag = number;
            border.Cursor = Cursors.Hand;
            border.MouseDown += CabinetCell_MouseDown;

            border.Child = stack;
            return border;
        }

        private void CabinetCell_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int number)
            {
                _currentEditingCabinetNum = number;
                if (_cabinetAllocations.ContainsKey(number))
                {
                    var alloc = _cabinetAllocations[number];
                    TxtCabinetModalTitle.Text = $"캐비닛 {number}번 상세 정보";
                    TxtCabinetModalName.Text = alloc.Student.Name;
                    TxtCabinetModalId.Text = alloc.Student.StudentId;
                    TxtCabinetModalPeriod.Text = alloc.Period;
                    
                    SetCabinetModalEditMode(false);
                    ModalCabinetDetails.Visibility = Visibility.Visible;
                }
                else
                {
                    var result = MessageBox.Show($"[캐비닛 {number}번] 사용 가능 (미배정) 상태입니다.\n이 캐비닛에 새로운 학생을 임의 배정하시겠습니까?", "캐비닛 배정", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        TxtCabinetModalTitle.Text = $"캐비닛 {number}번 임의 배정";
                        TxtCabinetModalName.Text = "";
                        TxtCabinetModalId.Text = "";
                        TxtCabinetModalPeriod.Text = $"{_currentSimulatedDate:yyyy.MM.dd}~{_currentSimulatedDate.AddMonths(1):yyyy.MM.dd}";
                        
                        SetCabinetModalEditMode(true);
                        ModalCabinetDetails.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void BtnCloseCabinetModal_Click(object sender, RoutedEventArgs e)
        {
            ModalCabinetDetails.Visibility = Visibility.Collapsed;
        }

        private void BtnEditCabinetInfo_Click(object sender, RoutedEventArgs e)
        {
            SetCabinetModalEditMode(!_isCabinetModalEditing);
        }

        private void SetCabinetModalEditMode(bool enable)
        {
            _isCabinetModalEditing = enable;
            TxtCabinetModalName.IsReadOnly = !enable;
            TxtCabinetModalId.IsReadOnly = !enable;
            TxtCabinetModalPeriod.IsReadOnly = !enable;

            if (enable)
            {
                TxtCabinetModalName.Background = Brushes.White; TxtCabinetModalName.BorderThickness = new Thickness(1);
                TxtCabinetModalId.Background = Brushes.White; TxtCabinetModalId.BorderThickness = new Thickness(1);
                TxtCabinetModalPeriod.Background = Brushes.White; TxtCabinetModalPeriod.BorderThickness = new Thickness(1);
                BtnSaveCabinetModal.Visibility = Visibility.Visible;
                BtnEditCabinetInfo.Content = "취소";
            }
            else
            {
                TxtCabinetModalName.Background = Brushes.Transparent; TxtCabinetModalName.BorderThickness = new Thickness(0);
                TxtCabinetModalId.Background = Brushes.Transparent; TxtCabinetModalId.BorderThickness = new Thickness(0);
                TxtCabinetModalPeriod.Background = Brushes.Transparent; TxtCabinetModalPeriod.BorderThickness = new Thickness(0);
                BtnSaveCabinetModal.Visibility = Visibility.Collapsed;
                BtnEditCabinetInfo.Content = "정보 수정";
            }
        }

        private void BtnSaveCabinetModal_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEditingCabinetNum == 0) return;

            string name = TxtCabinetModalName.Text.Trim();
            string id = TxtCabinetModalId.Text.Trim();
            string period = TxtCabinetModalPeriod.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(period))
            {
                MessageBox.Show("모든 항목을 입력해 주세요.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var student = new StudentInfo
            {
                StudentId = id,
                Name = name,
                Department = "소프트웨어융합학과"
            };

            _cabinetAllocations[_currentEditingCabinetNum] = (student, period);
            
            RenderCabinetGrid();
            UpdateAlertBadges();
            SetCabinetModalEditMode(false);
            ModalCabinetDetails.Visibility = Visibility.Collapsed;
            
            MessageBox.Show("캐비닛 정보가 저장되었습니다.", "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (!this.IsLoaded) return;
            if (ComboResolution == null) return;
            var selected = (ComboResolution.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selected == "기본 (1600 x 900)")
            {
                this.Width = 1600;
                this.Height = 900;
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
            else if (selected == "기타 (1200 x 780)")
            {
                this.Width = 1200;
                this.Height = 780;
            }
        }

        private void BtnShowApprovalHistory_Click(object sender, RoutedEventArgs e)
        {
            GridApprovalHistoryList.ItemsSource = null;
            GridApprovalHistoryList.ItemsSource = _approvalHistory;
            ModalApprovalHistory.Visibility = Visibility.Visible;
        }

        private void BtnRentalHistory_Click(object sender, RoutedEventArgs e)
        {
            GridRentalHistoryList.ItemsSource = null;
            GridRentalHistoryList.ItemsSource = _rentalHistory;
            ModalRentalHistory.Visibility = Visibility.Visible;
        }

        private void BtnSaveGoogleFormSettings_Click(object sender, RoutedEventArgs e)
        {
            bool hasSangsang = !string.IsNullOrWhiteSpace(TxtSangsangLabFormUrl.Text);
            bool hasCabinet = !string.IsNullOrWhiteSpace(TxtCabinetFormUrl.Text);
            bool hasEquipment = !string.IsNullOrWhiteSpace(TxtEquipmentFormUrl.Text);
            bool hasApiKey = !string.IsNullOrWhiteSpace(TxtGoogleFormApiKey.Text);

            if (!hasSangsang && !hasCabinet && !hasEquipment)
            {
                MessageBox.Show("적어도 하나의 구글 폼 URL을 입력해야 합니다.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!hasApiKey)
            {
                MessageBox.Show("구글 폼 API 인증 키를 입력해 주세요.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("구글 폼 연동 설정이 성공적으로 저장 및 적용되었습니다!", "설정 저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BindEquipmentRentals()
        {
            string query = TxtSearchEquipment != null ? TxtSearchEquipment.Text.Trim().ToLower() : "";
            var filtered = _rentals.Where(r => 
                string.IsNullOrEmpty(query) || 
                r.StudentName.ToLower().Contains(query) || 
                r.StudentId.ToLower().Contains(query) || 
                r.Department.ToLower().Contains(query) ||
                r.EquipmentType.ToLower().Contains(query)
            ).ToList();

            if (GridQuestRentals != null)
            {
                GridQuestRentals.ItemsSource = null;
                GridQuestRentals.ItemsSource = filtered.Where(r => r.EquipmentType.Contains("VR") || r.EquipmentType.Contains("Quest") || r.EquipmentType.Contains("본체")).ToList();
            }
            if (GridLaptopRentals != null)
            {
                GridLaptopRentals.ItemsSource = null;
                GridLaptopRentals.ItemsSource = filtered.Where(r => !r.EquipmentType.Contains("VR") && !r.EquipmentType.Contains("Quest") && !r.EquipmentType.Contains("본체")).ToList();
            }
        }

        private void BtnImportDashboardExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = "대시보드 좌석 배치 엑셀 가져오기"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var rows = MiniExcelLibs.MiniExcel.Query(dialog.FileName).ToList();
                    int importedCount = 0;
                    foreach (IDictionary<string, object> row in rows)
                    {
                        var values = row.Values.ToList();
                        if (values.Count < 2) continue;

                        string col0 = values[0]?.ToString() ?? "";
                        if (col0 == "SeatNumber" || col0 == "좌석번호" || string.IsNullOrWhiteSpace(col0)) continue;

                        int.TryParse(col0, out int seatNum);
                        if (seatNum <= 0) continue;

                        string id = values.Count > 1 ? (values[1]?.ToString() ?? "") : "";
                        string name = values.Count > 2 ? (values[2]?.ToString() ?? "") : "";
                        string dept = values.Count > 3 ? (values[3]?.ToString() ?? "") : "소프트웨어융합학과";
                        string advisor = values.Count > 4 ? (values[4]?.ToString() ?? "") : "김동욱 교수";
                        string email = values.Count > 5 ? (values[5]?.ToString() ?? "") : "";
                        bool isFixed = false;
                        if (values.Count > 6)
                        {
                            bool.TryParse(values[6]?.ToString() ?? "", out isFixed);
                        }

                        var targetSeat = _activeSeats.FirstOrDefault(s => s.SeatNumber == seatNum);
                        if (targetSeat != null && !targetSeat.IsPillar)
                        {
                            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                            {
                                targetSeat.Student = null;
                            }
                            else
                            {
                                targetSeat.Student = new StudentInfo
                                {
                                    StudentId = id,
                                    Name = name,
                                    Department = dept,
                                    Advisor = advisor,
                                    Email = email
                                };
                                targetSeat.IsFixed = isFixed || dept.Contains("대학원");
                            }
                            importedCount++;
                        }
                    }

                    RenderSeatGrid();
                    MessageBox.Show($"엑셀 파일로부터 {importedCount}개의 좌석 배치를 적용했습니다.", "가져오기 성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"엑셀 파일을 읽는 도중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnImportMainframeExcel_Click(object sender, RoutedEventArgs e)
        {
            ImportEquipmentExcel("본체 (HP Z2 G9)");
        }

        private void BtnImportLaptopExcel_Click(object sender, RoutedEventArgs e)
        {
            ImportEquipmentExcel("노트북(HP OMEN 게이밍 노트북)");
        }

        private void ImportEquipmentExcel(string targetEquipType)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = $"{targetEquipType} 대여 현황 엑셀 가져오기"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var rows = MiniExcelLibs.MiniExcel.Query(dialog.FileName).ToList();
                    int importedCount = 0;
                    foreach (IDictionary<string, object> row in rows)
                    {
                        var values = row.Values.ToList();
                        if (values.Count < 2) continue;

                        string col0 = values[0]?.ToString() ?? "";
                        if (col0 == "번호" || col0 == "No" || col0 == "No." || string.IsNullOrWhiteSpace(col0)) continue;

                        // Skip rows with missing StudentId (Column 8 / Index 8) or StudentName (Column 11 / Index 11)
                        string id = values.Count > 8 ? (values[8]?.ToString() ?? "") : "";
                        string studentName = values.Count > 11 ? (values[11]?.ToString() ?? "") : "";
                        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(studentName)) continue;

                        int.TryParse(values.Count > 1 ? (values[1]?.ToString() ?? "") : "", out int qty);
                        string extra = values.Count > 2 ? (values[2]?.ToString() ?? "") : "";
                        string loc = values.Count > 3 ? (values[3]?.ToString() ?? "") : "";
                        string purp = values.Count > 4 ? (values[4]?.ToString() ?? "") : "";
                        string rem = values.Count > 5 ? (values[5]?.ToString() ?? "") : "";
                        string dept = values.Count > 6 ? (values[6]?.ToString() ?? "") : "";
                        string level = values.Count > 7 ? (values[7]?.ToString() ?? "") : "";
                        string phone = values.Count > 9 ? (values[9]?.ToString() ?? "") : "";
                        string advisor = values.Count > 10 ? (values[10]?.ToString() ?? "") : "";

                        DateTime rentalDate = _currentSimulatedDate;
                        if (values.Count > 12 && DateTime.TryParse(values[12]?.ToString() ?? "", out var rd)) rentalDate = rd;

                        int period = 7;
                        if (values.Count > 13 && DateTime.TryParse(values[13]?.ToString() ?? "", out var dd))
                        {
                            period = (dd - rentalDate).Days;
                            if (period <= 0) period = 7;
                        }

                        DateTime? retDate = null;
                        if (values.Count > 14 && DateTime.TryParse(values[14]?.ToString() ?? "", out var retd)) retDate = retd;

                        string equipType = targetEquipType;

                        var rental = new RentalItem
                        {
                            Quantity = qty,
                            ExtraItems = extra,
                            Location = loc,
                            Purpose = purp,
                            Remarks = rem,
                            Department = dept,
                            YearLevel = level,
                            StudentId = id,
                            Phone = phone,
                            Advisor = advisor,
                            StudentName = studentName,
                            RentalDate = rentalDate,
                            RentalPeriodDays = period,
                            ReturnDate = retDate,
                            EquipmentType = equipType,
                            IsReturned = retDate != null
                        };

                        _rentals.Add(rental);
                        _rentalHistory.Add(rental);
                        importedCount++;
                    }

                    BindEquipmentRentals();
                    UpdateAlertBadges();
                    MessageBox.Show($"엑셀 파일로부터 {importedCount}건의 기자재 대여 내역을 추가했습니다.", "가져오기 성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"엑셀 파일을 읽는 도중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnImportCabinetExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = "캐비닛 배정 현황 엑셀 가져오기"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var rows = MiniExcelLibs.MiniExcel.Query(dialog.FileName).ToList();
                    int importedCount = 0;
                    foreach (IDictionary<string, object> row in rows)
                    {
                        var values = row.Values.ToList();
                        if (values.Count < 2) continue;

                        string col0 = values[0]?.ToString() ?? "";
                        if (col0 == "CabinetNumber" || col0 == "캐비닛번호" || string.IsNullOrWhiteSpace(col0)) continue;

                        int.TryParse(col0, out int cabNum);
                        if (cabNum <= 0 || cabNum > 48) continue;

                        string id = values.Count > 1 ? (values[1]?.ToString() ?? "") : "";
                        string name = values.Count > 2 ? (values[2]?.ToString() ?? "") : "";
                        string dept = values.Count > 3 ? (values[3]?.ToString() ?? "") : "소프트웨어융합학과";
                        string period = values.Count > 4 ? (values[4]?.ToString() ?? "") : $"{_currentSimulatedDate:MM/dd}~{_currentSimulatedDate.AddMonths(1):MM/dd}";

                        var student = new StudentInfo
                        {
                            StudentId = id,
                            Name = name,
                            Department = dept
                        };

                        _cabinetAllocations[cabNum] = (student, period);
                        importedCount++;
                    }

                    RenderCabinetGrid();
                    UpdateAlertBadges();
                    MessageBox.Show($"엑셀 파일로부터 {importedCount}개의 캐비닛 배정을 완료했습니다.", "가져오기 성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"엑셀 파일을 읽는 도중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnImportDataManageExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = "마스터 학생 데이터베이스 엑셀 가져오기"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var rows = MiniExcelLibs.MiniExcel.Query(dialog.FileName).ToList();
                    int importedCount = 0;
                    foreach (IDictionary<string, object> row in rows)
                    {
                        var values = row.Values.ToList();
                        if (values.Count < 2) continue;

                        string col0 = values[0]?.ToString() ?? "";
                        if (col0 == "StudentId" || col0 == "학번" || string.IsNullOrWhiteSpace(col0)) continue;

                        string id = col0;
                        string name = values.Count > 1 ? (values[1]?.ToString() ?? "") : "";
                        string dept = values.Count > 2 ? (values[2]?.ToString() ?? "") : "소프트웨어융합학과";
                        string advisor = values.Count > 3 ? (values[3]?.ToString() ?? "") : "김동욱 교수";
                        string email = values.Count > 4 ? (values[4]?.ToString() ?? "") : "";

                        if (!_masterStudents.Any(m => m.StudentId == id))
                        {
                            var student = new StudentInfo
                            {
                                StudentId = id,
                                Name = name,
                                Department = dept,
                                Advisor = advisor,
                                Email = email
                            };
                            _masterStudents.Add(student);
                            importedCount++;
                        }
                    }

                    GridMasterStudents.ItemsSource = null;
                    GridMasterStudents.ItemsSource = _masterStudents;
                    MessageBox.Show($"엑셀 파일로부터 {importedCount}명의 마스터 학생 정보를 등록했습니다.", "가져오기 성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"엑셀 파일을 읽는 도중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TxtSearchEquipment_KeyUp(object sender, KeyEventArgs e)
        {
            BindEquipmentRentals();
        }

        private void TxtSearchCabinet_KeyUp(object sender, KeyEventArgs e)
        {
            RenderCabinetGrid();
        }

        private void BtnDeleteSelectedRental_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = new List<RentalItem>();
            if (GridQuestRentals != null)
            {
                foreach (var item in GridQuestRentals.SelectedItems)
                {
                    if (item is RentalItem rental)
                        selectedItems.Add(rental);
                }
            }
            if (GridLaptopRentals != null)
            {
                foreach (var item in GridLaptopRentals.SelectedItems)
                {
                    if (item is RentalItem rental)
                        selectedItems.Add(rental);
                }
            }

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("삭제할 대여 데이터를 목록에서 먼저 선택해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"선택한 {selectedItems.Count}개의 대여 데이터를 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                foreach (var item in selectedItems)
                {
                    _rentals.Remove(item);
                }
                BindEquipmentRentals();
                UpdateAlertBadges();
                MessageBox.Show("선택한 대여 데이터가 삭제되었습니다.", "삭제 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnDownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            string fileName = "2026-여름방학_기자제 (노트북(HP OMEN 게이밍 노트북) 대여 현황서양식.xlsx";
            string[] possiblePaths = new[]
            {
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpinnerApp", fileName),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "SpinnerApp", fileName),
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SpinnerApp", fileName),
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), fileName)
            };

            string sourcePath = "";
            foreach (var p in possiblePaths)
            {
                if (System.IO.File.Exists(p))
                {
                    sourcePath = p;
                    break;
                }
            }

            if (string.IsNullOrEmpty(sourcePath))
            {
                MessageBox.Show("기자재 대여 양식 파일을 찾을 수 없습니다. (SpinnerApp 폴더에 파일이 존재하는지 확인해 주세요)", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "기자재 대여 양식 다운로드",
                FileName = fileName
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    System.IO.File.Copy(sourcePath, dialog.FileName, true);
                    MessageBox.Show("기자재 대여 템플릿 양식 파일이 성공적으로 다운로드되었습니다.", "다운로드 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 저장 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}