namespace Halisaha.UnitTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void DomainAssemblyDoesNotReferenceInfrastructure()
    {
        var references = typeof(Halisaha.Domain.AssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name);

        Assert.DoesNotContain("Halisaha.Infrastructure", references);
    }
}
