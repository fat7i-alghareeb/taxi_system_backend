using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Taxi.Application.UnitTests.Infrastructure;

internal static class DbSetMockFactory
{
    public static DbSet<T> Create<T>(List<T> data)
        where T : class
    {
        var asyncEnum = new TestAsyncEnumerable<T>(data);
        var iq = (IQueryable<T>)asyncEnum;

        var dbSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();

        ((IQueryable<T>)dbSet).Provider.Returns(iq.Provider);
        ((IQueryable<T>)dbSet).Expression.Returns(iq.Expression);
        ((IQueryable<T>)dbSet).ElementType.Returns(iq.ElementType);
        ((IQueryable<T>)dbSet).GetEnumerator().Returns(iq.GetEnumerator());
        ((IAsyncEnumerable<T>)dbSet)
            .GetAsyncEnumerator(Arg.Any<CancellationToken>())
            .Returns(asyncEnum.GetAsyncEnumerator());

        return dbSet;
    }
}
