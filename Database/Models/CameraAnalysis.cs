﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Models;

public class CameraAnalysis
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid Id { get; set; }

	[Required]
	public Guid CameraId { get; set; }

	public virtual Camera Camera { get; set; } = null!;

	[Required]
	public DateTime DateTime { get; set; }

	[Required]
	public int CowCount { get; set; }

	[Required]
	public int CalfCount { get; set; }
}