# Sudoku inteligente (versión sencilla)

Proyecto final de Inteligencia Artificial. La computadora resuelve un Sudoku 9×9 con
**Backtracking** y compara dos formas de elegir la siguiente celda:

- **Básico:** toma la primera celda vacía.
- **MRV (Minimum Remaining Values):** toma la celda vacía con **menos números posibles**.

## Archivos (solo 3)

| Archivo | Qué contiene |
|---|---|
| `SudokuSencillo/Solucionador.cs` | La IA: restricciones, backtracking, MRV, conteo de intentos y retrocesos (~140 líneas). |
| `SudokuSencillo/Form1.cs` | La ventana: tablero, botones, ejemplos, validación y paso a paso. |
| `SudokuSencillo/Program.cs` | Abre la ventana. |

## Cómo ejecutar

- **Rápido:** doble clic en `ejecutable/SudokuSencillo.exe` (no necesita instalar nada).
- **Visual Studio 2022:** abrir `SudokuSencillo.sln` y presionar F5.
- **Consola:** `dotnet run --project SudokuSencillo`

## Cómo se usa

1. Elige un **Ejemplo** (Fácil, Medio, Difícil, Vacío, Sin solución, Con error).
2. Elige el **Algoritmo** (Básico o MRV).
3. **Resolver:** llena el tablero y muestra intentos, retrocesos y tiempo.
4. **Paso a paso:** cada clic muestra una decisión. Amarillo = coloca un número, naranja = retrocede.
5. **Validar:** revisa si hay números repetidos.
6. **Comparar Básico vs MRV:** resuelve el tablero actual con los dos y muestra cuántos intentos hizo cada uno.

Solo se aceptan los números del 1 al 9. Las pistas originales (gris) no se pueden cambiar.

## Cómo funciona (explicación técnica)

**Como problema de restricciones (CSP):**
- Variables: las 81 celdas.
- Dominio: los números del 1 al 9.
- Restricciones: un número no se repite en su fila, su columna ni su subcuadro 3×3 (método `EsValido`).

**Backtracking (método `Resolver`):**
1. Elegir una celda vacía (`ElegirCelda`).
2. Probar cada número válido para esa celda (`Candidatos`). Cada prueba cuenta como un **intento**.
3. Si con ese número se puede terminar el tablero, ya está resuelto.
4. Si no, se borra el número (**retroceso**) y se prueba el siguiente.
5. Si ningún número sirve, se regresa a la celda anterior. Si se regresa hasta el inicio, **no existe solución**.

**Heurística MRV:** en lugar de tomar la primera celda vacía, la IA cuenta los candidatos de cada
celda y elige la que tiene menos. Si una celda tiene un solo candidato, el número es obligatorio y
no se equivoca. Si tiene cero, se da cuenta de inmediato de que va por mal camino.

## Resultados medidos

| Tablero | Básico: intentos | Básico: retrocesos | MRV: intentos | MRV: retrocesos |
|---|---:|---:|---:|---:|
| Fácil | 4,208 | 4,157 | 51 | 0 |
| Medio | 13,528 | 13,474 | 103 | 49 |
| Difícil | 49,558 | 49,498 | 10,101 | 10,041 |
| Vacío | 391 | 310 | 81 | 0 |
| Sin solución | 332 | 332 | 3 | 3 |
| Con error | lo rechaza la validación antes de buscar | | | |

**Conclusión:** MRV hace muchos menos intentos y retrocesos porque elige primero las celdas más
fáciles de decidir, igual que lo haría una persona. En el tablero fácil resuelve sin equivocarse ni una vez.
En tableros pequeños el tiempo es parecido porque MRV gasta un poco más en contar candidatos,
pero el número de intentos siempre es mucho menor.

## Pruebas realizadas

| Caso | Resultado esperado | Resultado obtenido |
|---|---|---|
| Fácil, Medio, Difícil | Se resuelven sin números repetidos | ✔ con los dos algoritmos |
| Vacío | Encuentra un Sudoku válido | ✔ |
| Sin solución | Mensaje "No existe solución" | ✔ |
| Con error (5 repetido) | Aviso de tablero inválido, no busca | ✔ |
| Escribir una letra o símbolo | Se rechaza con un mensaje | ✔ |
| Intentar cambiar una pista | No se puede (celda de solo lectura) | ✔ |
