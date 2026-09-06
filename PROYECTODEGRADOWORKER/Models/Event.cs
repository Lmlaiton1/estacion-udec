using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicioSensorica.Models
{
	[Table("Events")] // <- nombre de la tabla en SQL Server. asegura que mapeamos a la tabla que ya existe en tu BD.
	public class Event
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		public string Date { get; set; } = string.Empty;

		[Required]
		public string Notification { get; set; } = string.Empty;

		[Required]
		public bool Visto { get; set; } = false;

        // Foreign keys

        [Required]
		public Guid DeviceId { get; set; }

		[Required]
		public Guid ImpactId { get; set; }

		[Required]
		public Guid System_TypeId { get; set; }
		
	}
}
