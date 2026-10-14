using Microsoft.Data.Sqlite;

namespace LacteosMadeline.Tests;

/// <summary>
/// Reglas aplicadas por la propia base de datos (triggers, índices y llaves
/// foráneas). Son la última línea de defensa: deben rechazar datos inválidos
/// aunque un error de programación salte las validaciones de los servicios.
/// </summary>
public class ReglasIntegridadTests : PruebaConBaseDatos
{
    private const int SqliteConstraint = 19;

    private static void AssertRechazada(string sql)
    {
        var error = Assert.Throws<SqliteException>(() => Ejecutar(sql));
        Assert.Equal(SqliteConstraint, error.SqliteErrorCode);
    }

    [Fact]
    public void ExistenciaNegativa_SeRechaza()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 2);

        AssertRechazada($"UPDATE Productos SET Existencia = -1 WHERE IdProducto = {idProducto};");
        Assert.Equal(2, Existencia(idProducto));
    }

    [Fact]
    public void ProductoConPrecioNegativo_SeRechaza()
    {
        AssertRechazada("INSERT INTO Productos (Nombre, IdCategoria, PrecioVenta, Existencia) VALUES ('X', 1, -1, 0);");
    }

    [Fact]
    public void VentaSinCajaAbierta_SeRechaza()
    {
        AssertRechazada("INSERT INTO Ventas (FechaHora, Total, MetodoPago, IdCaja) VALUES ('2026-01-01 10:00:00', 5, 'Efectivo', NULL);");
    }

    [Fact]
    public void VentaEnCajaCerrada_SeRechaza()
    {
        var caja = AbrirCaja();
        Ejecutar($"UPDATE Cajas SET Estado = 'Cerrada' WHERE IdCaja = {caja.IdCaja};");

        AssertRechazada($"INSERT INTO Ventas (FechaHora, Total, MetodoPago, IdCaja) VALUES ('2026-01-01 10:00:00', 5, 'Efectivo', {caja.IdCaja});");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, -1)]
    public void LineaDeVentaInvalida_SeRechaza(int cantidad, decimal precio)
    {
        var caja = AbrirCaja();
        var idProducto = CrearProducto("Queso fresco", 3.50m, 2);
        Ejecutar($"INSERT INTO Ventas (FechaHora, Total, MetodoPago, IdCaja) VALUES ('2026-01-01 10:00:00', 0, 'Efectivo', {caja.IdCaja});");

        AssertRechazada($"""
            INSERT INTO DetalleVentas (IdVenta, IdProducto, NombreProducto, Cantidad, PrecioUnitario, Subtotal)
            VALUES (1, {idProducto}, 'Queso fresco', {cantidad}, {precio.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 0);
            """);
    }

    [Fact]
    public void DetalleDeVentaInexistente_SeRechazaPorLlaveForanea()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 2);

        AssertRechazada($"""
            INSERT INTO DetalleVentas (IdVenta, IdProducto, NombreProducto, Cantidad, PrecioUnitario, Subtotal)
            VALUES (999, {idProducto}, 'Queso fresco', 1, 350, 350);
            """);
    }

    [Fact]
    public void SegundaCajaAbierta_SeRechazaPorIndiceUnico()
    {
        AbrirCaja();

        AssertRechazada("INSERT INTO Cajas (FechaApertura, MontoInicial, Estado) VALUES ('2026-01-01 08:00:00', 0, 'Abierta');");
    }

    [Theory]
    [InlineData("Ingreso", 5)]
    [InlineData("Entrada", 0)]
    public void MovimientoDeCajaInvalido_SeRechaza(string tipo, decimal monto)
    {
        var caja = AbrirCaja();

        AssertRechazada($"INSERT INTO MovimientosCaja (IdCaja, FechaHora, Tipo, Monto) VALUES ({caja.IdCaja}, '2026-01-01 10:00:00', '{tipo}', {monto});");
    }

    [Fact]
    public void MermaConCantidadCero_SeRechaza()
    {
        var idProducto = CrearProducto("Queso fresco", 3.50m, 2);

        AssertRechazada($"INSERT INTO Mermas (FechaHora, IdProducto, NombreProducto, Cantidad, Motivo) VALUES ('2026-01-01 10:00:00', {idProducto}, 'Queso fresco', 0, 'Daño');");
    }
}
