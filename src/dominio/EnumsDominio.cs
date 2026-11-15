namespace PosCafeteria.Dominio;

public enum RolUsuario
{
    Cajero = 1,
    Administrador = 2
}

public enum TipoMovimientoStock
{
    Apertura = 1,
    Reposicion = 2,
    Venta = 3,
    Ajuste = 4,
    AnulacionVenta = 5
}

public enum EstadoVenta
{
    Borrador = 1,
    Confirmada = 2,
    Anulada = 3,
    BorradorCancelado = 4
}

public enum ModalidadAtencion
{
    Local = 1,
    ParaLlevar = 2
}

public enum MetodoPago
{
    Efectivo = 1,
    YapePlin = 2,
    Tarjeta = 3,
    SinCobroAutorizado = 4
}

public enum TipoDocumentoCliente
{
    Ninguno = 0,
    Dni = 1,
    Ruc = 2
}

public enum EstadoTurno
{
    Abierto = 1,
    Cerrado = 2
}

public enum TipoMovimientoCaja
{
    IngresoManual = 1,
    EgresoManual = 2,
    VentaEfectivo = 3,
    PropinaEfectivo = 4,
    ReembolsoEfectivo = 5,
    ReembolsoDigital = 6
}