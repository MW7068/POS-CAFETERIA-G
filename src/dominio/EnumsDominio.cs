namespace PosCafeteria.Dominio;

public enum RolUsuario { Administrador = 1, Cajero = 2 }
public enum TipoMovimientoStock { EntradaProducto = 1, SalidaProducto = 2 }
public enum TipoDocumentoCliente { Ninguno = 0, Dni = 1, Ruc = 2 }
public enum EstadoVenta { Borrador = 1, Confirmada = 2, Anulada = 3 }
public enum ModalidadVenta { Local = 1, ParaLlevar = 2 }
public enum MetodoPago { Efectivo = 1, YapePlin = 2, Tarjeta = 3, SinCobroAutorizado = 4 }
public enum EstadoCaja { Abierta = 1, Cerrada = 2 }
public enum TipoMovimientoCaja { Ingreso = 1, Egreso = 2 }
public enum OrigenMovimientoCaja { Venta = 1, Propina = 2, IngresoManual = 3, Gasto = 4, Retiro = 5, Reembolso = 6 }