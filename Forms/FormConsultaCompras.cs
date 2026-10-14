using LacteosMadeline.Models;
using LacteosMadeline.Services;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Consulta histórica de compras con filtros por fecha y proveedor.
/// Al seleccionar una compra muestra el detalle de productos (sección 16 lineamientos).
/// </summary>
public partial class FormConsultaCompras : Form
{
    private readonly CompraService _compraService = new();
    private readonly ProveedorService _proveedorService = new();

    public FormConsultaCompras()
    {
        InitializeComponent();
        Tema.Aplicar(this);
        ConfigurarDisenoAdaptable();
        Load += FormConsultaCompras_Load;
    }

    private void FormConsultaCompras_Load(object? sender, EventArgs e)
    {
        dtpDesde.Value = DateTime.Today.AddDays(-30);
        dtpHasta.Value = DateTime.Today;
        CargarProveedores();
        BuscarCompras();
    }

    private void CargarProveedores()
    {
        var todos = _proveedorService.Buscar();

        // Usar clase concreta para que DisplayMember/ValueMember funcionen
        var lista = new List<ItemCombo> { new ItemCombo(0, "(Todos)") };
        lista.AddRange(todos.Select(p => new ItemCombo(p.IdProveedor, p.Nombre)));

        cboProveedor.DisplayMember = "Nombre";
        cboProveedor.ValueMember   = "Id";
        cboProveedor.DataSource    = lista;
    }

    private void btnBuscar_Click(object? sender, EventArgs e)
    {
        BuscarCompras();
    }

    private void BuscarCompras()
    {
        var desde = dtpDesde.Value.Date;
        var hasta = dtpHasta.Value.Date;

        var compras = _compraService.ConsultarHistorial(desde, hasta);

        // Filtro adicional por proveedor
        if (cboProveedor.SelectedValue is int idProveedor && idProveedor > 0)
        {
            compras = compras.Where(c => c.IdProveedor == idProveedor).ToList();
        }

        dgvCompras.DataSource = null;
        dgvCompras.DataSource = compras;

        if (dgvCompras.Columns.Count > 0 && dgvCompras.Columns["IdCompra"] != null)
        {
            dgvCompras.Encabezado("IdCompra", "# Compra");
            dgvCompras.Encabezado("FechaHora", "Fecha / Hora");
            dgvCompras.OcultarColumna("IdProveedor");
            dgvCompras.Encabezado("NombreProveedor", "Proveedor");
            dgvCompras.Encabezado("Total", "Total");
            if (dgvCompras.Columns["Detalles"] is { } colDetalles) colDetalles.Visible = false;
        }

        dgvDetalle.DataSource = null;
        lblTotalCompras.Text = $"Compras encontradas: {compras.Count}  |  Total: {compras.Sum(c => c.Total):C2}";
    }

    private void dgvCompras_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvCompras.CurrentRow?.DataBoundItem is not Compra compra) return;

        var compraCompleta = _compraService.ObtenerPorId(compra.IdCompra);
        if (compraCompleta is null) return;

        dgvDetalle.DataSource = null;
        dgvDetalle.DataSource = compraCompleta.Detalles;

        if (dgvDetalle.Columns.Count > 0 && dgvDetalle.Columns["IdDetalleCompra"] != null)
        {
            dgvDetalle.OcultarColumna("IdDetalleCompra");
            dgvDetalle.OcultarColumna("IdCompra");
            dgvDetalle.OcultarColumna("IdProducto");
            dgvDetalle.Encabezado("NombreProducto", "Producto");
            dgvDetalle.Encabezado("Cantidad", "Cantidad");
            dgvDetalle.Encabezado("CostoUnitario", "Costo Unit.");
            dgvDetalle.Encabezado("Subtotal", "Subtotal");
        }
    }

    /// <summary>
    /// Anclajes para que la pantalla se expanda dentro de la ventana
    /// maximizada: las tablas crecen y los botones se mantienen en su borde.
    /// </summary>
    private void ConfigurarDisenoAdaptable()
    {
        Diseno.Anclar(Diseno.ArribaAncho, dgvCompras, lblTotalCompras);
        Diseno.Anclar(Diseno.Todo, dgvDetalle);
    }
}

/// <summary>
/// Clase auxiliar para poblar ComboBox con DisplayMember/ValueMember.
/// Los tipos anónimos no son compatibles con binding de WinForms porque
/// sus propiedades son internas al ensamblado.
/// </summary>
public class ItemCombo
{
    public int Id { get; }
    public string Nombre { get; }
    public ItemCombo(int id, string nombre) { Id = id; Nombre = nombre; }
}
