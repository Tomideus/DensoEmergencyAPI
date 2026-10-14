using DensoEmergencyAPI.Web.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace DensoEmergencyAPI.Web.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        // Inyección de dependencias para leer la configuración (appsettings.json)
        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Variable de entorno SUPABASE_CONNECTION_STRING no configurada.");
        }

        /// <summary>
        /// RF_01: Obtiene empleados que marcaron "Entrada" hoy y tienen un rol base asignado.
        /// </summary>
        public async Task<List<Empleado>> ObtenerEmpleadosPresentesHoyAsync()
        {
            var empleados = new List<Empleado>();
            const string sql = @"
                SELECT DISTINCT e.""UserID"", e.""EnrollNumber"", e.""Name"", e.""Surname"", e.""DepartID"", e.""id_rol_base""
                FROM ""Empleados"" e
                INNER JOIN ""Registros_Asistencia"" ra ON e.""EnrollNumber"" = ra.""EnrollNumber""
                WHERE ra.""InOutMode"" = 'Entrada' 
                  AND ra.""DateTime""::date = CURRENT_DATE
                  AND e.""id_rol_base"" IS NOT NULL";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                empleados.Add(new Empleado
                {
                    UserID = reader.GetString(0),
                    EnrollNumber = reader.GetString(1),
                    Name = reader.GetString(2),
                    Surname = reader.GetString(3),
                    DepartID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    IdRolBase = reader.IsDBNull(5) ? null : reader.GetInt32(5)
                });
            }

            return empleados;
        }

        /// <summary>
        /// RF_01 Restricción: Verifica si el operario ya tenía una asignación vigente para ese día.
        /// </summary>
        public async Task<bool> YaTieneAsignacionHoyAsync(string userId)
        {
            const string sql = @"
                SELECT COUNT(1) 
                FROM ""Asignacion_Roles"" 
                WHERE ""UserID"" = @UserID AND ""Fecha"" = CURRENT_DATE";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("UserID", userId);

            var count = (long)await cmd.ExecuteScalarAsync();
            return count > 0;
        }

        /// <summary>
        /// Guarda la asignación en la base de datos con TipoAsignacion = 'Automática'.
        /// </summary>
        public async Task<bool> GuardarAsignacionAutomaticaAsync(string userId, int rolId, DateTime fecha, string turno)
        {
            const string sql = @"
                INSERT INTO ""Asignacion_Roles"" (""UserID"", ""Fecha"", ""Turno"", ""RolID"", ""TipoAsignacion"")
                VALUES (@UserID, @Fecha, @Turno, @RolID, 'Automática')
                RETURNING ""AsignacionID""";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("UserID", userId);
            cmd.Parameters.AddWithValue("Fecha", fecha.Date);
            cmd.Parameters.AddWithValue("Turno", turno);
            cmd.Parameters.AddWithValue("RolID", rolId);

            try
            {
                var asignacionId = (int)await cmd.ExecuteScalarAsync();
                return asignacionId > 0;
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"[ERROR DB] No se pudo guardar la asignación para UserID {userId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper para determinar el turno basado en la hora.
        /// </summary>
        public string DeterminarTurno(DateTime horaRegistro)
        {
            if (horaRegistro.Hour >= 6 && horaRegistro.Hour < 14) return "Mañana";
            if (horaRegistro.Hour >= 14 && horaRegistro.Hour < 22) return "Tarde";
            return "Noche";
        }
    }
}