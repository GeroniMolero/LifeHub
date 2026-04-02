# Este archivo queda como puntero.

La guia operativa y verificada del proyecto esta en:

- `README.md`

Para evitar contradicciones, usa solo `README.md` como fuente principal.
```

## 📚 Recursos Adicionales

- [Documentación Angular](https://angular.io/docs)
- [Documentación .NET](https://docs.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr)

## 💡 Tips Útiles

1. **Debugging en Visual Studio Code:**
   ```bash
   # Backend
   dotnet watch run
   
   # Frontend
   npm start
   ```

2. **Hot Reload:**
   - El backend se recarga automáticamente con `dotnet watch`
   - El frontend se recarga automáticamente con `ng serve`

3. **Base de Datos:**
   - Usa SQL Server Management Studio (SSMS) para inspeccionar la BD
   - Las migraciones se aplican automáticamente al iniciar

4. **Tokens JWT:**
   - Los tokens expiran después de 60 minutos (configurable)
   - Se envían en el header: `Authorization: Bearer <token>`

## ✅ Checklist de Verificación

Antes de considerar el setup completado:

- [ ] Backend compila sin errores
- [ ] BD se crea correctamente
- [ ] Frontend compila sin errores
- [ ] Puedes registrar un usuario
- [ ] Puedes iniciar sesión
- [ ] Puedes acceder a las páginas protegidas
- [ ] API responde en Swagger

---

**¡Listo para comenzar a desarrollar!** 🎉

Si tienes problemas, revisa los archivos README.md en cada carpeta.
