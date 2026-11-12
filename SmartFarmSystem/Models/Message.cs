using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartFarmSystem.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Subject")]
        [StringLength(200)]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "Message")]
        [StringLength(2000)]
        public string Body { get; set; }

        [Required]
        [Display(Name = "Sent By")]
        public string SenderId { get; set; }

        [ForeignKey("SenderId")]
        public virtual IdentityUser? Sender { get; set; }

        [Display(Name = "Sent To")]
        public string? ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual IdentityUser? Receiver { get; set; }

        [Display(Name = "Sent On")]
        public DateTime SentAt { get; set; } = DateTime.Now;

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Read On")]
        public DateTime? ReadAt { get; set; }

        [Display(Name = "Is Deleted By Sender")]
        public bool IsDeletedBySender { get; set; } = false;

        [Display(Name = "Is Deleted By Receiver")]
        public bool IsDeletedByReceiver { get; set; } = false;

        // For replies
        public int? ParentMessageId { get; set; }

        [ForeignKey("ParentMessageId")]
        public virtual Message? ParentMessage { get; set; }
    }
}