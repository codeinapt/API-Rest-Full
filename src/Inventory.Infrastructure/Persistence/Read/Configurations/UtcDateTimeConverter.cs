using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Inventory.Infrastructure.Persistence.Read.Configurations;

internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    applicationValue => applicationValue,
    databaseValue => DateTime.SpecifyKind(databaseValue, DateTimeKind.Utc));
