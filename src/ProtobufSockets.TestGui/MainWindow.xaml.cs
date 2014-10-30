using System;
using System.Windows;

namespace ProtobufSockets.TestGui
{
    public partial class MainWindow : Window
    {
        private int _port = 23456;
        public MainWindow()
        {
            InitializeComponent();

            PublisherAddressTextBox.Text = "0.0.0.0:" + _port;
            SubscriberAddressTextBox.Text = "";

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
    }
}
