using DensoEmergencyAPI.Web.Models;
using DensoEmergencyAPI.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DensoEmergencyAPI.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AsignacionController : ControllerBase
    {
        private readonly DatabaseService _dbService;

        public AsignacionController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        /// <summary>
        /// GET /api/asignacion/empleados-presentes
        /// Obtiene la lista de empleados presentes hoy con rol base asignado
        /// </summary>
        [HttpGet("empleados-presentes")]
        public async Task<ActionResult<List<Empleado>>> GetEmpleadosPresentes()
        {
            try
            {
                var empleados = await _dbService.ObtenerEmpleadosPresentesHoyAsync();
                return Ok(empleados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// POST /api/asignacion/ejecutar
        /// Ejecuta manualmente el proceso de asignación
        /// </summary>
        [HttpPost("ejecutar")]
        public async Task<ActionResult<string>> EjecutarAsignacionManual()
        {
            try
            {
                var empleadosPresentes = await _dbService.ObtenerEmpleadosPresentesHoyAsync();
                int asignacionesRealizadas = 0;

                foreach (var empleado in empleadosPresentes)
                {
                    bool yaAsignado = await _dbService.YaTieneAsignacionHoyAsync(empleado.UserID);
                    if (yaAsignado) continue;

                    if (empleado.IdRolBase.HasValue)
                    {
                        string turno = _dbService.DeterminarTurno(DateTime.Now);
                        bool exito = await _dbService.GuardarAsignacionAutomaticaAsync(
                            empleado.UserID,
                            empleado.IdRolBase.Value,
                            DateTime.Now,
                            turno
                        );

                        if (exito) asignacionesRealizadas++;
                    }
                }

                return Ok($"✅ Asignación manual completada. {asignacionesRealizadas} nuevas asignaciones registradas.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// GET /api/asignacion/estado
        /// Verifica el estado del sistema y la conexión a la base de datos
        /// </summary>
        [HttpGet("estado")]
        public async Task<ActionResult<object>> GetEstado()
        {
            try
            {
                var empleados = await _dbService.ObtenerEmpleadosPresentesHoyAsync();
                return Ok(new
                {
                    Estado = "Operativo",
                    EmpleadosPresentes = empleados.Count,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Estado = "Error", Mensaje = ex.Message });
            }
        }
    }
}