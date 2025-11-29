using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.UI.WindowManagement;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LibLog_v1;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class USBScanWindow : Window
{
    public USBScanWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new SizeInt32(700, 600));
        CenterWindow();

        txtISBNInput.IsEnabled = true;
        
    }

    private void CenterWindow()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;

        if (area == null) return;

        AppWindow.Move(new PointInt32((area.Value.Width - AppWindow.Size.Width) / 2, (area.Value.Height - AppWindow.Size.Height) / 2));
    }

    #region Barcode Reading
    private async void txtISBNInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        string isbn = txtISBNInput.Text; // store input

        if (isbn != null)
        {
            isbn = isbn.Replace("-", ""); // remove hyphens
            isbn = isbn.Replace(" ", ""); // remove spaces
            isbn = isbn.Trim(); // remove leading/trailing whitespace

            if (isbn.Length == 10 || isbn.Length == 13)
            {
                // check if valid
                if (await IsValidISBN(isbn))
                {
                    var data = await APIHandler.RetrieveData(isbn);
                    // display book details from API
                    txtBookDetails.Text = $"Title: {data.Title} \nAuthor: {data.Author}";
                    if (data.CoverImage is byte[] imageBytes && imageBytes.Length > 0)
                    {
                        var bitmapImage = new BitmapImage();
                        using (var stream = new MemoryStream(imageBytes))
                        {
                            stream.Position = 0;
                            var randomAccessStream = stream.AsRandomAccessStream();
                            await bitmapImage.SetSourceAsync(randomAccessStream);
                        }
                        bookCoverIcon.Source = bitmapImage;
                    }
                    else
                    {
                        bookCoverIcon.Source = null;
                    }
                    txtISBNInput.IsEnabled = false;
                }
                else
                {
                    txtISBNInput.Text = "Invalid ISBN Number - Clear and Try Again";
                }
            }
        } 
    }

    public static async Task<bool> IsValidISBN(string isbn)
    {
        bool isValid = false;
        var data = await APIHandler.RetrieveData(isbn);
        string invalid = "Not Available";

        if (data.ToString().Contains(invalid))
            isValid = false;
        else
            isValid = true;
        

        return isValid;
    }

    #endregion

    private async void btnScanSubmit_Click(object sender, RoutedEventArgs e)
    {
        // Take API info gathered on book, add it to main library
        await DataAccess.AddData(txtISBNInput.Text);
        this.Close();
    }

    private void btnScanClear_Click(object sender, RoutedEventArgs e)
    {
        txtISBNInput.Text = "";
        txtISBNInput.IsEnabled = true;
        txtBookDetails.Text = "";
    }

    private void cbMultiScan_Checked(object sender, RoutedEventArgs e)
    {
        // replace API call for multi book scan and adjust logic accordingly
    }
}
