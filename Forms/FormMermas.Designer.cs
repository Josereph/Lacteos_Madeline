namespace LacteosMadeline.Forms;

partial class FormMermas
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

    private Label lblTitulo;
    private Label lblSubtitulo;

    // Columna izquierda: captura
    private GroupBox grpCaptura;
    private Label lblProducto;
    private ComboBox cboProducto;
    private Label lblExistencia;
    private Label lblCantidad;
    private NumericUpDown nudCantidad;
    private Label lblMotivo;
    private ComboBox cboMotivo;
    private Label lblObservacion;
    private TextBox txtObservacion;
    private Label lblCostoEstimado;
    private Button btnRegistrarMerma;
    private Button btnLimpiar;

    // Columna derecha: historial / auditoría
    private GroupBox grpHistorial;
    private Label lblDesde;
    private DateTimePicker dtpDesde;
    private Label lblHasta;
    private DateTimePicker dtpHasta;
    private Button btnFiltrar;
    private DataGridView dgvMermas;
    private Label lblTotalPerdida;

    private Label lblMensaje;

    private TableLayoutPanel tlpRaiz;
    private Panel panelFiltros;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        lblTitulo = new Label();
        lblSubtitulo = new Label();
        grpCaptura = new GroupBox();
        lblProducto = new Label();
        cboProducto = new ComboBox();
        lblExistencia = new Label();
        lblCantidad = new Label();
        nudCantidad = new NumericUpDown();
        lblMotivo = new Label();
        cboMotivo = new ComboBox();
        lblObservacion = new Label();
        txtObservacion = new TextBox();
        lblCostoEstimado = new Label();
        btnRegistrarMerma = new Button();
        btnLimpiar = new Button();
        grpHistorial = new GroupBox();
        lblDesde = new Label();
        dtpDesde = new DateTimePicker();
        lblHasta = new Label();
        dtpHasta = new DateTimePicker();
        btnFiltrar = new Button();
        dgvMermas = new DataGridView();
        lblTotalPerdida = new Label();
        lblMensaje = new Label();
        tlpRaiz = new TableLayoutPanel();
        panelFiltros = new Panel();

        ((System.ComponentModel.ISupportInitialize)nudCantidad).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvMermas).BeginInit();
        grpCaptura.SuspendLayout();
        grpHistorial.SuspendLayout();
        SuspendLayout();

        // ── Encabezado ──────────────────────────────────────────────────
        lblTitulo.AutoSize = true;
        lblTitulo.Text = "Bajas de inventario (mermas)";
        // No se agrega al formulario: el título lo muestra la barra superior del sistema.

        lblSubtitulo.Dock = DockStyle.Fill;
        lblSubtitulo.TextAlign = ContentAlignment.MiddleLeft;
        lblSubtitulo.ForeColor = Color.DimGray;
        lblSubtitulo.Text = "Producto vencido o dañado: descuenta existencias sin afectar ventas ni caja.";

        // ── Captura ─────────────────────────────────────────────────────
        grpCaptura.Text = "Registrar baja";
        grpCaptura.Dock = DockStyle.Fill;
        grpCaptura.Margin = new Padding(0, 0, 8, 0);

        int x = 18, w = 352;

        lblProducto.AutoSize = true;
        lblProducto.Location = new Point(x, 30);
        lblProducto.Text = "Producto:";

        cboProducto.Location = new Point(x, 52);
        cboProducto.Size = new Size(w, 25);
        cboProducto.Font = new Font("Segoe UI", 11.25F);
        cboProducto.DropDownStyle = ComboBoxStyle.DropDown;
        cboProducto.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        cboProducto.AutoCompleteSource = AutoCompleteSource.ListItems;

        lblExistencia.AutoSize = false;
        lblExistencia.Location = new Point(x, 82);
        lblExistencia.Size = new Size(w, 20);
        lblExistencia.ForeColor = Color.DimGray;

        lblCantidad.AutoSize = true;
        lblCantidad.Location = new Point(x, 110);
        lblCantidad.Text = "Cantidad a dar de baja:";

        nudCantidad.Location = new Point(x, 132);
        nudCantidad.Size = new Size(110, 25);
        nudCantidad.Minimum = 1;
        nudCantidad.Maximum = 1;
        nudCantidad.Value = 1;
        nudCantidad.TextAlign = HorizontalAlignment.Right;

        lblMotivo.AutoSize = true;
        lblMotivo.Location = new Point(x, 168);
        lblMotivo.Text = "Motivo:";

        cboMotivo.Location = new Point(x, 190);
        cboMotivo.Size = new Size(w, 25);
        cboMotivo.DropDownStyle = ComboBoxStyle.DropDownList;

        lblObservacion.AutoSize = false;
        lblObservacion.Location = new Point(x, 226);
        lblObservacion.Size = new Size(w, 20);
        lblObservacion.Text = "Observación (opcional):";

        txtObservacion.Location = new Point(x, 248);
        txtObservacion.Size = new Size(w, 84);
        txtObservacion.Font = new Font("Segoe UI", 10.5F);
        txtObservacion.Multiline = true;
        txtObservacion.WordWrap = true;
        txtObservacion.MaxLength = 250;

        lblCostoEstimado.AutoSize = false;
        lblCostoEstimado.Location = new Point(x, 340);
        lblCostoEstimado.Size = new Size(w, 22);
        lblCostoEstimado.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
        lblCostoEstimado.ForeColor = Color.Firebrick;

        btnRegistrarMerma.Location = new Point(x, 372);
        btnRegistrarMerma.Size = new Size(230, 48);
        btnRegistrarMerma.Text = "Registrar baja";
        btnRegistrarMerma.Click += btnRegistrarMerma_Click;

        btnLimpiar.Location = new Point(x + 240, 372);
        btnLimpiar.Size = new Size(112, 48);
        btnLimpiar.Text = "Limpiar";
        btnLimpiar.Click += btnLimpiar_Click;

        grpCaptura.Controls.AddRange(new Control[]
        {
            lblProducto, cboProducto, lblExistencia, lblCantidad, nudCantidad,
            lblMotivo, cboMotivo, lblObservacion, txtObservacion,
            lblCostoEstimado, btnRegistrarMerma, btnLimpiar
        });

        // ── Historial ───────────────────────────────────────────────────
        grpHistorial.Text = "Historial de bajas";
        grpHistorial.Dock = DockStyle.Fill;
        grpHistorial.Margin = new Padding(8, 0, 0, 0);
        grpHistorial.Padding = new Padding(16, 6, 16, 8);

        panelFiltros.Dock = DockStyle.Top;
        panelFiltros.Height = 52;

        lblDesde.AutoSize = true;
        lblDesde.Location = new Point(0, 17);
        lblDesde.Text = "Desde:";

        dtpDesde.Location = new Point(50, 13);
        dtpDesde.Size = new Size(130, 25);
        dtpDesde.Format = DateTimePickerFormat.Short;

        lblHasta.AutoSize = true;
        lblHasta.Location = new Point(194, 17);
        lblHasta.Text = "Hasta:";

        dtpHasta.Location = new Point(242, 13);
        dtpHasta.Size = new Size(130, 25);
        dtpHasta.Format = DateTimePickerFormat.Short;

        btnFiltrar.Location = new Point(392, 9);
        btnFiltrar.Size = new Size(130, 34);
        btnFiltrar.Text = "Filtrar";
        btnFiltrar.Click += btnFiltrar_Click;

        dgvMermas.Dock = DockStyle.Fill;
        dgvMermas.ReadOnly = true;
        dgvMermas.AllowUserToAddRows = false;
        dgvMermas.AllowUserToDeleteRows = false;
        dgvMermas.AllowUserToResizeRows = false;
        dgvMermas.MultiSelect = false;
        dgvMermas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvMermas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        lblTotalPerdida.AutoSize = false;
        lblTotalPerdida.Dock = DockStyle.Bottom;
        lblTotalPerdida.Height = 34;
        lblTotalPerdida.TextAlign = ContentAlignment.MiddleRight;
        lblTotalPerdida.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        lblTotalPerdida.ForeColor = Color.Firebrick;

        panelFiltros.Controls.AddRange(new Control[] { lblDesde, dtpDesde, lblHasta, dtpHasta, btnFiltrar });
        grpHistorial.Controls.Add(dgvMermas);
        grpHistorial.Controls.Add(lblTotalPerdida);
        grpHistorial.Controls.Add(panelFiltros);
        dgvMermas.BringToFront();

        // ── Mensajes ────────────────────────────────────────────────────
        lblMensaje.Dock = DockStyle.Fill;
        lblMensaje.TextAlign = ContentAlignment.MiddleLeft;
        lblMensaje.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);

        // ── Estructura: captura (ancho fijo) | historial (resto) ────────
        tlpRaiz.Dock = DockStyle.Fill;
        tlpRaiz.Padding = new Padding(16, 6, 16, 8);
        tlpRaiz.ColumnCount = 2;
        tlpRaiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 410F));
        tlpRaiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpRaiz.RowCount = 3;
        tlpRaiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        tlpRaiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpRaiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        tlpRaiz.Controls.Add(lblSubtitulo, 0, 0);
        tlpRaiz.SetColumnSpan(lblSubtitulo, 2);
        tlpRaiz.Controls.Add(grpCaptura, 0, 1);
        tlpRaiz.Controls.Add(grpHistorial, 1, 1);
        tlpRaiz.Controls.Add(lblMensaje, 0, 2);
        tlpRaiz.SetColumnSpan(lblMensaje, 2);
        lblMensaje.ForeColor = Color.DarkRed;

        // ── Form ────────────────────────────────────────────────────────
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 560);
        Controls.Add(tlpRaiz);
        Text = "Lácteos Madeline – Mermas";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        ((System.ComponentModel.ISupportInitialize)nudCantidad).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvMermas).EndInit();
        grpCaptura.ResumeLayout(false);
        grpCaptura.PerformLayout();
        grpHistorial.ResumeLayout(false);
        grpHistorial.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
