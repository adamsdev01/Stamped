using Microsoft.EntityFrameworkCore;
using Stamped.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Infrastructure.Data
{
    public class StampedDbContext : DbContext
    {
        public StampedDbContext(DbContextOptions<StampedDbContext> options) : base(options) { }

        public DbSet<Resume> Resumes => Set<Resume>();
        public DbSet<JobPosting> JobPostings => Set<JobPosting>();
        public DbSet<JobMatch> JobMatches => Set<JobMatch>();
        public DbSet<JobApplication> Applications => Set<JobApplication>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Resume>()
                .Property(r => r.Skills)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        }
    }
}
