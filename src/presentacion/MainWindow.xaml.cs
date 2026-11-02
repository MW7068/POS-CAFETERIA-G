
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PosCafeteria.Presentacion;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Producto> _productos = new();
    private readonly ObservableCollection<Producto> _productosFiltrados = new();
    private readonly ObservableCollection<DetalleVenta> _carrito = new();

    private bool _inicializando = true;

    private const decimal IgvPredeterminado = 10.5m;

    public MainWindow()
    {
        InitializeComponent();

        CargarProductos();

        itemsProductos.ItemsSource = _productosFiltrados;
        listaCarrito.ItemsSource = _carrito;

        cmbCategoria.Items.Add("Todas las categorías");

        foreach (string categoria in _productos
                     .Select(p => p.Categoria)
                     .Distinct()
                     .OrderBy(c => c))
        {
            cmbCategoria.Items.Add(categoria);
        }

        cmbCategoria.SelectedIndex = 0;
        cmbDescuento.SelectedIndex = 0;

        txtIgv.Text = IgvPredeterminado.ToString(
            CultureInfo.InvariantCulture);

        _inicializando = false;

        FiltrarProductos();
        ActualizarResumen();
    }

    private void CargarProductos()
    {
        // Productos provisionales de demostración.
        // Se reemplazarán por el catálogo real del sistema.

        _productos.Add(new Producto("P001", "Café americano", 22.10m,
            "Café", "☕"));

        _productos.Add(new Producto("P002", "Café espresso", 18.00m,
            "Café", "☕"));

        _productos.Add(new Producto("P003", "Capuchino", 25.00m,
            "Café", "🥤"));

        _productos.Add(new Producto("P004", "Latte", 24.00m,
            "Café", "☕"));

        _productos.Add(new Producto("P005", "Chocolate caliente", 20.00m,
            "Bebidas", "🍫"));

        _productos.Add(new Producto("P006", "Limonada", 12.00m,
            "Bebidas", "🍋"));

        _productos.Add(new Producto("P007", "Sándwich clásico", 18.50m,
            "Snacks", "🥪"));

        _productos.Add(new Producto("P008", "Cheesecake", 16.00m,
            "Postres", "🍰"));
    }

    private void FiltrarProductos()
    {
        if (_inicializando)
            return;

        _productosFiltrados.Clear();

        string busqueda = txtBuscar.Text.Trim();

        string categoria = cmbCategoria.SelectedItem?.ToString()
                           ?? "Todas las categorías";

        var resultado = _productos.Where(p =>
            (categoria == "Todas las categorías" ||
             p.Categoria == categoria) &&
            (string.IsNullOrWhiteSpace(busqueda) ||
             p.Nombre.Contains(busqueda,
                 StringComparison.OrdinalIgnoreCase)));

        foreach (Producto producto in resultado)
        {
            _productosFiltrados.Add(producto);
        }
    }

    private void Buscar_TextChanged(object sender, TextChangedEventArgs e)
    {
        FiltrarProductos();
    }

    private void Categoria_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        FiltrarProductos();
    }

    private void AgregarProducto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button boton ||
            boton.Tag is not Producto producto)
            return;

        DetalleVenta? existente = _carrito.FirstOrDefault(
            d => d.Codigo == producto.Codigo);

        if (existente != null)
        {
            existente.Cantidad++;
        }
        else
        {
            var detalle = new DetalleVenta(producto);
            detalle.PropertyChanged += Detalle_PropertyChanged;
            _carrito.Add(detalle);
        }

        ActualizarResumen();
    }

    private void Detalle_PropertyChanged(
        object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DetalleVenta.Cantidad) ||
            e.PropertyName == nameof(DetalleVenta.Importe))
        {
            ActualizarResumen();
        }
    }

    private void AumentarCantidad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button boton &&
            boton.Tag is DetalleVenta detalle)
        {
            detalle.Cantidad++;
        }
    }

    private void DisminuirCantidad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button boton &&
            boton.Tag is DetalleVenta detalle)
        {
            if (detalle.Cantidad > 1)
                detalle.Cantidad--;
            else
                EliminarDetalle(detalle);
        }
    }

    private void EliminarProducto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button boton &&
            boton.Tag is DetalleVenta detalle)
        {
            EliminarDetalle(detalle);
        }
    }

    private void EliminarDetalle(DetalleVenta detalle)
    {
        detalle.PropertyChanged -= Detalle_PropertyChanged;
        _carrito.Remove(detalle);
        ActualizarResumen();
    }

    private void Configuracion_TextChanged(
        object sender, RoutedEventArgs e)
    {
        if (_inicializando || cmbDescuento == null || txtIgv == null)
            return;

        ActualizarResumen();
    }

    private decimal ObtenerPorcentajeIgv()
    {
        if (txtIgv == null)
            return IgvPredeterminado;

        if (decimal.TryParse(
                txtIgv.Text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal porcentaje) &&
            porcentaje >= 0 &&
            porcentaje <= 100)
        {
            return porcentaje;
        }

        return IgvPredeterminado;
    }

    private decimal ObtenerPorcentajeDescuento()
    {
        if (cmbDescuento == null ||
            cmbDescuento.SelectedItem is not ComboBoxItem item)
        {
            return 0m;
        }

        string texto = item.Content?.ToString() ?? "0";
        string numero = texto.Replace("%", "").Trim();

        if (decimal.TryParse(
                numero,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal porcentaje))
        {
            return porcentaje;
        }

        return 0m;
    }

    private void ActualizarResumen()
    {
        if (_inicializando ||
            txtSubtotal == null ||
            txtImporteDescuento == null ||
            txtBaseImponible == null ||
            txtImporteIgv == null ||
            txtTotal == null ||
            txtIgvLabel == null ||
            txtCantidadProductos == null ||
            btnConfirmar == null ||
            cmbDescuento == null ||
            txtIgv == null)
        {
            return;
        }

        decimal subtotal = _carrito.Sum(d => d.Importe);

        decimal porcentajeDescuento = ObtenerPorcentajeDescuento();
        decimal porcentajeIgv = ObtenerPorcentajeIgv();

        decimal descuento = subtotal * porcentajeDescuento / 100m;
        decimal baseImponible = subtotal - descuento;
        decimal igv = baseImponible * porcentajeIgv / 100m;
        decimal total = baseImponible + igv;

        txtSubtotal.Text = FormatearMoneda(subtotal);
        txtImporteDescuento.Text = FormatearMoneda(descuento);
        txtBaseImponible.Text = FormatearMoneda(baseImponible);
        txtImporteIgv.Text = FormatearMoneda(igv);
        txtTotal.Text = FormatearMoneda(total);

        txtIgvLabel.Text = $"IGV ({porcentajeIgv:N2} %)";

        int cantidad = _carrito.Sum(d => d.Cantidad);

        txtCantidadProductos.Text =
            $"{cantidad} {(cantidad == 1 ? "producto" : "productos")}";

        btnConfirmar.IsEnabled = _carrito.Count > 0;
    }

    private static string FormatearMoneda(decimal importe)
    {
        return importe.ToString(
            "C2",
            CultureInfo.GetCultureInfo("es-PE"));
    }

    private void Cantidad_PreviewTextInput(
        object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void Porcentaje_PreviewTextInput(
        object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(c =>
            char.IsDigit(c) || c == '.' || c == ',');
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        if (_carrito.Count == 0)
            return;

        MessageBoxResult respuesta = MessageBox.Show(
            "¿Deseas cancelar y eliminar todos los productos de esta venta?",
            "Cancelar venta",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (respuesta == MessageBoxResult.Yes)
        {
            foreach (DetalleVenta detalle in _carrito)
            {
                detalle.PropertyChanged -= Detalle_PropertyChanged;
            }

            _carrito.Clear();
            ActualizarResumen();
        }
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        if (_carrito.Count == 0)
        {
            MessageBox.Show(
                "Agrega al menos un producto antes de continuar.",
                "Venta vacía",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (!decimal.TryParse(
                txtIgv.Text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal porcentajeIgv) ||
            porcentajeIgv < 0 ||
            porcentajeIgv > 100)
        {
            MessageBox.Show(
                "Ingresa un porcentaje de IGV válido entre 0 y 100.",
                "IGV no válido",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            txtIgv.Focus();
            return;
        }

        MessageBox.Show(
            "El detalle de la venta está preparado.\n\n" +
            "La confirmación y el registro definitivo están pendientes " +
            "de conectarse al servicio de ventas del proyecto.\n\n" +
            "No se ha registrado ni enviado ninguna venta.",
            "Integración pendiente",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}

public class Producto
{
    public string Codigo { get; }
    public string Nombre { get; }
    public decimal Precio { get; }
    public string Categoria { get; }
    public string Icono { get; }

    public Producto(
        string codigo,
        string nombre,
        decimal precio,
        string categoria,
        string icono)
    {
        Codigo = codigo;
        Nombre = nombre;
        Precio = precio;
        Categoria = categoria;
        Icono = icono;
    }
}

public class DetalleVenta : INotifyPropertyChanged
{
    private int _cantidad = 1;

    public string Codigo { get; }
    public string Nombre { get; }
    public decimal Precio { get; }

    public int Cantidad
    {
        get => _cantidad;
        set
        {
            int nuevaCantidad = Math.Max(1, value);

            if (_cantidad == nuevaCantidad)
                return;

            _cantidad = nuevaCantidad;

            OnPropertyChanged(nameof(Cantidad));
            OnPropertyChanged(nameof(Importe));
        }
    }

    public decimal Importe => Precio * Cantidad;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DetalleVenta(Producto producto)
    {
        Codigo = producto.Codigo;
        Nombre = producto.Nombre;
        Precio = producto.Precio;
    }

    private void OnPropertyChanged(string propiedad)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propiedad));
    }
}
