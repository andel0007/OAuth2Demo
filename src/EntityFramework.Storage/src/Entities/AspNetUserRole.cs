using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;



namespace IdentityServer4.EntityFramework.Entities
{
    public partial class AspNetUserRole 
    {
        public string UserId { get; set; }
        public string RoleId { get; set; }

        public virtual AspNetRole Role { get; set; }
        public virtual AspNetUser User { get; set; }
    }
}
