using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using EasyModbus;

namespace ModbusSimV1
{
    public partial class Form1 : Form
    {
        // Backing store for the register cards on the UI – we drive these collections from
        // the automation routines and the incoming Modbus traffic, so keep them readily accessible.
        private readonly BindingList<RegisterItem> _registerItems = new();
        private readonly Dictionary<int, RegisterDisplay> _registerDisplays = new();

        // Single timer that powers both manual cyclic polling and automation-driven flows.
        private readonly System.Windows.Forms.Timer _pollTimer;
        private const int PollTimerIntervalMs = 1000;

        // Active Modbus client and connection bookkeeping state.
        private ModbusClient? _modbusClient;
        private string? _connectedPortName;

        // Selection + polling state shared across various helpers.
        private RegisterItem? _selectedRegister;
        private bool _isPolling;
        private readonly object _stateLock = new();
        private readonly Dictionary<int, DateTime> _lastPollByEvent = new();
        private int? _automationLastProcessedEvent;

        // Centralised register map – keeping all addresses in one place makes future table updates
        // significantly less error-prone than sprinkling literals throughout the file.
        private const int RegVersionNumber = 0x0000;
        private const int RegControllerStatus = 0x0001;
        private const int RegEventTracker = 0x0002;
        private const int RegPollCounter = 0x0003;
        private const int RegTotalPriceToPay = 0x0004;
        private const int RegSelectedProgramNumber = 0x0006;
        private const int RegRemainingTime = 0x0007;
        private const int RegPaymentSystemStatus = 0x000C;
        private const int RegPaidAmount = 0x000D;
        private const int RegDiscountAmount = 0x000F;
        private const int RegCurrency = 0x0015;
        private const int RegProgramPriceStart = 0x0016;   // Program Price 1 (UDINT, two words)
        private const int RegProgramPriceEnd = 0x003C;     // Program Price 20 (UDINT, two words)
        private const int RegExtraRinseModifier = 0x003E;
        private const int RegExtraSoapModifier = 0x0040;
        private const int RegExtraPrewashModifier = 0x0042;
        private const int RegReservedEnd = 0x0046;         // Last reserved slot from the provided table

        // Program price synchronisation sweeps the entire payment table anytime the slave raises the
        // “Program Price Update Available” bit.  Capture the span once so every caller uses the same
        // boundaries.
        private const int RegPriceTablePollStart = RegPaymentSystemStatus;
        private const int RegPriceTablePollEnd = RegReservedEnd;

        // Helpers for word/byte conversions and bounded RX/TX log storage.
        private const bool SWAP_WORDS = true;
        private const int RxTxLogCapacity = 500;
        private static readonly PropertyInfo? SendDataProperty = typeof(ModbusClient).GetProperty("SendData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly PropertyInfo? ReceiveDataProperty = typeof(ModbusClient).GetProperty("ReceiveData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo? SendDataField = typeof(ModbusClient).GetField("sendData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo? ReceiveDataField = typeof(ModbusClient).GetField("receiveData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private int _rxTxSequence;

        // ===== Event dropdown on the right panel =====
        // Primary translation map for the Event Tracker register.  This drives both the
        // combo box labels and the automation state machine below.
        private readonly Dictionary<int, string> _eventMapByValue = new()
        {
            {10, "Idle"},
            {20, "Program Selection"},
            {21, "Extra Selection"},
            {30, "Payment"},
            {40, "Starting"},
            {50, "Cycling"},
            {60, "Cycle Finished"},
            {999, "Machine Unavailable"}
        };
        private Dictionary<string, int>? _eventMapByName;
        private bool _suppressEventCombo = false;
        private int? _lastEventTrackerValue = null;

        // Default poll cadence for the background timer.  Individual states stretch/shorten it.
        // Single RNG instance that feeds the automation's pseudo-random payment/program picks.
        private readonly Random _rng = new Random();

        // Timestamp bookkeeping for automation throttling and state transitions.
        private DateTime _startingEnteredAt = DateTime.MinValue;
        private DateTime _cyclingLastTick = DateTime.MinValue;
        private DateTime _cycleFinishedEnteredAt = DateTime.MinValue;
        private DateTime _lastManualPoll = DateTime.MinValue;

        public Form1()
        {
            InitializeComponent();

            // after InitializeComponent();
            cmbEvent.Items.AddRange(_eventMapByValue.Values.ToArray());

            // default selection (sync to current Event Tracker if present)
            var evt = GetRegisterByAddress(RegEventTracker);
            int initVal = evt?.LastValue ?? 10;
            if (_eventMapByValue.TryGetValue(initVal, out var initLabel))
                cmbEvent.SelectedItem = initLabel;
            else
                cmbEvent.SelectedItem = _eventMapByValue[10];


            // English labels
            grpConnection.Text = "Connection";
            lblPort.Text = "PORT:";
            btnConnect.Text = "Connect";
            btnDisconnect.Text = "Disconnect";
            btnRefreshPorts.Text = "Refresh";
            lblConnectionStatus.Text = "Status: Disconnected";
            grpSelectedRegister.Text = "Selected Register";
            grpAutomation.Text = "Automation";
            chkAutomationEnabled.Text = "Run automation";
            lblActivity.Text = "Activity Log";
            lblRxTx.Text = "RX/TX Log (hex)";

            // Build Event dropdown in the automation box (designer must have cmbEvent + lblEvent)
            _eventMapByName = _eventMapByValue.ToDictionary(kv => kv.Value, kv => kv.Key);
            cmbEvent.Items.Clear();
            foreach (var kv in _eventMapByValue.OrderBy(k => k.Key))
                cmbEvent.Items.Add(kv.Value);
            cmbEvent.SelectedItem = "Idle";
            cmbEvent.SelectedIndexChanged += CmbEvent_SelectedIndexChanged;

            _pollTimer = new System.Windows.Forms.Timer { Interval = PollTimerIntervalMs };
            _pollTimer.Tick += PollTimer_Tick;

            flowRegisters.Resize += FlowRegisters_Resize;

            _lastEventTrackerValue = 10;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            PopulateRegisterItems();
            BuildRegisterCards();
            RefreshPortList();
            UpdateConnectionState();
        }

        // ===== helpers for 32-bit words =====
        private static uint CombineToUInt32(int word0, int word1)
        {
            int hi = SWAP_WORDS ? word1 : word0;
            int lo = SWAP_WORDS ? word0 : word1;
            return ((uint)(ushort)hi << 16) | (uint)(ushort)lo;
        }
        private static int[] SplitUInt32ToWords(uint value)
        {
            ushort hi = (ushort)(value >> 16);
            ushort lo = (ushort)(value & 0xFFFF);
            if (SWAP_WORDS) (hi, lo) = (lo, hi);
            return new[] { (int)hi, (int)lo };
        }

        // ===== registers UI =====
        private void PopulateRegisterItems()
        {
            _registerItems.Clear();
            var items = new List<RegisterItem>
            {
                new RegisterItem { Name = "Version Number", Address = RegVersionNumber, ChangeableBy = "SLAVE", Type = "UINT", Size = "1", ValueRange = "0 - 65535", Description = "Simulator software version.", LastValue = 1 },
                new RegisterItem { Name = "Controller Status", Address = RegControllerStatus, ChangeableBy = "MASTER", Type = "BITFIELD", Size = "1", ValueRange = "Bit0=Price Update OK, Bit1=Overpayment Enabled", Description = "Vector Controller Status" },
                new RegisterItem { Name = "Event Tracker", Address = RegEventTracker, ChangeableBy = "MASTER", Type = "UINT", Size = "1", ValueRange = "0 - 65535", Description = "Changes depending on the machine page." },
                new RegisterItem { Name = "Poll Counter", Address = RegPollCounter, ChangeableBy = "MASTER", Type = "UINT", Size = "1", ValueRange = "0 - 65535", Description = "Increased by 1 every poll" },
                new RegisterItem { Name = "Total Price To Pay", Address = RegTotalPriceToPay, ChangeableBy = "MASTER", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Selected program due amount." },
                new RegisterItem { Name = "Selected Program No", Address = RegSelectedProgramNumber, ChangeableBy = "MASTER", Type = "UINT", Size = "1", ValueRange = "0 - 255", Description = "Selected program number by customer" },
                new RegisterItem { Name = "Remaining Time (Min.)", Address = RegRemainingTime, ChangeableBy = "MASTER", Type = "UINT", Size = "1", ValueRange = "0 - 9999", Description = "Current cycle remaining time" },
                new RegisterItem { Name = "Payment System Status", Address = RegPaymentSystemStatus, ChangeableBy = "SLAVE", Type = "BITFIELD", Size = "1", ValueRange = "Bit0=Price Update, Bit1=Discount, Bit2=System Update", Description = "Payment system status bits." },
                new RegisterItem { Name = "Paid Amount", Address = RegPaidAmount, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "0.00 - 999.99", Description = "Total paid amount in current txn." },
                new RegisterItem { Name = "Discount Amount", Address = RegDiscountAmount, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "0.00 - 999.99", Description = "Applied discount." },
                new RegisterItem { Name = "Currency", Address = RegCurrency, ChangeableBy = "SLAVE", Type = "UINT", Size = "1", ValueRange = "0-5", Description = "0=None 1=Token 2=USD 3=TL 4=EUR 5=GBP" },
                new RegisterItem { Name = "Program 1 Price", Address = RegProgramPriceStart + 0, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 1 Price" },
                new RegisterItem { Name = "Program 2 Price", Address = RegProgramPriceStart + 0x0002, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 2 Price" },
                new RegisterItem { Name = "Program 3 Price", Address = RegProgramPriceStart + 0x0004, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 3 Price" },
                new RegisterItem { Name = "Program 4 Price", Address = RegProgramPriceStart + 0x0006, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 4 Price" },
                new RegisterItem { Name = "Program 5 Price", Address = RegProgramPriceStart + 0x0008, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 5 Price" },
                new RegisterItem { Name = "Program 6 Price", Address = RegProgramPriceStart + 0x000A, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 6 Price" },
                new RegisterItem { Name = "Program 7 Price", Address = RegProgramPriceStart + 0x000C, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 7 Price" },
                new RegisterItem { Name = "Program 8 Price", Address = RegProgramPriceStart + 0x000E, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 8 Price" },
                new RegisterItem { Name = "Program 9 Price", Address = RegProgramPriceStart + 0x0010, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 9 Price" },
                new RegisterItem { Name = "Program 10 Price", Address = RegProgramPriceStart + 0x0012, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 10 Price" },
                new RegisterItem { Name = "Program 11 Price", Address = RegProgramPriceStart + 0x0014, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 11 Price" },
                new RegisterItem { Name = "Program 12 Price", Address = RegProgramPriceStart + 0x0016, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 12 Price" },
                new RegisterItem { Name = "Program 13 Price", Address = RegProgramPriceStart + 0x0018, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 13 Price" },
                new RegisterItem { Name = "Program 14 Price", Address = RegProgramPriceStart + 0x001A, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 14 Price" },
                new RegisterItem { Name = "Program 15 Price", Address = RegProgramPriceStart + 0x001C, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 15 Price" },
                new RegisterItem { Name = "Program 16 Price", Address = RegProgramPriceStart + 0x001E, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 16 Price" },
                new RegisterItem { Name = "Program 17 Price", Address = RegProgramPriceStart + 0x0020, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 17 Price" },
                new RegisterItem { Name = "Program 18 Price", Address = RegProgramPriceStart + 0x0022, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 18 Price" },
                new RegisterItem { Name = "Program 19 Price", Address = RegProgramPriceStart + 0x0024, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 19 Price" },
                new RegisterItem { Name = "Program 20 Price", Address = RegProgramPriceEnd, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Program 20 Price" },
                new RegisterItem { Name = "Extra Rinse Modifier", Address = RegExtraRinseModifier, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Extra Rinse Modifier" },
                new RegisterItem { Name = "Extra Soap Modifier", Address = RegExtraSoapModifier, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Extra Soap Modifier" },
                new RegisterItem { Name = "Extra Prewash Modifier", Address = RegExtraPrewashModifier, ChangeableBy = "SLAVE", Type = "UDINT", Size = "2", ValueRange = "1-999999(9999,99)", Description = "Extra Prewash Modifier" },
            };
            foreach (var it in items)
                if (string.Equals(it.Type, "UDINT", StringComparison.OrdinalIgnoreCase)) it.WordLength = 2;
            foreach (var it in items) _registerItems.Add(it);
        }

        private void BuildRegisterCards()
        {
            flowRegisters.SuspendLayout();
            flowRegisters.Controls.Clear();
            _registerDisplays.Clear();

            foreach (var item in _registerItems)
            {
                var display = CreateRegisterDisplay(item);
                _registerDisplays[item.Address] = display;
                flowRegisters.Controls.Add(display.Container);
            }
            flowRegisters.ResumeLayout();
            if (_registerItems.Any()) SelectRegister(_registerItems[0], false);
            FlowRegisters_Resize(null, EventArgs.Empty);
        }

        private RegisterDisplay CreateRegisterDisplay(RegisterItem item)
        {
            var container = new Panel
            {
                BackColor = Color.FromArgb(72, 88, 88),
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(5),
                Width = Math.Max(200, flowRegisters.ClientSize.Width - flowRegisters.Padding.Horizontal - 20),
                Height = 80,
                Tag = item
            };

            var nameLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                Text = item.Name,
                Tag = item,
                Cursor = Cursors.Hand
            };

            var valueBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = HorizontalAlignment.Right,
                Text = item.LastValue?.ToString() ?? "0",
                Tag = item,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 6, 0, 6)
            };

            var readOnlyColor = item.IsWritable ? Color.FromArgb(224, 242, 241) : Color.FromArgb(236, 239, 241);
            var editableColor = item.IsWritable ? Color.FromArgb(129, 199, 212) : readOnlyColor;
            valueBox.BackColor = readOnlyColor;
            valueBox.ReadOnly = true;
            valueBox.Cursor = Cursors.Arrow;
            valueBox.TabStop = false;

            var metaLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gainsboro,
                Text = $"Addr: 0x{item.Address:X4} | {item.ChangeableBy}",
                Tag = item
            };

            container.Controls.Add(metaLabel);
            container.Controls.Add(valueBox);
            container.Controls.Add(nameLabel);

            nameLabel.Click += RegisterCard_Click;
            metaLabel.Click += RegisterCard_Click;
            container.Click += RegisterCard_Click;
            valueBox.Click += RegisterCard_Click;
            valueBox.Enter += RegisterValueBox_Enter;

            return new RegisterDisplay
            {
                Item = item,
                Container = container,
                NameLabel = nameLabel,
                MetaLabel = metaLabel,
                ValueBox = valueBox,
                BaseColor = container.BackColor,
                HighlightColor = ControlPaint.Light(container.BackColor, 0.25f),
                ReadOnlyBackColor = readOnlyColor,
                EditableBackColor = editableColor
            };
        }

        private void RegisterCard_Click(object? sender, EventArgs e)
        {
            if (sender is Control c && c.Tag is RegisterItem it) SelectRegister(it);
        }
        private void RegisterValueBox_Enter(object? sender, EventArgs e)
        {
            if (sender is Control c && c.Tag is RegisterItem it) SelectRegister(it, false);
        }

        private void SelectRegister(RegisterItem item, bool focusValueBox = true)
        {
            var previous = _selectedRegister;
            if (previous is not null && _registerDisplays.TryGetValue(previous.Address, out var prev))
            {
                prev.ValueBox.ReadOnly = true;
                prev.ValueBox.BackColor = prev.ReadOnlyBackColor;
                prev.ValueBox.Cursor = Cursors.Arrow;
                prev.ValueBox.TabStop = false;
            }
            _selectedRegister = item;
            foreach (var disp in _registerDisplays.Values)
            {
                bool sel = ReferenceEquals(disp.Item, item);
                disp.Container.BackColor = sel ? disp.HighlightColor : disp.BaseColor;
                disp.NameLabel.ForeColor = sel ? Color.White : Color.WhiteSmoke;
                disp.MetaLabel.ForeColor = sel ? Color.WhiteSmoke : Color.Gainsboro;
            }
            lblSelectedRegisterName.Text = item.Name;
            txtSelectedRegisterInfo.Text = item.CreateDescriptionText();
            UpdateConnectionState();
            if (focusValueBox && _registerDisplays.TryGetValue(item.Address, out var s) && !s.ValueBox.ReadOnly)
            { s.ValueBox.Focus(); s.ValueBox.SelectAll(); }
        }

        // ===== connection & buttons =====
        private void RefreshPortList()
        {
            var selectedPort = cmbPort.SelectedItem as string;
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            cmbPort.BeginUpdate();
            cmbPort.Items.Clear();
            cmbPort.Items.AddRange(ports);
            cmbPort.EndUpdate();
            if (!string.IsNullOrEmpty(selectedPort) && ports.Contains(selectedPort)) cmbPort.SelectedItem = selectedPort;
            else if (ports.Any()) cmbPort.SelectedIndex = 0;
        }

        private void btnRefreshPorts_Click(object? sender, EventArgs e) => RefreshPortList();

        private void btnConnect_Click(object? sender, EventArgs e)
        {
            if (cmbPort.SelectedItem is not string portName)
            { MessageBox.Show("Select a COM port first.", "Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                DisconnectInternal();
                _modbusClient = new ModbusClient(portName) { Baudrate = 9600, Parity = Parity.Even, StopBits = StopBits.One, UnitIdentifier = 1 };
                AttachClientEvents(_modbusClient);
                _modbusClient.Connect();
                _connectedPortName = portName;
                ResetRxTxLog();
                AppendActivity($"{portName} Connected");
                EnsurePollingTimerRunning();
                SeedVersionRegister();
            }
            catch (Exception ex)
            { AppendActivity($"Connection Error: {ex.Message}"); MessageBox.Show($"Could not connect: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); DisconnectInternal(); }
            finally { UpdateConnectionState(); }
        }

        private void btnDisconnect_Click(object? sender, EventArgs e) { DisconnectInternal(); UpdateConnectionState(); }

        private void DisconnectInternal()
        {
            StopPollingTimer();
            if (_modbusClient is { } existing)
            {
                DetachClientEvents(existing);
                if (existing.Connected)
                {
                    try { existing.Disconnect(); AppendActivity("Disconnected."); }
                    catch (Exception ex) { AppendActivity($"Disconnect error: {ex.Message}"); }
                }
            }
            _modbusClient = null; _connectedPortName = null;
            lock (_stateLock)
            {
                _lastPollByEvent.Clear();
                _automationLastProcessedEvent = null;
                _startingEnteredAt = DateTime.MinValue;
                _cyclingLastTick = DateTime.MinValue;
                _cycleFinishedEnteredAt = DateTime.MinValue;
                _lastManualPoll = DateTime.MinValue;
                _lastEventTrackerValue = 10;
            }
        }

        private void UpdateConnectionState()
        {
            bool connected = _modbusClient?.Connected ?? false;
            btnConnect.Enabled = !connected; btnDisconnect.Enabled = connected; btnRefreshPorts.Enabled = !connected; cmbPort.Enabled = !connected;
            btnRead.Enabled = connected && _selectedRegister is not null; btnWrite.Enabled = connected && _selectedRegister?.IsWritable == true;
            lblConnectionStatus.Text = connected ? $"Status: {_connectedPortName ?? "Connected"}" : "Status: Disconnected";
            if (_selectedRegister is RegisterItem sel && _registerDisplays.TryGetValue(sel.Address, out var d))
            {
                bool canEdit = connected && sel.IsWritable; d.ValueBox.ReadOnly = !canEdit; d.ValueBox.BackColor = canEdit ? d.EditableBackColor : d.ReadOnlyBackColor; d.ValueBox.Cursor = canEdit ? Cursors.IBeam : Cursors.Arrow; d.ValueBox.TabStop = canEdit;
            }
        }

        private bool EnsureConnected(bool showMessage = true)
        {
            if (_modbusClient is { Connected: true }) return true;
            if (showMessage) MessageBox.Show("Connect first.", "Connection required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private bool EnsureRegisterSelected()
        {
            if (_selectedRegister is not null) return true;
            MessageBox.Show("Select a register first.", "Select Register", MessageBoxButtons.OK, MessageBoxIcon.Information); return false;
        }

        private void btnRead_Click(object? sender, EventArgs e)
        { if (!EnsureRegisterSelected() || !EnsureConnected()) return; ReadRegister(_selectedRegister!, true, true); }

        private void btnWrite_Click(object? sender, EventArgs e)
        {
            if (!EnsureRegisterSelected() || !EnsureConnected()) return;
            var item = _selectedRegister!; if (!item.IsWritable) { MessageBox.Show("This register is read-only.", "Write Blocked", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (!_registerDisplays.TryGetValue(item.Address, out var display)) return;
            try
            {
                if (string.Equals(item.Type, "UDINT", StringComparison.OrdinalIgnoreCase))
                {
                    if (!ulong.TryParse(display.ValueBox.Text, out var ul) || ul > uint.MaxValue) { MessageBox.Show("UDINT must be 0..4294967295.", "Check", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    uint u32 = (uint)ul; var words = SplitUInt32ToWords(u32); _modbusClient!.WriteMultipleRegisters(item.Address, words);
                    AppendActivity($"{item.Name} (0x{item.Address:X4}) written: {u32}"); UpdateRegisterValueDisplay(item, u32.ToString());
                }
                else
                {
                    if (!ushort.TryParse(display.ValueBox.Text, out ushort value)) { MessageBox.Show("UINT must be 0..65535.", "Check", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    _modbusClient!.WriteSingleRegister(item.Address, value); AppendActivity($"{item.Name} (0x{item.Address:X4}) written: {value}"); UpdateRegisterValueDisplay(item, value);
                }
            }
            catch (Exception ex) { AppendActivity($"Write error ({item.Name}): {ex.Message}"); MessageBox.Show($"Write failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ReadRegister(RegisterItem item, bool showErrors, bool logActivity)
        {
            if (!EnsureConnected(showErrors)) return;
            try
            {
                var values = _modbusClient!.ReadHoldingRegisters(item.Address, item.WordLength);
                if (values.Length == 0) return;
                if (string.Equals(item.Type, "UDINT", StringComparison.OrdinalIgnoreCase))
                { if (values.Length < 2) throw new Exception("Could not read 2 words for UDINT."); uint u32 = CombineToUInt32(values[0], values[1]); UpdateRegisterValueDisplay(item, u32.ToString()); if (logActivity) AppendActivity($"{item.Name} (0x{item.Address:X4}) read: {u32}"); }
                else
                { int u16 = values[0] & 0xFFFF; UpdateRegisterValueDisplay(item, u16); if (logActivity) AppendActivity($"{item.Name} (0x{item.Address:X4}) read: {u16}"); }
            }
            catch (Exception ex) { AppendActivity($"Read error ({item.Name}): {ex.Message}"); if (showErrors) MessageBox.Show($"Read failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private RegisterItem? GetRegisterByAddress(int address)
        {
            if (_registerDisplays.TryGetValue(address, out var display))
            {
                return display.Item;
            }

            return _registerItems.FirstOrDefault(r => r.Address == address);
        }

        private void UpdateRegisterValueDisplay(RegisterItem item, int value)
        {
            item.LastValue = value;
            if (item.Address == RegEventTracker) // Event Tracker -> sync combo
            {
                if (_eventMapByValue.TryGetValue(value, out var label))
                {
                    Ui(() => { _suppressEventCombo = true; if (!Equals(cmbEvent.SelectedItem, label)) cmbEvent.SelectedItem = label; _suppressEventCombo = false; });
                }
                OnEventTrackerValueObserved(value);
            }
            if (_registerDisplays.TryGetValue(item.Address, out var display))
            {
                string newText = value.ToString(); if (!string.Equals(display.ValueBox.Text, newText, StringComparison.Ordinal)) Ui(() => display.ValueBox.Text = newText);
            }
        }
        private void UpdateRegisterValueDisplay(RegisterItem item, string text)
        {
            item.LastValue = null; if (_registerDisplays.TryGetValue(item.Address, out var display)) if (!string.Equals(display.ValueBox.Text, text, StringComparison.Ordinal)) Ui(() => display.ValueBox.Text = text);
        }

        private void PollTimer_Tick(object? sender, EventArgs e)
        {
            if (_isPolling) return; _isPolling = true;
            System.Threading.Tasks.Task.Run(() => { try { lock (_pollLock) { CyclicStep(); } } finally { _isPolling = false; } });
        }

        private void EnsurePollingTimerRunning()
        {
            if (!_pollTimer.Enabled)
            {
                _pollTimer.Start();
            }
        }

        private void StopPollingTimer()
        {
            if (_pollTimer.Enabled)
            {
                _pollTimer.Stop();
            }
        }

        // ===== CYCLIC / AUTOMATION =====
        private readonly object _pollLock = new();
        private T UiGet<T>(Func<T> f) { if (InvokeRequired) return (T)Invoke(f); return f(); }
        private void Ui(Action a) { if (InvokeRequired) BeginInvoke(a); else a(); }

        private List<RegisterItem> GetItemsInRange(int startAddr, int endAddr) => _registerItems.Where(it => it.Address >= startAddr && it.Address <= endAddr).OrderBy(it => it.Address).ToList();

        private Dictionary<int, int> BulkReadRangeUpdate(List<RegisterItem> items, ref bool polled)
        {
            var result = new Dictionary<int, int>(); if (items.Count == 0 || !EnsureConnected(false)) return result;
            int start = items.Min(it => it.Address); int maxEnd = items.Max(it => it.Address + it.WordLength); int totalWords = maxEnd - start; if (totalWords <= 0) return result;
            int[] all; try { all = _modbusClient!.ReadHoldingRegisters(start, totalWords); polled = true; } catch { Ui(() => StopPollingTimer()); return result; }
            foreach (var item in items)
            {
                int offset = item.Address - start; if (offset < 0 || offset >= all.Length) continue;
                if (string.Equals(item.Type, "UDINT", StringComparison.OrdinalIgnoreCase))
                { if (offset + 1 >= all.Length) continue; uint u32 = CombineToUInt32(all[offset], all[offset + 1]); Ui(() => UpdateRegisterValueDisplay(item, u32.ToString())); }
                else
                { int u16 = all[offset] & 0xFFFF; result[item.Address] = u16; Ui(() => UpdateRegisterValueDisplay(item, u16)); }
            }
            return result;
        }

        private void WriteUdint(int addr, uint value) => _modbusClient!.WriteMultipleRegisters(addr, SplitUInt32ToWords(value));
        private ushort ReadUInt16(int addr)
        {
            var words = _modbusClient!.ReadHoldingRegisters(addr, 1);
            return words.Length > 0 ? (ushort)words[0] : (ushort)0;
        }
        private uint ReadUdint(int addr)
        {
            var v = _modbusClient!.ReadHoldingRegisters(addr, 2);
            return CombineToUInt32(v[0], v[1]);
        }
        private void WriteMasterRange0to6()
        {
            if (!EnsureConnected(false)) return; var items = GetItemsInRange(RegVersionNumber, RegRemainingTime).Where(i => i.IsWritable).ToList();
            foreach (var item in items)
            {
                if (item.Address == RegPollCounter) continue; // poll counter handled separately
                if (!_registerDisplays.TryGetValue(item.Address, out var disp)) continue;
                try
                {
                    if (string.Equals(item.Type, "UDINT", StringComparison.OrdinalIgnoreCase))
                    { string text = UiGet(() => disp.ValueBox.Text); if (!ulong.TryParse(text, out var ul) || ul > uint.MaxValue) continue; uint u32 = (uint)ul; WriteUdint(item.Address, u32); Ui(() => UpdateRegisterValueDisplay(item, u32.ToString())); }
                    else
                    {
                        string text = UiGet(() => disp.ValueBox.Text); if (!ushort.TryParse(text, out ushort value)) continue;
                        _modbusClient!.WriteSingleRegister(item.Address, value); Ui(() => UpdateRegisterValueDisplay(item, value));
                    }
                }
                catch { /* ignore write errors in cyclic */ }
            }
        }

        private void SeedVersionRegister()
        {
            if (!EnsureConnected(false)) return;
            try
            {
                _modbusClient!.WriteSingleRegister(RegVersionNumber, 1);
                var versionItem = GetRegisterByAddress(RegVersionNumber);
                if (versionItem != null) Ui(() => UpdateRegisterValueDisplay(versionItem, 1));
            }
            catch (Exception ex)
            {
                AppendActivity($"Failed to seed version register: {ex.Message}");
            }
        }

        // Ensure the master-writable run registers (total/program/time) are reset to zero.
        // We invoke this whenever an automation state requires a clean slate before continuing.
        private void ResetRunRegistersToZero()
        {
            try
            {
                bool totalNeedsZero = true;
                if (_registerDisplays.TryGetValue(RegTotalPriceToPay, out var totalDisplay))
                {
                    string text = UiGet(() => totalDisplay.ValueBox.Text);
                    totalNeedsZero = !string.Equals(text, "0", StringComparison.Ordinal);
                }

                bool programNeedsZero = true;
                if (_registerDisplays.TryGetValue(RegSelectedProgramNumber, out var programDisplay))
                {
                    string text = UiGet(() => programDisplay.ValueBox.Text);
                    programNeedsZero = !string.Equals(text, "0", StringComparison.Ordinal);
                }

                bool remainingNeedsZero = true;
                if (_registerDisplays.TryGetValue(RegRemainingTime, out var remainingDisplay))
                {
                    string text = UiGet(() => remainingDisplay.ValueBox.Text);
                    remainingNeedsZero = !string.Equals(text, "0", StringComparison.Ordinal);
                }

                if (totalNeedsZero)
                {
                    WriteUdint(RegTotalPriceToPay, 0u);
                    var totalItem = GetRegisterByAddress(RegTotalPriceToPay);
                    if (totalItem != null) Ui(() => UpdateRegisterValueDisplay(totalItem, "0"));
                }

                if (programNeedsZero)
                {
                    _modbusClient!.WriteSingleRegister(RegSelectedProgramNumber, 0);
                    var programItem = GetRegisterByAddress(RegSelectedProgramNumber);
                    if (programItem != null) Ui(() => UpdateRegisterValueDisplay(programItem, 0));
                }

                if (remainingNeedsZero)
                {
                    _modbusClient!.WriteSingleRegister(RegRemainingTime, 0);
                    var remainingItem = GetRegisterByAddress(RegRemainingTime);
                    if (remainingItem != null) Ui(() => UpdateRegisterValueDisplay(remainingItem, 0));
                }
            }
            catch (Exception ex)
            {
                AppendActivity($"Failed to zero master registers: {ex.Message}");
            }
        }

        // The payment system raises bit0 of the Payment System Status register to signal that
        // updated program prices are available.  Callers can request a full sweep of the price table
        // (0x000C–0x0046 in the new layout) while
        // the flag is raised; regardless, we mirror the acknowledgement by forcing controller-
        // status bit0 high.  Once the slave drops its availability flag we immediately clear our
        // acknowledgement.
        private void SynchronizeProgramPriceUpdate(ushort paymentStatus, bool pollFullRange, ref bool performedPoll)
        {
            bool priceUpdateAvailable = (paymentStatus & 0x0001) != 0;

            if (priceUpdateAvailable)
            {
                if (pollFullRange)
                {
                    BulkReadRangeUpdate(GetItemsInRange(RegPriceTablePollStart, RegPriceTablePollEnd), ref performedPoll);
                }

                // Availability asserted → acknowledge by raising the controller's finished flag.
                SetControllerStatusBit0(true, ref performedPoll);

                // If the slave keeps the availability flag up for multiple scans we simply keep
                // the acknowledgement high; the payment system will drop its flag once it has
                // observed the controller bit and completed the update cycle.
                return;
            }

            // Availability cleared → always mirror it by resetting the finished flag so the UI
            // never shows the controller as done when the payment system has nothing pending.
            SetControllerStatusBit0(false, ref performedPoll);
        }

        // Convenience helper to raise/lower controller status bit0 while keeping the UI in sync
        // and avoiding redundant writes if the bit is already in the desired state.
        private void SetControllerStatusBit0(bool on, ref bool performedPoll)
        {
            ushort current;
            var statusItem = GetRegisterByAddress(RegControllerStatus);
            if (statusItem?.LastValue is int last)
            {
                current = (ushort)last;
            }
            else
            {
                try
                {
                    current = ReadUInt16(RegControllerStatus);
                    performedPoll = true;
                    if (statusItem != null) Ui(() => UpdateRegisterValueDisplay(statusItem, current));
                }
                catch (Exception ex)
                {
                    AppendActivity($"Failed to read controller status: {ex.Message}");
                    return;
                }
            }

            ushort desired = on ? (ushort)(current | 0x0001) : (ushort)(current & ~0x0001);
            if (desired == current) return;

            try
            {
                _modbusClient!.WriteSingleRegister(RegControllerStatus, desired);
                if (statusItem != null) Ui(() => UpdateRegisterValueDisplay(statusItem, desired));
            }
            catch (Exception ex)
            {
                AppendActivity($"Failed to update controller status bit: {ex.Message}");
            }
        }

        // Bump Poll Counter (0x0003) and mirror the value in the register card.  Only invoked
        // after an actual Modbus read/write to stay faithful to the hardware behavior.
        private void IncrementPollCounter()
        {
            try
            {
                ushort current = ReadUInt16(RegPollCounter);
                ushort next = (ushort)((current + 1) & 0xFFFF);
                _modbusClient!.WriteSingleRegister(RegPollCounter, next);
                var pollItem = GetRegisterByAddress(RegPollCounter);
                if (pollItem != null) Ui(() => UpdateRegisterValueDisplay(pollItem, next));
            }
            catch (Exception ex)
            {
                AppendActivity($"Poll counter update failed: {ex.Message}");
            }
        }

        private void CyclicStep()
        {
            if (!EnsureConnected(false)) { Ui(() => StopPollingTimer()); return; }

            bool automationEnabled = UiGet(() => chkAutomationEnabled.Checked);
            if (!automationEnabled)
            {
                HandleManualPolling();
                return;
            }

            int curEvent;
            int? lastProcessed;
            DateTime lastPoll;
            DateTime startingEnteredAt;
            DateTime cycleFinishedEnteredAt;

            lock (_stateLock)
            {
                curEvent = _lastEventTrackerValue ?? 10;
                lastProcessed = _automationLastProcessedEvent;
                if (!_lastPollByEvent.TryGetValue(curEvent, out lastPoll))
                {
                    lastPoll = DateTime.MinValue;
                }
                startingEnteredAt = _startingEnteredAt;
                cycleFinishedEnteredAt = _cycleFinishedEnteredAt;
            }

            DateTime now = DateTime.UtcNow;
            bool eventChanged = lastProcessed != curEvent;

            if (eventChanged)
            {
                lock (_stateLock)
                {
                    _automationLastProcessedEvent = curEvent;
                    _lastPollByEvent[curEvent] = DateTime.MinValue;
                    _startingEnteredAt = curEvent == 40 ? now : DateTime.MinValue;
                    _cyclingLastTick = curEvent == 50 ? now : DateTime.MinValue;
                    _cycleFinishedEnteredAt = curEvent == 60 ? now : DateTime.MinValue;
                    startingEnteredAt = _startingEnteredAt;
                    cycleFinishedEnteredAt = _cycleFinishedEnteredAt;
                }
                lastPoll = DateTime.MinValue;
            }

            TimeSpan pollInterval = GetPollIntervalForEvent(curEvent);
            bool shouldPollNow = lastPoll == DateTime.MinValue || now - lastPoll >= pollInterval;

            bool performedPoll = false;

            switch (curEvent)
            {
                case 10: // Idle
                {
                    if (eventChanged)
                    {
                        ResetRunRegistersToZero();
                    }
                    if (shouldPollNow && PollPaymentStatus("Idle", pollFullRange: true, ref performedPoll))
                    {
                        MarkEventPoll(curEvent, now);
                    }
                    break;
                }
                case 20: // Program Selection
                case 21: // Extra Selection
                {
                    if (eventChanged)
                    {
                        ResetRunRegistersToZero();
                    }
                    if (shouldPollNow && PollPaymentStatus("Selection", pollFullRange: true, ref performedPoll))
                    {
                        MarkEventPoll(curEvent, now);
                    }
                    break;
                }
                case 30: // Payment
                {
                    if (eventChanged)
                    {
                        RandomizePaymentRegisters();
                    }
                    if (shouldPollNow && PollPaymentAndAdvance(ref performedPoll))
                    {
                        MarkEventPoll(curEvent, now);
                    }
                    break;
                }
                case 40: // Starting
                {
                    if (shouldPollNow && PollPaymentStatus("Starting", pollFullRange: true, ref performedPoll))
                    {
                        MarkEventPoll(curEvent, now);
                    }
                    HandleStartingTransition(now, startingEnteredAt);
                    break;
                }
                case 50: // Cycling
                {
                    if (shouldPollNow && PollPaymentStatus("Cycling", pollFullRange: true, ref performedPoll))
                    {
                        MarkEventPoll(curEvent, now);
                    }
                    HandleCyclingCountdown(now, ref performedPoll);
                    break;
                }
                case 60: // Cycle Finished
                {
                    if (shouldPollNow && PollPaymentStatus("Cycle Finished", pollFullRange: true, ref performedPoll))
                    {
                        MarkEventPoll(curEvent, now);
                    }
                    HandleCycleFinishedReturn(now, cycleFinishedEnteredAt);
                    break;
                }
                case 999: // Machine Unavailable
                {
                    if (shouldPollNow && PollPaymentStatus("Machine Unavailable", pollFullRange: true, ref performedPoll))
                    {
                        MarkEventPoll(curEvent, now);
                    }
                    break;
                }
                default:
                {
                    if (shouldPollNow && PollPaymentStatus("Automation", pollFullRange: true, ref performedPoll))
                    {
                        MarkEventPoll(curEvent, now);
                    }
                    break;
                }
            }

            if (performedPoll)
            {
                IncrementPollCounter();
            }
        }

        private void HandleManualPolling()
        {
            DateTime now = DateTime.UtcNow;
            if (now - _lastManualPoll < TimeSpan.FromSeconds(2)) return;

            bool performedPoll = false;

            WriteMasterRange0to6();
            var map = BulkReadRangeUpdate(GetItemsInRange(RegPaymentSystemStatus, RegDiscountAmount), ref performedPoll);
            if (map.TryGetValue(RegPaymentSystemStatus, out int statusValue))
            {
                SynchronizeProgramPriceUpdate((ushort)(statusValue & 0xFFFF), pollFullRange: true, ref performedPoll);
            }

            if (performedPoll)
            {
                IncrementPollCounter();
            }

            lock (_stateLock)
            {
                _lastManualPoll = now;
            }
        }

        private void PrimeEventTrackerFromSlave()
        {
            if (_modbusClient is not { Connected: true }) return;

            try
            {
                var arr = _modbusClient.ReadHoldingRegisters(RegEventTracker, 1);
                if (arr.Length == 0) return;
                int value = arr[0] & 0xFFFF;
                var evtItem = GetRegisterByAddress(RegEventTracker);
                if (evtItem != null) Ui(() => UpdateRegisterValueDisplay(evtItem, value));
                IncrementPollCounter();
            }
            catch (Exception ex)
            {
                AppendActivity($"Event tracker read failed: {ex.Message}");
            }
        }

        private void MarkEventPoll(int eventValue, DateTime timestamp)
        {
            lock (_stateLock)
            {
                _lastPollByEvent[eventValue] = timestamp;
            }
        }

        private TimeSpan GetPollIntervalForEvent(int eventValue) => eventValue switch
        {
            10 => TimeSpan.FromMinutes(1),
            20 => TimeSpan.FromSeconds(5),
            21 => TimeSpan.FromSeconds(5),
            30 => TimeSpan.FromSeconds(2),
            40 => TimeSpan.FromSeconds(5),
            50 => TimeSpan.FromMinutes(1),
            60 => TimeSpan.FromMinutes(1),
            999 => TimeSpan.FromMinutes(1),
            _ => TimeSpan.FromSeconds(5)
        };

        private bool PollPaymentStatus(string context, bool pollFullRange, ref bool performedPoll)
        {
            try
            {
                ushort status = ReadUInt16(RegPaymentSystemStatus);
                performedPoll = true;
                var statusItem = GetRegisterByAddress(RegPaymentSystemStatus);
                if (statusItem != null) Ui(() => UpdateRegisterValueDisplay(statusItem, status));
                SynchronizeProgramPriceUpdate(status, pollFullRange, ref performedPoll);
                return true;
            }
            catch (Exception ex)
            {
                AppendActivity($"{context} payment poll failed: {ex.Message}");
                return false;
            }
        }

        private void RandomizePaymentRegisters()
        {
            try
            {
                uint total = (uint)_rng.Next(1, 100000);
                ushort program = (ushort)_rng.Next(1, 21);
                ushort remaining = (ushort)_rng.Next(5, 46);

                WriteUdint(RegTotalPriceToPay, total);
                _modbusClient!.WriteSingleRegister(RegSelectedProgramNumber, program);
                _modbusClient!.WriteSingleRegister(RegRemainingTime, remaining);

                var totalItem = GetRegisterByAddress(RegTotalPriceToPay);
                if (totalItem != null) Ui(() => UpdateRegisterValueDisplay(totalItem, total.ToString()));
                var programItem = GetRegisterByAddress(RegSelectedProgramNumber);
                if (programItem != null) Ui(() => UpdateRegisterValueDisplay(programItem, program));
                var remainingItem = GetRegisterByAddress(RegRemainingTime);
                if (remainingItem != null) Ui(() => UpdateRegisterValueDisplay(remainingItem, remaining));

                AppendActivity($"Payment randomised: total={total}, program={program}, remaining={remaining}");
            }
            catch (Exception ex)
            {
                AppendActivity($"Payment randomisation failed: {ex.Message}");
            }
        }

        private bool PollPaymentAndAdvance(ref bool performedPoll)
        {
            try
            {
                ushort paymentStatus = ReadUInt16(RegPaymentSystemStatus);
                performedPoll = true;
                var statusItem = GetRegisterByAddress(RegPaymentSystemStatus);
                if (statusItem != null) Ui(() => UpdateRegisterValueDisplay(statusItem, paymentStatus));
                SynchronizeProgramPriceUpdate(paymentStatus, pollFullRange: true, ref performedPoll);

                uint paidAmount = ReadUdint(RegPaidAmount);
                performedPoll = true;
                var paidItem = GetRegisterByAddress(RegPaidAmount);
                if (paidItem != null) Ui(() => UpdateRegisterValueDisplay(paidItem, paidAmount.ToString()));

                uint discountAmount = ReadUdint(RegDiscountAmount);
                performedPoll = true;
                var discountItem = GetRegisterByAddress(RegDiscountAmount);
                if (discountItem != null) Ui(() => UpdateRegisterValueDisplay(discountItem, discountAmount.ToString()));

                uint totalToPay = ReadUdint(RegTotalPriceToPay);
                performedPoll = true;
                var totalItem = GetRegisterByAddress(RegTotalPriceToPay);
                if (totalItem != null) Ui(() => UpdateRegisterValueDisplay(totalItem, totalToPay.ToString()));

                bool paymentReady = paidAmount >= totalToPay;
                bool requireDiscountCheck = (paymentStatus & 0x0002) != 0 || (paymentStatus & 0x0004) != 0;
                if (requireDiscountCheck)
                {
                    paymentReady = paidAmount + discountAmount >= totalToPay;
                }

                if (paymentReady)
                {
                    try
                    {
                        _modbusClient!.WriteSingleRegister(RegEventTracker, 40);
                        var evtItem = GetRegisterByAddress(RegEventTracker);
                        if (evtItem != null) Ui(() => UpdateRegisterValueDisplay(evtItem, 40));
                        AppendActivity("Payment satisfied → Starting");
                    }
                    catch (Exception ex)
                    {
                        AppendActivity($"Failed to advance to Starting: {ex.Message}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                AppendActivity($"Payment polling failed: {ex.Message}");
                return false;
            }
        }

        private void HandleStartingTransition(DateTime now, DateTime enteredAt)
        {
            if (enteredAt == DateTime.MinValue) return;
            if (now - enteredAt < TimeSpan.FromSeconds(5)) return;

            try
            {
                WriteUdint(RegTotalPriceToPay, 0u);
                var totalItem = GetRegisterByAddress(RegTotalPriceToPay);
                if (totalItem != null) Ui(() => UpdateRegisterValueDisplay(totalItem, "0"));

                _modbusClient!.WriteSingleRegister(RegEventTracker, 50);
                var evtItem = GetRegisterByAddress(RegEventTracker);
                if (evtItem != null) Ui(() => UpdateRegisterValueDisplay(evtItem, 50));
                AppendActivity("Starting complete → Cycling");
            }
            catch (Exception ex)
            {
                AppendActivity($"Failed to advance to Cycling: {ex.Message}");
                return;
            }

            lock (_stateLock)
            {
                _startingEnteredAt = DateTime.MinValue;
            }
        }

        private void HandleCyclingCountdown(DateTime now, ref bool performedPoll)
        {
            bool shouldTick;
            lock (_stateLock)
            {
                if (_cyclingLastTick == DateTime.MinValue)
                {
                    _cyclingLastTick = now;
                    return;
                }

                shouldTick = now - _cyclingLastTick >= TimeSpan.FromSeconds(5);
                if (shouldTick)
                {
                    _cyclingLastTick = now;
                }
            }

            if (!shouldTick) return;

            ushort remaining;
            var remainingItem = GetRegisterByAddress(RegRemainingTime);
            if (remainingItem?.LastValue is int lastValue)
            {
                remaining = (ushort)lastValue;
            }
            else
            {
                try
                {
                    remaining = ReadUInt16(RegRemainingTime);
                    performedPoll = true;
                    if (remainingItem != null) Ui(() => UpdateRegisterValueDisplay(remainingItem, remaining));
                }
                catch (Exception ex)
                {
                    AppendActivity($"Cycling decrement failed: {ex.Message}");
                    return;
                }
            }

            if (remaining == 0) return;

            ushort next = (ushort)(remaining - 1);
            try
            {
                _modbusClient!.WriteSingleRegister(RegRemainingTime, next);
                if (remainingItem != null) Ui(() => UpdateRegisterValueDisplay(remainingItem, next));
            }
            catch (Exception ex)
            {
                AppendActivity($"Cycling decrement failed: {ex.Message}");
                return;
            }

            if (next == 0)
            {
                try
                {
                    _modbusClient!.WriteSingleRegister(RegEventTracker, 60);
                    var evtItem = GetRegisterByAddress(RegEventTracker);
                    if (evtItem != null) Ui(() => UpdateRegisterValueDisplay(evtItem, 60));
                    AppendActivity("Cycle finished → Cycle Finished");
                    lock (_stateLock)
                    {
                        _cycleFinishedEnteredAt = DateTime.UtcNow;
                        _automationLastProcessedEvent = null;
                    }
                }
                catch (Exception ex)
                {
                    AppendActivity($"Failed to advance to Cycle Finished: {ex.Message}");
                }
            }
        }

        private void HandleCycleFinishedReturn(DateTime now, DateTime enteredAt)
        {
            if (enteredAt == DateTime.MinValue) return;
            if (now - enteredAt < TimeSpan.FromSeconds(10)) return;

            try
            {
                _modbusClient!.WriteSingleRegister(RegEventTracker, 10);
                var evtItem = GetRegisterByAddress(RegEventTracker);
                if (evtItem != null) Ui(() => UpdateRegisterValueDisplay(evtItem, 10));
                AppendActivity("Cycle finished timeout → Idle");
                ResetRunRegistersToZero();
            }
            catch (Exception ex)
            {
                AppendActivity($"Failed to return to Idle: {ex.Message}");
                return;
            }

            lock (_stateLock)
            {
                _cycleFinishedEnteredAt = DateTime.MinValue;
            }
        }

        private void OnEventTrackerValueObserved(int value)
        {
            lock (_stateLock)
            {
                int? previous = _lastEventTrackerValue;
                _lastEventTrackerValue = value;
                if (previous == value) return;

                _automationLastProcessedEvent = null;
                _lastPollByEvent[value] = DateTime.MinValue;

                if (value != 40) _startingEnteredAt = DateTime.MinValue;
                if (value != 50) _cyclingLastTick = DateTime.MinValue;
                if (value != 60) _cycleFinishedEnteredAt = DateTime.MinValue;
            }
        }

        // ===== Event combobox handler =====
        private void CmbEvent_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressEventCombo) return; if (cmbEvent.SelectedItem is not string name) return;
            if (_eventMapByName == null || !_eventMapByName.TryGetValue(name, out int value)) return;
            try { if (_modbusClient is { Connected: true }) { _modbusClient.WriteSingleRegister(RegEventTracker, (ushort)value); AppendActivity($"Machine Event → {name} ({value})"); } }
            catch (Exception ex) { AppendActivity($"Machine Event write error: {ex.Message}"); }
            var evtItem = _registerItems.FirstOrDefault(r => r.Address == RegEventTracker); if (evtItem != null) UpdateRegisterValueDisplay(evtItem, value);
        }

        private void CmbMachineEvent_SelectedIndexChanged(object? sender, EventArgs e) => CmbEvent_SelectedIndexChanged(sender, e);

        private void chkAutomationEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            bool enabled = chkAutomationEnabled.Checked;
            AppendActivity(enabled ? "Automation enabled." : "Automation disabled.");
            lock (_stateLock)
            {
                _automationLastProcessedEvent = null;
                _lastPollByEvent.Clear();
                if (!enabled)
                {
                    _startingEnteredAt = DateTime.MinValue;
                    _cyclingLastTick = DateTime.MinValue;
                    _cycleFinishedEnteredAt = DateTime.MinValue;
                }
            }

            if (enabled)
            {
                PrimeEventTrackerFromSlave();
            }
        }

        private void headerPanel_Paint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(64, Color.Black));
            e.Graphics.DrawLine(pen, 0, e.ClipRectangle.Bottom - 1, e.ClipRectangle.Right, e.ClipRectangle.Bottom - 1);
        }

        private void flowRegisters_Paint(object? sender, PaintEventArgs e)
        {
            // No custom painting required; method exists to satisfy designer hook.
        }

        private void label1_Click(object? sender, EventArgs e)
        {
            // Intentionally left empty; label is informational only.
        }

        private void lstActivity_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Keep the activity log read-only by clearing the selection.
            if (lstActivity.SelectedIndex >= 0) lstActivity.ClearSelected();
        }

        private void lstRxTx_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstRxTx.SelectedIndex >= 0) lstRxTx.ClearSelected();
        }

        // ===== misc UI =====
        private void FlowRegisters_Resize(object? sender, EventArgs e)
        { int width = flowRegisters.ClientSize.Width - flowRegisters.Padding.Horizontal - 8; foreach (Control c in flowRegisters.Controls) c.Width = Math.Max(200, width); }

        private void AppendActivity(string message)
        {
            void DoAppend()
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                lstActivity.Items.Insert(0, $"[{timestamp}] {message}");
                while (lstActivity.Items.Count > 300) lstActivity.Items.RemoveAt(lstActivity.Items.Count - 1);
            }
            if (InvokeRequired) BeginInvoke((Action)DoAppend); else DoAppend();
        }

        private void AppendRxTx(bool isTransmit, byte[] data)
        {
            if (data.Length == 0) return;
            var hex = BitConverter.ToString(data).Replace("-", " ");
            int sequence = Interlocked.Increment(ref _rxTxSequence);
            string direction = isTransmit ? "Tx" : "Rx";
            string line = $"{sequence:D6}-> {direction}::{hex}";

            void DoAppend()
            {
                lstRxTx.Items.Insert(0, line);
                while (lstRxTx.Items.Count > RxTxLogCapacity)
                {
                    lstRxTx.Items.RemoveAt(lstRxTx.Items.Count - 1);
                }
            }

            if (InvokeRequired) BeginInvoke((Action)DoAppend); else DoAppend();
        }

        private void ResetRxTxLog()
        {
            _rxTxSequence = 0;
            void DoReset() => lstRxTx.Items.Clear();
            if (InvokeRequired) BeginInvoke((Action)DoReset); else DoReset();
        }

        private void AttachClientEvents(ModbusClient client)
        {
            client.SendDataChanged += OnModbusSendDataChanged;
            client.ReceiveDataChanged += OnModbusReceiveDataChanged;
        }

        private void DetachClientEvents(ModbusClient client)
        {
            client.SendDataChanged -= OnModbusSendDataChanged;
            client.ReceiveDataChanged -= OnModbusReceiveDataChanged;
        }

        private void OnModbusSendDataChanged(object sender)
        {
            if (sender is not ModbusClient client) return;
            var buffer = GetClientBuffer(client, SendDataProperty, SendDataField);
            if (buffer != null && buffer.Length > 0) AppendRxTx(true, buffer);
        }

        private void OnModbusReceiveDataChanged(object sender)
        {
            if (sender is not ModbusClient client) return;
            var buffer = GetClientBuffer(client, ReceiveDataProperty, ReceiveDataField);
            if (buffer != null && buffer.Length > 0) AppendRxTx(false, buffer);
        }

        private static byte[]? GetClientBuffer(ModbusClient client, PropertyInfo? property, FieldInfo? field)
        {
            if (property != null && property.GetValue(client) is byte[] propData && propData.Length > 0)
            {
                var copy = new byte[propData.Length];
                Buffer.BlockCopy(propData, 0, copy, 0, propData.Length);
                return copy;
            }

            if (field != null && field.GetValue(client) is byte[] fieldData && fieldData.Length > 0)
            {
                int length = fieldData.Length;
                while (length > 0 && fieldData[length - 1] == 0) length--;
                if (length <= 0) return null;
                var copy = new byte[length];
                Buffer.BlockCopy(fieldData, 0, copy, 0, length);
                return copy;
            }

            return null;
        }

        // ===== inner types =====
        private class RegisterItem
        {
            public string Name { get; set; } = string.Empty;
            public int Address { get; set; }
            public int WordLength { get; set; } = 1;
            public string ChangeableBy { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public string ValueRange { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int? LastValue { get; set; }
            public bool IsWritable => string.Equals(ChangeableBy, "MASTER", StringComparison.OrdinalIgnoreCase);
            public string DisplayName => $"{Name} (0x{Address:X4})";
            public string CreateDescriptionText() => $"Addr: 0x{Address:X4} | Changeable By: {ChangeableBy}{Environment.NewLine}" +
                                                     $"Type: {Type} | Size: {Size}{Environment.NewLine}" +
                                                     $"Range: {ValueRange}{Environment.NewLine}" +
                                                     Description;
        }
        private class RegisterDisplay
        {
            public required RegisterItem Item { get; init; }
            public required Panel Container { get; init; }
            public required Label NameLabel { get; init; }
            public required Label MetaLabel { get; init; }
            public required TextBox ValueBox { get; init; }
            public required Color BaseColor { get; init; }
            public required Color HighlightColor { get; init; }
            public required Color ReadOnlyBackColor { get; init; }
            public required Color EditableBackColor { get; init; }
        }
    }
}
