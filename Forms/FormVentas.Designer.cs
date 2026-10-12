namespace LacteosMadeline.Forms;

partial class FormVentas
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    // Estructura: izquierda 65 % (búsqueda + carrito), derecha 35 % (cobro).
    private TableLayoutPanel tlpRaiz;
    private TableLayoutPanel tlpIzquierda;
    private TableLayoutPanel tlpBusqueda;
    private TableLayoutPanel tlpCobro;
    private TableLayoutPanel tlpMetodos;
    private Panel panelPieCarrito;

    private GroupBox grpBusqueda;
    private GroupBox grpCarrito;
    private GroupBox grpCobro;

    private Label lblProducto;
    private ComboBox cboProducto;
    private Label lblCantidad;
    private NumericUpDown nudCantidad;
    private Button btnAgregar;

    private DataGridView dgvCarrito;
    private Button btnEliminarLinea;
    private Label lblDesglose;

    private Label lblTotalTitulo;
    private Label lblTotal;
    private Label lblMetodoPago;
    private RadioButton rbEfectivo;
    private RadioButton rbTarjeta;
    private RadioButton rbTransferencia;
    private Button btnConfirmar;
    private Button btnCancelar;
    private Label lblCambioTitulo;
    private Label lblCambio;
    private Label lblRecibido;
    private Label lblAtajos;
    private Label lblMensaje;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        tlpRaiz = new TableLayoutPanel();
        tlpIzquierda = new TableLayoutPanel();
        tlpBusqueda = new TableLayoutPanel();
        tlpCobro = new TableLayoutPanel();
        tlpMetodos = new TableLayoutPanel();
        panelPieCarrito = new Panel();
        grpBusqueda = new GroupBox();
        grpCarrito = new GroupBox();
        grpCobro = new GroupBox();
        lblProducto = new Label();
        cboProducto = new ComboBox();
        lblCantidad = new Label();
        nudCantidad = new NumericUpDown();
        btnAgregar = new Button();
        dgvCarrito = new DataGridView();
        btnEliminarLinea = new Button();
        lblDesglose = new Label();
        lblTotalTitulo = new Label();
        lblTotal = new Label();
        lblMetodoPago = new Label();
        rbEfectivo = new RadioButton();
        rbTarjeta = new RadioButton();
        rbTransferencia = new RadioButton();
        btnConfirmar = new Button();
        btnCancelar = new Button();
        lblCambioTitulo = new Label();
        lblCambio = new Label();
        lblRecibido = new Label();
        lblAtajos = new Label();
        lblMensaje = new Label();

        ((System.ComponentModel.ISupportInitialize)nudCantidad).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvCarrito).BeginInit();
        tlpRaiz.SuspendLayout();
        SuspendLayout();

        var fuenteEntrada = new Font("Segoe UI", 13F);
        var fuenteEtiquetaGrande = new Font("Segoe UI", 9.75F, FontStyle.Bold);

        // ── Raíz ────────────────────────────────────────────────────────
        tlpRaiz.Dock = DockStyle.Fill;
        tlpRaiz.Padding = new Padding(16, 12, 16, 8);
        tlpRaiz.ColumnCount = 2;
        tlpRaiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        tlpRaiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        tlpRaiz.RowCount = 2;
        tlpRaiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpRaiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        // ── Izquierda: búsqueda + carrito ───────────────────────────────
        tlpIzquierda.Dock = DockStyle.Fill;
        tlpIzquierda.Margin = new Padding(0, 0, 8, 0);
        tlpIzquierda.ColumnCount = 1;
        tlpIzquierda.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpIzquierda.RowCount = 2;
        tlpIzquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 98F));
        tlpIzquierda.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        grpBusqueda.Text = "Buscar producto";
        grpBusqueda.Dock = DockStyle.Fill;
        grpBusqueda.Margin = new Padding(0, 0, 0, 10);
        grpBusqueda.Padding = new Padding(12, 8, 12, 10);

        tlpBusqueda.Dock = DockStyle.Fill;
        tlpBusqueda.ColumnCount = 5;
        tlpBusqueda.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpBusqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpBusqueda.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpBusqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
        tlpBusqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 196F));
        tlpBusqueda.RowCount = 1;
        tlpBusqueda.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        lblProducto.AutoSize = true;
        lblProducto.Anchor = AnchorStyles.Left;
        lblProducto.Text = "Producto";
        lblProducto.Font = fuenteEtiquetaGrande;

        // Se puede escribir para buscar el producto por nombre.
        cboProducto.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        cboProducto.Font = fuenteEntrada;
        cboProducto.DropDownStyle = ComboBoxStyle.DropDown;
        cboProducto.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        cboProducto.AutoCompleteSource = AutoCompleteSource.ListItems;
        cboProducto.Margin = new Padding(8, 3, 16, 3);

        lblCantidad.AutoSize = true;
        lblCantidad.Anchor = AnchorStyles.Left;
        lblCantidad.Text = "Cantidad";
        lblCantidad.Font = fuenteEtiquetaGrande;

        nudCantidad.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        nudCantidad.Font = fuenteEntrada;
        nudCantidad.Minimum = 1;
        nudCantidad.Maximum = 100000;
        nudCantidad.Value = 1;
        nudCantidad.TextAlign = HorizontalAlignment.Right;
        nudCantidad.Margin = new Padding(8, 3, 12, 3);

        btnAgregar.Dock = DockStyle.Fill;
        btnAgregar.Margin = new Padding(0, 2, 0, 2);
        btnAgregar.Text = "Agregar  (Enter)";
        btnAgregar.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnAgregar.Click += btnAgregar_Click;

        tlpBusqueda.Controls.Add(lblProducto, 0, 0);
        tlpBusqueda.Controls.Add(cboProducto, 1, 0);
        tlpBusqueda.Controls.Add(lblCantidad, 2, 0);
        tlpBusqueda.Controls.Add(nudCantidad, 3, 0);
        tlpBusqueda.Controls.Add(btnAgregar, 4, 0);
        grpBusqueda.Controls.Add(tlpBusqueda);

        grpCarrito.Text = "Detalle de la venta";
        grpCarrito.Dock = DockStyle.Fill;
        grpCarrito.Margin = new Padding(0);
        grpCarrito.Padding = new Padding(12, 8, 12, 10);

        dgvCarrito.Dock = DockStyle.Fill;
        dgvCarrito.ReadOnly = true;
        dgvCarrito.AllowUserToAddRows = false;
        dgvCarrito.AllowUserToDeleteRows = false;
        dgvCarrito.AllowUserToResizeRows = false;
        dgvCarrito.MultiSelect = false;
        dgvCarrito.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCarrito.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        panelPieCarrito.Dock = DockStyle.Bottom;
        panelPieCarrito.Height = 56;
        panelPieCarrito.Padding = new Padding(0, 10, 0, 0);

        btnEliminarLinea.Dock = DockStyle.Left;
        btnEliminarLinea.Width = 280;
        btnEliminarLinea.Text = "Quitar producto seleccionado";
        btnEliminarLinea.Click += btnEliminarLinea_Click;

        lblDesglose.Dock = DockStyle.Fill;
        lblDesglose.TextAlign = ContentAlignment.MiddleRight;
        lblDesglose.Font = new Font("Segoe UI", 11F, FontStyle.Bold);

        panelPieCarrito.Controls.Add(lblDesglose);
        panelPieCarrito.Controls.Add(btnEliminarLinea);

        grpCarrito.Controls.Add(dgvCarrito);
        grpCarrito.Controls.Add(panelPieCarrito);
        dgvCarrito.BringToFront();

        tlpIzquierda.Controls.Add(grpBusqueda, 0, 0);
        tlpIzquierda.Controls.Add(grpCarrito, 0, 1);

        // ── Derecha: cobro ──────────────────────────────────────────────
        grpCobro.Text = "Cobro";
        grpCobro.Dock = DockStyle.Fill;
        grpCobro.Margin = new Padding(8, 0, 0, 0);
        grpCobro.Padding = new Padding(16, 8, 16, 10);

        tlpCobro.Dock = DockStyle.Fill;
        tlpCobro.ColumnCount = 1;
        tlpCobro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        float[] alturas = { 22, 74, 26, 30, 54, 74, 50, 14, 22, 62, 26 };
        tlpCobro.RowCount = alturas.Length + 1;
        foreach (var alto in alturas) tlpCobro.RowStyles.Add(new RowStyle(SizeType.Absolute, alto));
        tlpCobro.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        lblTotalTitulo.Dock = DockStyle.Fill;
        lblTotalTitulo.Text = "TOTAL A PAGAR";
        lblTotalTitulo.Font = fuenteEtiquetaGrande;
        lblTotalTitulo.ForeColor = Color.DimGray;
        lblTotalTitulo.TextAlign = ContentAlignment.BottomLeft;

        lblTotal.Dock = DockStyle.Fill;
        lblTotal.Font = new Font("Segoe UI", 36F, FontStyle.Bold);
        lblTotal.TextAlign = ContentAlignment.MiddleRight;
        lblTotal.Text = "$0.00";

        lblMetodoPago.Dock = DockStyle.Fill;
        lblMetodoPago.Text = "MÉTODO DE PAGO";
        lblMetodoPago.Font = fuenteEtiquetaGrande;
        lblMetodoPago.ForeColor = Color.DimGray;
        lblMetodoPago.TextAlign = ContentAlignment.BottomLeft;

        tlpMetodos.Dock = DockStyle.Fill;
        tlpMetodos.Margin = new Padding(0, 3, 0, 3);
        tlpMetodos.ColumnCount = 3;
        for (var i = 0; i < 3; i++) tlpMetodos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        tlpMetodos.RowCount = 1;
        tlpMetodos.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        foreach (var (opcion, texto, columna) in new[] { (rbEfectivo, "Efectivo", 0), (rbTarjeta, "Tarjeta", 1), (rbTransferencia, "Transferencia", 2) })
        {
            opcion.Appearance = Appearance.Button;
            opcion.Dock = DockStyle.Fill;
            opcion.Margin = new Padding(columna == 0 ? 0 : 3, 0, columna == 2 ? 0 : 3, 0);
            opcion.Text = texto;
            opcion.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            tlpMetodos.Controls.Add(opcion, columna, 0);
        }

        btnConfirmar.Dock = DockStyle.Fill;
        btnConfirmar.Margin = new Padding(0, 8, 0, 4);
        btnConfirmar.Text = "Cobrar y confirmar  (F12)";
        btnConfirmar.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        btnConfirmar.Click += btnConfirmar_Click;

        btnCancelar.Dock = DockStyle.Fill;
        btnCancelar.Margin = new Padding(0, 4, 0, 4);
        btnCancelar.Text = "Cancelar venta  (Esc)";
        btnCancelar.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnCancelar.Click += btnCancelar_Click;

        lblCambioTitulo.Dock = DockStyle.Fill;
        lblCambioTitulo.Text = "CAMBIO A ENTREGAR";
        lblCambioTitulo.Font = fuenteEtiquetaGrande;
        lblCambioTitulo.ForeColor = Color.DimGray;
        lblCambioTitulo.TextAlign = ContentAlignment.BottomLeft;

        lblCambio.Dock = DockStyle.Fill;
        lblCambio.Font = new Font("Segoe UI", 32F, FontStyle.Bold);
        lblCambio.TextAlign = ContentAlignment.MiddleRight;
        lblCambio.ForeColor = Color.DimGray;
        lblCambio.Text = "$0.00";

        lblRecibido.Dock = DockStyle.Fill;
        lblRecibido.TextAlign = ContentAlignment.MiddleRight;
        lblRecibido.ForeColor = Color.DimGray;

        lblAtajos.Dock = DockStyle.Fill;
        lblAtajos.TextAlign = ContentAlignment.BottomLeft;
        lblAtajos.Font = new Font("Segoe UI", 8.75F);
        lblAtajos.ForeColor = Color.DimGray;
        lblAtajos.Text = "Escriba el nombre del producto, pulse Tab, indique la cantidad y pulse Enter para agregar.";

        // Las filas 2 y 7 son espacios de separación entre bloques.
        var filas = new Control[]
        {
            lblTotalTitulo, lblTotal, new Label { Dock = DockStyle.Fill }, lblMetodoPago, tlpMetodos,
            btnConfirmar, btnCancelar, new Label { Dock = DockStyle.Fill },
            lblCambioTitulo, lblCambio, lblRecibido, lblAtajos
        };
        for (var i = 0; i < filas.Length; i++) tlpCobro.Controls.Add(filas[i], 0, i);

        grpCobro.Controls.Add(tlpCobro);

        // ── Mensajes ────────────────────────────────────────────────────
        lblMensaje.Dock = DockStyle.Fill;
        lblMensaje.TextAlign = ContentAlignment.MiddleLeft;
        lblMensaje.ForeColor = Color.DarkRed;
        lblMensaje.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);

        tlpRaiz.Controls.Add(tlpIzquierda, 0, 0);
        tlpRaiz.Controls.Add(grpCobro, 1, 0);
        tlpRaiz.Controls.Add(lblMensaje, 0, 1);
        tlpRaiz.SetColumnSpan(lblMensaje, 2);

        // ── FormVentas ──────────────────────────────────────────────────
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 600);
        KeyPreview = true;
        Controls.Add(tlpRaiz);
        Text = "Punto de Venta";
        ((System.ComponentModel.ISupportInitialize)nudCantidad).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvCarrito).EndInit();
        tlpRaiz.ResumeLayout(false);
        ResumeLayout(false);
    }
}
