using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections;
using System.Linq;
using System.Collections.ObjectModel;
using krusach.Models;

namespace krusach
{
    public partial class EditWindow : Window
    {
        private object _item;
        private Dictionary<string, FrameworkElement> _controls = new Dictionary<string, FrameworkElement>();
        private Dictionary<string, IEnumerable> _lookups;
        private DataGrid _detailsGrid;

        public EditWindow(object item, string title, Dictionary<string, IEnumerable> lookups = null)
        {
            InitializeComponent();
            _item = item;
            _lookups = lookups;
            TxtTitle.Text = title;
            GenerateFields();
            
            // Если это заказ, добавляем таблицу позиций
            if (_item is Order order)
            {
                AddOrderDetailsGrid(order);
            }
        }

        private Dictionary<string, string> _translations = new Dictionary<string, string>
        {
            { "Name", "Название" },
            { "Sku", "Артикул" },
            { "PurchasePrice", "Цена закупки" },
            { "WholesalePrice", "Оптовая цена" },
            { "StockQuantity", "Количество на складе" },
            { "ImagePath", "Путь к изображению" },
            { "CompanyName", "Название компании" },
            { "Inn", "ИНН" },
            { "Type", "Тип (Поставщик/Покупатель)" },
            { "Rating", "Рейтинг (0-5)" },
            { "OperationType", "Тип операции (Приход/Расход)" },
            { "OrderDate", "Дата" },
            { "Status", "Статус" },
            { "Discount", "Скидка (%)" },
            { "Email", "Электронная почта" },
            { "ContactInfo", "Контактные данные" },
            { "ContractorId", "Контрагент" },
            { "CategoryId", "Категория" }
        };

        private void GenerateFields()
        {
            try
            {
                var properties = _item.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    bool isLookup = _lookups != null && _lookups.ContainsKey(prop.Name);
                    bool isStatus = prop.Name == "Status";
                    bool isOperationType = prop.Name == "OperationType";

                    if (!isLookup && !isStatus && !isOperationType)
                    {
                        if (prop.Name == "Id" || prop.Name == "Category" || 
                            prop.Name == "Contractor" || prop.Name == "Orders" || prop.Name == "OrderDetails" || 
                            prop.Name == "Product" || prop.Name == "Error" || prop.Name == "Item" || 
                            prop.Name == "DaysRemaining" || prop.Name == "IsProcessed" ||
                            prop.Name == "TotalSum" || prop.Name == "HasOrderDetails") continue;

                        if (prop.PropertyType.IsGenericType && 
                            prop.PropertyType.GetGenericTypeDefinition() != typeof(Nullable<>)) continue;
                        if (!prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string) && 
                            prop.PropertyType != typeof(decimal) && prop.PropertyType != typeof(DateTime) &&
                            prop.PropertyType != typeof(decimal?) && prop.PropertyType != typeof(int?) &&
                            prop.PropertyType != typeof(DateTime?)) continue;
                    }

                    var label = new TextBlock 
                    { 
                        Text = _translations.ContainsKey(prop.Name) ? _translations[prop.Name] : prop.Name, 
                        Margin = new Thickness(0, 10, 0, 5), 
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (Brush)FindResource("MainTextBrush")
                    };
                    FieldsPanel.Children.Add(label);

                    FrameworkElement control;

                    if (isStatus || isOperationType)
                    {
                        var items = isStatus 
                            ? new List<string> { "Новый", "В обработке", "Оплачен", "Завершен", "Отменен" }
                            : new List<string> { "Приход", "Расход" };
                        
                        var currentVal = prop.GetValue(_item)?.ToString();
                        if (!string.IsNullOrEmpty(currentVal) && !items.Contains(currentVal))
                            items.Add(currentVal);

                        var cb = new ComboBox { ItemsSource = items, Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(5) };
                        cb.SetBinding(ComboBox.TextProperty, new System.Windows.Data.Binding(prop.Name) { Source = _item, Mode = System.Windows.Data.BindingMode.TwoWay });
                        cb.SetBinding(ComboBox.SelectedItemProperty, new System.Windows.Data.Binding(prop.Name) { Source = _item, Mode = System.Windows.Data.BindingMode.TwoWay });
                        control = cb;
                    }
                    else if (prop.Name == "Rating")
                    {
                        var starPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                        var stars = new List<TextBlock>();
                        int currentRating = (int)(prop.GetValue(_item) ?? 0);

                        for (int i = 1; i <= 5; i++)
                        {
                            int starValue = i;
                            var star = new TextBlock 
                            { 
                                Text = "★", 
                                FontSize = 24, 
                                Cursor = Cursors.Hand, 
                                Margin = new Thickness(0, 0, 5, 0),
                                Foreground = i <= currentRating ? Brushes.Gold : Brushes.Gray
                            };

                            star.MouseDown += (s, e) => 
                            {
                                prop.SetValue(_item, starValue);
                                // Обновляем визуально все звезды в панели
                                for (int j = 0; j < 5; j++)
                                {
                                    stars[j].Foreground = (j < starValue) ? Brushes.Gold : Brushes.Gray;
                                }
                            };
                            stars.Add(star);
                            starPanel.Children.Add(star);
                        }
                        control = starPanel;
                    }
                    else if (isLookup)
                    {
                        var cb = new ComboBox 
                        { 
                            ItemsSource = _lookups[prop.Name],
                            SelectedValuePath = "Id",
                            DisplayMemberPath = prop.Name.Contains("Contractor") ? "CompanyName" : "Name",
                            Margin = new Thickness(0, 0, 0, 10),
                            Padding = new Thickness(5),
                            Tag = prop.Name
                        };
                        cb.SetBinding(ComboBox.SelectedValueProperty, new System.Windows.Data.Binding(prop.Name) 
                        { 
                            Source = _item, 
                            Mode = System.Windows.Data.BindingMode.TwoWay,
                            ValidatesOnDataErrors = true,
                            UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                        });
                        control = cb;
                    }
                    else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                    {
                        var dp = new DatePicker { Tag = prop.Name, Margin = new Thickness(0,0,0,10) };
                        dp.SetBinding(DatePicker.SelectedDateProperty, new System.Windows.Data.Binding(prop.Name) 
                        { 
                            Source = _item, 
                            Mode = System.Windows.Data.BindingMode.TwoWay,
                            ValidatesOnDataErrors = true
                        });
                        control = dp;
                    }
                    else
                    {
                        var tb = new TextBox { Tag = prop.Name };
                        tb.SetResourceReference(TextBox.BackgroundProperty, "ActionBackgroundBrush");
                        tb.SetResourceReference(TextBox.ForegroundProperty, "MainTextBrush");
                        tb.BorderThickness = new Thickness(0, 0, 0, 1);
                        tb.SetResourceReference(TextBox.BorderBrushProperty, "SeparatorBrush");
                        tb.Padding = new Thickness(5);
                        tb.Margin = new Thickness(0, 0, 0, 10);
                        
                        tb.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding(prop.Name) 
                        { 
                            Source = _item, 
                            Mode = System.Windows.Data.BindingMode.TwoWay, 
                            ValidatesOnDataErrors = true,
                            UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                        });
                        control = tb;
                    }

                    _controls[prop.Name] = control;
                    FieldsPanel.Children.Add(control);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка при генерации полей: {ex.Message}");
            }
        }

        private void AddOrderDetailsGrid(Order order)
        {
            var label = new TextBlock 
            { 
                Text = "СОСТАВ ЗАКАЗА", 
                Margin = new Thickness(0, 20, 0, 10), 
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("MainTextBrush"),
                Opacity = 0.7
            };
            FieldsPanel.Children.Add(label);

            _detailsGrid = new DataGrid
            {
                ItemsSource = order.OrderDetails,
                AutoGenerateColumns = false,
                CanUserAddRows = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("MainTextBrush"),
                MinHeight = 150,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Колонки для позиций заказа
            var colProduct = new DataGridComboBoxColumn
            {
                Header = "Товар",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                SelectedValueBinding = new System.Windows.Data.Binding("ProductId"),
                SelectedValuePath = "Id",
                DisplayMemberPath = "Name"
            };
            if (_lookups != null && _lookups.ContainsKey("ProductId"))
                colProduct.ItemsSource = _lookups["ProductId"];

            _detailsGrid.Columns.Add(colProduct);
            _detailsGrid.Columns.Add(new DataGridTextColumn { Header = "Кол-во", Binding = new System.Windows.Data.Binding("Quantity"), Width = 70 });
            _detailsGrid.Columns.Add(new DataGridTextColumn { Header = "Цена", Binding = new System.Windows.Data.Binding("UnitPrice"), Width = 80 });

            _detailsGrid.CellEditEnding += (s, e) =>
            {
                if (e.Column.Header.ToString() == "Товар" && e.EditingElement is ComboBox cb && cb.SelectedValue != null)
                {
                    int productId = (int)cb.SelectedValue;
                    if (_lookups != null && _lookups.ContainsKey("ProductId"))
                    {
                        var products = _lookups["ProductId"] as System.Collections.IEnumerable;
                        var product = products?.Cast<Product>().FirstOrDefault(p => p.Id == productId);
                        if (product != null && e.Row.Item is OrderDetail detail)
                        {
                            detail.UnitPrice = product.WholesalePrice;
                            // Находим колонку цены и обновляем её визуально (опционально, т.к. есть Binding)
                        }
                    }
                }
            };

            FieldsPanel.Children.Add(_detailsGrid);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (_item is System.ComponentModel.IDataErrorInfo dataErrorInfo)
            {
                var properties = _item.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var error = dataErrorInfo[prop.Name];
                    if (!string.IsNullOrEmpty(error))
                    {
                        MessageBox.Show($"Ошибка в поле '{(_translations.ContainsKey(prop.Name) ? _translations[prop.Name] : prop.Name)}': {error}", "Валидация");
                        return;
                    }
                }
            }
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
    public class RatingToIndexConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int rating) return rating - 1;
            return -1; // Ничего не выбрано
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int index && index >= 0) return index + 1;
            return null;
        }
    }
}
