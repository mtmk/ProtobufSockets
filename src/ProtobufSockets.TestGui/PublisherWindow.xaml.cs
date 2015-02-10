using System;
using System.Net;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;
using ProtobufSockets.Tests;

namespace ProtobufSockets.TestGui
{
    public partial class PublisherWindow : Window
    {
        private readonly IPEndPoint _endPoint;
        private Publisher _publisher;
        private int _started;
        private int _sendData;
        private readonly object _sync = new object();

        public PublisherWindow(string text, int i)
        {
            InitializeComponent();

            Title += " - " + i;

            var s = text.Split(':');
            _endPoint = new IPEndPoint(IPAddress.Parse(s[0]), int.Parse(s[1]));

            AddressLabel.Content = _endPoint.ToString();

            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += (_, e) =>
            {
                lock (_sync)
                {
                    if (_publisher != null)
                    {
                        PublisherTextBox.Text = JsonConvert.SerializeObject(_publisher.GetStats(true), Formatting.Indented);
                    }
                    else
                    {
                        PublisherTextBox.Text = "";
                    }
                }
            };
            dispatcherTimer.Interval = TimeSpan.FromSeconds(.7);
            dispatcherTimer.Start();

            Closed += (_, e) => Publisher_Stop_Button_Click(null, null);
        }

        private void Publisher_Start_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Interlocked.CompareExchange(ref _started, 0, 0) == 1) return;

            lock (_sync)
                _publisher = new Publisher(_endPoint);

            Interlocked.Exchange(ref _started, 1);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                int i = 0;
                while (Interlocked.CompareExchange(ref _started, 0, 0) == 1)
                {
                    if (Interlocked.CompareExchange(ref _sendData, 0, 0) == 1)
                    {
                        i++;
                        _publisher.Publish(new Message { Payload = "message" + i });
                        int i1 = i;
                        Dispatcher.Invoke((Action) (() =>
                        {
                            PublisherLabel.Content = "count: " + i1;
                        }));
                    }
                    Thread.Sleep(100);
                }

                lock (_sync)
                {
                    _publisher.Dispose();
                    _publisher = null;
                }
            });
        }

        private void Publisher_Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            Interlocked.Exchange(ref _started, 0);
        }

        private void SendDataCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            Interlocked.Exchange(ref _sendData, 1);
        }

        private void SendDataCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Interlocked.Exchange(ref _sendData, 0);
        }
    }
}
