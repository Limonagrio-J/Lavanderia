# Cómo integrar el CRUD de Cliente en tu proyecto

## 1. Crear el proyecto base
```
dotnet new mvc -n PrototipoCRUD
cd PrototipoCRUD
dotnet add package Npgsql
```

## 2. Copiar los archivos de este zip
Copia las carpetas `Models`, `Data`, `Controllers` y `Views` dentro de `PrototipoCRUD`
(la carpeta `Views/Cliente` se agrega junto a `Views/Home`).

## 3. Registrar el DAO en `Program.cs`
Busca la línea `builder.Services.AddControllersWithViews();` y agrega debajo:
```csharp
builder.Services.AddScoped<PrototipoCRUD.Data.ClienteDAO>();
```

## 4. Cadena de conexión (sin subir tu contraseña a GitHub)
En `appsettings.json` deja un valor de ejemplo:
```json
"ConnectionStrings": {
  "Pepokita": "Host=localhost;Port=5432;Database=pepokita;Username=postgres;Password=CAMBIAR"
}
```
Y guarda tu contraseña real solo en tu equipo con user-secrets:
```
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Pepokita" "Host=localhost;Port=5432;Database=pepokita;Username=postgres;Password=TU_PASSWORD_REAL"
```
(Cada integrante del equipo ejecuta el segundo comando con su propia contraseña.)

## 5. Enlace en el menú
En `Views/Shared/_Layout.cshtml`, dentro del `<ul class="navbar-nav ...">`, agrega:
```html
<li class="nav-item">
    <a class="nav-link text-dark" asp-controller="Cliente" asp-action="Index">Clientes</a>
</li>
```

## 6. Ejecutar
```
dotnet run
```
Abre la URL que muestra la terminal y entra a `/Cliente`.

## 7. Subir a GitHub
```
git init
git add .
git commit -m "Prototipo CRUD de Cliente con MVC y PostgreSQL"
git branch -M main
git remote add origin https://github.com/USUARIO/REPOSITORIO.git
git push -u origin main
```
