using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MRIV.Models;

[Table("tbrequisition")]
public partial class Tbrequisition
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("addedby")]
    [StringLength(10)]
    [Unicode(false)]
    public string? Addedby { get; set; }

    [Column("issued_by_approval")]
    [StringLength(50)]
    [Unicode(false)]
    public string? IssuedByApproval { get; set; }

    [Column("req_officer_approval")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ReqOfficerApproval { get; set; }

    [Column("receiving_officer_approval")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ReceivingOfficerApproval { get; set; }

    [Column("fwdapprove")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Fwdapprove { get; set; }

    [Column("req_officer")]
    [StringLength(100)]
    [Unicode(false)]
    public string? ReqOfficer { get; set; }

    [Column("rec_officer")]
    [StringLength(100)]
    [Unicode(false)]
    public string? RecOfficer { get; set; }

    [Column("code1")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Code1 { get; set; }

    [Column("item1")]
    [StringLength(8000)]
    [Unicode(false)]
    public string? Item1 { get; set; }

    [Column("issued1", TypeName = "numeric(18, 0)")]
    public decimal? Issued1 { get; set; }

    [Column("issuepoint")]
    [StringLength(150)]
    [Unicode(false)]
    public string? Issuepoint { get; set; }

    [Column("usepoint")]
    [StringLength(150)]
    [Unicode(false)]
    public string? Usepoint { get; set; }

    [Column("issuedate", TypeName = "datetime")]
    public DateTime? Issuedate { get; set; }

    [Column("sysdate", TypeName = "datetime")]
    public DateTime? Sysdate { get; set; }

    [Column("req_username")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ReqUsername { get; set; }

    [Column("external")]
    [StringLength(100)]
    [Unicode(false)]
    public string? External { get; set; }

    [Column("sup_comments")]
    [StringLength(255)]
    [Unicode(false)]
    public string? SupComments { get; set; }

    [Column("rec_comments")]
    [StringLength(255)]
    [Unicode(false)]
    public string? RecComments { get; set; }

    [Column("externalmail")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Externalmail { get; set; }

    [Column("remarks1")]
    [StringLength(8000)]
    [Unicode(false)]
    public string? Remarks1 { get; set; }

    [Column("rejected_sup")]
    [StringLength(155)]
    [Unicode(false)]
    public string? RejectedSup { get; set; }

    [Column("rejected_rec")]
    [StringLength(155)]
    [Unicode(false)]
    public string? RejectedRec { get; set; }

    [Column("applicant")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Applicant { get; set; }

    [Column("recdate", TypeName = "datetime")]
    public DateTime? Recdate { get; set; }

    [Column("sup_date", TypeName = "datetime")]
    public DateTime? SupDate { get; set; }

    [Column("admin")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Admin { get; set; }

    [Column("admin_officer")]
    [StringLength(50)]
    [Unicode(false)]
    public string? AdminOfficer { get; set; }

    [Column("fwd_date", TypeName = "datetime")]
    public DateTime? FwdDate { get; set; }

    [Column("adminapprove")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Adminapprove { get; set; }

    [Column("admin_date", TypeName = "datetime")]
    public DateTime? AdminDate { get; set; }

    [Column("adminapprove_date", TypeName = "datetime")]
    public DateTime? AdminapproveDate { get; set; }

    [Column("receivedby")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Receivedby { get; set; }

    [Column("collectorid", TypeName = "numeric(18, 0)")]
    public decimal? Collectorid { get; set; }

    [Column("collectorname")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Collectorname { get; set; }
}
