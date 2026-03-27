using CSharpFunctionalExtensions;
using Shared;

namespace MinimalTaxiService.Application.Database;

public interface ITransactionScope : IDisposable
{
    UnitResult<Error> Commit();

    UnitResult<Error> Rollback();

    new void Dispose();
}