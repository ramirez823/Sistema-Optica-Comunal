# Sistema de Gestión - Óptica Comunal

Sistema administrativo y clínico para red de ópticas, desarrollado en **.NET 8** con arquitectura limpia (Clean Architecture).

##  Requisitos Previos

Antes de ejecutar el proyecto, asegúrate de tener instalado:
* **.NET 8 SDK** (Recomendado: v8.0.x)
* **SQL Server** (Express o Developer)
* **Visual Studio 2022** (con carga de trabajo ASP.NET y desarrollo web)

##  Instalación y Puesta en Marcha

Sigue estos pasos estrictos para levantar el entorno de desarrollo:

### 1. Configurar Base de Datos
El proyecto usa **Entity Framework Core Code First**. No necesitas crear la BD manualmente, el código lo hará por ti.

1. Abre el archivo `OC.Web/appsettings.json`.
2. Verifica la cadena de conexión `DefaultConnection`.
   * Si usas SQL Express: `Server=.\\SQLEXPRESS;...`
   * Si usas LocalDB: `Server=(localdb)\\mssqllocaldb;...`
   * **Importante:** Asegúrate de que `Trusted_Connection=True` y `TrustServerCertificate=True`.

### 2. Generar la Base de Datos
Abre la **Consola del Administrador de Paquetes** en Visual Studio:

1. Selecciona **Proyecto predeterminado:** `OC.Data`.
2. Ejecuta el comando:
   ```powershell
   Update-Database -StartupProject OC.Web