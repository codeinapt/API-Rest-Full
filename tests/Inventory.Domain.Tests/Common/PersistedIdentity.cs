namespace Inventory.Domain.Tests.Common;

internal static class PersistedIdentity
{
    public static TEntity Assign<TEntity>(TEntity entity, int id)
        where TEntity : class
    {
        typeof(TEntity).GetProperty("Id")!.SetValue(entity, id);

        return entity;
    }
}
