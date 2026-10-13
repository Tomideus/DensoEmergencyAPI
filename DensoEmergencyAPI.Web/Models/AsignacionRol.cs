namespace DensoEmergencyAPI.Web.Models
{
    public class AsignacionRol
    {
        public int AsignacionID { get; set; }
        public string UserID { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Turno { get; set; } = string.Empty; // "Mañana", "Tarde", "Noche"
        public int RolID { get; set; }
        public string TipoAsignacion { get; set; } = string.Empty; // "Automática" o "Manual"
    }
}