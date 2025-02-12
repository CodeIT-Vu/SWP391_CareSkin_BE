﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWP391_CareSkin_BE.Models
{
    [Table("PromotionProduct")]
    public class PromotionProduct
    {
        [Key]
        public int Product_Id { get; set; }

        [Key]
        public int Promotion_Id { get; set; }

        public virtual Product Product { get; set; }
        public virtual Promotion Promotion { get; set; }
    }
}
