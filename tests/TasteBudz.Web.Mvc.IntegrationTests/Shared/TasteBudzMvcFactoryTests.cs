using System.Reflection;

namespace TasteBudz.Web.Mvc.IntegrationTests.Shared;

public sealed class TasteBudzMvcFactoryTests
{
    [Fact]
    public void FactoriesUseIsolatedDatabaseDirectories()
    {
        using var first = new TasteBudzMvcFactory();
        using var second = new TasteBudzMvcFactory();

        var firstDirectory = Path.GetDirectoryName(GetDatabasePath(first));
        var secondDirectory = Path.GetDirectoryName(GetDatabasePath(second));

        Assert.NotNull(firstDirectory);
        Assert.NotNull(secondDirectory);
        Assert.NotEqual(firstDirectory, secondDirectory);
    }

    private static string GetDatabasePath(TasteBudzMvcFactory factory)
    {
        var field = typeof(TasteBudzMvcFactory).GetField("databasePath", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<string>(field.GetValue(factory));
    }
}
