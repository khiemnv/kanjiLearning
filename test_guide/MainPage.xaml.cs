using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace test_guide
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            //test reg
            //string txt = "（～に）関する;  \t（～に）かんする; (QUAN); về～, liên quan～";
            //string part = @";\s*";
            //Regex reg = new Regex(part);
            //var m = reg.Split(txt);

            editCanvas.Tapped += EditCanvas_Tapped;
            acceptCanvas.Tapped += AcceptCanvas_Tapped;

            txtTerm.AcceptsReturn = true;
            txtTerm.TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap;
            txtTerm.Header = "Description";
            txtTerm.PlaceholderText = "place holder text";
            ScrollViewer.SetVerticalScrollBarVisibility(txtTerm, ScrollBarVisibility.Auto);
        }

        private void AcceptCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            txtTerm.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void EditCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            txtTerm.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        static bool status = false;
        private void Canvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            status = !status;
            if (status)
            {
                SolidColorBrush mySolidColorBrush = new SolidColorBrush();

                // Describes the brush's color using RGB values. 
                // Each value has a range of 0-255.
                mySolidColorBrush.Color = Color.FromArgb(255, 255, 255, 0);
                starEllipse.Fill = new SolidColorBrush() { Color = Colors.Yellow };
                //starEllipse.Stroke = Brushes.Black;
                starEllipse.Stroke = new SolidColorBrush() { Color = Colors.White };
                starEllipse.StrokeThickness = 5;
                starPolyline.Stroke = new SolidColorBrush() { Color = Colors.Blue };
                starPolyline.StrokeThickness = 7;
            }
            else
            {
                SolidColorBrush mySolidColorBrush = new SolidColorBrush();

                // Describes the brush's color using RGB values. 
                // Each value has a range of 0-255.

                //mySolidColorBrush.Color = Color.FromArgb(255, 0, 255, 0);
                //mySolidColorBrush.Color = Colors.White;
                starEllipse.Fill = new SolidColorBrush() { Color = Colors.Gray };
                starEllipse.Stroke = new SolidColorBrush() { Color = Colors.White};
                starEllipse.StrokeThickness = 5;
                starPolyline.Stroke = new SolidColorBrush() { Color = Colors.White };
                starPolyline.StrokeThickness = 7;
            }
        }
    }
}
