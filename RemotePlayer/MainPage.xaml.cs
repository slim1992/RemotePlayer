using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace RemotePlayer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private HostName hostName;
        private string stringToSend;
        private int lastSend;
        private DispatcherTimer timerSend = new DispatcherTimer();
        private DataWriter writer;

        private double sensorX = 0;
        private Accelerometer _accelerometer;
        private uint _desiredReportInterval;
        bool isSend = false;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void rootPage_Loaded(object sender, RoutedEventArgs e)
        {
            timerSend.Interval = TimeSpan.FromMilliseconds(50);
            timerSend.Tick += timerSend_Tick;
        }

        /// <summary>
        /// 定时处理传感器数据并发送
        /// </summary>
        void timerSend_Tick(object sender, object e)
        {
            if (isSend)
            {
                
                if (lastSend != 3 && sensorX > 0.15)
                {
                    stringToSend = "3";
                    lastSend = 3;
                    sendSocket();
                }
                else if (lastSend != 2 && sensorX < -0.15)
                {
                    stringToSend = "2";
                    lastSend = 2;
                    sendSocket();
                }
                else if (lastSend != 4 && (sensorX > -0.15 && sensorX < 0.15))
                {
                    stringToSend = "4";
                    lastSend = 4;
                    sendSocket();
                }
                lastSensor = sensorX;
            }
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        /// <summary>
        /// 连接Udp服务
        /// </summary>
        private async void connectButton_Click(object sender, RoutedEventArgs e)
        {
            hostName = new HostName(hostIP.Text);
            DatagramSocket socket = new DatagramSocket();
            CoreApplication.Properties.Add("clientSocket", socket);
            try
            {
                await socket.ConnectAsync(hostName, hostPort.Text);
                CoreApplication.Properties.Add("connected", null);
            }
            catch (Exception ee)
            {
            }
            finally
            {
                connectButton.IsEnabled = false;
                _accelerometer = Accelerometer.GetDefault();
                if (_accelerometer != null)
                {
                    // Select a report interval that is both suitable for the purposes of the app and supported by the sensor.
                    // This value will be used later to activate the sensor.
                    //    send();
                    //}

                    uint minReportInterval = _accelerometer.MinimumReportInterval;
                    _desiredReportInterval = minReportInterval > 16 ? minReportInterval : 16;

                    _accelerometer.ReportInterval = _desiredReportInterval;

                    _accelerometer.ReadingChanged += new TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(ReadingChanged);
                    readyToSend();
                }
                else
                {
                    readyToSend();
                    MessageDialog msg = new MessageDialog("No accelerometer found");
                    msg.ShowAsync();
                }
            }
        }

        /// <summary>
        /// 读取传感器变化
        /// </summary>
        private async void ReadingChanged(object sender, AccelerometerReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                AccelerometerReading reading = e.Reading;
                sensorX = reading.AccelerationX;
            });
        }

        /// <summary>
        /// 开始发送数据
        /// </summary>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            isSend = true;
            timerSend.Start();
            FirstGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            SecondGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
        
        /// <summary>
        /// Udp发送前准备
        /// </summary>
        private void readyToSend()
        {
            object outValue;
            DatagramSocket socket;
            if (!CoreApplication.Properties.TryGetValue("clientSocket", out outValue))
            {
                return;
            }
            socket = (DatagramSocket)outValue;

            if (!CoreApplication.Properties.TryGetValue("clientDataWriter", out outValue))
            {
                writer = new DataWriter(socket.OutputStream);
                CoreApplication.Properties.Add("clientDataWriter", writer);
            }
            else
            {
                writer = (DataWriter)outValue;
            }
        }

        /// <summary>
        /// Udp发送指定字符串
        /// </summary>
        private async void sendSocket()
        {
            writer.WriteString(stringToSend);
            try
            {
                await writer.StoreAsync();
            }
            catch (Exception ee)
            {
            }
        }


        /// <summary>
        /// 单次点击手刹处理
        /// </summary>
        private void brakeButton_Click(object sender, RoutedEventArgs e)
        {
            stringToSend = "5";
            lastSend = 5;
            sendSocket();
        }

        /// <summary>
        /// 自动前进打开
        /// </summary>
        private void forwordButton_Checked(object sender, RoutedEventArgs e)
        {
            stringToSend = "0";
            lastSend = 0;
            sendSocket();
        }

        /// <summary>
        /// 取消自动前进
        /// </summary>
        private void forwordButton_Unchecked(object sender, RoutedEventArgs e)
        {
            stringToSend = "1";
            lastSend = 1;
            sendSocket();
        }

        /// <summary>
        /// 自动刹车
        /// </summary>
        private void stopButton_Checked(object sender, RoutedEventArgs e)
        {
            stringToSend = "7";
            lastSend = 7;
            sendSocket();
        }

        /// <summary>
        /// 取消自动刹车
        /// </summary>
        private void stopButton_Unchecked(object sender, RoutedEventArgs e)
        {
            stringToSend = "8";
            lastSend = 8;
            sendSocket();
        }

        /// <summary>
        /// 开启氮氧
        /// </summary>
        private void n2oButton_Click(object sender, RoutedEventArgs e)
        {
            stringToSend = "9";
            lastSend = 9;
            sendSocket();
        }
    }
}
