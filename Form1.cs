using LacteosMadeline.Forms;

namespace LacteosMadeline;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
    }

    private void btnCategorias_Click(object? sender, EventArgs e)
    {
        using var formCategorias = new FormCategorias();
        formCategorias.ShowDialog(this);
    }

    private void btnProductos_Click(object? sender, EventArgs e)
    {
        using var formProductos = new FormProductos();
        formProductos.ShowDialog(this);
    }

    private void btnVentas_Click(object? sender, EventArgs e)
    {
        using var formVentas = new FormVentas();
        formVentas.ShowDialog(this);
    }

    private void btnSalir_Click(object? sender, EventArgs e)
    {
        Close();
    }
}
