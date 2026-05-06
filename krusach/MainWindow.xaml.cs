using System.Windows;
using System.Windows.Controls;
using krusach.ViewModels;
using krusach.Models;
using krusach.Services;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System;
using System.Linq;

namespace krusach
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _viewModel = new MainViewModel();
                DataContext = _viewModel;
                
                this.Loaded += MainWindow_Loaded;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Критическая ошибка инициализации:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.LoadData();
                _viewModel.TryAutoLogin();
                UpdateGridColumns();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadData();
            
            // После загрузки нужно обновить ItemsSource, если биндинги не сработали (хотя в MVVM они должны быть в XAML)
            // Но для начала просто обновим комбобоксы в колонках, так как они ссылаются на статические ресурсы или коллекции
            UpdateGridColumns();
        }

        private void UpdateGridColumns()
        {
            ColCategory.ItemsSource = _viewModel.Categories;
            ColContractor.ItemsSource = _viewModel.Contractors;
            ColProduct.ItemsSource = _viewModel.Products;
        }

        private void OrdersGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Теперь это обрабатывается через SelectedItem binding в XAML, но оставим для совместимости пока что
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SaveChanges();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PbPassword.Password;
            _viewModel.Login();
            PbPassword.Password = ""; // Очищаем после попытки
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e) => _viewModel.Logout();

        private void BtnToggleRegister_Click(object sender, RoutedEventArgs e) => _viewModel.IsRegistering = !_viewModel.IsRegistering;

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PbPassword.Password;
            _viewModel.Register();
            PbPassword.Password = "";
        }

        private void BtnApproveManager_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is User user) _viewModel.ApproveUser(user, "Manager");
        }

        private void BtnApproveAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is User user) _viewModel.ApproveUser(user, "Admin");
        }

        private void CbLightTheme_Click(object sender, RoutedEventArgs e)
        {
            var dict = new ResourceDictionary();
            if (CbLightTheme.IsChecked == true)
                dict.Source = new System.Uri("Themes/LightTheme.xaml", System.UriKind.Relative);
            else
                dict.Source = new System.Uri("Themes/DarkTheme.xaml", System.UriKind.Relative);

            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dict);
            _viewModel.LogAction($"Смена темы на {(CbLightTheme.IsChecked == true ? "светлую" : "темную")}", "");
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "Products_Export.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.ExportProductsToCsv(dialog.FileName);
            }
        }

        private void BtnPriceList_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = "PriceList.pdf" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ReportService.GeneratePriceList(_viewModel.Products, sfd.FileName);
                    _viewModel.LogAction($"Сгенерирован прайс-лист: {Path.GetFileName(sfd.FileName)}", sfd.FileName);
                    Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при генерации PDF: {ex.Message}");
                }
            }
        }

        private void BtnBulkPrice_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.BulkUpdatePrices(10); // Увеличить на 10%
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Backup files (*.txt)|*.txt",
                FileName = $"Backup_{System.DateTime.Now:yyyyMMdd}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.BackupData(dialog.FileName);
            }
        }

        private void BtnAutoReorder_Click(object sender, RoutedEventArgs e) => _viewModel.AutoReorder();
        // --- CRUD HANDLERS ---
        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new Product { Name = "Новый товар", CategoryId = _viewModel.Categories.FirstOrDefault()?.Id ?? 0 };
            var lookups = new Dictionary<string, System.Collections.IEnumerable> { { "CategoryId", _viewModel.Categories } };
            var editWin = new EditWindow(newItem, "Новый товар", lookups) { Owner = this };
            if (editWin.ShowDialog() == true)
            {
                _viewModel.AddProduct(newItem);
            }
        }

        private void BtnDeleteProduct_Click(object sender, RoutedEventArgs e) => _viewModel.DeleteProduct(ProductsGrid.SelectedItem as Product);

        private void BtnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is Product selected)
            {
                var lookups = new Dictionary<string, System.Collections.IEnumerable> { { "CategoryId", _viewModel.Categories } };
                var editWin = new EditWindow(selected, "Правка товара", lookups) { Owner = this };
                if (editWin.ShowDialog() == true) 
                {
                    _viewModel.SaveChanges();
                    _viewModel.UpdateStatistics();
                    _viewModel.LogAction($"Изменен товар: {selected.Name}", $"ID: {selected.Id}");
                }
                else
                {
                    _viewModel.LoadData();
                }
            }
        }

        private void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new Category { Name = "Новая категория" };
            var editWin = new EditWindow(newItem, "Новая категория") { Owner = this };
            if (editWin.ShowDialog() == true)
            {
                _viewModel.AddCategory(newItem);
            }
        }

        private void BtnDeleteCategory_Click(object sender, RoutedEventArgs e) => _viewModel.DeleteCategory(CategoriesGrid.SelectedItem as Category);

        private void BtnEditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesGrid.SelectedItem is Category selected)
            {
                var editWin = new EditWindow(selected, "Правка категории") { Owner = this };
                if (editWin.ShowDialog() == true) 
                {
                    _viewModel.LogAction($"Изменена категория: {selected.Name}", $"ID: {selected.Id}");
                }
                else
                {
                    _viewModel.LoadData();
                }
            }
        }

        private void BtnAddContractor_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new Contractor { CompanyName = "Новая компания", Type = "Поставщик", Inn = "0000000000" };
            var editWin = new EditWindow(newItem, "Новый контрагент") { Owner = this };
            if (editWin.ShowDialog() == true)
            {
                _viewModel.AddContractor(newItem);
            }
        }

        private void BtnDeleteContractor_Click(object sender, RoutedEventArgs e) => _viewModel.DeleteContractor(ContractorsGrid.SelectedItem as Contractor);

        private void BtnEditContractor_Click(object sender, RoutedEventArgs e)
        {
            if (ContractorsGrid.SelectedItem is Contractor selected)
            {
                var editWin = new EditWindow(selected, "Правка контрагента") { Owner = this };
                if (editWin.ShowDialog() == true) 
                {
                    _viewModel.LogAction($"Изменен контрагент: {selected.CompanyName}", $"ID: {selected.Id}");
                }
                else
                {
                    _viewModel.LoadData();
                }
            }
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new Order { OrderDate = DateTime.Now, Status = "Новый", OperationType = "Расход", ContractorId = _viewModel.Contractors.FirstOrDefault()?.Id };
            var lookups = new Dictionary<string, System.Collections.IEnumerable> 
            { 
                { "ContractorId", _viewModel.Contractors },
                { "ProductId", _viewModel.Products } 
            };
            var editWin = new EditWindow(newItem, "Новый заказ", lookups) { Owner = this };
            if (editWin.ShowDialog() == true)
            {
                _viewModel.AddOrder(newItem);
                OrdersGrid.Items.Refresh();
            }
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e) => _viewModel.DeleteOrder(_viewModel.SelectedOrder);

        private void BtnEditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedOrder is Order selected)
            {
                var lookups = new Dictionary<string, System.Collections.IEnumerable> 
                { 
                    { "ContractorId", _viewModel.Contractors },
                    { "ProductId", _viewModel.Products } 
                };
                var editWin = new EditWindow(selected, "Правка заказа", lookups) { Owner = this };
                if (editWin.ShowDialog() == true)
                {
                    _viewModel.SaveChanges();
                    _viewModel.UpdateStatistics();
                    _viewModel.LogAction($"Изменен заказ №{selected.Id}", $"Статус: {selected.Status}");
                    OrdersGrid.Items.Refresh(); // Принудительно обновляем таблицу
                }
                else
                {
                    _viewModel.LoadData(); // Сбрасываем изменения в памяти
                }
            }
        }

        private async void BtnSendEmail_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedOrder is Order selected)
            {
                var contractor = _viewModel.Contractors.FirstOrDefault(c => c.Id == selected.ContractorId);
                if (contractor == null || string.IsNullOrWhiteSpace(contractor.Email))
                {
                    MessageBox.Show("У контрагента не указан Email! Отредактируйте данные контрагента на вкладке 'Контрагенты'.");
                    return;
                }

                try
                {
                    _viewModel.StatusText = "Генерация счета...";
                    string tempFile = Path.Combine(Path.GetTempPath(), $"Invoice_{selected.Id}.pdf");
                    ReportService.GenerateInvoice(selected, tempFile);

                    _viewModel.StatusText = "Отправка письма...";
                    await System.Threading.Tasks.Task.Run(() => 
                    {
                        EmailService.SendInvoice(
                            contractor.Email, 
                            $"Счет №{selected.Id} от Оптовой Базы", 
                            $"Здравствуйте, {contractor.CompanyName}!\n\nВо вложении ваш счет №{selected.Id}.\n\nС уважением,\nОптовая База 2.0", 
                            tempFile
                        );
                    });

                    _viewModel.StatusText = "Письмо отправлено!";
                    _viewModel.LogAction($"Счет №{selected.Id} отправлен на {contractor.Email}", contractor.Email);
                    MessageBox.Show($"Счет успешно отправлен на адрес {contractor.Email}");
                    
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
                catch (Exception ex)
                {
                    _viewModel.StatusText = "Ошибка отправки";
                    MessageBox.Show($"Ошибка при отправке: {ex.Message}\n\nУбедитесь, что вы настроили SMTP-сервер в EmailService.cs!");
                }
            }
            else
            {
                MessageBox.Show("Сначала выберите заказ!");
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedOrder is Order selected)
            {
                var sfd = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"Invoice_{selected.Id}.pdf" };
                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        ReportService.GenerateInvoice(selected, sfd.FileName);
                        _viewModel.LogAction($"Сгенерирована накладная №{selected.Id}", sfd.FileName);
                        Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при генерации PDF: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Сначала выберите заказ в таблице!");
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
