using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDev.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace FastDev.Web.Persistence
{ 
    public class AContext : DbContext
    {
        public AContext(DbContextOptions<AContext> options)
            : base(options)
        { }

        public DbSet<A> A { get; set; }
    }
}
