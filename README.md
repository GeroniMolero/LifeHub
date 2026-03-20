# LifeHub

Guia principal y verificada para ejecutar el proyecto sin errores de contexto.

## Flujo real de trabajo

El flujo que se usa en este proyecto es hibrido:

1. Backend + base de datos en Docker.
2. Frontend en local con Angular CLI.

## Requisitos minimos

1. Docker Desktop funcionando.
2. Node.js 20+.
3. Dependencias de frontend instaladas una vez en `LifeHub-Frontend`.

Nota: no necesitas .NET SDK local para este flujo, porque el backend corre dentro del contenedor.

## Arranque (paso a paso)

### Terminal 1 - backend y base de datos

Desde la raiz del repo:

```powershell
docker compose -f docker-compose.dev.yml up -d mssql backend
```

Comprobar estado:

```powershell
docker compose -f docker-compose.dev.yml ps
```

Comprobar API:

```powershell
Invoke-WebRequest http://localhost:5000/swagger/v1/swagger.json -UseBasicParsing
```

Si va bien, devuelve status code `200`.

### Terminal 2 - frontend local

```powershell
cd LifeHub-Frontend
npm install
npm run start
```

Si ya tienes Angular CLI global y prefieres usarlo:

```powershell
cd LifeHub-Frontend
ng serve
```

## URLs de trabajo

1. Frontend: http://localhost:4200
2. Backend: http://localhost:5000
3. Swagger: http://localhost:5000/swagger

## Validaciones rapidas

1. Backend responde: `GET /swagger/v1/swagger.json` con 200.
2. Frontend responde: abrir http://localhost:4200 en navegador.

## Problemas comunes

### Puerto 4200 ocupado

Si aparece `Port 4200 is already in use`, significa que ya tienes un frontend arrancado o hay otro proceso usando ese puerto.

Opciones:

1. Cerrar el proceso que ya usa 4200 y volver a lanzar `npm run start`.
2. Levantar en otro puerto:

```powershell
npm run start -- --port 4201
```

### Backend no responde en 5000

Revisar logs:

```powershell
docker compose -f docker-compose.dev.yml logs backend --tail 100
```

### Limpiar y reiniciar Docker de desarrollo

```powershell
docker compose -f docker-compose.dev.yml down
docker compose -f docker-compose.dev.yml up -d mssql backend
```

## Estructura relevante

1. `LifeHub-Backend`: API .NET 8
2. `LifeHub-Frontend`: app Angular
3. `docker-compose.dev.yml`: flujo hibrido (backend+db)
4. `docker-compose.yml`: todo en Docker
