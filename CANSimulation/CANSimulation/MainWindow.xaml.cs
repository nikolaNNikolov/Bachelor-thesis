using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace CANSimulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    { 

        //enums
        private enum lightBeamIndicatorItems
        {
            Inactive    = 0,
            High_Beams  = 1,
            Low_Beams   = 2,
            Side_Beams  = 3,
            Error       = 4
        }

        private enum doorOpenIndicatorItems
        {
            Inactive    = 0,
            LF          = 1,
            LF_LR       = 2,
            LF_LR_RF    = 3,
            LR_RF_RR    = 4,
            LR_RR       = 5,
            LR_RF       = 6,
            LR          = 7,
            RF_RR       = 8,
            RF          = 9,
            RR          = 10,
            LF_LR_RF_RR = 11,
        }

        private enum styleMenuItems
        {
            Classic     = 1,
            Progressive = 2
        }

        private enum transmisionItems
        {
            Automatic   = 1,
            Manual      = 2
        }

        private enum gearAutoItems
        {
            P = 1,
            R = 2,
            N = 3,
            D = 4,
            L = 5,
            S = 6
        }

        private enum gearManualItems
        {
            Neutral         = 0,
            First_Gear      = 1,
            Second_Gear     = 2,
            Third_Gear      = 3,
            Fourth_Gear     = 4,
            Fifth_Gear      = 5,
            Sixth_Gear      = 6,
            Reverse         = 7
        }

        //private variables
        private string messagesToSend { get; set; }
        private string consoleOutputText { get; set; }
        private bool isActiveConnection { get; set; }
        private CancellationTokenSource currentCTS { get; set; }

        //Socket Communication variables
        private int port;
        private int byteCount;
        private byte[] sendData;
        private bool isConnectedEstablished;
        private bool isConnectedHalted;
        private NetworkStream networkStream;
        private TcpClient tcpClient;

        //public variables
        public event PropertyChangedEventHandler PropertyChanged;
        public string ConsoleOutputText
        {
            get { return consoleOutputText.ToString(); }
            set { consoleOutputText = value; RaisePropertyChanged(nameof(ConsoleOutputText)); }
        }
        public bool IsActiveConnection
        {
            get { return isActiveConnection; }
            set { isActiveConnection = value; RaisePropertyChanged(nameof(IsActiveConnection)); }
        }
        public bool IsConnectedEstablished
        {
            get { return isConnectedEstablished; }
            set { isConnectedEstablished = value; RaisePropertyChanged(nameof(IsConnectedEstablished)); }
        }
        public bool IsConnectedHalted
        {
            get { return isConnectedHalted; }
            set { isConnectedHalted = value; RaisePropertyChanged(nameof(IsConnectedHalted)); }
        }

        //public functions
        public MainWindow()
        {
            InitializeComponent();

            //fill ComboBox Items
            lightBeamRTT.ItemsSource        = Enum.GetValues(typeof(lightBeamIndicatorItems)).Cast<lightBeamIndicatorItems>();
            doorOpenRTT.ItemsSource         = Enum.GetValues(typeof(doorOpenIndicatorItems)).Cast<doorOpenIndicatorItems>();
            styleChange.ItemsSource         = Enum.GetValues(typeof(styleMenuItems)).Cast<styleMenuItems>();
            transmissionType.ItemsSource    = Enum.GetValues(typeof(transmisionItems)).Cast<transmisionItems>();
            gearType.ItemsSource            = Enum.GetValues(typeof(gearAutoItems)).Cast<gearAutoItems>();


            lightBeamRTT.SelectedItem       = lightBeamRTT.Items.GetItemAt(0);
            doorOpenRTT.SelectedItem        = doorOpenRTT.Items.GetItemAt(0);
            styleChange.SelectedItem        = styleChange.Items.GetItemAt(0);
            transmissionType.SelectedItem   = transmissionType.Items.GetItemAt(0);
            gearType.SelectedItem           = gearType.Items.GetItemAt(0);

            ConsoleOutputText = "Console Output:";
            messagesToSend = "";
            IsActiveConnection = true;
            IsConnectedEstablished = false;
            IsConnectedHalted = !IsConnectedEstablished;
            DataContext = this;
        }

        //private functions
        private void scrollConsoleToBottom(object sender, TextChangedEventArgs e)
        {
            consoleOutput.CaretIndex = consoleOutput.Text.Length;
            consoleOutput.ScrollToEnd();
        }

        private void sendInvalidData(object sender, RoutedEventArgs e)
        {
            //**************************************************************************************************\\
            //  VIP_VALUES Message is to look like this:                                                        \\
            //                                                                                                  \\
            //| SPEEDOMETER_VALUE || SPEED_VALID || TACHOMETER_VALUE || TACHO_VALID || FUEL_LEVEL |             \\
            //| ODO_RUN_VALUE || ODO_TOTAL_VALUE || GEAR_VALUE || INNER_TEMP_VALUE || OUTER_TEMP_VALUE |        \\
            //| COOLING_TEMP_VALUE || LEFT_BLINKERS_VALUE || RIGHT_BLINKERS_VALUE || LIGHTS_VALUE |             \\
            //| DOORS_VALUE || AIRBAG_VALUE || HANDBRAKE_ERROR_VALUE || ENGINE_OIL_ERROR_VALUE |                \\
            //| BATTERY_CHARGE_ERROR_VALUE || HAZARD_LIGHTS_WARNING || STYLE_VALUE |                            \\
            //**************************************************************************************************\\

            messagesToSend = "|" + "-1" + "|";
            messagesToSend += "|" + "0"  + "|";
            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "0"  + "|";
            messagesToSend += "|" + "-1" + "|";

            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "90" + "|";
            messagesToSend += "|" + "90" + "|";

            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "-1" + "|";

            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "-1" + "|";

            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "-1" + "|";
            messagesToSend += "|" + "1"  + "|";

            handleMessageSendingToTCP();

        }

        private void connectToTCP(object sender, RoutedEventArgs e)
        {
            string ip = insertIPTextBox.Text;
            ConsoleOutputText += "\nTrying to connect to IP adrress: " + ip + "...";
            if (IsConnectedEstablished == false)
            {
                if (!int.TryParse("8000", out port))
                {
                    ConsoleOutputText += "\nNot established Port connection";
                }
                try
                {
                    tcpClient = new TcpClient(ip, port);
                    ConsoleOutputText += "\nEstablished Port connection: " + ip + " at port: " + port;
                    IsConnectedEstablished = true;
                    IsConnectedHalted = !IsConnectedEstablished;
                }
                catch (System.Net.Sockets.SocketException)
                {
                    ConsoleOutputText += "\nNot established Port connection at IP: " + ip;
                    IsConnectedEstablished = false;
                    IsConnectedHalted = !IsConnectedEstablished;
                }
            }
        }

        private void haltConnectionToTCP(object sender, RoutedEventArgs e)
        {
            if (IsConnectedEstablished == true)
            {
                networkStream.Close();
                tcpClient.Close();
                IsConnectedEstablished = false;
                IsConnectedHalted = !IsConnectedEstablished;

                ConsoleOutputText += "\nConnection terminated";
            }
            else
            {/*do nothing*/ }
        }

        private void sendMessagesOnce(object sender, RoutedEventArgs e)
        {
            fillMessagesToSend();
            handleMessageSendingToTCP();
        }

        private void clearConsole(object sender, RoutedEventArgs e)
        {
            ConsoleOutputText = "Console Output:";
        }

        private void checkGearTransmissionTypes(object sender, EventArgs e)
        {
            if (checkIfTransmissionIsAuto())
            {
                gearType.ItemsSource = Enum.GetValues(typeof(gearAutoItems)).Cast<gearAutoItems>();
                gearType.SelectedItem = gearType.Items.GetItemAt(0);
            }
            else
            {
                gearType.ItemsSource = Enum.GetValues(typeof(gearManualItems)).Cast<gearManualItems>();
                gearType.SelectedItem = gearType.Items.GetItemAt(0);
            }
        }

        private void fillMessagesToSend()
        {
            //**************************************************************************************************\\
            //  VIP_VALUES Message is to look like this:                                                        \\
            //                                                                                                  \\
            //| SPEEDOMETER_VALUE || SPEED_VALID || TACHOMETER_VALUE || TACHO_VALID || FUEL_LEVEL |             \\
            //| ODO_RUN_VALUE || ODO_TOTAL_VALUE || GEAR_VALUE || INNER_TEMP_VALUE || OUTER_TEMP_VALUE |        \\
            //| COOLING_TEMP_VALUE || LEFT_BLINKERS_VALUE || RIGHT_BLINKERS_VALUE || LIGHTS_VALUE |             \\
            //| DOORS_VALUE || AIRBAG_VALUE || HANDBRAKE_ERROR_VALUE || ENGINE_OIL_ERROR_VALUE |                \\
            //| BATTERY_CHARGE_ERROR_VALUE || HAZARD_LIGHTS_WARNING || STYLE_VALUE |                            \\
            //**************************************************************************************************\\
            string lightBeamIndicatorValue = lightBeamRTT.SelectedIndex.ToString();
            int lightBeamIndicatorIndex = (int)Enum.Parse(typeof(lightBeamIndicatorItems), lightBeamIndicatorValue);

            string doorOpenIndicatorValue = doorOpenRTT.SelectedIndex.ToString();
            int doorOpenIndicatorIndex = (int)Enum.Parse(typeof(doorOpenIndicatorItems), doorOpenIndicatorValue);

            string styleValue = styleChange.SelectedItem.ToString();
            int styleIndex = (int)Enum.Parse(typeof(styleMenuItems), styleValue);

            float innerTempFloat = float.Parse(InnerTempValue.Content.ToString());
            float outerTempFloat = float.Parse(OuterTempValue.Content.ToString());
            float coolTempFloat  = float.Parse(CoolTempValue.Content.ToString());

            float odoRunFloat   = float.Parse(OdoRunValue.Content.ToString());
            float odoTotalFloat = float.Parse(OdoTotalValue.Content.ToString());

            string gearToSend = gearType.SelectedItem.ToString();
            if (!checkIfTransmissionIsAuto())
            {
                string manualGearValue = gearToSend;
                int manualGearIndex = (int)Enum.Parse(typeof(gearManualItems), manualGearValue);
                gearToSend = manualGearIndex.ToString();
            }
            else
            {   /*do nothing*/  }

            //Create Full Message
            messagesToSend  = "|" + SpeedoValue.Content.ToString()                                      + "|";
            messagesToSend += "|" + ((this.SpeedoSlider.IsEnabled == true) ? "1" : "0")                 + "|";
            messagesToSend += "|" + TachoValue.Content.ToString()                                       + "|";
            messagesToSend += "|" + ((this.SpeedoSlider.IsEnabled == true) ? "1" : "0")                 + "|";
            messagesToSend += "|" + FuelValue.Content.ToString()                                        + "|";

            messagesToSend += "|" + Math.Round((decimal)odoRunFloat, 2).ToString().Replace(",", ".")    + "|";
            messagesToSend += "|" + Math.Round((decimal)odoTotalFloat, 2).ToString().Replace(",", ".")  + "|";
            messagesToSend += "|" + gearToSend                                                          + "|";
            messagesToSend += "|" + Math.Round((decimal)innerTempFloat, 2).ToString().Replace(",", ".") + "|";
            messagesToSend += "|" + Math.Round((decimal)outerTempFloat, 2).ToString().Replace(",", ".") + "|";

            messagesToSend += "|" + Math.Round((decimal)coolTempFloat, 2).ToString().Replace(",", ".")  + "|";
            messagesToSend += "|" + ((this.leftBlinkerRTT.IsChecked == true) ? "0" : "1")               + "|";
            messagesToSend += "|" + ((this.rightBlinkerRTT.IsChecked == true) ? "0" : "1")              + "|";
            messagesToSend += "|" + lightBeamIndicatorIndex.ToString()                                  + "|";

            messagesToSend += "|" + doorOpenIndicatorValue.ToString()                                   + "|";
            messagesToSend += "|" + ((this.airbagRTT.IsChecked == true) ? "0" : "1")                    + "|";
            messagesToSend += "|" + ((this.handbrakeRTT.IsChecked == true) ? "0" : "1")                 + "|";
            messagesToSend += "|" + ((this.engineOilRTT.IsChecked == true) ? "0" : "1")                 + "|";

            messagesToSend += "|" + ((this.batteryChargeRTT.IsChecked == true) ? "0" : "1")             + "|";
            messagesToSend += "|" + ((this.hazardLightRTT.IsChecked == true) ? "0" : "1")               + "|";
            messagesToSend += "|" + styleIndex.ToString()                                               + "|";
        }

        private bool handleMessageSendingToTCP()
        {
            if (IsConnectedEstablished == false)
            {
                ConsoleOutputText += "\nTCP Connection not established";
                return false;
            }
            else
            {
                try
                {
                    byteCount = Encoding.ASCII.GetByteCount(messagesToSend);
                    sendData = new byte[byteCount];
                    sendData = Encoding.ASCII.GetBytes(messagesToSend);
                    networkStream = tcpClient.GetStream();
                    networkStream.Write(sendData, 0, sendData.Length);

                    ConsoleOutputText += "\nSend Following Message to HMI: \n" + messagesToSend;
                }
                catch (System.NullReferenceException)
                {
                    ConsoleOutputText += "\nNot established Port connection";
                }
            }
            return true;
        }

        private bool checkIfTransmissionIsAuto()
        {
            return transmissionType.SelectionBoxItem.ToString() == transmisionItems.Automatic.ToString();
        }

        //public async functions
        private async void sendMessagesConstantly(object sender, RoutedEventArgs e)
        {
            if (IsActiveConnection)
            {
                IsActiveConnection = false;
                commandSendConst.Content = "Stop";

                CancellationTokenSource cts = new CancellationTokenSource();
                currentCTS = cts;
                await handleConstantMessageSendingToHMI(currentCTS.Token);
            }
            else
            {
                IsActiveConnection = true;
                commandSendConst.Content = "Send Constantly";

                //Stop task handleConstantMessageSendingToHMI
                currentCTS.Cancel();

                ConsoleOutputText += "\nStopped sending Meesages to HMI";
            }
        }

        private async Task handleConstantMessageSendingToHMI(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                //send messages to HMI Python
                fillMessagesToSend();
                if (!handleMessageSendingToTCP())
                {
                    IsActiveConnection = true;
                    commandSendConst.Content = "Send Constantly";
                    currentCTS = null;
                    break;
                }
                else
                {
                    Trace.WriteLine("\nSend Following Message to HMI: \n" + messagesToSend);
                }

                if (token.IsCancellationRequested)
                    break;
                else
                    await Task.Delay(500);

            }
        }

        //protected functions
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
