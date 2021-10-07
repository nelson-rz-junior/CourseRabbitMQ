using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourseDataAccess.Models
{
    public enum Status { Pending, Processing, Approved, Error }

    public class Course
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Customer")]
        public string CustomerFullName { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Course")]
        public string ProductName { get; set; }

        [DataType(DataType.Currency)]
        [Column(TypeName = "money")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        public Guid TransactionId { get; set; }

        public bool Queued { get; set; }

        public bool Error { get; set; }

        [Display(Name = "Retry Count")]
        public int Retry { get; set; }

        public bool Processed { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}")]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}")]
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public string ErrorMessage { get; set; }
    }
}
