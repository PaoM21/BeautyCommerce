# Sincronización de imágenes desde Google Drive

BeautyCommerce puede sincronizar fotos de producto desde una carpeta de Google Drive hacia Cloudinary (el almacenamiento real de imágenes) y asignarlas automáticamente al producto correcto según el SKU. Este documento explica cómo configurarlo y cómo usarlo día a día.

## Cómo funciona

1. El equipo sube fotos a una carpeta de Google Drive.
2. Desde el panel de Admin → Productos, se hace clic en **"Sincronizar imágenes desde Drive"**.
3. El backend lee la carpeta con la API de Google Drive, sube cada foto a Cloudinary, y la asigna al producto cuyo **SKU coincide con el nombre del archivo**.
4. El resultado (imágenes sincronizadas, archivos sin coincidencia, errores) se muestra en pantalla.

Volver a correr la sincronización es seguro: si una foto ya fue sincronizada antes, simplemente se reemplaza por la versión más reciente (no se duplica).

## Convención de nombres en Drive

El nombre del archivo (sin la extensión) debe ser el **SKU exacto del producto** tal como está en el sistema (Admin → Producto → Variantes).

| Archivo en Drive | Resultado |
|---|---|
| `SKU12345.jpg` | Foto principal del producto con ese SKU |
| `SKU12345-1.jpg` | Igual que el anterior — también cuenta como principal |
| `SKU12345-2.jpg` | Segunda foto del mismo producto (no principal) |
| `SKU12345-3.png` | Tercera foto del mismo producto |

Formatos soportados: cualquier imagen (`.jpg`, `.png`, `.webp`, etc). Otros archivos en la carpeta (PDFs, documentos) se ignoran automáticamente.

Si un archivo no coincide con ningún SKU, se reporta como "sin coincidencia" en el resultado — no se pierde, simplemente no se asigna hasta corregir el nombre.

## Configuración (una sola vez)

### 1. Google Cloud — cuenta de servicio para leer la carpeta de Drive

1. Entra a [Google Cloud Console](https://console.cloud.google.com/) y crea un proyecto (o usa uno existente).
2. Habilita la **Google Drive API** para ese proyecto (menú "APIs y servicios" → "Habilitar APIs y servicios" → buscar "Google Drive API").
3. Crea una **cuenta de servicio** ("APIs y servicios" → "Credenciales" → "Crear credenciales" → "Cuenta de servicio"). No necesita ningún rol especial de IAM.
4. Dentro de la cuenta de servicio, ve a "Claves" → "Agregar clave" → "Crear clave nueva" → tipo **JSON**. Se descarga un archivo `.json` — este es el que necesitas para `GoogleDrive:ServiceAccountJson`.
5. Copia el email de la cuenta de servicio (algo como `beautycommerce-sync@tu-proyecto.iam.gserviceaccount.com`).
6. En Google Drive, crea (o usa) la carpeta donde el equipo subirá las fotos, y **compártela con ese email** como "Lector" (Viewer). Sin este paso, la API no podrá ver los archivos aunque las credenciales sean correctas.
7. Copia el **ID de la carpeta**: es la parte final de la URL cuando abres la carpeta en el navegador, `https://drive.google.com/drive/folders/`**`ESTE_ID`**.

### 2. Cloudinary — almacenamiento final de las imágenes

1. Crea una cuenta gratuita en [cloudinary.com](https://cloudinary.com/) (el plan free no pide tarjeta de crédito).
2. En el Dashboard verás directamente **Cloud name**, **API Key** y **API Secret**.

### 3. Configurar las credenciales en el backend

**Nunca pegues estos valores en `appsettings.json` ni en ningún archivo que se suba a git** — son secretos. El proyecto ya tiene habilitado [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) para desarrollo local (`UserSecretsId` en `BeautyCommerce.API.csproj`).

Desde `backend/src/BeautyCommerce.API`, ejecuta:

```bash
dotnet user-secrets set "GoogleDrive:FolderId" "EL_ID_DE_TU_CARPETA"
dotnet user-secrets set "GoogleDrive:ServiceAccountJson" "$(cat ruta/al/archivo-descargado.json)"
dotnet user-secrets set "Cloudinary:CloudName" "tu-cloud-name"
dotnet user-secrets set "Cloudinary:ApiKey" "tu-api-key"
dotnet user-secrets set "Cloudinary:ApiSecret" "tu-api-secret"
```

En PowerShell, para el JSON de la cuenta de servicio (que es multilínea), es más fácil así:

```powershell
$json = Get-Content -Raw "ruta\al\archivo-descargado.json"
dotnet user-secrets set "GoogleDrive:ServiceAccountJson" "$json"
```

### 4. En producción

En el servidor de producción no hay `user-secrets` (es solo para desarrollo local). Usa **variables de entorno** con el mismo esquema de nombres que ya usa este proyecto para `Jwt__Key` (doble guion bajo en vez de `:`):

```
GoogleDrive__FolderId=...
GoogleDrive__ServiceAccountJson=...
Cloudinary__CloudName=...
Cloudinary__ApiKey=...
Cloudinary__ApiSecret=...
```

O, si el hosting lo soporta, un secret manager (Azure Key Vault, AWS Secrets Manager, etc.) inyectado como variables de entorno.

## Endpoint

`POST /api/media/sync-drive-images` — requiere rol `Admin`. Devuelve:

```json
{
  "updatedProducts": [
    { "productId": "...", "productName": "Labial Mate Rojo", "sourceFile": "SKU12345.jpg", "imageUrl": "https://res.cloudinary.com/...", "isPrimary": true }
  ],
  "unmatchedFiles": ["foto-sin-sku.jpg"],
  "errors": [],
  "filesProcessed": 2
}
```

## Alcance actual / próximos pasos

- La sincronización es **manual** (botón en Admin → Productos). Si más adelante se quiere automática (ej. cada noche), se puede agregar un `IHostedService` con un timer que llame al mismo `SyncImagesFromDriveCommand` — la lógica ya está separada para reutilizarse así.
- El emparejamiento es por **SKU de variante**, no por producto directamente, porque así está modelado hoy (`ProductVariant.SKU`); las imágenes siguen siendo a nivel de producto, compartidas entre variantes.
- No se valida el tamaño ni la resolución de las imágenes antes de subirlas — si hace falta (ej. mínimo 1000×1000px), se puede agregar como validación adicional en `SyncImagesFromDriveCommandHandler`.
