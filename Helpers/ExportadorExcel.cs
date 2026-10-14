using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace LacteosMadeline.Helpers;

/// <summary>
/// Genera un archivo de Excel (.xlsx) con una tabla: título, fila de
/// encabezados en negrita y los datos. Los números y montos se guardan
/// como números (se pueden sumar en Excel) y las fechas como fecha.
/// Se arma directamente el formato Office Open XML, sin dependencias.
/// </summary>
public static class ExportadorExcel
{
    // Índices de estilo definidos en styles.xml.
    private const int EstiloNormal = 0;
    private const int EstiloEncabezado = 1;
    private const int EstiloMoneda = 2;
    private const int EstiloFecha = 3;
    private const int EstiloTitulo = 4;
    private const int EstiloDecimal = 5;

    /// <summary>
    /// Exporta las columnas visibles de la tabla, en el orden en que se ven.
    /// Las columnas con formato "C2" se exportan con formato de moneda.
    /// </summary>
    public static (bool Exitoso, string Detalle) Exportar(DataGridView tabla, string ruta, string titulo)
    {
        var columnas = tabla.Columns.Cast<DataGridViewColumn>()
            .Where(c => c.Visible)
            .OrderBy(c => c.DisplayIndex)
            .ToList();

        var encabezados = columnas.Select(c => c.HeaderText).ToList();
        var monedas = columnas.Select(c => c.DefaultCellStyle.Format?.StartsWith('C') == true).ToList();
        var filas = tabla.Rows.Cast<DataGridViewRow>()
            .Where(f => !f.IsNewRow)
            .Select(f => columnas.Select(c => f.Cells[c.Index].Value).ToList())
            .ToList();

        return Exportar(ruta, titulo, encabezados, filas, monedas);
    }

    public static (bool Exitoso, string Detalle) Exportar(
        string ruta, string titulo, IReadOnlyList<string> encabezados, IReadOnlyList<IReadOnlyList<object?>> filas,
        IReadOnlyList<bool>? columnasMoneda = null)
    {
        try
        {
            var directorio = Path.GetDirectoryName(Path.GetFullPath(ruta));
            if (!string.IsNullOrEmpty(directorio)) Directory.CreateDirectory(directorio);
            if (File.Exists(ruta)) File.Delete(ruta);

            using (var zip = ZipFile.Open(ruta, ZipArchiveMode.Create))
            {
                Escribir(zip, "[Content_Types].xml", TiposContenido);
                Escribir(zip, "_rels/.rels", Relaciones);
                Escribir(zip, "xl/workbook.xml", LibroDeTrabajo(titulo));
                Escribir(zip, "xl/_rels/workbook.xml.rels", RelacionesLibro);
                Escribir(zip, "xl/styles.xml", Estilos);
                Escribir(zip, "xl/worksheets/sheet1.xml", Hoja(titulo, encabezados, filas, columnasMoneda));
            }

            return (true, $"Archivo guardado en:\n{ruta}");
        }
        catch (IOException ex)
        {
            LogHelper.Error($"Exportar a Excel ({ruta})", ex);
            return (false, $"No se pudo guardar el archivo. Si está abierto en Excel, ciérrelo e intente de nuevo.\nDetalle: {ex.Message}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"Exportar a Excel ({ruta})", ex);
            return (false, $"No se pudo exportar: {ex.Message}");
        }
    }

    private static string Hoja(string titulo, IReadOnlyList<string> encabezados, IReadOnlyList<IReadOnlyList<object?>> filas,
        IReadOnlyList<bool>? columnasMoneda)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");

        sb.Append("<cols>");
        for (var c = 0; c < encabezados.Count; c++)
        {
            var largo = Math.Max(encabezados[c].Length, filas.Count == 0 ? 0 : filas.Max(f => Texto(f.ElementAtOrDefault(c)).Length));
            var anchoColumna = Math.Clamp(largo + 3, 8, 60);
            sb.Append(CultureInfo.InvariantCulture, $"""<col min="{c + 1}" max="{c + 1}" width="{anchoColumna}" customWidth="1"/>""");
        }
        sb.Append("</cols><sheetData>");

        // Fila 1: título; fila 2: fecha de generación; fila 4: encabezados.
        sb.Append($"""<row r="1">{CeldaTexto("A1", titulo, EstiloTitulo)}</row>""");
        sb.Append($"""<row r="2">{CeldaTexto("A2", $"Generado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", ConfiguracionNegocio.Cultura)}", EstiloNormal)}</row>""");

        sb.Append("""<row r="4">""");
        for (var c = 0; c < encabezados.Count; c++)
        {
            sb.Append(CeldaTexto($"{Columna(c)}4", encabezados[c], EstiloEncabezado));
        }
        sb.Append("</row>");

        for (var f = 0; f < filas.Count; f++)
        {
            var numeroFila = f + 5;
            sb.Append(CultureInfo.InvariantCulture, $"""<row r="{numeroFila}">""");
            for (var c = 0; c < encabezados.Count; c++)
            {
                var esMoneda = columnasMoneda is not null && c < columnasMoneda.Count && columnasMoneda[c];
                sb.Append(Celda($"{Columna(c)}{numeroFila}", filas[f].ElementAtOrDefault(c), esMoneda));
            }
            sb.Append("</row>");
        }

        sb.Append("</sheetData>");
        if (encabezados.Count > 0)
        {
            sb.Append($"""<autoFilter ref="A4:{Columna(encabezados.Count - 1)}{Math.Max(4, filas.Count + 4)}"/>""");
        }
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static string Celda(string referencia, object? valor, bool esMoneda) => valor switch
    {
        null => string.Empty,
        decimal d => CeldaNumero(referencia, d.ToString(CultureInfo.InvariantCulture), esMoneda ? EstiloMoneda : EstiloDecimal),
        int or long or short => CeldaNumero(referencia, Convert.ToString(valor, CultureInfo.InvariantCulture)!, EstiloNormal),
        double or float => CeldaNumero(referencia, Convert.ToDouble(valor).ToString("R", CultureInfo.InvariantCulture), esMoneda ? EstiloMoneda : EstiloDecimal),
        DateTime fecha => CeldaNumero(referencia, fecha.ToOADate().ToString("R", CultureInfo.InvariantCulture), EstiloFecha),
        bool b => CeldaTexto(referencia, b ? "Sí" : "No", EstiloNormal),
        _ => CeldaTexto(referencia, Texto(valor), EstiloNormal)
    };

    private static string CeldaNumero(string referencia, string valor, int estilo) =>
        $"""<c r="{referencia}" s="{estilo}"><v>{valor}</v></c>""";

    private static string CeldaTexto(string referencia, string texto, int estilo) =>
        $"""<c r="{referencia}" s="{estilo}" t="inlineStr"><is><t xml:space="preserve">{SecurityElement.Escape(LimpiarControl(texto))}</t></is></c>""";

    private static string Texto(object? valor) => valor switch
    {
        null => string.Empty,
        DateTime fecha => fecha.ToString("dd/MM/yyyy HH:mm", ConfiguracionNegocio.Cultura),
        IFormattable f => f.ToString(null, ConfiguracionNegocio.Cultura),
        _ => valor.ToString() ?? string.Empty
    };

    /// <summary>XML no admite caracteres de control (salvo tabulador y saltos de línea).</summary>
    private static string LimpiarControl(string texto) =>
        new(texto.Where(c => c >= ' ' || c is '\t' or '\n' or '\r').ToArray());

    /// <summary>0 → A, 25 → Z, 26 → AA…</summary>
    private static string Columna(int indice)
    {
        var nombre = string.Empty;
        for (var n = indice + 1; n > 0; n = (n - 1) / 26)
        {
            nombre = (char)('A' + (n - 1) % 26) + nombre;
        }
        return nombre;
    }

    private static void Escribir(ZipArchive zip, string nombre, string contenido)
    {
        var entrada = zip.CreateEntry(nombre, CompressionLevel.Optimal);
        using var flujo = new StreamWriter(entrada.Open(), new UTF8Encoding(false));
        flujo.Write(contenido);
    }

    private static string LibroDeTrabajo(string titulo)
    {
        // Nombre de hoja: máximo 31 caracteres y sin : \ / ? * [ ]
        var nombreHoja = new string(titulo.Where(c => !":\\/?*[]".Contains(c)).Take(31).ToArray());
        if (nombreHoja.Length == 0) nombreHoja = "Reporte";

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
            <sheets><sheet name="{SecurityElement.Escape(nombreHoja)}" sheetId="1" r:id="rId1"/></sheets>
            </workbook>
            """;
    }

    private const string TiposContenido = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
        <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
        <Default Extension="xml" ContentType="application/xml"/>
        <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
        <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
        <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private const string Relaciones = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string RelacionesLibro = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
        <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    // Estilos: 0 normal, 1 encabezado (negrita con fondo), 2 moneda, 3 fecha, 4 título, 5 decimal.
    private const string Estilos = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
        <numFmts count="3">
          <numFmt numFmtId="164" formatCode="&quot;$&quot;#,##0.00"/>
          <numFmt numFmtId="165" formatCode="dd/mm/yyyy hh:mm"/>
          <numFmt numFmtId="166" formatCode="#,##0.###"/>
        </numFmts>
        <fonts count="3">
          <font><sz val="11"/><name val="Calibri"/></font>
          <font><b/><sz val="11"/><name val="Calibri"/></font>
          <font><b/><sz val="14"/><name val="Calibri"/></font>
        </fonts>
        <fills count="3">
          <fill><patternFill patternType="none"/></fill>
          <fill><patternFill patternType="gray125"/></fill>
          <fill><patternFill patternType="solid"><fgColor rgb="FFEFECE3"/><bgColor indexed="64"/></patternFill></fill>
        </fills>
        <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
        <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
        <cellXfs count="6">
          <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
          <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/>
          <xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
          <xf numFmtId="165" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
          <xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1"/>
          <xf numFmtId="166" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
        </cellXfs>
        </styleSheet>
        """;
}
