
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PosCafeteria.Dominio;

namespace PosCafeteria.Presentacion;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ProductoCatalogo> _productos = new();
    private readonly ObservableCollection<ProductoCatalogo> _productosFiltrados = new();
    private readonly ObservableCollection<DetalleVentaVista> _carrito = new();

    private Venta _ventaActual = new();

    private const decimal TasaIGV = 0.18m;

    private bool _inicializando = true;
    private bool _confirmando;

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

        _inicializando = false;

        FiltrarProductos();
        ActualizarResumen();
    }

    // CATÁLOGO PROVISIONAL

    private void CargarProductos()
    {
        AgregarProductoCatalogo(1, "Café americano", 22.10m, "Café", "☕");
        AgregarProductoCatalogo(2, "Café espresso", 18.00m, "Café", "☕");
        AgregarProductoCatalogo(3, "Capuchino", 25.00m, "Café", "🥤");
        AgregarProductoCatalogo(4, "Latte", 24.00m, "Café", "☕");
        AgregarProductoCatalogo(5, "Chocolate caliente", 20.00m, "Bebidas", "🍫");
        AgregarProductoCatalogo(6, "Limonada", 12.00m, "Bebidas", "🍋");
        AgregarProductoCatalogo(7, "Sándwich clásico", 18.50m, "Snacks", "🥪");
        AgregarProductoCatalogo(8, "Cheesecake", 16.00m, "Postres", "🍰");
    }

    private void AgregarProductoCatalogo(
        int id,
        string nombre,
        decimal precio,
        string categoria,
        string icono)
    {
        var producto = new Producto
        {
            Id = id,
            Nombre = nombre,
            Precio = precio,
            Stock = 100
        };

        _productos.Add(
            new ProductoCatalogo(producto, categoria, icono));
    }

    // BÚSQUEDA Y FILTRADO

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
             p.Nombre.Contains(
                 busqueda,
                 StringComparison.OrdinalIgnoreCase)));

        foreach (ProductoCatalogo producto in resultado)
        {
            _productosFiltrados.Add(producto);
        }
    }

    private void Buscar_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        FiltrarProductos();
    }

    private void Categoria_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        FiltrarProductos();
    }

    // AGREGAR PRODUCTOS

    private void AgregarProducto_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button boton ||
            boton.Tag is not ProductoCatalogo producto)
        {
            return;
        }

        try
        {
            _ventaActual.AgregarProducto(producto.Dominio, 1);

            RefrescarCarrito();
            ActualizarResumen();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(
                ex.Message,
                "No se pudo agregar el producto",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    // ACTUALIZAR CARRITO DESDE EL DOMINIO

    private void RefrescarCarrito()
    {
        _carrito.Clear();

        foreach (DetalleVenta detalle in _ventaActual.Detalles)
        {
            ProductoCatalogo? producto = _productos.FirstOrDefault(
                p => p.Dominio.Id == detalle.ProductoId);

            if (producto == null)
                continue;

            _carrito.Add(
                new DetalleVentaVista(producto, detalle));
        }
    }

    // RECONSTRUIR LA VENTA

    private void ReconstruirVenta(
        DetalleVentaVista? productoModificado = null,
        int? nuevaCantidad = null,
        int? productoEliminar = null)
    {
        var nuevaVenta = new Venta();

        foreach (DetalleVentaVista detalle in _carrito)
        {
            int productoId = detalle.Producto.Dominio.Id;

            if (productoEliminar == productoId)
                continue;

            int cantidad = detalle.Cantidad;

            if (productoModificado != null &&
                productoModificado.Producto.Dominio.Id == productoId &&
                nuevaCantidad.HasValue)
            {
                cantidad = nuevaCantidad.Value;
            }

            nuevaVenta.AgregarProducto(
                detalle.Producto.Dominio,
                cantidad);
        }

        _ventaActual = nuevaVenta;

        RefrescarCarrito();
        ActualizarResumen();
    }

    // AUMENTAR CANTIDAD

    private void AumentarCantidad_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button boton ||
            boton.Tag is not DetalleVentaVista detalle)
        {
            return;
        }

        ReconstruirVenta(
            detalle,
            detalle.Cantidad + 1);
    }

    // DISMINUIR CANTIDAD

    private void DisminuirCantidad_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button boton ||
            boton.Tag is not DetalleVentaVista detalle)
        {
            return;
        }

        if (detalle.Cantidad > 1)
        {
            ReconstruirVenta(
                detalle,
                detalle.Cantidad - 1);
        }
        else
        {
            EliminarDetalle(detalle);
        }
    }

    // VALIDACIÓN DE CANTIDADES

    private void Cantidad_PreviewTextInput(
        object sender,
        TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void Cantidad_Pasting(
        object sender,
        DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            e.CancelCommand();
            return;
        }

        string texto = e.DataObject.GetData(
            DataFormats.UnicodeText)?.ToString() ?? "";

        if (!texto.All(char.IsDigit))
        {
            e.CancelCommand();
        }
    }

    private void Cantidad_LostFocus(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            textBox.DataContext is not DetalleVentaVista detalle)
        {
            return;
        }

        string texto = textBox.Text.Trim();

        bool cantidadValida =
            int.TryParse(
                texto,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int cantidad) &&
            cantidad > 0;

        if (!cantidadValida)
        {
            MessageBox.Show(
                "Ingresa una cantidad entera mayor que cero.",
                "Cantidad no válida",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            detalle.RestaurarCantidadTexto();

            textBox.Text = detalle.CantidadTexto;
            return;
        }

        if (cantidad != detalle.Cantidad)
        {
            ReconstruirVenta(detalle, cantidad);
        }
        else
        {
            detalle.RestaurarCantidadTexto();
            textBox.Text = detalle.CantidadTexto;
        }
    }

    // ELIMINAR PRODUCTOS

    private void EliminarProducto_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is Button boton &&
            boton.Tag is DetalleVentaVista detalle)
        {
            EliminarDetalle(detalle);
        }
    }

    private void EliminarDetalle(DetalleVentaVista detalle)
    {
        ReconstruirVenta(
            productoEliminar: detalle.Producto.Dominio.Id);
    }

    // CÁLCULO DE SUBTOTAL, IGV Y TOTAL

    private (decimal Subtotal, decimal IGV, decimal Total)
        CalcularTotales()
    {
        decimal subtotal = _ventaActual.Total;

        decimal igv = Math.Round(
            subtotal * TasaIGV,
            2,
            MidpointRounding.AwayFromZero);

        decimal total = subtotal + igv;

        return (subtotal, igv, total);
    }

    // ACTUALIZAR RESUMEN

    private void ActualizarResumen()
    {
        if (txtTotal == null ||
            txtCantidadProductos == null ||
            btnConfirmar == null)
        {
            return;
        }

        var totales = CalcularTotales();

        txtSubtotal.Text = FormatearMoneda(totales.Subtotal);
        txtIGV.Text = FormatearMoneda(totales.IGV);
        txtTotal.Text = FormatearMoneda(totales.Total);

        int cantidad = _ventaActual.Detalles.Sum(
            d => d.Cantidad);

        txtCantidadProductos.Text =
            $"{cantidad} {(cantidad == 1 ? "producto" : "productos")}";

        btnConfirmar.IsEnabled =
            _ventaActual.Detalles.Count > 0 && !_confirmando;
    }

    // FORMATO DE MONEDA PERUANA

    private static string FormatearMoneda(decimal importe)
    {
        return "S/ " + importe.ToString(
            "N2",
            CultureInfo.GetCultureInfo("es-PE"));
    }

    // CANCELAR VENTA

    private void Cancelar_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_ventaActual.Detalles.Count == 0)
            return;

        MessageBoxResult respuesta = MessageBox.Show(
            "¿Deseas cancelar y eliminar todos los productos de esta venta?",
            "Cancelar venta",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (respuesta == MessageBoxResult.Yes)
        {
            _ventaActual = new Venta();

            RefrescarCarrito();
            ActualizarResumen();
        }
    }

    // CONFIRMACIÓN DE DEMOSTRACIÓN

    private void Confirmar_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_confirmando)
            return;

        if (_ventaActual.Detalles.Count == 0)
        {
            MessageBox.Show(
                "Agrega al menos un producto antes de continuar.",
                "Venta vacía",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        _confirmando = true;
        btnConfirmar.IsEnabled = false;

        try
        {
            var totales = CalcularTotales();

            string subtotal = FormatearMoneda(totales.Subtotal);
            string igv = FormatearMoneda(totales.IGV);
            string total = FormatearMoneda(totales.Total);

            MessageBox.Show(
                "El detalle de la venta está preparado correctamente.\n\n" +
                $"Subtotal: {subtotal}\n" +
                $"IGV (18 %): {igv}\n" +
                $"TOTAL A PAGAR: {total}\n\n" +
                "Esta es una demostración de la interfaz. " +
                "Todavía no se ha conectado el registro de ventas " +
                "con la base de datos.\n\n" +
                "No se ha guardado ni enviado ninguna venta.",
                "Venta preparada - Prototipo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            _confirmando = false;
            ActualizarResumen();
        }
    }
}

// MODELO DE PRESENTACIÓN PARA PRODUCTOS DEL CATÁLOGO

public class ProductoCatalogo
{
    public Producto Dominio { get; }

    public string Nombre => Dominio.Nombre;

    public decimal Precio => Dominio.Precio;

    public string Categoria { get; }

    public string Icono { get; }

    public ProductoCatalogo(
        Producto dominio,
        string categoria,
        string icono)
    {
        Dominio = dominio;
        Categoria = categoria;
        Icono = icono;
    }
}

// MODELO DE PRESENTACIÓN PARA DETALLE DE VENTA

public class DetalleVentaVista
{
    public ProductoCatalogo Producto { get; }

    public DetalleVenta Dominio { get; }

    public string Nombre => Producto.Nombre;

    public decimal Precio => Dominio.PrecioUnitario;

    public int Cantidad => Dominio.Cantidad;

    public decimal Importe => Cantidad * Precio;

    public string CantidadTexto { get; set; }

    public DetalleVentaVista(
        ProductoCatalogo producto,
        DetalleVenta dominio)
    {
        Producto = producto;
        Dominio = dominio;

        CantidadTexto = Cantidad.ToString(
            CultureInfo.InvariantCulture);
    }

    public void RestaurarCantidadTexto()
    {
        CantidadTexto = Cantidad.ToString(
            CultureInfo.InvariantCulture);
    }
}
