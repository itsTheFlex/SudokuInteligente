namespace SudokuSencillo;

/// <summary>Ventana del juego: tablero de 9x9 cajas de texto y botones.</summary>
public class Form1 : Form
{
    private readonly TextBox[,] celdas = new TextBox[9, 9];
    private readonly bool[,] esPista = new bool[9, 9];   // números originales del ejemplo

    private readonly ComboBox cmbEjemplos = new();
    private readonly ComboBox cmbAlgoritmo = new();
    private readonly Label lblInfo = new();

    // Datos para el modo paso a paso
    private List<Paso>? pasos;
    private int pasoActual;
    private bool pasoResuelto;
    private bool escribiendoLaIA;   // true mientras el programa (no el usuario) cambia las celdas

    // Ejemplos: 81 caracteres, '.' = vacío
    private readonly Dictionary<string, string> ejemplos = new()
    {
        ["Fácil"] = "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79",
        ["Medio"] = "..9748...7.........2.1.9.....7...24..64.1.59..98...3.....8.3.2.........6...2759..",
        ["Difícil"] = "8..........36......7..9.2...5...7.......457.....1...3...1....68..85...1..9....4..",
        ["Vacío"] = new string('.', 81),
        ["Sin solución"] = "531.7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79",
        ["Con error (5 repetido)"] = "53..7...56..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79",
    };

    public Form1()
    {
        Text = "Sudoku inteligente - Backtracking y MRV";
        ClientSize = new Size(720, 470);
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        CrearTablero();
        CrearControles();
        CargarEjemplo("Fácil");
        ActiveControl = cmbEjemplos;
    }

    // ------------------------------------------------------------------ Interfaz

    private void CrearTablero()
    {
        for (int f = 0; f < 9; f++)
        {
            for (int c = 0; c < 9; c++)
            {
                var caja = new TextBox
                {
                    MaxLength = 1,
                    TextAlign = HorizontalAlignment.Center,
                    Font = new Font("Segoe UI", 18),
                    Width = 44,
                    // Separación extra cada 3 celdas para que se vean los subcuadros 3x3
                    Left = 15 + c * 46 + (c / 3) * 6,
                    Top = 15 + f * 46 + (f / 3) * 6,
                };
                caja.KeyPress += ValidarTecla;
                caja.TextChanged += (s, e) => { if (!escribiendoLaIA) pasos = null; }; // si el usuario edita, se reinicia el paso a paso
                celdas[f, c] = caja;
                Controls.Add(caja);
            }
        }
    }

    private void CrearControles()
    {
        int x = 460, y = 15;

        Controls.Add(new Label { Text = "Ejemplo:", Left = x, Top = y + 3, AutoSize = true });
        cmbEjemplos.SetBounds(x + 80, y, 160, 28);
        cmbEjemplos.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbEjemplos.Items.AddRange(ejemplos.Keys.ToArray());
        cmbEjemplos.SelectedIndex = 0;
        cmbEjemplos.SelectedIndexChanged += (s, e) => CargarEjemplo(cmbEjemplos.Text);
        Controls.Add(cmbEjemplos);

        y += 40;
        Controls.Add(new Label { Text = "Algoritmo:", Left = x, Top = y + 3, AutoSize = true });
        cmbAlgoritmo.SetBounds(x + 80, y, 160, 28);
        cmbAlgoritmo.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbAlgoritmo.Items.AddRange(new[] { "Básico", "MRV" });
        cmbAlgoritmo.SelectedIndex = 1;
        cmbAlgoritmo.SelectedIndexChanged += (s, e) => pasos = null;
        Controls.Add(cmbAlgoritmo);

        y += 45;
        AgregarBoton("Resolver", x, y, BtnResolver_Click);
        AgregarBoton("Paso a paso", x + 125, y, BtnPaso_Click);
        y += 42;
        AgregarBoton("Validar", x, y, BtnValidar_Click);
        AgregarBoton("Limpiar", x + 125, y, (s, e) => CargarEjemplo(cmbEjemplos.Text));
        y += 42;
        AgregarBoton("Comparar Básico vs MRV", x, y, BtnComparar_Click, 245);

        y += 50;
        lblInfo.SetBounds(x, y, 245, 210);
        lblInfo.BorderStyle = BorderStyle.FixedSingle;
        lblInfo.Padding = new Padding(4);
        Controls.Add(lblInfo);
    }

    private void AgregarBoton(string texto, int x, int y, EventHandler alHacerClic, int ancho = 120)
    {
        var boton = new Button { Text = texto, Left = x, Top = y, Width = ancho, Height = 34 };
        boton.Click += alHacerClic;
        Controls.Add(boton);
    }

    // ------------------------------------------------------------------ Validación de entradas

    /// <summary>Solo se permiten los números del 1 al 9 (y borrar).</summary>
    private void ValidarTecla(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar)) return;          // Retroceso, Supr, etc.
        if (e.KeyChar >= '1' && e.KeyChar <= '9') return;

        e.Handled = true;                                // se ignora la tecla
        MostrarInfo($"Entrada inválida: \"{e.KeyChar}\". Solo se permiten números del 1 al 9.", Color.Firebrick);
    }

    // ------------------------------------------------------------------ Leer y escribir el tablero

    private void CargarEjemplo(string nombre)
    {
        string texto = ejemplos[nombre];
        escribiendoLaIA = true;
        for (int f = 0; f < 9; f++)
        {
            for (int c = 0; c < 9; c++)
            {
                char ch = texto[f * 9 + c];
                var caja = celdas[f, c];
                esPista[f, c] = ch != '.';
                caja.Text = esPista[f, c] ? ch.ToString() : "";
                caja.ReadOnly = esPista[f, c];                       // las pistas no se editan
                caja.ForeColor = Color.Black;
                caja.BackColor = esPista[f, c] ? Color.Gainsboro : Color.White;
                caja.Font = new Font("Segoe UI", 18, esPista[f, c] ? FontStyle.Bold : FontStyle.Regular);
            }
        }
        escribiendoLaIA = false;
        pasos = null;
        MostrarInfo($"Ejemplo cargado: {nombre}.\nEscribe números o pide a la IA que resuelva.", Color.Black);
    }

    private int[,] LeerTablero()
    {
        var t = new int[9, 9];
        for (int f = 0; f < 9; f++)
            for (int c = 0; c < 9; c++)
                t[f, c] = int.TryParse(celdas[f, c].Text, out int n) ? n : 0;
        return t;
    }

    private void MostrarTablero(int[,] t)
    {
        escribiendoLaIA = true;
        for (int f = 0; f < 9; f++)
            for (int c = 0; c < 9; c++)
                if (!esPista[f, c])
                {
                    celdas[f, c].Text = t[f, c] == 0 ? "" : t[f, c].ToString();
                    celdas[f, c].ForeColor = Color.Green;   // números puestos por la IA
                    celdas[f, c].BackColor = Color.White;
                }
        escribiendoLaIA = false;
    }

    private void MostrarInfo(string texto, Color color)
    {
        lblInfo.Text = texto;
        lblInfo.ForeColor = color;
    }

    /// <summary>Revisa que no haya números repetidos antes de buscar.</summary>
    private bool RevisarAntesDeResolver(int[,] tablero)
    {
        if (Solucionador.TableroValido(tablero, out string error)) return true;
        MessageBox.Show("El tablero no es válido:\n" + error, "Entrada inválida",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    // ------------------------------------------------------------------ Botones

    private void BtnResolver_Click(object? sender, EventArgs e)
    {
        var tablero = LeerTablero();
        if (!RevisarAntesDeResolver(tablero)) return;

        var ia = new Solucionador(tablero, cmbAlgoritmo.Text == "MRV");
        var reloj = System.Diagnostics.Stopwatch.StartNew();
        bool resuelto = ia.Resolver();
        reloj.Stop();

        string datos = $"Algoritmo: {cmbAlgoritmo.Text}\n" +
                       $"Intentos: {ia.Intentos:N0}\n" +
                       $"Retrocesos: {ia.Retrocesos:N0}\n" +
                       $"Tiempo: {reloj.Elapsed.TotalMilliseconds:N1} ms";

        if (resuelto)
        {
            MostrarTablero(ia.Tablero);
            MostrarInfo("¡Resuelto!\n\n" + datos, Color.Green);
        }
        else
        {
            MostrarInfo("No existe solución.\n\n" + datos, Color.Firebrick);
            MessageBox.Show("No existe solución para este tablero.", "Sin solución",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// La primera vez resuelve y guarda todos los pasos.
    /// Cada clic muestra un paso: amarillo = coloca un número, naranja = retrocede.
    /// </summary>
    private void BtnPaso_Click(object? sender, EventArgs e)
    {
        if (pasos == null)
        {
            var tablero = LeerTablero();
            if (!RevisarAntesDeResolver(tablero)) return;
            var ia = new Solucionador(tablero, cmbAlgoritmo.Text == "MRV");
            pasoResuelto = ia.Resolver();
            pasos = ia.Pasos;
            pasoActual = 0;
        }

        // Quitar el resaltado del paso anterior
        foreach (var caja in celdas)
            if (caja.BackColor != Color.Gainsboro) caja.BackColor = Color.White;

        if (pasoActual >= pasos.Count)
        {
            MostrarInfo(pasoResuelto ? "Fin: ¡tablero resuelto!" : "Fin: no existe solución para este tablero.",
                pasoResuelto ? Color.Green : Color.Firebrick);
            return;
        }

        Paso p = pasos[pasoActual++];
        var celda = celdas[p.Fila, p.Columna];
        escribiendoLaIA = true;
        celda.Text = p.Valor == 0 ? "" : p.Valor.ToString();
        celda.ForeColor = Color.Green;
        celda.BackColor = p.Valor == 0 ? Color.Orange : Color.Khaki;
        escribiendoLaIA = false;

        string accion = p.Valor == 0
            ? $"RETROCEDE: borra la fila {p.Fila + 1}, columna {p.Columna + 1} porque no llevó a una solución."
            : $"Coloca {p.Valor} en la fila {p.Fila + 1}, columna {p.Columna + 1}.";
        MostrarInfo($"Paso {pasoActual} de {pasos.Count}\n\n{accion}", Color.Black);
    }

    private void BtnValidar_Click(object? sender, EventArgs e)
    {
        var tablero = LeerTablero();
        if (!Solucionador.TableroValido(tablero, out string error))
            MostrarInfo("Hay un error:\n" + error, Color.Firebrick);
        else if (tablero.Cast<int>().Contains(0))
            MostrarInfo("Sin números repetidos hasta ahora.\nFaltan celdas por llenar.", Color.Black);
        else
            MostrarInfo("¡Correcto! El tablero está completo.", Color.Green);
    }

    /// <summary>Resuelve el mismo tablero con los dos algoritmos y compara intentos y retrocesos.</summary>
    private void BtnComparar_Click(object? sender, EventArgs e)
    {
        var tablero = LeerTablero();
        if (!RevisarAntesDeResolver(tablero)) return;

        var basico = new Solucionador(tablero, usarMRV: false);
        var mrv = new Solucionador(tablero, usarMRV: true);
        basico.Resolver();
        mrv.Resolver();

        MostrarInfo("Comparación en este tablero:\n\n" +
                    $"Básico:\n  {basico.Intentos:N0} intentos\n  {basico.Retrocesos:N0} retrocesos\n\n" +
                    $"MRV:\n  {mrv.Intentos:N0} intentos\n  {mrv.Retrocesos:N0} retrocesos\n\n" +
                    "MRV elige la celda con menos opciones, así se equivoca menos.", Color.Black);
    }
}
