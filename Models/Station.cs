using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MRIV.Models;

[Table("Station")]
public partial class Station
{
    [Key]
    [Column("StationID")]
    public int StationId { get; set; }

    [Column("Station_Name")]
    [StringLength(50)]
    [Unicode(false)]
    public string? StationName { get; set; }
}
