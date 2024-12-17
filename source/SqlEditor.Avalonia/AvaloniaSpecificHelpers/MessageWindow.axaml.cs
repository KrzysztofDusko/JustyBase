using Avalonia.Controls;

namespace JustyBase.Views
{
    public partial class MessageWindow : Window
    {
        public MessageWindow() : this("Message Content", "Information")
        {
            //this.KeyDown += MessageWindow_KeyDown;
            //this.contentTextBlock.KeyDown += MessageWindow_KeyDown;
        }

        //private void MessageWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        //{
        //    if (e.Key == Avalonia.Input.Key.Escape)
        //    {
        //        (sender as Window)?.Close();
        //    }
        //}

        public MessageWindow(string content = "Message Content", string title = "Information")
        {
            InitializeComponent();
            this.titleLabel.Text = title;
            this.contentTextBlock.Text = content;
            tbOk.Click += TbOk_Click;
        }

        private void TbOk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
