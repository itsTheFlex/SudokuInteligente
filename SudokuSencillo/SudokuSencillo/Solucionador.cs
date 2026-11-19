namespace SudokuSencillo;

/// <summary>
/// Un paso de la búsqueda. Sirve para mostrar la solución "paso a paso".
/// Si Valor es 0 significa que la IA RETROCEDIÓ (borró esa celda).
/// </summary>
public record Paso(int Fila, int Columna, int Valor);

/// <summary>
/// Resuelve un Sudoku 9x9 con Backtracking.
/// Tiene dos formas de elegir la siguiente celda:
///   - Básico: toma la primera celda vacía (de izquierda a derecha, de arriba abajo).
///   - MRV:    toma la celda vacía con MENOS números posibles (Minimum Remaining Values).
/// </summary>
public class Solucionador
{
    public int[,] Tablero = new int[9, 9];   // 0 = celda vacía
    public bool UsarMRV;                     // false = básico, true = MRV
    public long Intentos;                    // cuántos números se colocaron a prueba
    public long Retrocesos;                  // cuántas veces hubo que borrar un número
    public List<Paso> Pasos = new();         // historial para el modo paso a paso

    private const int MaxPasosGuardados = 200_000; // para no llenar la memoria

    public Solucionador(int[,] tablero, bool usarMRV)
    {
        Tablero = (int[,])tablero.Clone(); // se trabaja sobre una copia
        UsarMRV = usarMRV;
    }

    // ------------------------------------------------------------------
    // RESTRICCIONES: un número no se puede repetir en su fila,
    // en su columna ni en su subcuadro 3x3.
    // ------------------------------------------------------------------
    public static bool EsValido(int[,] t, int fila, int col, int num)
    {
        for (int i = 0; i < 9; i++)
        {
            if (i != col && t[fila, i] == num) return false;  // fila
            if (i != fila && t[i, col] == num) return false;  // columna
        }

        int f0 = fila / 3 * 3, c0 = col / 3 * 3;              // esquina del subcuadro
        for (int f = f0; f < f0 + 3; f++)
            for (int c = c0; c < c0 + 3; c++)
                if ((f != fila || c != col) && t[f, c] == num) return false;

        return true;
    }

    /// <summary>Revisa que los números que ya están no se repitan (tablero de entrada válido).</summary>
    public static bool TableroValido(int[,] t, out string error)
    {
        for (int f = 0; f < 9; f++)
            for (int c = 0; c < 9; c++)
                if (t[f, c] != 0 && !EsValido(t, f, c, t[f, c]))
                {
                    error = $"El {t[f, c]} de la fila {f + 1}, columna {c + 1} está repetido.";
                    return false;
                }
        error = "";
        return true;
    }

    /// <summary>Números que todavía se pueden poner en una celda (su "dominio").</summary>
    private List<int> Candidatos(int fila, int col)
    {
        var lista = new List<int>();
        for (int num = 1; num <= 9; num++)
            if (EsValido(Tablero, fila, col, num)) lista.Add(num);
        return lista;
    }

    // ------------------------------------------------------------------
    // Elegir la siguiente celda vacía. Aquí está la diferencia entre
    // el backtracking básico y el que usa la heurística MRV.
    // Devuelve false si ya no hay celdas vacías.
    // ------------------------------------------------------------------
    private bool ElegirCelda(out int fila, out int col)
    {
        fila = -1; col = -1;
        int menosCandidatos = 10;

        for (int f = 0; f < 9; f++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (Tablero[f, c] != 0) continue;

                if (!UsarMRV)
                {
                    fila = f; col = c;      // BÁSICO: la primera vacía y listo
                    return true;
                }

                int cantidad = Candidatos(f, c).Count;  // MRV: la de menos candidatos
                if (cantidad < menosCandidatos)
                {
                    menosCandidatos = cantidad;
                    fila = f; col = c;
                }
            }
        }
        return fila != -1;
    }

    // ------------------------------------------------------------------
    // BACKTRACKING (recursivo)
    // 1. Elegir una celda vacía.
    // 2. Probar cada número válido.
    // 3. Si con ese número se puede terminar el tablero -> ¡listo!
    // 4. Si no, borrarlo (retroceder) y probar el siguiente.
    // ------------------------------------------------------------------
    public bool Resolver()
    {
        if (!ElegirCelda(out int fila, out int col))
            return true; // no quedan celdas vacías: tablero resuelto

        // Si la celda no tiene candidatos, la lista queda vacía y se retrocede de inmediato.
        foreach (int num in Candidatos(fila, col))
        {
            Tablero[fila, col] = num;
            Intentos++;
            Guardar(new Paso(fila, col, num));

            if (Resolver()) return true;

            Tablero[fila, col] = 0;  // no funcionó: retroceder
            Retrocesos++;
            Guardar(new Paso(fila, col, 0));
        }
        return false; // ningún número sirvió aquí
    }

    private void Guardar(Paso paso)
    {
        if (Pasos.Count < MaxPasosGuardados) Pasos.Add(paso);
    }
}
