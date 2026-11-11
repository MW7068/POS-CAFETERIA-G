
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PosCafeteria
{
    public partial class MainWindow : Window
    {
        private bool mobileMenuOpen = false;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += Window_Loaded;
        }

        // INICIALIZAR LA VENTANA
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResponsiveLayout();

            ClockText.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        }

        // ADAPTAR LA INTERFAZ AL TAMAÑO DE PANTALLA
        private void Window_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            ResponsiveLayout();
        }

        private void ResponsiveLayout()
        {
            double width = ActualWidth;

            if (width < 760)
            {
                SidebarColumn.Width = new GridLength(0);
                MenuButton.Visibility = Visibility.Visible;
                mobileMenuOpen = false;
            }
            else
            {
                SidebarColumn.Width = new GridLength(230);
                MenuButton.Visibility = Visibility.Collapsed;
                mobileMenuOpen = false;
            }

            // TARJETAS DE INDICADORES
            if (width < 760)
            {
                StatsGrid.Columns = 2;
            }
            else
            {
                StatsGrid.Columns = 4;
            }

            // ACCESOS RAPIDOS
            if (width < 500)
            {
                ActionsGrid.Columns = 1;
            }
            else
            {
                ActionsGrid.Columns = 3;
            }
        }

        // ABRIR Y CERRAR MENU EN PANTALLAS PEQUEÑAS
        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            if (mobileMenuOpen)
            {
                SidebarColumn.Width = new GridLength(0);
                mobileMenuOpen = false;
            }
            else
            {
                SidebarColumn.Width = new GridLength(210);
                mobileMenuOpen = true;
            }
        }

        // NAVEGACION ENTRE SECCIONES
        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string section)
            {
                PageTitle.Text = section;

                switch (section)
                {
                    case "Inicio":

                        WelcomeTitle.Text = "¡Bienvenido al POS Cafetería!";

                        PageDescription.Text =
                            "Administra tu cafetería de manera rápida y sencilla.";

                        SalesMessage.Text =
                            "Todavía no hay ventas registradas.";

                        break;

                    case "Ventas":

                        WelcomeTitle.Text = "Gestión de ventas";

                        PageDescription.Text =
                            "Aquí podrás registrar y administrar las ventas de tu cafetería.";

                        SalesMessage.Text =
                            "Módulo de ventas preparado para implementar el registro de operaciones.";

                        break;

                    case "Productos":

                        WelcomeTitle.Text = "Gestión de productos";

                        PageDescription.Text =
                            "Administra el catálogo, los precios y el stock de tus productos.";

                        SalesMessage.Text =
                            "Módulo de productos preparado para implementar el inventario.";

                        break;

                    case "Clientes":

                        WelcomeTitle.Text = "Gestión de clientes";

                        PageDescription.Text =
                            "Registra y consulta la información de tus clientes.";

                        SalesMessage.Text =
                            "Módulo de clientes preparado para implementar el registro de clientes.";

                        break;

                    case "Reportes":

                        WelcomeTitle.Text = "Reportes del sistema";

                        PageDescription.Text =
                            "Consulta los resultados y movimientos de tu cafetería.";

                        SalesMessage.Text =
                            "Módulo de reportes preparado para implementar estadísticas.";

                        break;

                    case "Configuración":

                        WelcomeTitle.Text = "Configuración del sistema";

                        PageDescription.Text =
                            "Administra las preferencias de tu aplicación POS.";

                        SalesMessage.Text =
                            "Configuraciones del sistema.";

                        break;
                }

                // CERRAR MENU MOVIL DESPUES DE SELECCIONAR
                if (ActualWidth < 760)
                {
                    SidebarColumn.Width = new GridLength(0);
                    mobileMenuOpen = false;
                }
            }
        }

        // CERRAR SESION
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "¿Deseas cerrar la aplicación?",
                "Cerrar sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
