namespace OutWit.Common.DependencyInjection.Tests.Mock
{
    #region Interfaces

    public interface IRequiredService
    {
        string Name { get; }
    }

    public interface IOptionalService
    {
        int Value { get; }
    }

    public interface IScopedMarkerService
    {
        Guid Id { get; }
    }

    #endregion

    #region Implementations

    public class RequiredServiceImpl : IRequiredService
    {
        public string Name => "Required";
    }

    public class OptionalServiceImpl : IOptionalService
    {
        public int Value => 42;
    }

    public class ScopedMarkerService : IScopedMarkerService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    #endregion
}
