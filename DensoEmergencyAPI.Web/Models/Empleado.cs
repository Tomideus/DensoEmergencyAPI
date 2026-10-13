namespace DensoEmergencyAPI.Web.Models
{
    public class Empleado
    {
        public string UserID { get; set; } = string.Empty;
        public string EnrollNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public int? DepartID { get; set; }
        public int? IdRolBase { get; set; } // Clave para la asignación automática
        public string? PhotoURL { get; set; }
    }
}