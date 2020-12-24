﻿using ApplicationCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config
{
    public class CatalogBrandConfiguration : IEntityTypeConfiguration<CatalogBrand>
    {
        public void Configure(EntityTypeBuilder<CatalogBrand> builder)
        {
            builder.HasKey(ci => ci.Id);

            builder.Property(cb => cb.Brand)
                .IsRequired()
                .HasMaxLength(100);
        }
    }
}
