# Lácteos Madeline

Sistema de escritorio para Windows de punto de venta e inventario de un negocio de lácteos. Está hecho en C# con WinForms (.NET 10) y SQLite. Funciona sin conexión a Internet y guarda los datos en la propia PC.

## Qué hace

| Módulo | Funciones |
|---|---|
| Punto de Venta | Búsqueda por nombre o código de barras, venta por unidad o por peso (1.250 lb), descuentos, cliente opcional, cobro en efectivo con cambio, tarjeta o transferencia, y ticket |
| Ventas Realizadas | Consulta por fecha o número, reimpresión del ticket y anulación con motivo (devuelve el inventario y cuadra la caja) |
| Clientes | Catálogo de clientes (nombre, DUI/NIT, teléfono, dirección, correo) |
| Control de Caja | Apertura, entradas y salidas, arqueo, cierre con diferencia y corte de caja imprimible |
| Productos e Inventario | Productos con código, unidad de venta, precios y stock mínimo; categorías; ajustes de existencia registrados |
| Compras | Registro de compras a proveedores (actualiza el costo promedio) y consulta |
| Mermas | Bajas por caducidad o daño, con valor de la pérdida |
| Reportes | Ventas, compras, inventario valorizado, bajo stock, caja, más vendidos, utilidad y ajustes, con exportación a Excel |
| Administración | Usuarios y PIN, datos del negocio para el ticket, respaldo externo (USB/OneDrive), respaldo manual y restauración |

### Usuarios y permisos

Cada persona entra con su nombre y un PIN de 4 a 6 números. El PIN se guarda cifrado. La primera vez que se abre el sistema (base de datos vacía), se pide crear el usuario **administrador**.

| Rol | Puede |
|---|---|
| Administrador | Todo |
| Cajero | Punto de venta, ventas realizadas, clientes y caja. Para anular una venta o aplicar un descuento necesita que un administrador escriba su PIN. |

Cada venta, caja, movimiento, compra, merma, ajuste y anulación registra quién la hizo. Tras 5 PIN incorrectos seguidos, el inicio de sesión se bloquea 30 segundos. Si alguien olvida su PIN, un administrador le asigna otro en Administración → Usuarios.

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

Para pasar los datos a otra PC, genere un respaldo en **Administración → Configuración y respaldos → Hacer respaldo ahora**, instale el programa en la otra PC y use **Restaurar un respaldo**. Antes de restaurar se guarda una copia de los datos que había.

Para protegerse de una falla del disco, elija en esa misma pantalla una **carpeta de copia externa** (una USB o una carpeta de OneDrive): cada respaldo automático se copiará también ahí.

## Usar el proyecto como plantilla para otro negocio

1. **Datos del negocio**: desde el propio programa, en Administración → Configuración: nombre, dirección, teléfono, NIT y ancho del ticket (32 caracteres para papel de 58 mm; 42 o 48 para 80 mm). Los valores iniciales, la moneda y la cultura están en [Helpers/ConfiguracionNegocio.cs](Helpers/ConfiguracionNegocio.cs).
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
| `Helpers/` | Dinero, cantidades, fechas, PIN, ticket y corte de caja, respaldos, exportación a Excel, log y configuración. |
| `Models/` | Clases de datos. |
| `Tests/` | Pruebas automatizadas (xUnit). |
| `scripts/` | Instalación y generación del paquete. |

### Reglas importantes del código

- **Dinero**: en C# es `decimal`; en la base se guarda en **centavos enteros** (`INTEGER`). Toda lectura o escritura pasa por `Helpers/Dinero.cs`. Los montos que escribe el usuario se leen con `Dinero.TryParse`, que acepta tanto `3.50` como `3,50`.
- **Cantidades**: en C# son `decimal` con hasta 3 decimales; en la base se guardan en **milésimas enteras** (1.250 lb = 1250), a través de `Helpers/Cantidades.cs`. Los productos con unidad "Unidad" solo admiten cantidades enteras.
- **Ventas**: no se editan ni se borran (lo impiden triggers): solo se anulan, una vez. Una venta anulada en la misma jornada deja de contar en el arqueo; si es de una jornada anterior y se pagó en efectivo, la devolución se registra como salida de la caja abierta.
- **Usuario actual**: `Services/Sesion.cs`. Los servicios lo usan para registrar quién hizo cada operación y para exigir un administrador en anulaciones y descuentos.
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
| v4 | Venta por peso (unidad y cantidades en milésimas) y códigos de producto |
| v5 | Usuarios, clientes, anulación de ventas, descuentos, costo por venta y configuración |

### Pruebas automatizadas

Están en `Tests/LacteosMadeline.Tests`. Cada prueba usa su propia base SQLite temporal y nunca toca la base del negocio. Cubren ventas (por unidad y por peso), descuentos, anulaciones, caja, compras y costo promedio, utilidad, inventario, mermas, usuarios y PIN, clientes, configuración, reportes, migraciones, respaldos y restauración, exportación a Excel, reglas de integridad y conversión de montos y fechas con distintas configuraciones regionales. Además, unas pruebas de humo abren todas las pantallas y diálogos, como administrador y como cajero.
