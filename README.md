# Lácteos Madeline

Sistema de escritorio para Windows de punto de venta e inventario de un negocio de lácteos: ventas con ticket, control de caja con arqueo, compras a proveedores, mermas, reportes y respaldos automáticos.

Está hecho en C# con WinForms (.NET 10) y SQLite. Funciona sin conexión a Internet y guarda los datos en la propia PC.

## Instalar en una PC

Hay dos formas. Ninguna necesita permisos de administrador.

### A. Con el paquete .zip (recomendado para el negocio)

No necesita tener .NET instalado.

1. En una PC de desarrollo, ejecute `scripts\publicar.cmd`. Corre las pruebas y genera `dist\LacteosMadeline-<versión>-win-x64.zip`.
2. Lleve el .zip a la otra PC (USB, correo, etc.) y descomprímalo.
3. Haga doble clic en **`Instalar.cmd`**.

### B. Desde el repositorio

1. Clone o descargue el repositorio.
2. Haga doble clic en **`Instalar.cmd`**. Si falta el SDK de .NET 10, el instalador ofrece instalarlo con `winget` (o se descarga en <https://dotnet.microsoft.com/download/dotnet/10.0>). Luego compila e instala.

En los dos casos el programa queda en `%LOCALAPPDATA%\Programs\LacteosMadeline`, con accesos directos en el Escritorio y en el menú Inicio, y se abre al terminar.

| Acción | Cómo |
|---|---|
| Actualizar | Ejecutar `Instalar.cmd` de la versión nueva. Los datos se conservan y la base se actualiza sola, con un respaldo previo. |
| Desinstalar | `Desinstalar.cmd`. Quita el programa y los accesos directos, pero conserva los datos. |

Requisitos: Windows 10 u 11 de 64 bits.

## Dónde están los datos

Todo vive en `%LOCALAPPDATA%\LacteosMadeline` (separado del programa, así que reinstalar no lo borra):

| Archivo / carpeta | Contenido |
|---|---|
| `LacteosMadeline.db` | Base de datos SQLite |
| `Respaldos\` | Respaldo automático en cada cierre de caja (se conservan los 60 más recientes) |
| `Respaldos\Migraciones\` | Respaldo previo a cada actualización de la estructura (no se borran) |
| `logs.txt` | Registro de errores para soporte técnico |

Los datos son por usuario de Windows. Si hay que usar otra ruta (por ejemplo, para pruebas), defina la variable de entorno `LACTEOS_DB_PATH` con la ruta completa del archivo `.db`.

Para pasar los datos a otra PC, genere un respaldo con el botón **Respaldo de datos** y, con el programa cerrado, copie ese archivo como `LacteosMadeline.db` en la carpeta de datos de la otra PC.

## Usar el proyecto como plantilla para otro negocio

1. **Datos del negocio y formato regional**: cambie [Helpers/ConfiguracionNegocio.cs](Helpers/ConfiguracionNegocio.cs). Ahí están el nombre y los datos del ticket, el ancho del papel (32 caracteres para 58 mm; 42 o 48 para 80 mm), la cultura y el símbolo de moneda.
2. **Nombre y versión del programa**: en [LacteosMadeline.csproj](LacteosMadeline.csproj) (`Product`, `Company`, `Version`). Suba `Version` en cada entrega: es la que aparece en el nombre del .zip.
3. **Colores y tipografía**: [Forms/Tema.cs](Forms/Tema.cs).
4. **Rutas de instalación**: al inicio de [scripts/instalar.ps1](scripts/instalar.ps1).

## Desarrollo

Requiere el SDK de .NET 10. La versión mínima está fijada en [global.json](global.json).

```
dotnet build LacteosMadeline.slnx
dotnet test LacteosMadeline.slnx
dotnet run --project LacteosMadeline.csproj
```

### Estructura

| Carpeta | Responsabilidad |
|---|---|
| `Forms/` | Pantallas. Solo capturan datos y muestran resultados, y siempre llaman a servicios, nunca a repositorios. |
| `Services/` | Reglas del negocio y validaciones. Devuelven `(Exitoso, Mensaje, …)` en lugar de lanzar excepciones a la pantalla. |
| `Repositories/` | Acceso a SQLite. Las operaciones de varios pasos (venta, compra, merma, cierre, ajuste) van en una sola transacción. |
| `Data/` | Conexión, creación y migraciones de la base (`Data/Migraciones/`). |
| `Helpers/` | Dinero, fechas, ticket, respaldos, log, configuración del negocio. |
| `Models/` | Clases de datos. |
| `Tests/` | Pruebas automatizadas (xUnit). |
| `scripts/` | Instalación y generación del paquete. |

### Reglas importantes del código

- **Dinero**: en C# es `decimal`; en la base se guarda en **centavos enteros** (`INTEGER`). Toda lectura o escritura pasa por `Helpers/Dinero.cs`. Los montos que escribe el usuario se leen con `Dinero.TryParse`, que acepta tanto `3.50` como `3,50`.
- **Fechas**: en la base son texto `yyyy-MM-dd HH:mm:ss` con cultura invariante, siempre a través de `Helpers/Fechas.cs`.
- **Errores de negocio**: los repositorios lanzan `ReglaNegocioException` (existencia insuficiente, caja cerrada…). Los servicios la convierten en un mensaje para el usuario. No se usa `InvalidOperationException` para esto, porque .NET también la lanza por fallos técnicos.
- **Existencias**: solo cambian por ventas, compras, mermas o ajustes. Los ajustes quedan registrados en la tabla `AjustesInventario`. Modificar un producto nunca cambia su existencia.
- **Integridad**: la propia base rechaza datos inválidos mediante triggers, índices únicos, llaves foráneas y `CHECK`. Es la última línea de defensa.

### Cambios en la estructura de la base de datos

La versión se guarda en `PRAGMA user_version` y se actualiza al iniciar el programa (`Data/DatabaseInitializer.cs` y `Data/MigradorBaseDatos.cs`):

- Antes de migrar una base con datos se genera un respaldo verificado en `Respaldos\Migraciones`.
- Cada migración se aplica en una sola transacción: si falla, se revierte completa y la base queda en la versión anterior.
- Si la base pertenece a una versión más nueva del programa, este no la abre.
- Para cambiar la estructura, cree una clase en `Data/Migraciones/` y agréguela al final de la lista `Migraciones` con el número siguiente. **Nunca modifique una migración ya entregada.**

| Versión | Cambio |
|---|---|
| v1 | Estructura base: productos, ventas, compras, caja, mermas, índices y reglas de integridad |
| v2 | Montos en centavos enteros |
| v3 | Registro de ajustes de inventario |

### Pruebas automatizadas

Están en `Tests/LacteosMadeline.Tests`. Cada prueba usa su propia base SQLite temporal y nunca toca la base del negocio. Cubren ventas, caja, compras, inventario, mermas, reportes, migraciones, respaldos, reglas de integridad y conversión de montos y fechas con distintas configuraciones regionales. Además, una prueba de humo abre todas las pantallas.
