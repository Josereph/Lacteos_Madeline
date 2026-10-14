using LacteosMadeline.Helpers;
using LacteosMadeline.Models;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Pantalla de administración de productos: registrar, buscar, consultar,
/// modificar, asociar categoría y activar/desactivar (sección 6 de los
/// lineamientos).
/// </summary>
public partial class FormProductos : Form
{
    private readonly ProductoService _productoService = new();
    private readonly InventarioService _inventarioService = new();

    private int _idSeleccionado;

    // Existencia que se mostró al seleccionar el producto. Si el usuario la
    // cambia, se registra un ajuste; si mientras tanto hubo una venta o una
    // compra, el ajuste se rechaza en lugar de borrar ese movimiento.
    private int _existenciaMostrada;

    public FormProductos()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        ConfigurarDisenoAdaptable();
        Load += FormProductos_Load;
    }

    private void FormProductos_Load(object? sender, EventArgs e)
    {
        CargarCategorias();
        CargarProductos();
    }

    private void CargarCategorias()
    {
        var categorias = _productoService.CategoriasActivas();
        cboCategoria.DisplayMember = "Nombre";
        cboCategoria.ValueMember = "IdCategoria";
        cboCategoria.DataSource = categorias;
    }

    private void CargarProductos(string filtro = "")
    {
        var productos = _productoService.Buscar(filtro);

        dgvProductos.DataSource = null;
        dgvProductos.DataSource = productos;

        if (dgvProductos.Columns["IdCategoria"] != null)
        {
            dgvProductos.OcultarColumna("IdCategoria");
            dgvProductos.Encabezado("IdProducto", "#");
            dgvProductos.Encabezado("NombreCategoria", "Categoría");
            dgvProductos.Encabezado("PrecioCompra", "P. Compra");
            dgvProductos.Encabezado("PrecioVenta", "P. Venta");
            dgvProductos.Encabezado("Existencia", "Existencia");
            dgvProductos.Encabezado("StockMinimo", "Stock Mín.");
            dgvProductos.Encabezado("Estado", "Activo");
        }
    }

    private void btnBuscar_Click(object? sender, EventArgs e)
    {
        CargarProductos(txtBuscar.Text.Trim());
    }

    private void dgvProductos_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvProductos.CurrentRow?.DataBoundItem is not Producto producto)
        {
            return;
        }

        _idSeleccionado = producto.IdProducto;
        _existenciaMostrada = producto.Existencia;
        txtNombre.Text = producto.Nombre;
        cboCategoria.SelectedValue = producto.IdCategoria;
        txtPrecioCompra.Text = producto.PrecioCompra is decimal compra ? Dinero.ParaEditar(compra) : string.Empty;
        txtPrecioVenta.Text = Dinero.ParaEditar(producto.PrecioVenta);
        txtExistencia.Text = producto.Existencia.ToString();
        txtStockMinimo.Text = producto.StockMinimo.ToString();
    }

    private void btnNuevo_Click(object? sender, EventArgs e)
    {
        _idSeleccionado = 0;
        _existenciaMostrada = 0;
        txtNombre.Clear();
        txtPrecioCompra.Clear();
        txtPrecioVenta.Clear();
        txtExistencia.Clear();
        txtStockMinimo.Clear();
        dgvProductos.ClearSelection();
        txtNombre.Focus();
    }

    private void btnGuardar_Click(object? sender, EventArgs e)
    {
        if (cboCategoria.SelectedValue is not int idCategoria)
        {
            MostrarMensaje("Debe registrar al menos una categoría antes de agregar productos.");
            return;
        }

        decimal? precioCompra = null;
        if (!string.IsNullOrWhiteSpace(txtPrecioCompra.Text))
        {
            if (!Dinero.TryParse(txtPrecioCompra.Text, out var valorPrecioCompra))
            {
                MostrarMensaje("El precio de compra ingresado no es válido.");
                return;
            }
            precioCompra = valorPrecioCompra;
        }

        if (!Dinero.TryParse(txtPrecioVenta.Text, out var precioVenta))
        {
            MostrarMensaje("El precio de venta ingresado no es válido.");
            return;
        }

        if (!int.TryParse(txtExistencia.Text, out var existencia))
        {
            MostrarMensaje("La existencia ingresada no es válida.");
            return;
        }

        if (!int.TryParse(txtStockMinimo.Text, out var stockMinimo))
        {
            stockMinimo = 0;
        }

        var producto = new Producto
        {
            IdProducto = _idSeleccionado,
            Nombre = txtNombre.Text.Trim(),
            IdCategoria = idCategoria,
            PrecioCompra = precioCompra,
            PrecioVenta = precioVenta,
            Existencia = existencia,
            StockMinimo = stockMinimo,
            Estado = true
        };

        (bool Exitoso, string Mensaje) resultado;

        if (_idSeleccionado == 0)
        {
            var registro = _productoService.Registrar(producto);
            resultado = (registro.Exitoso, registro.Mensaje);
        }
        else
        {
            resultado = _productoService.Modificar(producto);

            if (resultado.Exitoso && existencia != _existenciaMostrada)
            {
                resultado = AjustarExistencia(producto.Nombre, existencia);
            }
        }

        MostrarMensaje(resultado.Mensaje, esError: !resultado.Exitoso);

        if (resultado.Exitoso)
        {
            btnNuevo_Click(sender, e);
            CargarProductos(txtBuscar.Text.Trim());
        }
    }

    /// <summary>
    /// Cambiar la existencia de un producto ya registrado es un ajuste de
    /// inventario: se confirma con el usuario y queda registrado con su motivo.
    /// </summary>
    private (bool Exitoso, string Mensaje) AjustarExistencia(string nombreProducto, int nuevaExistencia)
    {
        var respuesta = MessageBox.Show(this,
            $"Los datos de \"{nombreProducto}\" se guardaron.\n\n" +
            $"También cambió la existencia de {_existenciaMostrada} a {nuevaExistencia}. " +
            "Esto se registrará como un ajuste de inventario.\n\n¿Desea aplicar el ajuste?",
            "Ajuste de inventario", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

        if (respuesta != DialogResult.Yes)
        {
            return (true, "Producto modificado. La existencia no se cambió.");
        }

        var ajuste = _inventarioService.AjustarExistencia(_idSeleccionado, nuevaExistencia,
            InventarioService.MotivoCorreccionProducto, existenciaEsperada: _existenciaMostrada);

        return ajuste.Exitoso
            ? (true, $"Producto modificado. {ajuste.Mensaje}")
            : (false, $"Producto modificado, pero la existencia no se ajustó: {ajuste.Mensaje}");
    }

    private void btnActivarDesactivar_Click(object? sender, EventArgs e)
    {
        if (_idSeleccionado == 0)
        {
            MostrarMensaje("Seleccione un producto de la lista.");
            return;
        }

        var producto = _productoService.ObtenerPorId(_idSeleccionado);
        if (producto is null)
        {
            return;
        }

        _productoService.CambiarEstado(producto.IdProducto, !producto.Estado);
        CargarProductos(txtBuscar.Text.Trim());
    }

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        lblMensaje.ForeColor = esError ? Tema.Alerta : Tema.Exito;
        lblMensaje.Text = mensaje;
    }

    /// <summary>
    /// Anclajes para que la pantalla se expanda dentro de la ventana
    /// maximizada: las tablas crecen y los botones se mantienen en su borde.
    /// </summary>
    private void ConfigurarDisenoAdaptable()
    {
        Diseno.Anclar(Diseno.Todo, dgvProductos);
        Diseno.Anclar(Diseno.AbajoIzquierda,
            lblNombre, txtNombre, lblCategoria, cboCategoria,
            lblPrecioCompra, txtPrecioCompra, lblPrecioVenta, txtPrecioVenta,
            lblExistencia, txtExistencia, lblStockMinimo, txtStockMinimo,
            btnNuevo, btnGuardar, btnActivarDesactivar);
        Diseno.Anclar(Diseno.AbajoAncho, lblMensaje);
    }
}
