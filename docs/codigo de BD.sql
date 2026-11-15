
create database if not exists `apuqhawa_cafemamaoli` default character set utf8mb4 collate utf8mb4_unicode_ci;

-- 1. CATEGORIA

CREATE TABLE `Categoria` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(100) NOT NULL,
    `Activa` BOOLEAN NOT NULL DEFAULT TRUE,

    PRIMARY KEY (`Id`),

    CONSTRAINT `CK_Categoria_Nombre`
        CHECK (CHAR_LENGTH(TRIM(`Nombre`)) > 0),

    CONSTRAINT `CK_Categoria_Activa`
        CHECK (`Activa` IN (0, 1))
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 2. USUARIO

CREATE TABLE `Usuario` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `NombreUsuario` VARCHAR(100) NOT NULL,
    `Password` VARCHAR(255) COLLATE utf8mb4_bin NOT NULL,
    `Rol` ENUM('Administrador', 'Cajero') NOT NULL,
    `Activo` BOOLEAN NOT NULL DEFAULT TRUE,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Usuario_NombreUsuario` (`NombreUsuario`),

    CONSTRAINT `CK_Usuario_Nombre`
        CHECK (CHAR_LENGTH(TRIM(`NombreUsuario`)) > 0),

    CONSTRAINT `CK_Usuario_Password`
        CHECK (CHAR_LENGTH(`Password`) > 0),

    CONSTRAINT `CK_Usuario_Rol`
        CHECK (`Rol` IN ('Administrador', 'Cajero')),

    CONSTRAINT `CK_Usuario_Activo`
        CHECK (`Activo` IN (0, 1))
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 3. NEGOCIO

CREATE TABLE `negocio` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `NombreNegocio` VARCHAR(150) NOT NULL,
    `Ruc` VARCHAR(11) NULL,
    `TasaImpuesto` DECIMAL(5,2) NOT NULL,

    PRIMARY KEY (`Id`),

    CONSTRAINT `CK_negocio_Nombre`
        CHECK (CHAR_LENGTH(TRIM(`NombreNegocio`)) > 0),

    CONSTRAINT `CK_negocio_TasaImpuesto`
        CHECK (`TasaImpuesto` BETWEEN 0.00 AND 100.00)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 4. CLIENTE

CREATE TABLE `Cliente` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(150) NOT NULL,
    `TipoDocumento` ENUM('Ninguno', 'Dni', 'Ruc')
        NOT NULL DEFAULT 'Ninguno',
    `NumeroDocumento` VARCHAR(20) NOT NULL,
    `Telefono` VARCHAR(25) NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Cliente_Tipo_NumeroDocumento`
        (`TipoDocumento`, `NumeroDocumento`),

    CONSTRAINT `CK_Cliente_Nombre`
        CHECK (CHAR_LENGTH(TRIM(`Nombre`)) > 0),

    CONSTRAINT `CK_Cliente_TipoDocumento`
        CHECK (`TipoDocumento` IN ('Ninguno', 'Dni', 'Ruc')),

    CONSTRAINT `CK_Cliente_DocumentoRequerido`
        CHECK (
            `TipoDocumento` = 'Ninguno'
            OR CHAR_LENGTH(TRIM(`NumeroDocumento`)) > 0
        )
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 5. PRODUCTO

CREATE TABLE `Producto` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(150) NOT NULL,
    `Precio` DECIMAL(18,2) NOT NULL,
    `Stock` INT NOT NULL DEFAULT 0,
    `StockMinimo` INT NOT NULL DEFAULT 0,
    `CategoriaId` INT NULL,
    `Activo` BOOLEAN NOT NULL DEFAULT TRUE,

    PRIMARY KEY (`Id`),

    KEY `IX_Producto_CategoriaId` (`CategoriaId`),

    CONSTRAINT `FK_Producto_Categoria`
        FOREIGN KEY (`CategoriaId`)
        REFERENCES `Categoria` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_Producto_Nombre`
        CHECK (CHAR_LENGTH(TRIM(`Nombre`)) > 0),

    CONSTRAINT `CK_Producto_Precio`
        CHECK (`Precio` >= 0.00),

    CONSTRAINT `CK_Producto_Stock`
        CHECK (`Stock` >= 0),

    CONSTRAINT `CK_Producto_StockMinimo`
        CHECK (`StockMinimo` >= 0),

    CONSTRAINT `CK_Producto_Activo`
        CHECK (`Activo` IN (0, 1))
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 6. CAJA
-- CajaAbiertaUnica se calcula automáticamente.
-- Su índice UNIQUE permite una sola caja abierta.

CREATE TABLE `Caja` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `UsuarioId` INT NOT NULL,
    `FechaAperturaUtc` DATETIME(6)
        NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    `FechaCierreUtc` DATETIME(6) NULL,
    `MontoInicial` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `MontoContado` DECIMAL(18,2) NULL,
    `TotalIngresosEfectivo` DECIMAL(18,2) NULL,
    `TotalEgresosEfectivo` DECIMAL(18,2) NULL,
    `TotalEsperado` DECIMAL(18,2) NULL,
    `Diferencia` DECIMAL(18,2) NULL,
    `Estado` ENUM('Abierta', 'Cerrada')
        NOT NULL DEFAULT 'Abierta',
    `Observacion` TEXT NULL,

    `CajaAbiertaUnica` TINYINT GENERATED ALWAYS AS (
        CASE
            WHEN `Estado` = 'Abierta' THEN 1
            ELSE NULL
        END
    ) STORED,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Caja_UnaAbierta` (`CajaAbiertaUnica`),

    KEY `IX_Caja_Usuario_Fecha`
        (`UsuarioId`, `FechaAperturaUtc`),

    CONSTRAINT `FK_Caja_Usuario`
        FOREIGN KEY (`UsuarioId`)
        REFERENCES `Usuario` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_Caja_Estado`
        CHECK (`Estado` IN ('Abierta', 'Cerrada')),

    CONSTRAINT `CK_Caja_Montos`
        CHECK (
            `MontoInicial` >= 0.00
            AND (`MontoContado` IS NULL OR `MontoContado` >= 0.00)
            AND (
                `TotalIngresosEfectivo` IS NULL
                OR `TotalIngresosEfectivo` >= 0.00
            )
            AND (
                `TotalEgresosEfectivo` IS NULL
                OR `TotalEgresosEfectivo` >= 0.00
            )
            AND (`TotalEsperado` IS NULL OR `TotalEsperado` >= 0.00)
        ),

    CONSTRAINT `CK_Caja_CierreCompleto`
        CHECK (
            (
                `Estado` = 'Abierta'
                AND `FechaCierreUtc` IS NULL
                AND `MontoContado` IS NULL
                AND `TotalIngresosEfectivo` IS NULL
                AND `TotalEgresosEfectivo` IS NULL
                AND `TotalEsperado` IS NULL
                AND `Diferencia` IS NULL
            )
            OR
            (
                `Estado` = 'Cerrada'
                AND `FechaCierreUtc` IS NOT NULL
                AND `FechaCierreUtc` >= `FechaAperturaUtc`
                AND `MontoContado` IS NOT NULL
                AND `TotalIngresosEfectivo` IS NOT NULL
                AND `TotalEgresosEfectivo` IS NOT NULL
                AND `TotalEsperado` IS NOT NULL
                AND `Diferencia` IS NOT NULL
            )
        ),

    CONSTRAINT `CK_Caja_TotalEsperado`
        CHECK (
            `TotalEsperado` IS NULL
            OR `TotalEsperado` =
                `MontoInicial`
                + `TotalIngresosEfectivo`
                - `TotalEgresosEfectivo`
        ),

    CONSTRAINT `CK_Caja_Diferencia`
        CHECK (
            `Diferencia` IS NULL
            OR `Diferencia` = `MontoContado` - `TotalEsperado`
        )
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 7. VENTA

CREATE TABLE `Venta` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `IdentificadorOperacion` VARCHAR(64)
        COLLATE utf8mb4_bin NULL,
    `Fecha` DATETIME(6)
        NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    `UsuarioId` INT NOT NULL,
    `Estado` ENUM('Borrador', 'Confirmada', 'Anulada')
        NOT NULL DEFAULT 'Borrador',
    `Modalidad` ENUM('Local', 'ParaLlevar')
        NOT NULL DEFAULT 'Local',
    `NumeroMesa` INT NULL,
    `CajaId` INT NULL,
    `ClienteId` INT NULL,
    `Descuento` DECIMAL(18,2) NOT NULL DEFAULT 0.00,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Venta_IdentificadorOperacion`
        (`IdentificadorOperacion`),

    KEY `IX_Venta_UsuarioId` (`UsuarioId`),
    KEY `IX_Venta_Caja_Fecha` (`CajaId`, `Fecha`),
    KEY `IX_Venta_ClienteId` (`ClienteId`),
    KEY `IX_Venta_Estado_Fecha` (`Estado`, `Fecha`),

    CONSTRAINT `FK_Venta_Usuario`
        FOREIGN KEY (`UsuarioId`)
        REFERENCES `Usuario` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_Venta_Caja`
        FOREIGN KEY (`CajaId`)
        REFERENCES `Caja` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_Venta_Cliente`
        FOREIGN KEY (`ClienteId`)
        REFERENCES `Cliente` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_Venta_Estado`
        CHECK (`Estado` IN ('Borrador', 'Confirmada', 'Anulada')),

    CONSTRAINT `CK_Venta_Modalidad`
        CHECK (`Modalidad` IN ('Local', 'ParaLlevar')),

    CONSTRAINT `CK_Venta_Descuento`
        CHECK (`Descuento` >= 0.00),

    CONSTRAINT `CK_Venta_NumeroMesa`
        CHECK (`NumeroMesa` IS NULL OR `NumeroMesa` > 0),

    CONSTRAINT `CK_Venta_OperacionNoVacia`
        CHECK (
            `IdentificadorOperacion` IS NULL
            OR CHAR_LENGTH(TRIM(`IdentificadorOperacion`)) > 0
        ),

    CONSTRAINT `CK_Venta_Confirmada`
        CHECK (
            `Estado` <> 'Confirmada'
            OR (
                `CajaId` IS NOT NULL
                AND `IdentificadorOperacion` IS NOT NULL
            )
        )
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 8. DETALLEVENTA

CREATE TABLE `DetalleVenta` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `VentaId` INT NOT NULL,
    `ProductoId` INT NOT NULL,
    `NombreProducto` VARCHAR(150) NOT NULL,
    `Cantidad` INT NOT NULL,
    `PrecioUnitario` DECIMAL(18,2) NOT NULL,
    `Observacion` VARCHAR(500) NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_DetalleVenta_VentaId` (`VentaId`),
    KEY `IX_DetalleVenta_ProductoId` (`ProductoId`),

    CONSTRAINT `FK_DetalleVenta_Venta`
        FOREIGN KEY (`VentaId`)
        REFERENCES `Venta` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_DetalleVenta_Producto`
        FOREIGN KEY (`ProductoId`)
        REFERENCES `Producto` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_DetalleVenta_Nombre`
        CHECK (CHAR_LENGTH(TRIM(`NombreProducto`)) > 0),

    CONSTRAINT `CK_DetalleVenta_Cantidad`
        CHECK (`Cantidad` > 0),

    CONSTRAINT `CK_DetalleVenta_Precio`
        CHECK (`PrecioUnitario` >= 0.00)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 9. MOVIMIENTOSTOCK

CREATE TABLE `MovimientoStock` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductoId` INT NOT NULL,
    `UsuarioId` INT NOT NULL,
    `VentaId` INT NULL,
    `Tipo` ENUM('EntradaProducto', 'SalidaProducto') NOT NULL,
    `Cantidad` INT NOT NULL,
    `Motivo` VARCHAR(500) NOT NULL,
    `FechaUtc` DATETIME(6)
        NOT NULL DEFAULT (UTC_TIMESTAMP(6)),

    PRIMARY KEY (`Id`),

    KEY `IX_MovimientoStock_Producto_Fecha`
        (`ProductoId`, `FechaUtc`),

    KEY `IX_MovimientoStock_UsuarioId` (`UsuarioId`),
    KEY `IX_MovimientoStock_VentaId` (`VentaId`),

    CONSTRAINT `FK_MovimientoStock_Producto`
        FOREIGN KEY (`ProductoId`)
        REFERENCES `Producto` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_MovimientoStock_Usuario`
        FOREIGN KEY (`UsuarioId`)
        REFERENCES `Usuario` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_MovimientoStock_Venta`
        FOREIGN KEY (`VentaId`)
        REFERENCES `Venta` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_MovimientoStock_Tipo`
        CHECK (`Tipo` IN ('EntradaProducto', 'SalidaProducto')),

    CONSTRAINT `CK_MovimientoStock_Cantidad`
        CHECK (`Cantidad` > 0),

    CONSTRAINT `CK_MovimientoStock_Motivo`
        CHECK (CHAR_LENGTH(TRIM(`Motivo`)) > 0)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 10. PAGO

CREATE TABLE `Pago` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `VentaId` INT NOT NULL,
    `CajaId` INT NOT NULL,
    `UsuarioId` INT NOT NULL,

    `Metodo` ENUM(
        'Efectivo',
        'YapePlin',
        'Tarjeta',
        'SinCobroAutorizado'
    ) NOT NULL,

    `MontoConsumo` DECIMAL(18,2) NOT NULL,
    `MontoPropina` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `MontoTotal` DECIMAL(18,2) NOT NULL,
    `MontoRecibido` DECIMAL(18,2) NOT NULL,
    `ConfirmacionManualDigital` BOOLEAN NOT NULL DEFAULT FALSE,
    `ReferenciaOperacionDigital` VARCHAR(100) NULL,
    `FechaPagoUtc` DATETIME(6)
        NOT NULL DEFAULT (UTC_TIMESTAMP(6)),

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_Pago_VentaId` (`VentaId`),

    KEY `IX_Pago_Caja_Fecha` (`CajaId`, `FechaPagoUtc`),
    KEY `IX_Pago_UsuarioId` (`UsuarioId`),

    CONSTRAINT `FK_Pago_Venta`
        FOREIGN KEY (`VentaId`)
        REFERENCES `Venta` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_Pago_Caja`
        FOREIGN KEY (`CajaId`)
        REFERENCES `Caja` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_Pago_Usuario`
        FOREIGN KEY (`UsuarioId`)
        REFERENCES `Usuario` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_Pago_Metodo`
        CHECK (
            `Metodo` IN (
                'Efectivo',
                'YapePlin',
                'Tarjeta',
                'SinCobroAutorizado'
            )
        ),

    CONSTRAINT `CK_Pago_Montos`
        CHECK (
            `MontoConsumo` >= 0.00
            AND `MontoPropina` >= 0.00
            AND `MontoTotal` >= 0.00
            AND `MontoRecibido` >= 0.00
        ),

    CONSTRAINT `CK_Pago_Total`
        CHECK (`MontoTotal` = `MontoConsumo` + `MontoPropina`),

    CONSTRAINT `CK_Pago_Recibido`
        CHECK (
            `Metodo` = 'SinCobroAutorizado'
            OR `MontoRecibido` >= `MontoTotal`
        ),

    CONSTRAINT `CK_Pago_ConfirmacionBoolean`
        CHECK (`ConfirmacionManualDigital` IN (0, 1)),

    CONSTRAINT `CK_Pago_ConfirmacionDigital`
        CHECK (
            `Metodo` NOT IN ('YapePlin', 'Tarjeta')
            OR `ConfirmacionManualDigital` = TRUE
        )
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 11. COMPROBANTE

CREATE TABLE `Comprobante` (
    `Idcomprobante` INT NOT NULL AUTO_INCREMENT,
    `VentaId` INT NOT NULL,
    `Serie` VARCHAR(10) NOT NULL,
    `Numero` INT NOT NULL,
    `ClienteNombre` VARCHAR(150) NOT NULL,
    `Total` DECIMAL(18,2) NOT NULL,
    `FechaEmisionUtc` DATETIME(6)
        NOT NULL DEFAULT (UTC_TIMESTAMP(6)),

    PRIMARY KEY (`Idcomprobante`),

    UNIQUE KEY `UQ_Comprobante_VentaId` (`VentaId`),
    UNIQUE KEY `UQ_Comprobante_Serie_Numero` (`Serie`, `Numero`),

    CONSTRAINT `FK_Comprobante_Venta`
        FOREIGN KEY (`VentaId`)
        REFERENCES `Venta` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_Comprobante_Serie`
        CHECK (CHAR_LENGTH(TRIM(`Serie`)) > 0),

    CONSTRAINT `CK_Comprobante_Numero`
        CHECK (`Numero` > 0),

    CONSTRAINT `CK_Comprobante_ClienteNombre`
        CHECK (CHAR_LENGTH(TRIM(`ClienteNombre`)) > 0),

    CONSTRAINT `CK_Comprobante_Total`
        CHECK (`Total` >= 0.00)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;


-- 12. CAJAMOVIMIENTO

CREATE TABLE `CajaMovimiento` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `CajaId` INT NOT NULL,
    `UsuarioId` INT NOT NULL,
    `VentaId` INT NULL,
    `Tipo` ENUM('Ingreso', 'Egreso') NOT NULL,
    `Concepto` VARCHAR(150) NOT NULL,
    `MetodoPago` ENUM('Efectivo', 'YapePlin', 'Tarjeta') NOT NULL,
    `Monto` DECIMAL(18,2) NOT NULL,
    `Referencia` VARCHAR(100) NULL,
    `FechaUtc` DATETIME(6)
        NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    `Observacion` TEXT NULL,

    `Origen` ENUM(
        'Venta',
        'Propina',
        'IngresoManual',
        'Gasto',
        'Retiro',
        'Reembolso'
    ) NOT NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_CajaMovimiento_Caja_Fecha` (`CajaId`, `FechaUtc`),
    KEY `IX_CajaMovimiento_UsuarioId` (`UsuarioId`),
    KEY `IX_CajaMovimiento_VentaId` (`VentaId`),

    CONSTRAINT `FK_CajaMovimiento_Caja`
        FOREIGN KEY (`CajaId`)
        REFERENCES `Caja` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_CajaMovimiento_Usuario`
        FOREIGN KEY (`UsuarioId`)
        REFERENCES `Usuario` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `FK_CajaMovimiento_Venta`
        FOREIGN KEY (`VentaId`)
        REFERENCES `Venta` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT,

    CONSTRAINT `CK_CajaMovimiento_Tipo`
        CHECK (`Tipo` IN ('Ingreso', 'Egreso')),

    CONSTRAINT `CK_CajaMovimiento_Metodo`
        CHECK (`MetodoPago` IN ('Efectivo', 'YapePlin', 'Tarjeta')),

    CONSTRAINT `CK_CajaMovimiento_Monto`
        CHECK (`Monto` > 0.00),

    CONSTRAINT `CK_CajaMovimiento_Concepto`
        CHECK (CHAR_LENGTH(TRIM(`Concepto`)) > 0),

    CONSTRAINT `CK_CajaMovimiento_Direccion`
        CHECK (
            (
                `Origen` IN ('Venta', 'Propina', 'IngresoManual')
                AND `Tipo` = 'Ingreso'
            )
            OR
            (
                `Origen` IN ('Gasto', 'Retiro', 'Reembolso')
                AND `Tipo` = 'Egreso'
            )
        )
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;



-- La BD esta alojada en un servidor privado con MYSQL.
