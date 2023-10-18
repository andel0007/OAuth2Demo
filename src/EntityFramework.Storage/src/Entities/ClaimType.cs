using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable 1591

namespace IdentityServer4.EntityFramework.Entities
{
    public partial class ClaimType 
    {

        public int Id { get; set; }

        public string Type { get; set; }
        
        public string Name { get; set; }
        
        public string DisplayName { get; set; }
        
        public string Description { get; set; }
        
    }
}
