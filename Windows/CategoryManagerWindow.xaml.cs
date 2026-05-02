using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ESCenter.Core;
using ESCenter.Data;

namespace ESCenter.Windows
{
    public partial class CategoryManagerWindow : Window
    {
        private readonly ObservableCollection<string> _partsCategories = new();
        private readonly ObservableCollection<string> _inventoryTypes = new();
        private readonly CategoryRepository _repo;
        private bool _animatingClose;

        public CategoryManagerWindow(string initialTab = "Parts")
        {
            InitializeComponent();
            _repo = AppServices.Get<CategoryRepository>();
            DataContext = this;
            PartsListBox.ItemsSource = _partsCategories;
            InventoryListBox.ItemsSource = _inventoryTypes;
            Loaded += async (_, __) =>
            {
                WindowFader.SlideIn(this);
                await RefreshAsync();
                CategoryTabs.SelectedIndex = initialTab == "Inventory" ? 1 : 0;
            };
            Closing += Window_Closing;
        }

        private async Task RefreshAsync()
        {
            var parts = await _repo.GetByTypeAsync("Parts");
            _partsCategories.Clear();
            parts.ForEach(p => _partsCategories.Add(p));

            var inv = await _repo.GetByTypeAsync("Inventory");
            _inventoryTypes.Clear();
            inv.ForEach(i => _inventoryTypes.Add(i));
        }

        private async Task AddCategoryAsync(string name, string forType)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                AppLogger.Warning("Name cannot be empty.");
                return;
            }

            if (await _repo.ExistsAsync(name, forType))
            {
                AppLogger.Warning("Already exists.");
                return;
            }

            await _repo.InsertAsync(name, forType);
            await RefreshAsync();
        }

        private async Task DeleteCategoryAsync(string name, string forType)
        {
            await _repo.DeleteAsync(name, forType);
            await RefreshAsync();
        }

        private async void AddParts_Click(object sender, RoutedEventArgs e)
        {
            await AddCategoryAsync(PartsInput.Text, "Parts");
            PartsInput.Clear();
        }

        private async void AddInventory_Click(object sender, RoutedEventArgs e)
        {
            await AddCategoryAsync(InventoryInput.Text, "Inventory");
            InventoryInput.Clear();
        }

        private async void DeletePartCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.CommandParameter is string categoryName)
            {
                await DeleteCategoryAsync(categoryName, "Parts");
            }
        }

        private async void DeleteInventoryType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.CommandParameter is string categoryName)
            {
                await DeleteCategoryAsync(categoryName, "Inventory");
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_animatingClose)
            {
                return;
            }

            e.Cancel = true;
            _animatingClose = true;
            WindowFader.SlideOut(this, () =>
            {
                Hide();
                if (Content is UIElement c)
                {
                    c.RenderTransform = System.Windows.Media.Transform.Identity;
                }

                Opacity = 1;
                Close();
            });
        }
    }
}
