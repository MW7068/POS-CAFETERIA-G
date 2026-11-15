namespace PosCafeteria.Dominio;

public enum RolUsuario { Administrador = 1, Cajero = 2 }
public enum EstadoVenta { Borrador = 1, Confirmada = 2, Anulada = 3 }
public enum TipoMovimientoStock { Ingreso = 1, SalidaVenta = 2, Ajuste = 3, ReversoAnulacion = 4 }
public enum TipoDocumentoCliente { Ninguno = 0, Dni = 1, Ruc = 2 }
public enum MetodoPago { Efectivo = 1, YapePlin = 2, Tarjeta = 3, SinCobroAutorizado = 4 }
public enum EstadoTurno { Abierto = 1, Cerrado = 2 }
public enum TipoMovimientoCaja { VentaEfectivo = 1, IngresoManual = 2, EgresoManual = 3, ReembolsoEfectivo = 4 }