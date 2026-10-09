# Registro de lotes por línea y modalidad

Implementación en `feature/scanning-line-container`. El cambio SQL está preparado,
pero no ejecutado. No se consultó ni modificó SQL Server y no se realizó despliegue.

## Contrato del frontend

Se conserva `POST /api/Scanning/start`, con `Content-Type: application/json`.
No cambia la respuesta exitosa ni se modifica la interpretación de `scans`.

| Campo | Contrato |
| --- | --- |
| `payrollNumber` | Entero, conserva su comportamiento anterior. |
| `expectedPartCode` | Código esperado. Obligatorio y no blanco en modalidades explícitas. |
| `requiredQuantity` | Conserva la cantidad y las reglas actuales de registro del lote. |
| `shopOrder` | String nullable. Obligatorio y no blanco en modalidad `shopOrder`. Se puede omitir en `container`. |
| `scans` | Lista con la misma estructura y significado que antes; no puede ser nula ni contener elementos nulos. |
| `lineId` | Entero nullable para compatibilidad. Obligatorio, positivo y correspondiente a una línea activa en modalidades explícitas. |
| `containerNumber` | String nullable. Obligatorio en `container`; omitir o enviar null en `shopOrder`. |
| `validationMode` | String nullable. Valores exactos: `shopOrder` o `container`. Las solicitudes nuevas deben enviarlo. |

Los IDs de línea de los ejemplos son ilustrativos. El frontend debe utilizar los IDs
reales del catálogo, no derivarlos del nombre de la pantalla ni asumir que Línea 4 tiene ID 4.
La modalidad se declara explícitamente por solicitud; no se deduce de campos vacíos,
del nombre de la línea ni del código de producto. No se agregó una configuración que
restrinja cada línea a una modalidad particular: una línea activa puede recibir ambas.

### Línea 4 Empaques / modalidad contenedor

```json
{
  "payrollNumber": 12345,
  "expectedPartCode": "ABC123",
  "requiredQuantity": 1,
  "scans": [
    {
      "scannedPartCode": "ABC123",
      "isCorrect": true,
      "scanDate": "2026-10-09T12:00:00Z",
      "releasedByPayroll": null
    }
  ],
  "lineId": 4,
  "containerNumber": "ABC123",
  "validationMode": "container"
}
```

El backend compara `expectedPartCode.Trim()` y `containerNumber.Trim()` con igualdad
ordinal, sensible a mayúsculas. No elimina ceros iniciales, guiones ni espacios internos.
Guarda los valores originales recibidos, incluidos sus espacios, para conservar evidencia.
Solo en modalidad `container` usa el código de parte recortado para buscar en catálogo.

Esta implementación supone que el código del contenedor debe coincidir literalmente
con el producto. No interpreta etiquetas compuestas ni números de serie: si la etiqueta
real tiene otro formato, se requiere definirlo antes de conectar ese lector.

No se agregan automáticamente dos detalles por escanear parte y contenedor; las lecturas
enviadas en `scans` conservan su significado actual. El contenedor no se cuenta como una
unidad adicional. El ejemplo representa un lote de una unidad, no una regla nueva de conteo.

### MicroChannel / modalidad Shop Order explícita

```json
{
  "payrollNumber": 12345,
  "expectedPartCode": "ABC123",
  "requiredQuantity": 1,
  "shopOrder": "SO-001",
  "scans": [
    {
      "scannedPartCode": "ABC123",
      "isCorrect": true,
      "scanDate": "2026-10-09T12:00:00Z",
      "releasedByPayroll": null
    }
  ],
  "lineId": 8,
  "validationMode": "shopOrder"
}
```

Shop Order sigue siendo obligatorio para esta modalidad. No se compara cada lectura
contra `expectedPartCode`, no se recalcula `isCorrect`, no se altera `releasedByPayroll`
y no se introduce una exigencia de cantidad completada. El handler conserva:

```text
ScannedQuantity = número de detalles recibidos con IsCorrect = true
Status = "completed"
```

Tanto el encabezado como los detalles se guardan mediante un único `SaveChangesAsync`.
Las validaciones nuevas ocurren antes de agregar el lote al contexto.

### Solicitud anterior, sin cambios en el frontend

```json
{
  "payrollNumber": 12345,
  "expectedPartCode": "ABC123",
  "requiredQuantity": 1,
  "shopOrder": "SO-001",
  "scans": [
    {
      "scannedPartCode": "ABC123",
      "isCorrect": true,
      "scanDate": "2026-10-09T12:00:00Z",
      "releasedByPayroll": null
    }
  ]
}
```

Si `validationMode`, `lineId` y `containerNumber` se omiten o son null, se utiliza la
compatibilidad anterior. Se exige que `shopOrder` no sea null, tal como exigía antes
MVC por su tipo no nullable. Se conservan los strings vacíos aceptados por ese contrato.
La modalidad explícita `shopOrder` sí rechaza strings vacíos o con espacios.

Enviar un `lineId` o `containerNumber` no nulo sin modalidad devuelve 400; no permite
eludir las reglas del proceso. Una modalidad vacía, desconocida o con distinta
capitalización también devuelve 400.

Los clientes anteriores siguen registrando `LineId = null` y `ContainerNumber = ""`.
Incluso si el catálogo tiene una sola coincidencia, no se infiere ni asigna una línea.

### Respuestas

Éxito: `200 OK`, conserva exactamente los nombres y mensaje anteriores:

```json
{
  "validationId": 123,
  "message": "Sesión iniciada correctamente."
}
```

Errores de las reglas nuevas: `400 Bad Request`, `ValidationProblemDetails` con
`errors` por campo. Ejemplo de la sección relevante:

```json
{
  "status": 400,
  "errors": {
    "containerNumber": [
      "El código del contenedor debe coincidir con el número de parte."
    ]
  }
}
```

## Catálogo y asociación de productos

No hay semillas, exportaciones ni datos del catálogo en el repositorio que permitan
demostrar su cobertura. No se hizo obligatoria la existencia universal del producto.
El script de revisión incluye indicadores de cobertura y duplicados, pero esos
indicadores no prueban por sí solos que todos los productos operativos estén catalogados.

| Situación | Resultado |
| --- | --- |
| Solicitud anterior, una coincidencia activa global | Conserva la asociación a esa parte; línea null. |
| Solicitud anterior, ninguna coincidencia activa | Guarda con `IdPartNumber = null`, como antes. |
| Solicitud anterior, varias coincidencias activas | Guarda con `IdPartNumber = null`; no elige arbitrariamente una fila. |
| Modalidad explícita, una coincidencia activa en la línea | Guarda `LineId` y el `IdPartNumber` de esa línea. |
| Modalidad explícita, varias coincidencias activas en la línea | Rechaza con 400; requiere corregir la ambigüedad del catálogo. |
| Modalidad explícita, código completamente ausente del catálogo | Permite operar: guarda la línea, el código y `IdPartNumber = null`. |
| Modalidad explícita, código catalogado pero sin asociación activa a la línea | Rechaza con 400; no lo asocia a otra línea ni a una parte inactiva. |

La última regla también aplica si solo existen filas inactivas. Si un producto está
dado de alta únicamente en otra línea, debe agregarse su asociación a la nueva línea
antes de usarlo allí; esto no impide registrar productos completamente no catalogados.
No se crean productos ni asociaciones automáticamente.

La coincidencia del catálogo usa la comparación de la base de datos existente. No se
cambia la collation de SQL Server. La comparación entre ambos códigos de contenedor
es ordinal en C#, independiente de esa collation.

## Historial

```http
GET /api/Traceability/validations
GET /api/Traceability/validations?lineId=4
```

Sin parámetro devuelve los últimos 50 registros globales. Con parámetro filtra por
`ContainerValidation.LineId` antes de ordenar por ID descendente y limitar a 50.
Agrega `lineId` y `lineName` a cada elemento; conserva los campos y el arreglo actuales.
`containerNumber` ya formaba parte de la respuesta.

Los históricos mantienen `lineId = null` y `lineName = null`; el frontend puede mostrar
“Línea no determinada”. No hay fallback a `PartNumber.IdLine`. Las líneas inactivas
siguen siendo consultables. Una línea positiva sin registros devuelve `[]`; valores
no positivos o un filtro no numérico devuelven 400.

## SQL pendiente de revisión

- `Infrastructure/Persistence/Scripts/20261009_01_review_scanning_schema.sql`:
  consultas de solo lectura sobre columnas, relaciones, índices y cobertura.
- `Infrastructure/Persistence/Scripts/20261009_02_add_validation_line.sql`:
  agrega únicamente `dbo.ContainerValidations.lineId INT NULL` y su FK hacia
  `dbo.Lines.Id`, sin cascada. No se ejecutó ninguno de los scripts.

El script de cambio usa una transacción, valida estructuras existentes y permite
repetición cuando ya existe una columna/relación compatible. No rellena líneas antiguas,
no crea un valor default, no toca IDs de productos, no recrea tablas y no crea índices.
Si encuentra estructuras incompatibles, aborta; no intenta corregirlas automáticamente.

Hay que revisar el esquema `dbo`, la base objetivo, los tipos y la nulabilidad real de
`containerNumber` y `shopOrder` antes de autorizar la ejecución. No hay migraciones EF
ni snapshot versionados; por eso se entrega un script incremental, sin generar una
migración inicial que pudiera intentar recrear las tablas existentes.

El modelo EF conserva explícitamente los cuatro índices de FK que ya existían por
convención (`PartNumbers.IdClient`, `PartNumbers.IdLine`, `ContainerValidations.IdPartNumber`
y `ScanDetails.IdValidation`). Se deshabilita la convención automática para evitar
agregar un índice nuevo de `LineId`. Esto no crea ni modifica índices en SQL Server.

Orden para una implementación futura autorizada: revisar estructura y respaldo,
aplicar el script, después actualizar la API y los clientes. La API nueva necesita
la columna incluso al atender clientes antiguos. Para revertir la aplicación,
conservar la columna adicional; eliminarla después de recibir registros perdería datos.

## Alcance y límites

- `validationMode` selecciona reglas de la solicitud; no se agregó una columna para
  persistir la modalidad ni se deduce una modalidad histórica.
- No se agrega unicidad del contenedor: el código coincide con el producto y puede
  repetirse entre lotes. Tampoco se incorpora idempotencia en este cambio.
- La FK garantiza que la línea exista; la asociación producto/línea se verifica en
  Application. No hay una FK compuesta ni protección contra escrituras externas
  incompatibles o cambios concurrentes del catálogo. No reasignar filas históricas
  de `PartNumbers` a otras líneas; registrar una nueva asociación cuando corresponda.
- Se conserva la interpretación de lecturas y cantidades previa, incluido confiar
  en `isCorrect`. Endurecer ese comportamiento requiere una tarea separada.
- No se cambió el flujo existente de aprobadores de calidad, cuyo handler estaba comentado.
- `LineId` proviene del cliente y se valida contra el catálogo; no autentica físicamente
  una estación. No se implementó control de permisos por línea.
- La consulta puede beneficiarse de un índice por línea cuando se verifique el esquema
  real, el volumen y el plan de ejecución. No se agregó anticipadamente.

## Pruebas

```powershell
dotnet build MalIdentificados.slnx --nologo -m:1
dotnet test Tests/Tests.csproj --no-build --no-restore --nologo
```

Las pruebas HTTP usan los controladores reales, model binding, MediatR y EF con SQLite
en memoria. No cargan `Program.cs`, cadenas de conexión ni `appsettings`. `EnsureCreated`
solo crea el esquema efímero de pruebas; no ejecuta migraciones ni se conecta a SQL Server.

Cubren JSON anterior y nuevo, respuesta exitosa, conservación del conteo/lecturas,
productos ausentes y duplicados, resolución por línea, líneas inexistentes e inactivas,
Shop Order condicional, coincidencia de contenedor, ausencia de guardados tras errores,
historial global/filtrado, históricos sin línea, nulabilidad y restricción de la FK.
No reemplazan la revisión ni la prueba autorizada del script sobre SQL Server: SQLite
no reproduce su collation, límites de longitud ni todas sus reglas DDL.

Resultado de la validación del 2026-10-09: compilación correcta con 0 errores y
0 advertencias; 39 pruebas correctas, 0 fallidas y 0 omitidas. `git diff --check`
sin errores. El SHA-256 del archivo local preexistente `API/appsettings.Development.json`
coincide antes y después del trabajo.

## Archivos de implementación

- `Domain/Entities/ContainerValidation.cs`
- `Domain/Entities/Lines.cs`
- `Infrastructure/Persistence/ApplicationDbContext.cs`
- `Application/Features/Scanning/Commands/StartValidationCommand.cs`
- `Application/Features/Scanning/Commands/StartValidationCommandHandler.cs`
- `Application/Features/Scanning/Commands/ValidationModes.cs` (nuevo)
- `Application/Features/Scanning/Commands/BatchValidationException.cs` (nuevo)
- `API/Controllers/ScanningController.cs`
- `Application/Features/Traceability/Queries/GetTraceabilityQuery.cs`
- `Application/Features/Traceability/Queries/GetTraceabilityQueryHandler.cs`
- `Application/Features/Traceability/Queries/GetValidations.cs`
- `API/Controllers/TraceabilityController.cs`
- `MalIdentificados.slnx` y el nuevo proyecto `Tests/`
- Los dos scripts SQL y este documento.

`PartNumber`, `ScanDetail` e `IApplicationDbContext` no necesitan cambios para este alcance.
El cambio preexistente de `API/appsettings.Development.json` queda intacto.
