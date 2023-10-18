using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace IdentityServer4.EntityFramework.Entities
{
    
    public partial class AspNetUserLogin 
    {
        [Key]
        [Required]
        public string LoginProvider { get; set; }
        [Key]
        [Required]
        public string ProviderKey { get; set; }
        
        public string ProviderDisplayName { get; set; }
        public string UserId { get; set; }

        public virtual AspNetUser User { get; set; }
    }
}
