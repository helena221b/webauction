namespace iep.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using System.Web;

    [Table("Auction")]
    public partial class Auction
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Auction()
        {
            Bids = new HashSet<Bid>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        public int AuctionTime { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? OpenedOn { get; set; }

        public DateTime? CompletedOn { get; set; }

        public decimal StartPrice { get; set; }

        public decimal? CurrentPrice { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        [Required]
        public byte[] IMG { get; set; }

        public int? CurUser { get; set; }

        [StringLength(255)]
        public string FullName { get; set; }
        //onaj kod koga je trenutno aukcija njegovo ime

        public int? FirstUser { get; set; }

        [NotMapped]
        public HttpPostedFileBase IMGtemp { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Bid> Bids { get; set; }

    
    }
}
