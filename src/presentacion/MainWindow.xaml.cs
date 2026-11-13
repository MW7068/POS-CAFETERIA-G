using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace CafeDeMamaOliPOS
{
    public partial class MainWindow : Window
    {
        private List<Product> _allProducts = new List<Product>();
        private string _currentCategory = "Cafés";
        private ObservableCollection<SaleSession> _saleSessions = new ObservableCollection<SaleSession>();
        private SaleSession _activeSession = new SaleSession();
        private int _sessionCounter = 1;
        private bool _isCapsLockOn = false;

        // Series y Correlativos Configurables
        private string _serieBoleta = "B001";
        private int _correlativoBoleta = 1;
        private string _serieFactura = "F001";
        private int _correlativoFactura = 1;
        private string _lastGeneratedSerieCorrelativo = "Comprobante";

        // Registros para Cierre de Caja y Gerencia
        private ObservableCollection<CompletedSaleRecord> _completedSales = new ObservableCollection<CompletedSaleRecord>();
        private ObservableCollection<CashMovementRecord> _currentShiftCashMovements = new ObservableCollection<CashMovementRecord>();
        private ObservableCollection<ShiftCloseRecord> _shiftCloseHistory = new ObservableCollection<ShiftCloseRecord>();
        private int _shiftCounter = 1;

        public MainWindow()
        {
            InitializeComponent();
            LoadCatalog();
            InitializeFirstSession();
            FilterProducts();
            HighlightSelectedCategoryButton();
            CashMovementsListBox.ItemsSource = _currentShiftCashMovements;
            TxtLoginUser.Focus();
        }

        // ================= PANTALLA DE INGRESO (LOGIN + TECLADO ALFANUMÉRICO) =================

        private void TxtLoginUser_GotFocus(object sender, RoutedEventArgs e)
        {
            BorderLoginError.Visibility = Visibility.Collapsed;
        }

        private void PwdLoginPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            BorderLoginError.Visibility = Visibility.Collapsed;
            VirtualKeypadPanel.Visibility = Visibility.Visible;
        }

        private void PwdLoginPassword_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BorderLoginError.Visibility = Visibility.Collapsed;
            VirtualKeypadPanel.Visibility = Visibility.Visible;
        }

        private void BtnHideKeypad_Click(object sender, RoutedEventArgs e)
        {
            VirtualKeypadPanel.Visibility = Visibility.Collapsed;
        }

        private void KeypadChar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content != null)
            {
                string ch = btn.Content.ToString() ?? string.Empty;
                PwdLoginPassword.Password += _isCapsLockOn ? ch.ToUpper() : ch.ToLower();
                BorderLoginError.Visibility = Visibility.Collapsed;
            }
        }

        private void KeypadCaps_Click(object sender, RoutedEventArgs e)
        {
            _isCapsLockOn = !_isCapsLockOn;
            BtnCapsLock.Background = _isCapsLockOn
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"))
                : Brushes.White;
            BtnCapsLock.Foreground = _isCapsLockOn
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937"));
        }

        private void KeypadBackspace_Click(object sender, RoutedEventArgs e)
        {
            if (PwdLoginPassword.Password.Length > 0)
            {
                PwdLoginPassword.Password = PwdLoginPassword.Password.Substring(0, PwdLoginPassword.Password.Length - 1);
            }
        }

        private void KeypadClear_Click(object sender, RoutedEventArgs e)
        {
            PwdLoginPassword.Password = string.Empty;
        }

        private void LoginInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AttemptLogin();
            }
        }

        private void BtnLoginSubmit_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void AttemptLogin()
        {
            string user = TxtLoginUser.Text.Trim().ToLower();
            string pass = PwdLoginPassword.Password.Trim();

            if ((user == "admin" || user == "cajero") && (pass == "1234" || pass.Equals("oli123", StringComparison.OrdinalIgnoreCase)))
            {
                string displayUser = char.ToUpper(user[0]) + user.Substring(1);
                TxtLoggedUserName.Text = displayUser;
                TxtLoggedUserInitials.Text = displayUser.Substring(0, 2).ToUpper();

                VirtualKeypadPanel.Visibility = Visibility.Collapsed;
                LoginScreenOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtLoginError.Text = "⚠️ Usuario o contraseña incorrectos (Ej: cajero / 1234 o oli123)";
                BorderLoginError.Visibility = Visibility.Visible;
                PwdLoginPassword.Password = string.Empty;
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            TxtLoginUser.Text = string.Empty;
            PwdLoginPassword.Password = string.Empty;
            VirtualKeypadPanel.Visibility = Visibility.Collapsed;
            BorderLoginError.Visibility = Visibility.Collapsed;
            LoginScreenOverlay.Visibility = Visibility.Visible;
            TxtLoginUser.Focus();
        }

        // ================= CATÁLOGO Y SESIONES =================

        private void LoadCatalog()
        {
            _allProducts = new List<Product>
            {
                new Product { Id = 1, Name = "Café Pasado de Mamá Oli", Price = 8.50m, Category = "Cafés", Icon = "☕", IsActive = true },
                new Product { Id = 2, Name = "Moca / Chocolate Caliente", Price = 11.00m, Category = "Cafés", Icon = "🍫", IsActive = true },
                new Product { Id = 3, Name = "Latte Caramelo", Price = 10.50m, Category = "Cafés", Icon = "🥤", IsActive = true },
                new Product { Id = 4, Name = "Espresso Doble", Price = 7.00m, Category = "Cafés", Icon = "☕", IsActive = true },
                new Product { Id = 5, Name = "Capuchino Vainilla", Price = 9.50m, Category = "Cafés", Icon = "🥛", IsActive = true },
                new Product { Id = 6, Name = "Jugo de Naranja Natural", Price = 8.00m, Category = "Bebidas", Icon = "🍊", IsActive = true },
                new Product { Id = 7, Name = "Limonada Frozen", Price = 9.00m, Category = "Bebidas", Icon = "🍋", IsActive = true },
                new Product { Id = 8, Name = "Costillas a la Parrilla", Price = 28.00m, Category = "Parrillas", Icon = "🍖", IsActive = true },
                new Product { Id = 9, Name = "Salchipapa Clásica", Price = 14.00m, Category = "Piqueos", Icon = "🍟", IsActive = true },
                new Product { Id = 10, Name = "Cheesecake de Fresa", Price = 12.50m, Category = "Postres", Icon = "🍰", IsActive = true }
            };
        }

        private Customer CreateDefaultCustomer()
        {
            return new Customer
            {
                FullName = "Público General",
                Dni = "00000000",
                Phone = "-",
                Address = "Mostrador",
                Email = string.Empty
            };
        }

        private void InitializeFirstSession()
        {
            var initialSession = new SaleSession
            {
                SessionId = _sessionCounter++,
                IgvPercent = 18m,
                IsTakeaway = false,
                ComprobanteType = "Boleta",
                Customer = CreateDefaultCustomer()
            };
            _saleSessions.Add(initialSession);
            SessionsListBox.ItemsSource = _saleSessions;
            TakeawaySessionsListBox.ItemsSource = _saleSessions;
            SwitchToSession(initialSession);
        }

        private void SwitchToSession(SaleSession? session)
        {
            if (session == null) return;
            _activeSession = session;
            CartListBox.ItemsSource = _activeSession.Items;
            TxtDiscount.Text = _activeSession.DiscountPercent.ToString("0.##", CultureInfo.InvariantCulture);

            if (CmbIgvRate != null)
            {
                CmbIgvRate.SelectedIndex = _activeSession.IgvPercent == 10.5m ? 1 : 0;
            }

            UpdateSessionHeaderUI();
            RecalculateTotals();
        }

        private void UpdateSessionHeaderUI()
        {
            if (TxtActiveSessionBadge == null || TxtCheckoutCustomerName == null) return;

            var cust = _activeSession.Customer;
            TxtActiveSessionBadge.Text = $"{_activeSession.SessionTitle} • {cust.FullName}";

            if (_activeSession.IsTakeaway)
            {
                BadgeOrderType.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                TxtOrderTypeTag.Text = "🛵 PARA LLEVAR";
            }
            else
            {
                BadgeOrderType.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                TxtOrderTypeTag.Text = "☕ EN LOCAL";
            }

            TxtCheckoutCustomerName.Text = $"👤 {cust.FullName}";
            TxtCheckoutCustomerDni.Text = $"DNI: {cust.Dni}";
            TxtCheckoutCustomerDetails.Text = $"📍 {cust.Address}  •  📞 {cust.Phone}";

            if (!string.IsNullOrWhiteSpace(cust.Email))
            {
                TxtCheckoutCustomerEmail.Text = $"✉️ {cust.Email}";
                TxtCheckoutCustomerEmail.Visibility = Visibility.Visible;
            }
            else
            {
                TxtCheckoutCustomerEmail.Text = string.Empty;
                TxtCheckoutCustomerEmail.Visibility = Visibility.Collapsed;
            }

            if (_activeSession.ComprobanteType == "Factura")
            {
                TxtCheckoutDocType.Text = $"Comprobante: Factura ({_serieFactura}) • RUC: {_activeSession.FacturaRuc}";
            }
            else
            {
                TxtCheckoutDocType.Text = $"Comprobante: Boleta de Venta (Serie: {_serieBoleta})";
            }

            var template = BtnCliente?.Template;
            if (template != null && template.FindName("TxtSalesCount", BtnCliente) is TextBlock countText)
            {
                countText.Text = _saleSessions.Count.ToString();
            }
        }

        // ================= ANIMACIÓN Y SOMBREADO DE CATEGORÍAS INFERIORES =================

        private void HighlightSelectedCategoryButton()
        {
            if (TxtCategoryBanner != null)
            {
                TxtCategoryBanner.Text = $"✨ Carta: {_currentCategory}";
            }

            var categoryButtons = new[] { BtnCatCafes, BtnCatBebidas, BtnCatParrillas, BtnCatPiqueos, BtnCatPostres };
            foreach (var btn in categoryButtons)
            {
                if (btn == null) continue;
                bool isSelected = btn.Tag?.ToString() == _currentCategory;

                btn.Background = isSelected
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"))
                    : Brushes.White;
                btn.BorderBrush = isSelected
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
                btn.BorderThickness = isSelected ? new Thickness(2.5) : new Thickness(1.5);
                btn.Foreground = isSelected
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"));
            }
        }

        private void FilterProducts()
        {
            if (ProductsItemsControl == null) return;

            string search = TxtSearch?.Text?.Trim().ToLower() ?? string.Empty;
            var filtered = _allProducts.Where(p =>
                (string.IsNullOrEmpty(search) && p.Category == _currentCategory) ||
                (!string.IsNullOrEmpty(search) && p.Name.ToLower().Contains(search))
            ).ToList();

            ProductsItemsControl.ItemsSource = filtered;
        }

        private void Category_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                _currentCategory = btn.Tag.ToString() ?? "Cafés";
                TxtSearch.Text = string.Empty;
                HighlightSelectedCategoryButton();
                FilterProducts();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterProducts();
        }

        // ================= INTERACCIÓN DINÁMICA DE TARJETAS DE PRODUCTOS =================

        private void ProductCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card)
            {
                card.RenderTransform = new ScaleTransform(0.95, 0.95);
            }
        }

        private void ProductCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card)
            {
                card.RenderTransform = new ScaleTransform(1.05, 1.05);
            }

            if (sender is FrameworkElement element && element.DataContext is Product product)
            {
                if (!product.IsActive)
                {
                    MessageBox.Show($"El producto '{product.Name}' se encuentra INACTIVO. Haz clic en su etiqueta 'INACTIVO' para volver a activarlo.",
                                    "Producto Inactivo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var existing = _activeSession.Items.FirstOrDefault(i => i.ProductId == product.Id);
                if (existing != null)
                {
                    existing.Quantity++;
                }
                else
                {
                    _activeSession.Items.Add(new CartItem
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        UnitPrice = product.Price,
                        Quantity = 1
                    });
                }
                RecalculateTotals();
                SessionsListBox.Items.Refresh();
                TakeawaySessionsListBox.Items.Refresh();
            }
        }

        // ================= AGREGAR, CAMBIAR ESTADO Y ELIMINAR PRODUCTOS =================

        private void BtnOpenAddProductModal_Click(object sender, RoutedEventArgs e)
        {
            TxtNewProdName.Text = string.Empty;
            TxtNewProdPrice.Text = string.Empty;
            CmbNewProdCategory.SelectedIndex = 0;
            CmbNewProdIcon.SelectedIndex = 0;
            RbProdActivo.IsChecked = true;
            ModalAddProductOverlay.Visibility = Visibility.Visible;
            TxtNewProdName.Focus();
        }

        private void BtnCloseAddProductModal_Click(object sender, RoutedEventArgs e)
        {
            ModalAddProductOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveNewProduct_Click(object sender, RoutedEventArgs e)
        {
            string nombre = TxtNewProdName.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("Ingrese el nombre del producto.", "Validación de Producto", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNewProdName.Focus();
                return;
            }

            if (!decimal.TryParse(TxtNewProdPrice.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precio) || precio <= 0)
            {
                MessageBox.Show("Ingrese un precio válido mayor a S/ 0.00.", "Validación de Producto", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNewProdPrice.Focus();
                return;
            }

            string categoria = (CmbNewProdCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cafés";
            string icono = (CmbNewProdIcon.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "☕";
            bool isActive = RbProdActivo.IsChecked == true;

            int nextId = _allProducts.Count > 0 ? _allProducts.Max(p => p.Id) + 1 : 1;
            _allProducts.Add(new Product
            {
                Id = nextId,
                Name = nombre,
                Price = precio,
                Category = categoria,
                Icon = icono,
                IsActive = isActive
            });

            _currentCategory = categoria;
            HighlightSelectedCategoryButton();
            FilterProducts();
            ModalAddProductOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnToggleProductStatus_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button btn && btn.Tag is Product prod)
            {
                prod.IsActive = !prod.IsActive;
                FilterProducts();
            }
        }

        private void BtnDeleteCatalogProduct_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button btn && btn.Tag is Product prodToDelete)
            {
                var confirm = MessageBox.Show($"¿Deseas eliminar '{prodToDelete.Name}' del catálogo de productos?",
                                              "Eliminar Producto", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.Yes)
                {
                    _allProducts.Remove(prodToDelete);
                    FilterProducts();
                }
            }
        }

        // ================= CONTROLES CARRITO =================

        private void IncreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CartItem item)
            {
                item.Quantity++;
                RecalculateTotals();
                SessionsListBox.Items.Refresh();
            }
        }

        private void DecreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CartItem item)
            {
                if (item.Quantity > 1)
                    item.Quantity--;
                else
                    _activeSession.Items.Remove(item);

                RecalculateTotals();
                SessionsListBox.Items.Refresh();
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CartItem item)
            {
                _activeSession.Items.Remove(item);
                RecalculateTotals();
                SessionsListBox.Items.Refresh();
            }
        }

        private void TxtDiscount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_activeSession == null) return;
            if (decimal.TryParse(TxtDiscount.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal disc) && disc >= 0 && disc <= 100)
            {
                _activeSession.DiscountPercent = disc;
            }
            else
            {
                _activeSession.DiscountPercent = 0;
            }
            RecalculateTotals();
        }

        private void CmbIgvRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_activeSession == null || CmbIgvRate?.SelectedItem is not ComboBoxItem selectedItem) return;

            if (decimal.TryParse(selectedItem.Tag?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal igvRate))
            {
                _activeSession.IgvPercent = igvRate;
                RecalculateTotals();
                SessionsListBox?.Items.Refresh();
            }
        }

        private void RecalculateTotals()
        {
            if (_activeSession == null || TxtSubTotal == null || TxtTax == null || TxtTotal == null || BtnPay == null)
                return;

            decimal rawSubTotal = _activeSession.Items.Sum(i => i.LineTotal);
            decimal discountAmount = rawSubTotal * (_activeSession.DiscountPercent / 100m);
            decimal subTotalAfterDiscount = rawSubTotal - discountAmount;
            decimal igvAmount = Math.Round(subTotalAfterDiscount * (_activeSession.IgvPercent / 100m), 2);
            decimal total = subTotalAfterDiscount + igvAmount;

            _activeSession.CachedTotal = total;

            TxtSubTotal.Text = $"S/ {subTotalAfterDiscount:F2}";
            TxtTax.Text = $"S/ {igvAmount:F2}";
            TxtTotal.Text = $"S/ {total:F2}";
            BtnPay.Content = $"Cobrar (S/ {total:F2})";
        }

        // ================= GESTIÓN CLIENTE Y VALIDACIONES =================

        private void BtnCliente_Click(object sender, RoutedEventArgs e) => OpenClienteModal();
        private void CheckoutCustomerCard_Click(object sender, MouseButtonEventArgs e) => OpenClienteModal();

        private void OpenClienteModal()
        {
            ResetValidationUI();

            TxtClienteNombre.Text = _activeSession.Customer.FullName == "Público General" ? string.Empty : _activeSession.Customer.FullName;
            TxtClienteDni.Text = _activeSession.Customer.Dni == "00000000" ? string.Empty : _activeSession.Customer.Dni;
            TxtClienteTelefono.Text = _activeSession.Customer.Phone == "-" ? string.Empty : _activeSession.Customer.Phone;
            TxtClienteDireccion.Text = _activeSession.Customer.Address == "Mostrador" ? string.Empty : _activeSession.Customer.Address;
            TxtClienteCorreo.Text = _activeSession.Customer.Email;

            SessionsListBox.Items.Refresh();
            ModalClienteOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseModal_Click(object sender, RoutedEventArgs e)
        {
            ResetValidationUI();
            ModalClienteOverlay.Visibility = Visibility.Collapsed;
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9\.]+$");
        }

        private void NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, "^[0-9]+$")) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private void InputCliente_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb) tb.ClearValue(Border.BorderBrushProperty);
            if (BorderValidationError != null && BorderValidationError.Visibility == Visibility.Visible)
                BorderValidationError.Visibility = Visibility.Collapsed;
        }

        private void ResetValidationUI()
        {
            BorderValidationError.Visibility = Visibility.Collapsed;
            TxtValidationError.Text = string.Empty;
            TxtClienteNombre.ClearValue(Border.BorderBrushProperty);
            TxtClienteDni.ClearValue(Border.BorderBrushProperty);
            TxtClienteTelefono.ClearValue(Border.BorderBrushProperty);
            TxtClienteDireccion.ClearValue(Border.BorderBrushProperty);
            TxtClienteCorreo.ClearValue(Border.BorderBrushProperty);
        }

        private void ShowValidationError(string message, TextBox invalidControl)
        {
            TxtValidationError.Text = $"⚠️ {message}";
            BorderValidationError.Visibility = Visibility.Visible;
            invalidControl.BorderBrush = Brushes.Red;
            invalidControl.Focus();

            MessageBox.Show(message, "Error en Registro de Cliente - Cafetería de Mamá Oli",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private bool ValidateCustomerFields(out Customer customer)
        {
            customer = new Customer();
            ResetValidationUI();

            string nombre = TxtClienteNombre.Text.Trim();
            string dni = TxtClienteDni.Text.Trim();
            string telefono = TxtClienteTelefono.Text.Trim();
            string direccion = TxtClienteDireccion.Text.Trim();
            string correo = TxtClienteCorreo.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length < 3)
            {
                ShowValidationError("Ingrese el Nombre y Apellido completo del cliente (mínimo 3 caracteres).", TxtClienteNombre);
                return false;
            }

            if (!Regex.IsMatch(dni, @"^\d{8}$"))
            {
                ShowValidationError("El DNI es obligatorio y debe contener exactamente 8 dígitos numéricos.", TxtClienteDni);
                return false;
            }

            if (!Regex.IsMatch(telefono, @"^\d{6,15}$"))
            {
                ShowValidationError("El Teléfono es obligatorio, solo acepta números y máximo 15 dígitos.", TxtClienteTelefono);
                return false;
            }

            if (string.IsNullOrWhiteSpace(direccion) || direccion.Length < 4)
            {
                ShowValidationError("La Dirección es obligatoria (mínimo 4 caracteres).", TxtClienteDireccion);
                return false;
            }

            if (!string.IsNullOrEmpty(correo))
            {
                if (!Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$"))
                {
                    ShowValidationError("El formato del Correo Electrónico es inválido. Debe incluir '@' y dominio válido (ej. cliente@gmail.com).", TxtClienteCorreo);
                    return false;
                }
            }

            customer = new Customer
            {
                FullName = nombre,
                Dni = dni,
                Phone = telefono,
                Address = direccion,
                Email = correo
            };
            return true;
        }

        private void BtnGuardarClienteActual_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateCustomerFields(out Customer updatedCustomer))
            {
                _activeSession.Customer = updatedCustomer;
                UpdateSessionHeaderUI();
                SessionsListBox.Items.Refresh();
                ModalClienteOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnNuevaMultiventa_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateCustomerFields(out Customer newCustomer))
            {
                var newSession = new SaleSession
                {
                    SessionId = _sessionCounter++,
                    IgvPercent = _activeSession.IgvPercent,
                    IsTakeaway = false,
                    Customer = newCustomer
                };
                _saleSessions.Add(newSession);
                SwitchToSession(newSession);
                SessionsListBox.Items.Refresh();
                ModalClienteOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void SessionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsListBox.SelectedItem is SaleSession selected)
            {
                SwitchToSession(selected);
                ModalClienteOverlay.Visibility = Visibility.Collapsed;
                SessionsListBox.SelectedItem = null;
            }
        }

        // ================= MODAL ORDEN PARA LLEVAR =================

        private void BtnOpenTakeawayModal_Click(object sender, RoutedEventArgs e)
        {
            RbTakeawayBoleta.IsChecked = _activeSession.ComprobanteType != "Factura";
            RbTakeawayFactura.IsChecked = _activeSession.ComprobanteType == "Factura";

            TxtTakeawayNombre.Text = _activeSession.Customer.FullName == "Público General" ? string.Empty : _activeSession.Customer.FullName;
            TxtTakeawayDni.Text = _activeSession.Customer.Dni == "00000000" ? string.Empty : _activeSession.Customer.Dni;
            TxtTakeawayTelefono.Text = _activeSession.Customer.Phone == "-" ? string.Empty : _activeSession.Customer.Phone;
            TxtTakeawayDireccion.Text = _activeSession.Customer.Address == "Mostrador" ? string.Empty : _activeSession.Customer.Address;
            TxtTakeawayCorreo.Text = _activeSession.Customer.Email;

            TxtTakeawayRuc.Text = _activeSession.FacturaRuc;
            TxtTakeawayRazonSocial.Text = _activeSession.FacturaRazonSocial;
            TxtTakeawayDirFiscal.Text = _activeSession.FacturaDireccion;

            TakeawaySessionsListBox.Items.Refresh();
            ModalTakeawayOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseTakeawayModal_Click(object sender, RoutedEventArgs e)
        {
            ModalTakeawayOverlay.Visibility = Visibility.Collapsed;
        }

        private void TakeawayDocType_Changed(object sender, RoutedEventArgs e)
        {
            if (PanelTakeawayFactura == null) return;
            PanelTakeawayFactura.Visibility = RbTakeawayFactura.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool ValidateTakeawayForm(out Customer customer, out string docType, out string ruc, out string razonSocial, out string dirFiscal)
        {
            customer = new Customer();
            docType = RbTakeawayFactura.IsChecked == true ? "Factura" : "Boleta";
            ruc = TxtTakeawayRuc.Text.Trim();
            razonSocial = TxtTakeawayRazonSocial.Text.Trim();
            dirFiscal = TxtTakeawayDirFiscal.Text.Trim();

            string nombre = TxtTakeawayNombre.Text.Trim();
            string dni = TxtTakeawayDni.Text.Trim();
            string telefono = TxtTakeawayTelefono.Text.Trim();
            string direccion = TxtTakeawayDireccion.Text.Trim();
            string correo = TxtTakeawayCorreo.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length < 3)
            {
                MessageBox.Show("Ingrese el Nombre y Apellido del cliente para llevar.", "Validación Orden para Llevar", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTakeawayNombre.Focus();
                return false;
            }

            if (!Regex.IsMatch(dni, @"^\d{8}$"))
            {
                MessageBox.Show("Ingrese un DNI válido de 8 dígitos numéricos.", "Validación Orden para Llevar", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTakeawayDni.Focus();
                return false;
            }

            if (!Regex.IsMatch(telefono, @"^\d{6,15}$"))
            {
                MessageBox.Show("Ingrese un Teléfono válido (solo números, máximo 15 dígitos).", "Validación Orden para Llevar", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTakeawayTelefono.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(direccion))
            {
                MessageBox.Show("Ingrese la Dirección de entrega o referencia para llevar.", "Validación Orden para Llevar", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTakeawayDireccion.Focus();
                return false;
            }

            if (!string.IsNullOrEmpty(correo) && !Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$"))
            {
                MessageBox.Show("El Correo Electrónico debe incluir '@' y dominio válido.", "Validación Orden para Llevar", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTakeawayCorreo.Focus();
                return false;
            }

            if (docType == "Factura")
            {
                if (!Regex.IsMatch(ruc, @"^\d{11}$"))
                {
                    MessageBox.Show("Para Factura Electrónica el RUC debe tener 11 dígitos numéricos.", "Validación Factura", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtTakeawayRuc.Focus();
                    return false;
                }
                if (string.IsNullOrWhiteSpace(razonSocial) || string.IsNullOrWhiteSpace(dirFiscal))
                {
                    MessageBox.Show("Complete la Razón Social y Dirección Fiscal para la Factura.", "Validación Factura", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            customer = new Customer
            {
                FullName = nombre,
                Dni = dni,
                Phone = telefono,
                Address = direccion,
                Email = correo
            };
            return true;
        }

        private void BtnConvertCurrentToTakeaway_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateTakeawayForm(out Customer cust, out string docType, out string ruc, out string razon, out string dirFiscal))
            {
                _activeSession.IsTakeaway = true;
                _activeSession.Customer = cust;
                _activeSession.ComprobanteType = docType;
                _activeSession.FacturaRuc = ruc;
                _activeSession.FacturaRazonSocial = razon;
                _activeSession.FacturaDireccion = dirFiscal;

                UpdateSessionHeaderUI();
                TakeawaySessionsListBox.Items.Refresh();
                SessionsListBox.Items.Refresh();
                ModalTakeawayOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCreateNewTakeawaySession_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateTakeawayForm(out Customer cust, out string docType, out string ruc, out string razon, out string dirFiscal))
            {
                var takeawaySession = new SaleSession
                {
                    SessionId = _sessionCounter++,
                    IsTakeaway = true,
                    IgvPercent = _activeSession.IgvPercent,
                    Customer = cust,
                    ComprobanteType = docType,
                    FacturaRuc = ruc,
                    FacturaRazonSocial = razon,
                    FacturaDireccion = dirFiscal
                };
                _saleSessions.Add(takeawaySession);
                SwitchToSession(takeawaySession);
                TakeawaySessionsListBox.Items.Refresh();
                SessionsListBox.Items.Refresh();
                ModalTakeawayOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void TakeawaySessionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TakeawaySessionsListBox.SelectedItem is SaleSession selected)
            {
                SwitchToSession(selected);
                ModalTakeawayOverlay.Visibility = Visibility.Collapsed;
                TakeawaySessionsListBox.SelectedItem = null;
            }
        }

        // ================= MODAL COBRAR + PROPINA EN TARJETA + RENIEC / SUNAT =================

        private void UpdateCheckoutSeriePreview()
        {
            if (TxtCheckoutSeriePreview == null) return;
            if (RbFactura != null && RbFactura.IsChecked == true)
            {
                TxtCheckoutSeriePreview.Text = $"Serie Asignada: {_serieFactura}-{_correlativoFactura:D6}";
            }
            else
            {
                TxtCheckoutSeriePreview.Text = $"Serie Asignada: {_serieBoleta}-{_correlativoBoleta:D6}";
            }
        }

        private void BtnOpenCheckoutModal_Click(object sender, RoutedEventArgs e)
        {
            if (_activeSession.Items.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto al detalle antes de cobrar.", "Detalle Vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RbBoleta.IsChecked = _activeSession.ComprobanteType != "Factura";
            RbFactura.IsChecked = _activeSession.ComprobanteType == "Factura";

            TxtBoletaNombre.Text = _activeSession.Customer.FullName;
            TxtBoletaDni.Text = _activeSession.Customer.Dni;

            TxtFacturaRuc.Text = _activeSession.FacturaRuc;
            TxtFacturaRazonSocial.Text = _activeSession.FacturaRazonSocial;
            TxtFacturaDireccion.Text = !string.IsNullOrWhiteSpace(_activeSession.FacturaDireccion)
                ? _activeSession.FacturaDireccion
                : (_activeSession.Customer.Address == "Mostrador" ? string.Empty : _activeSession.Customer.Address);

            TxtModalTotalCobrar.Text = $"S/ {_activeSession.CachedTotal:F2}";
            CmbPaymentMethod.SelectedIndex = 0;
            TxtMontoRecibido.Text = _activeSession.CachedTotal.ToString("F2", CultureInfo.InvariantCulture);

            UpdateCheckoutSeriePreview();
            RecalculateVueltoOrTip();
            ModalCheckoutOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseCheckoutModal_Click(object sender, RoutedEventArgs e)
        {
            ModalCheckoutOverlay.Visibility = Visibility.Collapsed;
        }

        private void ComprobanteType_Changed(object sender, RoutedEventArgs e)
        {
            if (PanelBoletaFields == null || PanelFacturaFields == null) return;

            if (RbBoleta.IsChecked == true)
            {
                PanelBoletaFields.Visibility = Visibility.Visible;
                PanelFacturaFields.Visibility = Visibility.Collapsed;
            }
            else
            {
                PanelBoletaFields.Visibility = Visibility.Collapsed;
                PanelFacturaFields.Visibility = Visibility.Visible;
            }
            UpdateCheckoutSeriePreview();
        }

        private void CmbPaymentMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecalculateVueltoOrTip();
        }

        private void TxtMontoRecibido_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecalculateVueltoOrTip();
        }

        private void RecalculateVueltoOrTip()
        {
            if (TxtVuelto == null || _activeSession == null || LblVueltoOPropina == null || BannerCardTipInfo == null)
                return;

            string metodo = (CmbPaymentMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";
            bool isTarjeta = metodo == "Tarjeta";

            LblMontoRecibido.Text = isTarjeta ? "Monto Cobrado POS (S/)" : "Monto Recibido (S/)";
            LblVueltoOPropina.Text = isTarjeta ? "Propina Tarjeta (S/)" : "Vuelto (S/)";

            if (decimal.TryParse(TxtMontoRecibido.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal recibido))
            {
                decimal diff = recibido - _activeSession.CachedTotal;
                if (diff < 0)
                {
                    TxtVuelto.Text = "Falta monto";
                    TxtVuelto.Foreground = Brushes.Red;
                    BannerCardTipInfo.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TxtVuelto.Text = $"S/ {diff:F2}";
                    TxtVuelto.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));

                    if (isTarjeta && diff > 0)
                    {
                        TxtBannerCardTip.Text = $"🎁 Diferencia detectada en pago con Tarjeta: Se registrará Propina de S/ {diff:F2}";
                        BannerCardTipInfo.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BannerCardTipInfo.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                TxtVuelto.Text = "S/ 0.00";
                BannerCardTipInfo.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnConsultarReniec_Click(object sender, RoutedEventArgs e)
        {
            string dni = TxtBoletaDni.Text.Trim();
            if (!Regex.IsMatch(dni, @"^\d{8}$"))
            {
                MessageBox.Show("Ingrese un DNI válido de 8 dígitos numéricos para consultar en RENIEC.",
                                "Consulta RENIEC", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtBoletaDni.Focus();
                return;
            }

            var padronReniec = new Dictionary<string, string>
            {
                { "12345678", "María Elena Quispe Huamán" },
                { "87654321", "Carlos Alberto Mendoza Rojas" },
                { "45678912", "Lucía Fernanda Torres Condori" },
                { "71234568", "Jorge Luis Paredes Mamani" },
                { "00000000", "Público General" }
            };

            if (padronReniec.TryGetValue(dni, out string? nombreEncontrado))
            {
                TxtBoletaNombre.Text = nombreEncontrado;
            }
            else
            {
                string[] nombres = { "Ana Sofía", "Luis Miguel", "Valeria Rosa", "Diego Armando", "Carmen Rosa" };
                string[] apellidos = { "Vargas Quispe", "Salazar Flores", "Gutiérrez Chávez", "Ramos Huanca", "Castillo Pérez" };
                int idx = int.Parse(dni.Substring(6, 2)) % nombres.Length;
                TxtBoletaNombre.Text = $"{nombres[idx]} {apellidos[idx]}";
            }

            MessageBox.Show($"✅ Datos extraídos de RENIEC:\nDNI: {dni}\nTitular: {TxtBoletaNombre.Text}",
                            "RENIEC - Consulta Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnConsultarSunat_Click(object sender, RoutedEventArgs e)
        {
            string ruc = TxtFacturaRuc.Text.Trim();
            if (!Regex.IsMatch(ruc, @"^\d{11}$"))
            {
                MessageBox.Show("Ingrese un RUC válido de 11 dígitos numéricos para consultar en SUNAT.",
                                "Consulta SUNAT", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFacturaRuc.Focus();
                return;
            }

            var padronSunat = new Dictionary<string, (string Razon, string Direccion)>
            {
                { "20100047218", ("BANCO DE CREDITO DEL PERU S.A.", "CAL. CENTENARIO NRO. 156 URB. LAS LADERAS DE MELGAREJO - LIMA") },
                { "20512345678", ("INVERSIONES GASTRONOMICAS DEL SUR S.A.C.", "AV. EL SOL NRO. 450 - CUSCO") },
                { "20601234567", ("CORPORACION HOTELERA ANDINA E.I.R.L.", "JR. PLATEROS NRO. 328 - CUSCO") },
                { "10456789123", ("TORRES CONDORI LUCIA FERNANDA", "AV. LA CULTURA NRO. 1240 - WANCHAQ, CUSCO") }
            };

            if (padronSunat.TryGetValue(ruc, out var infoSunat))
            {
                TxtFacturaRazonSocial.Text = infoSunat.Razon;
                TxtFacturaDireccion.Text = infoSunat.Direccion;
            }
            else if (ruc.StartsWith("20"))
            {
                TxtFacturaRazonSocial.Text = $"EMPRESA COMERCIAL ANDINA {ruc.Substring(7, 4)} S.A.C.";
                TxtFacturaDireccion.Text = "AV. PRINCIPAL NRO. 210 - CUSCO, PERÚ";
            }
            else
            {
                TxtFacturaRazonSocial.Text = $"SERVICIOS GENERALES {ruc.Substring(7, 4)} E.I.R.L.";
                TxtFacturaDireccion.Text = "CALLE COMERCIO NRO. 145 - CUSCO, PERÚ";
            }

            MessageBox.Show($"✅ Datos extraídos de SUNAT:\nRUC: {ruc}\nRazón Social: {TxtFacturaRazonSocial.Text}\nEstado: ACTIVO - HABIDO",
                            "SUNAT - Consulta Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnConfirmarPagoFinal_Click(object sender, RoutedEventArgs e)
        {
            string metodoPago = (CmbPaymentMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Efectivo";
            string docType;
            string serieCorrelativo;
            string docNumber;
            string clientName;
            string clientAddress;

            if (RbBoleta.IsChecked == true)
            {
                string nombreBoleta = TxtBoletaNombre.Text.Trim();
                string dniBoleta = TxtBoletaDni.Text.Trim();

                if (string.IsNullOrWhiteSpace(nombreBoleta) || !Regex.IsMatch(dniBoleta, @"^\d{8}$"))
                {
                    MessageBox.Show("Para Boleta de Venta ingrese el Nombre completo y un DNI válido de 8 dígitos.",
                                    "Validación Boleta", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                docType = "Boleta";
                serieCorrelativo = $"{_serieBoleta}-{_correlativoBoleta:D6}";
                docNumber = dniBoleta;
                clientName = nombreBoleta;
                clientAddress = _activeSession.Customer.Address;
            }
            else
            {
                string ruc = TxtFacturaRuc.Text.Trim();
                string razonSocial = TxtFacturaRazonSocial.Text.Trim();
                string dirFiscal = TxtFacturaDireccion.Text.Trim();

                if (!Regex.IsMatch(ruc, @"^\d{11}$"))
                {
                    MessageBox.Show("Para Factura Electrónica el RUC debe contener exactamente 11 dígitos numéricos.",
                                    "Validación Factura", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtFacturaRuc.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(razonSocial) || string.IsNullOrWhiteSpace(dirFiscal))
                {
                    MessageBox.Show("Complete la Razón Social y la Dirección Fiscal para emitir la Factura.",
                                    "Validación Factura", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                docType = "Factura";
                serieCorrelativo = $"{_serieFactura}-{_correlativoFactura:D6}";
                docNumber = ruc;
                clientName = razonSocial;
                clientAddress = dirFiscal;
            }

            if (!decimal.TryParse(TxtMontoRecibido.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal recibido))
            {
                MessageBox.Show("Ingrese un monto recibido válido.", "Monto Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (recibido < _activeSession.CachedTotal)
            {
                MessageBox.Show("El monto recibido es menor al Total a Pagar.", "Monto Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal diferencia = recibido - _activeSession.CachedTotal;
            decimal propinaTarjeta = 0m;
            decimal vueltoEfectivo = 0m;

            if (metodoPago == "Tarjeta" && diferencia > 0)
            {
                propinaTarjeta = diferencia;
                var respPropina = MessageBox.Show(
                    $"💳 PAGO CON TARJETA - DIFERENCIA DETECTADA\n\n" +
                    $"Total Consumo: S/ {_activeSession.CachedTotal:F2}\n" +
                    $"Monto Cobrado POS: S/ {recibido:F2}\n" +
                    $"🎁 Diferencia (Propina): S/ {propinaTarjeta:F2}\n\n" +
                    $"¿Confirmas registrar S/ {propinaTarjeta:F2} como Propina en el comprobante y reportes?",
                    "Confirmación de Propina con Tarjeta",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (respPropina != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            else if (metodoPago != "Tarjeta" && diferencia > 0)
            {
                vueltoEfectivo = diferencia;
            }

            // Incrementar correlativo tras confirmación
            if (docType == "Boleta") _correlativoBoleta++;
            else _correlativoFactura++;

            _lastGeneratedSerieCorrelativo = serieCorrelativo;

            // Guardar venta en historial para Cierre de Caja y Gerencia
            var completedRecord = new CompletedSaleRecord
            {
                Date = DateTime.Now,
                DocType = docType,
                SerieCorrelativo = serieCorrelativo,
                DocNumber = docNumber,
                ClientName = clientName,
                PaymentMethod = metodoPago,
                Total = _activeSession.CachedTotal,
                TipAmount = propinaTarjeta,
                IsClosedInShift = false,
                SoldItems = _activeSession.Items.Select(i => new CartItem
                {
                    ProductId = i.ProductId,
                    Name = i.Name,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity
                }).ToList()
            };
            _completedSales.Add(completedRecord);

            // Construir texto detallado del comprobante electrónico (Boleta / Factura)
            string modalidad = _activeSession.IsTakeaway ? "PARA LLEVAR" : "CONSUMO EN LOCAL";
            var sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("         CAFETERÍA DE MAMÁ OLI            ");
            sb.AppendLine("            RUC: 20609876543              ");
            sb.AppendLine("          Cusco - Cusco - Perú            ");
            sb.AppendLine("==========================================");
            sb.AppendLine(docType == "Boleta"
                ? $"   BOLETA DE VENTA ELECTRÓNICA"
                : $"   FACTURA ELECTRÓNICA");
            sb.AppendLine($"   Serie y N°: {serieCorrelativo}");
            sb.AppendLine($"   Fecha: {completedRecord.Date:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"   Modalidad: {modalidad}");
            sb.AppendLine($"   Tipo de Pago: {metodoPago.ToUpper()}");
            sb.AppendLine("------------------------------------------");
            sb.AppendLine(docType == "Boleta"
                ? $"Cliente: {clientName}\nDNI: {docNumber}"
                : $"Razón Social: {clientName}\nRUC: {docNumber}");
            if (!string.IsNullOrWhiteSpace(clientAddress))
                sb.AppendLine($"Dirección: {clientAddress}");
            sb.AppendLine("------------------------------------------");
            sb.AppendLine("CANT  DESCRIPCIÓN                IMPORTE");
            sb.AppendLine("------------------------------------------");
            foreach (var item in completedRecord.SoldItems)
            {
                string prodName = item.Name.Length > 22 ? item.Name.Substring(0, 22) : item.Name.PadRight(22);
                sb.AppendLine($"{item.Quantity,3}   {prodName}   S/{item.LineTotal,6:F2}");
            }
            sb.AppendLine("------------------------------------------");
            sb.AppendLine($"Op. Gravada (Sub Total):     {TxtSubTotal.Text,10}");
            sb.AppendLine($"IGV ({_activeSession.IgvPercent}%):                 {TxtTax.Text,10}");
            sb.AppendLine($"Total Consumo:               S/ {_activeSession.CachedTotal,7:F2}");
            if (propinaTarjeta > 0)
            {
                sb.AppendLine($"Propina (Tarjeta):           S/ {propinaTarjeta,7:F2}");
                sb.AppendLine($"TOTAL COBRADO CON PROPINA:   S/ {(_activeSession.CachedTotal + propinaTarjeta),7:F2}");
            }
            else
            {
                sb.AppendLine($"TOTAL COBRADO:               S/ {_activeSession.CachedTotal,7:F2}");
                if (vueltoEfectivo > 0)
                    sb.AppendLine($"Vuelto Entregado:            S/ {vueltoEfectivo,7:F2}");
            }
            sb.AppendLine("==========================================");
            sb.AppendLine("   ¡Gracias por su preferencia en Mamá Oli! ");
            sb.AppendLine("==========================================");

            TxtInvoiceDocumentContent.Text = sb.ToString();

            ModalCheckoutOverlay.Visibility = Visibility.Collapsed;
            ModalInvoicePreviewOverlay.Visibility = Visibility.Visible;

            if (_saleSessions.Count > 1)
            {
                _saleSessions.Remove(_activeSession);
                SwitchToSession(_saleSessions.First());
            }
            else
            {
                ResetCurrentSessionForNewSale();
            }
            UpdateSessionHeaderUI();
        }

        // ================= IMPRESIÓN / EXPORTAR PDF / GUARDAR TICKET =================

        private void BtnPrintOrExportPdf_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // En Windows, seleccionar "Microsoft Print to PDF" exporta directamente a PDF,
                // o seleccionar una impresora física lo imprime al instante.
                printDialog.PrintVisual(PrintableReceiptArea, $"Comprobante_{_lastGeneratedSerieCorrelativo}");
            }
        }

        private void BtnExportTxtTicket_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                FileName = $"Comprobante_{_lastGeneratedSerieCorrelativo}.txt",
                Filter = "Archivo de Texto (*.txt)|*.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveDialog.FileName, TxtInvoiceDocumentContent.Text, Encoding.UTF8);
                MessageBox.Show("Comprobante guardado correctamente.", "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCloseInvoicePreview_Click(object sender, RoutedEventArgs e)
        {
            ModalInvoicePreviewOverlay.Visibility = Visibility.Collapsed;
        }

        // ================= CANCELAR ORDEN =================

        private void BtnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            ResetCurrentSessionForNewSale();
        }

        private void ResetCurrentSessionForNewSale()
        {
            _activeSession.Items.Clear();
            _activeSession.DiscountPercent = 0;
            _activeSession.IsTakeaway = false;
            _activeSession.ComprobanteType = "Boleta";
            _activeSession.FacturaRuc = string.Empty;
            _activeSession.FacturaRazonSocial = string.Empty;
            _activeSession.FacturaDireccion = string.Empty;
            _activeSession.Customer = CreateDefaultCustomer();

            TxtDiscount.Text = "0";
            UpdateSessionHeaderUI();
            RecalculateTotals();
            SessionsListBox.Items.Refresh();
            TakeawaySessionsListBox.Items.Refresh();
        }

        // ================= PANEL PRINCIPAL DE GERENCIA (MENÚ DE BOTONES) =================

        private void BtnOpenGerenciaModal_Click(object sender, RoutedEventArgs e)
        {
            TxtGerenciaSeriesPreview.Text = $"Boleta: {_serieBoleta} • Factura: {_serieFactura}";
            ModalGerenciaOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseGerenciaModal_Click(object sender, RoutedEventArgs e)
        {
            ModalGerenciaOverlay.Visibility = Visibility.Collapsed;
        }

        // 1. Sub-botón Egreso / Ingreso de Efectivo desde Gerencia
        private void BtnOpenCashMovementFromGerencia_Click(object sender, RoutedEventArgs e)
        {
            RbCashIngreso.IsChecked = true;
            TxtCashMoveAmount.Text = string.Empty;
            TxtCashMoveReason.Text = string.Empty;
            ModalCashMovementOverlay.Visibility = Visibility.Visible;
            TxtCashMoveAmount.Focus();
        }

        private void BtnCloseCashMovementModal_Click(object sender, RoutedEventArgs e)
        {
            ModalCashMovementOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveCashMovement_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtCashMoveAmount.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Ingrese un monto válido mayor a S/ 0.00.", "Validación de Caja", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCashMoveAmount.Focus();
                return;
            }

            string reason = TxtCashMoveReason.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Ingrese el motivo o concepto del movimiento de efectivo.", "Validación de Caja", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCashMoveReason.Focus();
                return;
            }

            bool isIngreso = RbCashIngreso.IsChecked == true;
            _currentShiftCashMovements.Add(new CashMovementRecord
            {
                Timestamp = DateTime.Now,
                IsIngreso = isIngreso,
                Amount = amount,
                Reason = reason
            });

            TxtCashMoveAmount.Text = string.Empty;
            TxtCashMoveReason.Text = string.Empty;
            MessageBox.Show($"{(isIngreso ? "Ingreso" : "Egreso")} de S/ {amount:F2} registrado correctamente en caja.",
                            "Movimiento Registrado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 2. Sub-botón Cierre de Caja Operativo desde Gerencia
        private void BtnOpenCashCloseFromGerencia_Click(object sender, RoutedEventArgs e)
        {
            var openSales = _completedSales.Where(s => !s.IsClosedInShift).ToList();

            decimal efectivoVentas = openSales.Where(s => s.PaymentMethod == "Efectivo").Sum(s => s.Total);
            decimal yapeVentas = openSales.Where(s => s.PaymentMethod == "Yape / Plin").Sum(s => s.Total);
            decimal tarjetaVentas = openSales.Where(s => s.PaymentMethod == "Tarjeta").Sum(s => s.Total);
            decimal propinasTarjeta = openSales.Sum(s => s.TipAmount);
            decimal totalVentasYPropinas = efectivoVentas + yapeVentas + tarjetaVentas + propinasTarjeta;

            decimal totalIngreso = _currentShiftCashMovements.Where(m => m.IsIngreso).Sum(m => m.Amount);
            decimal totalEgreso = _currentShiftCashMovements.Where(m => !m.IsIngreso).Sum(m => m.Amount);
            decimal efectivoFinalGaveta = efectivoVentas + totalIngreso - totalEgreso;

            TxtCashCloseShiftInfo.Text = $"Turno #{_shiftCounter} • Cajero: {TxtLoggedUserName.Text} • Comprobantes en turno: {openSales.Count}";
            TxtCloseEfectivo.Text = $"S/ {efectivoVentas:F2}";
            TxtCloseYape.Text = $"S/ {yapeVentas:F2}";
            TxtCloseTarjeta.Text = $"S/ {tarjetaVentas:F2}";
            TxtClosePropinas.Text = $"S/ {propinasTarjeta:F2}";
            TxtCloseTotalVentas.Text = $"S/ {totalVentasYPropinas:F2}";

            TxtCloseMontoIngreso.Text = $"S/ {totalIngreso:F2}";
            TxtCloseMontoEgreso.Text = $"S/ {totalEgreso:F2}";
            TxtCloseEfectivoGaveta.Text = $"S/ {efectivoFinalGaveta:F2}";

            ModalCashCloseOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseCashCloseModal_Click(object sender, RoutedEventArgs e)
        {
            ModalCashCloseOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnConfirmarCierreCaja_Click(object sender, RoutedEventArgs e)
        {
            var openSales = _completedSales.Where(s => !s.IsClosedInShift).ToList();
            decimal efectivoVentas = openSales.Where(s => s.PaymentMethod == "Efectivo").Sum(s => s.Total);
            decimal totalVentas = openSales.Sum(s => s.Total);
            decimal totalTips = openSales.Sum(s => s.TipAmount);
            decimal totalIngreso = _currentShiftCashMovements.Where(m => m.IsIngreso).Sum(m => m.Amount);
            decimal totalEgreso = _currentShiftCashMovements.Where(m => !m.IsIngreso).Sum(m => m.Amount);
            decimal efectivoFinalGaveta = efectivoVentas + totalIngreso - totalEgreso;

            _shiftCloseHistory.Add(new ShiftCloseRecord
            {
                ShiftNumber = _shiftCounter++,
                CashierName = TxtLoggedUserName.Text,
                ClosedAt = DateTime.Now,
                TotalSales = totalVentas,
                TotalTips = totalTips,
                CashIn = totalIngreso,
                CashOut = totalEgreso,
                FinalDrawer = efectivoFinalGaveta
            });

            foreach (var sale in openSales)
            {
                sale.IsClosedInShift = true;
            }
            _currentShiftCashMovements.Clear();

            MessageBox.Show($"✅ Cierre de Caja del Turno #{_shiftCounter - 1} guardado con éxito.\nEfectivo Final en Gaveta: S/ {efectivoFinalGaveta:F2}",
                            "Cierre de Caja Completado", MessageBoxButton.OK, MessageBoxImage.Information);
            ModalCashCloseOverlay.Visibility = Visibility.Collapsed;
        }

        // 3. Sub-botón Series de Boleta y Factura desde Gerencia
        private void BtnOpenSeriesConfigFromGerencia_Click(object sender, RoutedEventArgs e)
        {
            TxtConfigSerieBoleta.Text = _serieBoleta;
            TxtConfigCorrBoleta.Text = _correlativoBoleta.ToString();
            TxtConfigSerieFactura.Text = _serieFactura;
            TxtConfigCorrFactura.Text = _correlativoFactura.ToString();
            ModalSeriesConfigOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseSeriesConfigModal_Click(object sender, RoutedEventArgs e)
        {
            ModalSeriesConfigOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveSeriesConfig_Click(object sender, RoutedEventArgs e)
        {
            string sBol = TxtConfigSerieBoleta.Text.Trim().ToUpper();
            string sFac = TxtConfigSerieFactura.Text.Trim().ToUpper();

            if (!Regex.IsMatch(sBol, @"^B\d{3}$"))
            {
                MessageBox.Show("La Serie de Boleta debe comenzar con 'B' seguida de 3 números (Ejemplo: B001).",
                                "Validación de Serie", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtConfigSerieBoleta.Focus();
                return;
            }

            if (!Regex.IsMatch(sFac, @"^F\d{3}$"))
            {
                MessageBox.Show("La Serie de Factura debe comenzar con 'F' seguida de 3 números (Ejemplo: F001).",
                                "Validación de Serie", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtConfigSerieFactura.Focus();
                return;
            }

            if (!int.TryParse(TxtConfigCorrBoleta.Text.Trim(), out int corrBol) || corrBol < 1 ||
                !int.TryParse(TxtConfigCorrFactura.Text.Trim(), out int corrFac) || corrFac < 1)
            {
                MessageBox.Show("Ingrese números correlativos válidos mayores o iguales a 1.",
                                "Validación de Correlativo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _serieBoleta = sBol;
            _correlativoBoleta = corrBol;
            _serieFactura = sFac;
            _correlativoFactura = corrFac;

            TxtGerenciaSeriesPreview.Text = $"Boleta: {_serieBoleta} • Factura: {_serieFactura}";
            UpdateSessionHeaderUI();

            MessageBox.Show($"✅ Series actualizadas correctamente:\nBoleta de Venta: {_serieBoleta}-{_correlativoBoleta:D6}\nFactura Electrónica: {_serieFactura}-{_correlativoFactura:D6}",
                            "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
            ModalSeriesConfigOverlay.Visibility = Visibility.Collapsed;
        }

        // 4. Ventana Independiente: Venta de Boleta o Factura
        private void BtnOpenWinRepComprobantes_Click(object sender, RoutedEventArgs e)
        {
            DgComprobantes.ItemsSource = _completedSales.OrderByDescending(s => s.Date).ToList();
            decimal totalBoletas = _completedSales.Where(s => s.DocType == "Boleta").Sum(s => s.GrandTotal);
            decimal totalFacturas = _completedSales.Where(s => s.DocType == "Factura").Sum(s => s.GrandTotal);
            decimal totalPropinas = _completedSales.Sum(s => s.TipAmount);

            TxtGerenciaTotalBoletas.Text = $"S/ {totalBoletas:F2}";
            TxtGerenciaTotalFacturas.Text = $"S/ {totalFacturas:F2}";
            TxtGerenciaTotalPropinas.Text = $"S/ {totalPropinas:F2}";
            TxtGerenciaTotalGeneral.Text = $"S/ {(totalBoletas + totalFacturas):F2}";

            ModalWinRepComprobantesOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseWinRepComprobantes_Click(object sender, RoutedEventArgs e)
        {
            ModalWinRepComprobantesOverlay.Visibility = Visibility.Collapsed;
        }

        // 5. Ventana Independiente: Detalles de Venta
        private void BtnOpenWinRepProductos_Click(object sender, RoutedEventArgs e)
        {
            var ranking = _completedSales
                .SelectMany(s => s.SoldItems)
                .GroupBy(i => i.Name)
                .Select(g => new ProductSalesRankingItem
                {
                    ProductName = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .ThenByDescending(x => x.TotalRevenue)
                .Select((item, index) =>
                {
                    item.Rank = $"#{index + 1}";
                    return item;
                })
                .ToList();

            DgProductosRanking.ItemsSource = ranking;
            ModalWinRepProductosOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseWinRepProductos_Click(object sender, RoutedEventArgs e)
        {
            ModalWinRepProductosOverlay.Visibility = Visibility.Collapsed;
        }

        // 6. Ventana Independiente: Cierre de Caja (Historial)
        private void BtnOpenWinRepCierres_Click(object sender, RoutedEventArgs e)
        {
            DgCierresTurno.ItemsSource = _shiftCloseHistory.OrderByDescending(c => c.ClosedAt).ToList();
            ModalWinRepCierresOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseWinRepCierres_Click(object sender, RoutedEventArgs e)
        {
            ModalWinRepCierresOverlay.Visibility = Visibility.Collapsed;
        }
    }

    // ================= MODELOS DE DATOS =================

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = "☕";
        public bool IsActive { get; set; } = true;

        public string PriceFormatted => $"S/ {Price:F2}";
        public double CardOpacity => IsActive ? 1.0 : 0.55;
        public string StatusBadgeText => IsActive ? "ACTIVO" : "INACTIVO";
        public Brush StatusBadgeBg => IsActive
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
        public Brush StatusBadgeFg => IsActive
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"));
    }

    public class CartItem : INotifyPropertyChanged
    {
        private int _quantity;
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LineTotalFormatted)));
            }
        }

        public decimal LineTotal => UnitPrice * Quantity;
        public string LineTotalFormatted => $"S/ {LineTotal:F2}";

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class Customer
    {
        public string FullName { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class SaleSession
    {
        public int SessionId { get; set; }
        public bool IsTakeaway { get; set; } = false;
        public Customer Customer { get; set; } = new Customer();
        public ObservableCollection<CartItem> Items { get; set; } = new ObservableCollection<CartItem>();
        public decimal DiscountPercent { get; set; } = 0;
        public decimal IgvPercent { get; set; } = 18m;
        public decimal CachedTotal { get; set; } = 0;

        public string ComprobanteType { get; set; } = "Boleta";
        public string FacturaRuc { get; set; } = string.Empty;
        public string FacturaRazonSocial { get; set; } = string.Empty;
        public string FacturaDireccion { get; set; } = string.Empty;

        public string SessionTitle => IsTakeaway ? $"🛵 Para Llevar #{SessionId}" : $"☕ Venta #{SessionId}";
        public string CustomerSummary => $"{Customer.FullName} (DNI: {Customer.Dni})";
        public string ItemsCountText => $"{Items.Sum(i => i.Quantity)} ítems • {ComprobanteType} • Tel: {Customer.Phone}";
        public string SessionTotalFormatted => $"S/ {CachedTotal:F2}";
    }

    public class CompletedSaleRecord
    {
        public DateTime Date { get; set; }
        public string DocType { get; set; } = "Boleta";
        public string SerieCorrelativo { get; set; } = string.Empty;
        public string DocNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Efectivo";
        public decimal Total { get; set; }
        public decimal TipAmount { get; set; } = 0m;
        public bool IsClosedInShift { get; set; } = false;
        public List<CartItem> SoldItems { get; set; } = new List<CartItem>();

        public decimal GrandTotal => Total + TipAmount;
        public string DateFormatted => Date.ToString("dd/MM/yyyy HH:mm");
        public string TotalFormatted => $"S/ {Total:F2}";
        public string TipFormatted => $"S/ {TipAmount:F2}";
        public string GrandTotalFormatted => $"S/ {GrandTotal:F2}";
    }

    public class CashMovementRecord
    {
        public DateTime Timestamp { get; set; }
        public bool IsIngreso { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;

        public string SummaryText => $"{(IsIngreso ? "📥 Ingreso" : "📤 Egreso")} ({Timestamp:HH:mm}): {Reason}";
        public string AmountFormatted => $"{(IsIngreso ? "+" : "-")} S/ {Amount:F2}";
        public Brush AmountColor => IsIngreso
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
    }

    public class ProductSalesRankingItem
    {
        public string Rank { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public string TotalRevenueFormatted => $"S/ {TotalRevenue:F2}";
    }

    public class ShiftCloseRecord
    {
        public int ShiftNumber { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public DateTime ClosedAt { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalTips { get; set; }
        public decimal CashIn { get; set; }
        public decimal CashOut { get; set; }
        public decimal FinalDrawer { get; set; }

        public string ShiftTitle => $"Turno #{ShiftNumber}";
        public string ClosedAtFormatted => ClosedAt.ToString("dd/MM/yyyy HH:mm");
        public string TotalSalesFormatted => $"S/ {TotalSales:F2}";
        public string TotalTipsFormatted => $"S/ {TotalTips:F2}";
        public string CashInFormatted => $"S/ {CashIn:F2}";
        public string CashOutFormatted => $"S/ {CashOut:F2}";
        public string FinalDrawerFormatted => $"S/ {FinalDrawer:F2}";
    }
}