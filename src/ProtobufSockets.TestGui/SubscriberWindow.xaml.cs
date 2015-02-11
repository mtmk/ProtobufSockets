using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using ProtobufSockets.Tests;

namespace ProtobufSockets.TestGui
{
    public partial class SubscriberWindow : Window
    {
        private readonly IPEndPoint[] _endPoints;
        private Subscriber _subscriber;
        private readonly object _topicSync = new object();
        private string _topic = "*";

        private readonly object _sync = new object();

        public SubscriberWindow(string text, int i)
        {
            InitializeComponent();

            TopicTextBox.Text = Topic;
            Title += " - " + i;

            _endPoints = text.Split(' ')
                .Select(s => s.Split(':'))
                .Select(s => new IPEndPoint(IPAddress.Parse(s[0]), int.Parse(s[1])))
                .ToArray();

            AddressLabel.Content = _endPoints
                .Select(e => e.ToString())
                .Aggregate((a, b) => a + " " + b);

            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += (_, e) =>
            {
                lock (_sync)
                {
                    if (_subscriber != null)
                    {
                        SubscriberTextBox.Text = JsonConvert.SerializeObject(_subscriber.GetStats(true), Formatting.Indented);
                    }
                    else
                    {
                        SubscriberTextBox.Text = "";
                    }
                }
            };
            dispatcherTimer.Interval = TimeSpan.FromSeconds(.7);
            dispatcherTimer.Start();

            Closed += (_, e) => Subscriber_Stop_Button_Click(null, null);
        }

        private void Subscriber_Start_Button_Click(object sender, RoutedEventArgs e)
        {
            TopicTextBox.IsEnabled = false;

            lock (_sync)
            {
                if (_subscriber != null) return;
                _subscriber = new Subscriber(_endPoints, Title);
            }

            int i = 0;
            _subscriber.Subscribe<Message>(Topic, m =>
            {
                int count = Interlocked.Increment(ref i);
                Dispatcher.Invoke((Action)(() =>
                {
                    SubscriberLabel.Content = "count: " + count + " (" + m.Payload + ")";
                }));
            });
        }

        private void Subscriber_Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            TopicTextBox.IsEnabled = true;

            lock (_sync)
            {
                if (_subscriber != null)
                {
                    _subscriber.Dispose();
                    _subscriber = null;
                }
            }
        }

        private void Failover_Button_Click(object sender, RoutedEventArgs e)
        {
            lock (_sync)
            {
                if (_subscriber != null)
                {
                    _subscriber.FailOver();
                }
            }
        }

        public string Topic
        {
            get { lock (_topicSync) return _topic; }
            set { lock (_topicSync) _topic = value; }
        }

        private void TopicTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Topic = TopicTextBox.Text;
        }
    }
}
