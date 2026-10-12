using LacteosMadeline.Models;
using LacteosMadeline.Repositories;

using LacteosMadeline.Forms.Shell;

namespace LacteosMadeline.Forms;

/// <summary>
/// Consulta histórica de compras con filtros por fecha y proveedor.
/// Al seleccionar una compra muestra el detalle de productos (sección 16 lineamientos).
/// </summary>
public partial class FormConsultaCompras : Form
{
    private readonly CompraRepository _compraRepository = new();
    private readonly ProveedorRepository _proveedorRepository = new();

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
        var todos = _proveedorRepository.Buscar();

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

        var compras = _compraRepository.ObtenerHistorial(desde, hasta);

        // Filtro adicional por proveedor
        if (cboProveedor.SelectedValue is int idProveedor && idProveedor > 0)
        {
            compras = compras.Where(c => c.IdProveedor == idProveedor).ToList();
        }

        dgvCompras.DataSource = null;
        dgvCompras.DataSource = compras;

        if (dgvCompras.Columns.Count > 0 && dgvCompras.Columns["IdCompra"] != null)
        {
            dgvCompras.Columns["IdCompra"].HeaderText = "# Compra";
            dgvCompras.Columns["FechaHora"].HeaderText = "Fecha / Hora";
            dgvCompras.Columns["IdProveedor"].Visible = false;
            dgvCompras.Columns["NombreProveedor"].HeaderText = "Proveedor";
            dgvCompras.Columns["Total"].HeaderText = "Total";
            if (dgvCompras.Columns["Detalles"] is { } colDetalles) colDetalles.Visible = false;
        }

        dgvDetalle.DataSource = null;
        lblTotalCompras.Text = $"Compras encontradas: {compras.Count}  |  Total: {compras.Sum(c => c.Total):C2}";
    }

    private void dgvCompras_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvCompras.CurrentRow?.DataBoundItem is not Compra compra) return;

        var compraCompleta = _compraRepository.ObtenerPorId(compra.IdCompra);
        if (compraCompleta is null) return;

        dgvDetalle.DataSource = null;
        dgvDetalle.DataSource = compraCompleta.Detalles;

        if (dgvDetalle.Columns.Count > 0 && dgvDetalle.Columns["IdDetalleCompra"] != null)
        {
            dgvDetalle.Columns["IdDetalleCompra"].Visible = false;
            dgvDetalle.Columns["IdCompra"].Visible = false;
            dgvDetalle.Columns["IdProducto"].Visible = false;
            dgvDetalle.Columns["NombreProducto"].HeaderText = "Producto";
            dgvDetalle.Columns["Cantidad"].HeaderText = "Cantidad";
            dgvDetalle.Columns["CostoUnitario"].HeaderText = "Costo Unit.";
            dgvDetalle.Columns["Subtotal"].HeaderText = "Subtotal";
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
