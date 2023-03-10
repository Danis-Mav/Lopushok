using Lopushok.DB;
using Lopushok.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lopushok.Pages
{
    /// <summary>
    /// Interaction logic for ProductsListPage.xaml
    /// </summary>
    public partial class ProductsListPage : Page
    {
        const int productOnPage = 20;
        public int PageNumber { get; set; } = 1;

        public List<Product> Products { get; set; }
        public List<Product> FilteredProducts { get; set; }
        public List<ProductType> ProductTypes { get; set; }
        public Dictionary<string, Func<Product, object>> Sortings { get; set; }

        public ProductsListPage()
        {
            InitializeComponent();
            Products = DataAccess.GetProducts();
            FilteredProducts = Products.ToList();

            ProductTypes = DataAccess.GetProductTypes();

            ProductTypes.Insert(0, new ProductType() { Name = "Все типы" });

            Sortings = new Dictionary<string, Func<Product, object>>
            {
                { "Без сортировки", x => x.Id },
                { "Минимальная стоимость по убыванию", x => x.MinPrice },   //reverse
                { "Минимальная стоимость по возрастанию", x => x.MinPrice },
                { "Номер цеха по убыванию", x => x.WorkshopId },            //reverse
                { "Номер цеха по возрастанию", x => x.WorkshopId },
                { "Наименование по убыванию", x => x.Name },                //reverse
                { "Наименование по возрастанию", x => x.Name }
            };

            this.DataContext = this;
            DataAccess.NewItemAddedEvent += DataAccess_NewItemAddedEvent;
            GeneratePageNumbers();
        }

        private void DataAccess_NewItemAddedEvent()
        {
            Products = DataAccess.GetProducts();
            ApplyFilters(true);

            lvProducts.Items.Refresh();
        }

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cbProductType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters(bool filtersChanged = true)
        {
            var searchingText = tbSearch.Text.ToLower();
            var productType = cbProductType.SelectedItem as ProductType;

            if (productType == null)
                return;

            if (filtersChanged)
                PageNumber = 1;

            FilteredProducts = Products.FindAll(product => product.Name.ToLower().Contains(searchingText));

            if (productType.Id != 0)
                FilteredProducts = FilteredProducts.FindAll(product => product.ProductType == productType);


            
            lvProducts.ItemsSource = FilteredProducts.Skip((PageNumber - 1) * productOnPage).Take(productOnPage);

            GeneratePageNumbers();
            SetUnderline(PageNumber);
        }

        public void GeneratePageNumbers()
        {
            tbPageNumber.Inlines.Clear();
            for (int i = 0, pageNumber = 1; i < FilteredProducts.Count(); i += productOnPage, pageNumber += 1)
            {
                Hyperlink hyperlink = new Hyperlink()
                {
                    TextDecorations = null,
                    Foreground = new SolidColorBrush(Colors.Black)
                };

                hyperlink.Inlines.Add($" {pageNumber} ");
                hyperlink.Click += NavigateToPage;
                tbPageNumber.Inlines.Add(hyperlink);
            }
        }

        private void NavigateToPage(object sender, RoutedEventArgs e)
        {
            PageNumber = int.Parse(((sender as Hyperlink).Inlines.FirstOrDefault() as Run).Text);
            ApplyFilters(false);
        }

        private void btnPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (PageNumber > 1)
            {
                PageNumber--;
                ApplyFilters(false);
            }
        }

        private void btnNextPage_Click(object sender, RoutedEventArgs e)
        {
            int pageCount = FilteredProducts.Count % productOnPage == 0 ? FilteredProducts.Count / productOnPage : FilteredProducts.Count / productOnPage + 1;
            if (PageNumber < pageCount)
            {
                PageNumber++;
                ApplyFilters(false);
            }
        }

        public void SetUnderline(int pageNumber)
        {
            foreach (var inline in tbPageNumber.Inlines)
            {
                (inline as Hyperlink).TextDecorations = null;
                if (((inline as Hyperlink).Inlines.FirstOrDefault() as Run).Text.Contains($"{pageNumber}"))
                    inline.TextDecorations = TextDecorations.Underline;
            }
        }

        private void lvProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {   
            btnEditPrice.Visibility = lvProducts.SelectedItems == null ? Visibility.Hidden : Visibility.Visible;
        }

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProductPage(new Product(), true));
        }

        private void lvProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvProducts.SelectedItem as Product == null)
                return;
            NavigationService.Navigate(new ProductPage(lvProducts.SelectedItem as Product));

        }

        private void btnEditPrice_Click(object sender, RoutedEventArgs e)
        {
            var products = lvProducts.SelectedItems.Cast<Product>().ToList();
            var result = new Windows.EditPriceWindow(products).ShowDialog();

            if (result.Value)
                MessageBox.Show("Цены успешно обновлены", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
