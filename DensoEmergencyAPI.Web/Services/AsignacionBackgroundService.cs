using DensoEmergencyAPI.Web.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DensoEmergencyAPI.Web.Services
{
    public class AsignacionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AsignacionBackgroundService> _logger;
        private readonly int _intervaloSegundos;

        public AsignacionBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AsignacionBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            // Lee el intervalo desde appsettings.json (default: 60 segundos)
            _intervaloSegundos = configuration.GetValue<int>("UpdateIntervalSeconds", 60);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Servicio de Asignación de Roles iniciado. Intervalo: {Intervalo}s", _intervaloSegundos);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EjecutarAsignacionAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error durante la asignación automática");
                }

                // Esperar el intervalo configurado
                await Task.Delay(TimeSpan.FromSeconds(_intervaloSegundos), stoppingToken);
            }

            _logger.LogInformation("🛑 Servicio de Asignación de Roles detenido");
        }

        private async Task EjecutarAsignacionAsync()
        {
            // Crear un scope para resolver servicios scoped/transient
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();

            _logger.LogInformation("🔄 Iniciando ciclo de asignación...");

            // 1. Obtener empleados presentes hoy
            var empleadosPresentes = await dbService.ObtenerEmpleadosPresentesHoyAsync();
            _logger.LogInformation("👥 Empleados presentes con rol base: {Count}", empleadosPresentes.Count);

            int asignacionesRealizadas = 0;

            // 2. Procesar cada empleado
            foreach (var empleado in empleadosPresentes)
            {
                // Verificar si ya tiene asignación hoy (RF_01: no duplicar)
                bool yaAsignado = await dbService.YaTieneAsignacionHoyAsync(empleado.UserID);
                if (yaAsignado)
                {
                    _logger.LogDebug("️  {Nombre} ya tiene asignación hoy", empleado.Name);
                    continue;
                }

                // 3. Asignar rol basado en id_rol_base
                if (empleado.IdRolBase.HasValue)
                {
                    string turno = dbService.DeterminarTurno(DateTime.Now);
                    bool exito = await dbService.GuardarAsignacionAutomaticaAsync(
                        empleado.UserID,
                        empleado.IdRolBase.Value,
                        DateTime.Now,
                        turno
                    );

                    if (exito)
                    {
                        asignacionesRealizadas++;
                        _logger.LogInformation("✅ Asignado rol {RolID} a {Nombre} ({Turno})",
                            empleado.IdRolBase, empleado.Name, turno);
                    }
                }
            }

            _logger.LogInformation("🎯 Ciclo completado. {Count} nuevas asignaciones registradas", asignacionesRealizadas);
        }
    }
}