using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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
            PartsListBox.ItemTemplate = BuildTemplate("Parts");
            InventoryListBox.ItemTemplate = BuildTemplate("Inventory");
            Loaded += async (_, __) => { WindowFader.SlideIn(this); await RefreshAsync(); CategoryTabs.SelectedIndex = initialTab == "Inventory" ? 1 : 0; };
            Closing += Window_Closing;
        }

        private async System.Threading.Tasks.Task RefreshAsync(){var parts=await _repo.GetByTypeAsync("Parts");_partsCategories.Clear();parts.ForEach(p=>_partsCategories.Add(p));var inv=await _repo.GetByTypeAsync("Inventory");_inventoryTypes.Clear();inv.ForEach(i=>_inventoryTypes.Add(i));}
        private async System.Threading.Tasks.Task AddCategoryAsync(string name,string forType){name=name.Trim();if(string.IsNullOrWhiteSpace(name)){AppLogger.Warning("Name cannot be empty.");return;}if(await _repo.ExistsAsync(name,forType)){AppLogger.Warning("Already exists.");return;}await _repo.InsertAsync(name,forType);await RefreshAsync();}
        private async System.Threading.Tasks.Task DeleteCategoryAsync(string name,string forType){await _repo.DeleteAsync(name,forType);await RefreshAsync();}
        private async void AddParts_Click(object s,RoutedEventArgs e){await AddCategoryAsync(PartsInput.Text,"Parts");PartsInput.Clear();}
        private async void AddInventory_Click(object s,RoutedEventArgs e){await AddCategoryAsync(InventoryInput.Text,"Inventory");InventoryInput.Clear();}
        private async void Delete_Click(object s,RoutedEventArgs e){if(s is Button b&&b.Tag is string tag){var p=tag.Split('|');if(p.Length==2)await DeleteCategoryAsync(p[0],p[1]);}}
        private DataTemplate BuildTemplate(string forType){var x=$"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid><Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions><TextBlock Text='{{Binding}}' Foreground='{{StaticResource TextPrimaryBrush}}' FontSize='12'/><Button Grid.Column='1' Content='✕' Width='18' Height='18' Style='{{StaticResource PartsRemoveIconButtonStyle}}' Click='Delete_Click' Tag='{{Binding}}|{forType}'/></Grid></DataTemplate>";return (DataTemplate)System.Windows.Markup.XamlReader.Parse(x);}
        private void btnClose_Click(object s,RoutedEventArgs e)=>Close();
        private void TitleBar_MouseLeftButtonDown(object s, MouseButtonEventArgs e)=>DragMove();
        private void Window_Closing(object? sender, CancelEventArgs e){if(_animatingClose)return;e.Cancel=true;_animatingClose=true;WindowFader.SlideOut(this,()=>{Hide();if(Content is UIElement c){c.RenderTransform=System.Windows.Media.Transform.Identity;}Opacity=1;Close();});}
    }
}
