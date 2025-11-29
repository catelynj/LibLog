using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;

namespace LibLog_v1
{
    public sealed partial class MainWindow : Window
    {
        private List<Book> _books;

        IList<Book> allBooks = new List<Book>();
        ObservableCollection<Book> booksFiltered = new ObservableCollection<Book>();
        

        public MainWindow()
        {
            InitializeComponent();
            
            CenterWindow();

            AppTitleBar.Loaded += AppTitleBar_Loaded;
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Load books after the visual tree is ready to avoid blocking the UI thread
            if (Content is FrameworkElement fe)
            {
                fe.Loaded += MainWindow_Loaded;
            }

        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Asynchronously load books and populate collections without blocking the UI thread
            await LoadBooksAsync();

            allBooks = _books ?? new List<Book>();
            booksFiltered = new ObservableCollection<Book>(allBooks);
            lvBookshelf.ItemsSource = booksFiltered;
        }

        private async Task LoadBooksAsync()
        {
            _books = await DataAccess.GetAllBooks();
            lvBookshelf.ItemsSource = _books;
        }

        public static async Task<BitmapImage?> BytesToBitmapImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            var image = new BitmapImage();
            using (var ms = new MemoryStream(imageData))
            {
                await image.SetSourceAsync(ms.AsRandomAccessStream());
            }
            return image;
        }

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
                AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        private void CenterWindow()
        {
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;

            if (area == null) return;

            AppWindow.Move(new PointInt32((area.Value.Width - AppWindow.Size.Width) / 2, (area.Value.Height - AppWindow.Size.Height) / 2));
        }

        private void btnScanUSB_Click(object sender, RoutedEventArgs e)
        {
            var usbScanWindow = new USBScanWindow();
            usbScanWindow.Activate();
        }

        private void btnScanCamera_Click(object sender, RoutedEventArgs e)
        {
            var cameraScanWindow = new CameraScanWindow();
            cameraScanWindow.Activate();
        }

        private async void btnRefreshLibrary_Click(object sender, RoutedEventArgs e)
        {
            await DataAccess.GetAllBooks();
            _books = await DataAccess.GetAllBooks();
            lvBookshelf.ItemsSource = _books;

        }

        private void btnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            // clear filters gasp
            FilterTitle.Text = string.Empty;
            FilterAuthor.Text = string.Empty;
            booksFiltered.Clear();
            foreach (var book in allBooks)
            {
                booksFiltered.Add(book);
            }

        }

        private void lvBookshelf_ItemClick(object sender, ItemClickEventArgs e)
        {
            var currentBook = (Book)e.ClickedItem;
            BookDetails.DataContext = currentBook;

            SplitViewMain.IsPaneOpen = true;

        }

        private async void btnRemoveBook_Click(object sender, RoutedEventArgs e)
        {
            if (BookDetails.DataContext is not Book currentBook) return;
            DataAccess.RemoveData(currentBook.ISBN);

            _books = await DataAccess.GetAllBooks();
            lvBookshelf.ItemsSource = _books;

            SplitViewMain.IsPaneOpen = false;
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs args)
        {
            var filtered = allBooks.Where(book => Filter(book));
            Remove_NonMatching(filtered);
            AddBack_Books(filtered);
        }

        private bool Filter(Book book)
        {
            return book.Title.Contains(FilterTitle.Text, StringComparison.InvariantCultureIgnoreCase) &&
                   book.Author.Contains(FilterAuthor.Text, StringComparison.InvariantCultureIgnoreCase);
        }

        private void Remove_NonMatching(IEnumerable<Book> filteredData)
        {
            for (int i = booksFiltered.Count - 1; i >= 0; i--)
            {
                var item = booksFiltered[i];
                if (!filteredData.Contains(item))
                {
                    booksFiltered.Remove(item);
                }
            }
        }

        private void AddBack_Books(IEnumerable<Book> filteredData)
        {
            foreach (var item in filteredData)
            {
                if (!booksFiltered.Contains(item))
                {
                    booksFiltered.Add(item);
                }
            }
        }


        private void btnEditTags_Click(object sender, RoutedEventArgs e)
        {
            if(spEditTags.Visibility == Visibility.Collapsed)
            {
                spEditTags.Visibility = Visibility.Visible;
            }
            else
            {
                spEditTags.Visibility = Visibility.Collapsed;
            }
        }

        // Tags -> Able to add/remove from listbox BUT they aren't saved anywhere and the listbox applies to every book
        // TODO: Add 'Tags' column to DB & Book.cs so they are saved and loaded properly
        // This implementation is just for the demo :]
        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
            if (txtNewTag.Text != string.Empty)
            {
                lbTags.Items.Add(txtNewTag.Text);
                txtNewTag.Text = string.Empty;
            }
            else
                return;
        }

        private void btnRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            lbTags.Items.Remove(lbTags.SelectedItem);
        }



        //private void RadioButton_Checked(object sender, RoutedEventArgs e)
        //{
        //    if(rb_Read.IsChecked == true)
        //    {
        //        // display only read books
        //    }
        //    else if(rb_Unread.IsChecked == true)
        //    {
        //        // display only unread books
        //    }
        //    else if(rb_InProg.IsChecked == true)
        //    {
        //        // display in progress books
        //    }
        //    else
        //    {
        //        // 'All' is checked by default
        //        // display all books
        //    }

        //}



        #region NOT USED
        // CHANGED THE TITLE BAR -- COMPLETELY USELESS CODE BELOW


        //private void SetRegionsForCustomTitleBar()
        //{
        //    double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;

        //    RightPaddingCol.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
        //    LeftPaddingCol.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);

        //    //GeneralTransform transform = TitleBarSearchBox.TransformToVisual(null);
        //    //Rect bounds = transform.TransformBounds(new Rect(0, 0, TitleBarSearchBox.ActualWidth, TitleBarSearchBox.ActualHeight));

        //   // Windows.Graphics.RectInt32 SearchBoxRect = GetRect(bounds, scaleAdjustment);


        //    GeneralTransform transform = btnAddBook.TransformToVisual(null);
        //    Rect bounds = transform.TransformBounds(new Rect(0, 0, btnAddBook.ActualWidth, btnAddBook.ActualHeight));

        //    Windows.Graphics.RectInt32 AddBookRect = GetRect(bounds, scaleAdjustment);

        //    var rectArray = new Windows.Graphics.RectInt32[] { AddBookRect };

        //    InputNonClientPointerSource nonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        //    nonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
        //}

        //private Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale)
        //{
        //    return new Windows.Graphics.RectInt32(
        //        _X: (int)Math.Round(bounds.X * scale),
        //        _Y: (int)Math.Round(bounds.Y * scale),
        //        _Width: (int)Math.Round(bounds.Width * scale),
        //        _Height: (int)Math.Round(bounds.Height * scale)
        //    );
        //}

        #endregion

        
    }
}
