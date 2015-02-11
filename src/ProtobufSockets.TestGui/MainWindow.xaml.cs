using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace ProtobufSockets.TestGui
{
    public class TestTraceListener : TraceListener
    {
        public static Action<string> Message = _ => { };

        static readonly BlockingCollection<string> _q = new BlockingCollection<string>(1000);
        static readonly Timer _timer;

        static TestTraceListener()
        {
            var lok = new object();
            _timer = new Timer(_ =>
            {
                if (!Monitor.TryEnter(lok)) return;
                try
                {
                    for (int i = 0; i < 100; i++)
                    {
                        string m;
                        if (_q.TryTake(out m, 2))
                        {
                            Message(m);
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(lok);
                }
            }, null, 1000, 700);
        }

        public override void Write(string message)
        {
            _q.TryAdd(message, 2);
        }

        public override void WriteLine(string message)
        {
            Write(message + "\n");
        }
    }

    public partial class MainWindow : Window
    {
        private int _port = 23456;
        public MainWindow()
        {
            InitializeComponent();

            PublisherAddressTextBox.Text = "0.0.0.0:" + _port;
            SubscriberAddressTextBox.Text = "";

            TestTraceListener.Message += m =>
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    if (LogCheckBox.IsChecked == true)
                        DiagTextBox.Text += m;
                }));
            };

            Closed += (_, e) => Environment.Exit(0);
        }

        private int subs = 0;
        private void Subscriber_Button_Click(object sender, RoutedEventArgs e)
        {
            new SubscriberWindow(SubscriberAddressTextBox.Text, ++subs).Show();
        }

        private int pubs = 0;
        private void Publisher_Button_Click(object sender, RoutedEventArgs e)
        {
            new PublisherWindow(PublisherAddressTextBox.Text, ++pubs).Show();

            if (SubscriberAddressTextBox.Text == "")
            {
                SubscriberAddressTextBox.Text = "127.0.0.1:" + _port;
            }
            else
            {
                SubscriberAddressTextBox.Text += " 127.0.0.1:" + _port;
            }

            _port++;
            PublisherAddressTextBox.Text = "0.0.0.0:" + _port;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DiagTextBox.Text = "";
        }
    }
}
