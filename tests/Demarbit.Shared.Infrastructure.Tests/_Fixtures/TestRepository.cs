using Demarbit.Shared.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Demarbit.Shared.Infrastructure.Tests._Fixtures;

public class TestRepository(DbContext context) : RepositoryBase<TestAggregate, Guid>(context);
