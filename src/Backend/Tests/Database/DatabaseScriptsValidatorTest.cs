using System.Linq;
using Application.Database;
using DbUp.Engine;
using Xunit;

namespace Tests.Database;

public class DatabaseScriptsValidatorTest
{
    private readonly DatabaseScriptsValidator _target;

    public DatabaseScriptsValidatorTest()
    {
        _target = new DatabaseScriptsValidator();
    }
        
    [Fact]
    public void EnsureScriptNamingConventionsFollowed_NoDiscoveredScripts()
    {
        _target.EnsureScriptNamingConventionsFollowed(new SqlScript[0]);
    }
        
    [Theory]
    [InlineData("Scripts.0001_Script1.sql")]
    [InlineData("Scripts.0001_ScriptA.sql", "Scripts.0002_ScriptB.sql")]
    public void EnsureScriptNamingConventionsFollowed_AllDiscoveredScriptsMustStartWithANumber_ValidNames(params string[] discovered)
    {
        var sqlScripts = discovered.Select(CreateSqlScript);
        _target.EnsureScriptNamingConventionsFollowed(sqlScripts);
    }

    [Theory]
    [InlineData("Scripts.0001a_Script1.sql")]
    [InlineData("Scripts.000a_Script1.sql")]
    [InlineData("Scripts.001_Script1.sql")]
    [InlineData("Scripts.00001_Script1.sql")]
    [InlineData("Scripts.0001_ScriptA.sql", "Scripts.0001a_ScriptB.sql")]
    [InlineData("Scripts.0001a_ScriptA.sql", "Scripts.0001_ScriptB.sql")]
    public void EnsureScriptNamingConventionsFollowed_AllDiscoveredScriptsMustStartWithANumber_InvalidNames(params string[] discovered)
    {
        var sqlScripts = discovered.Select(CreateSqlScript);
        Assert.ThrowsAny<DatabaseValidationException>(() => _target.EnsureScriptNamingConventionsFollowed(sqlScripts));
    }
        
    [Theory]
    [InlineData("Scripts.0001_Script1.sql")]
    [InlineData("Scripts.0001_Script1.sql", "Scripts.0002_Script2.sql")]
    [InlineData("Scripts.0001_ScriptB.sql", "Scripts.0002_ScriptA.sql")]
    public void EnsureScriptNamingConventionsFollowed_AllDiscoveredScriptNumbersMustBeInDistinct_Valid(params string[] discovered)
    {
        var sqlScripts = discovered.Select(CreateSqlScript);
        _target.EnsureScriptNamingConventionsFollowed(sqlScripts);
    }
        
    [Theory]
    [InlineData("Scripts.0001_Script1.sql", "Scripts.0001_Script2.sql")]
    [InlineData("Scripts.0001_Script1.sql", "Scripts.0002_Script2.sql", "Scripts.0001_Script3.sql")]
    public void EnsureScriptNamingConventionsFollowed_AllDiscoveredScriptNumbersMustBeDistinct_Invalid(params string[] discovered)
    {
        var sqlScripts = discovered.Select(CreateSqlScript);
        Assert.ThrowsAny<DatabaseValidationException>(() => _target.EnsureScriptNamingConventionsFollowed(sqlScripts));
    }

    [Theory]
    [InlineData]
    [InlineData("Scripts.0001_Script1.sql")]
    [InlineData("Scripts.0001_Script1.sql", "Scripts.0002_Script2.sql")]
    [InlineData("Scripts.0001_Script1.sql", "Scripts.0002_Script2.sql", "Scripts.0003_Script3.sql")]
    public void EnsureScriptNamingConventionsFollowed_AllDiscoveredScriptNumbersMustBeContiguous_Valid(params string[] discovered)
    {
        var sqlScripts = discovered.Select(CreateSqlScript);
        _target.EnsureScriptNamingConventionsFollowed(sqlScripts);
    }

    [Theory]
    [InlineData("Scripts.0001_Script1.sql", "Scripts.0003_Script2.sql")]
    [InlineData("Scripts.0001_Script1.sql", "Scripts.0002_Script2.sql", "Scripts.0004_Script3.sql")]
    public void EnsureScriptNamingConventionsFollowed_AllDiscoveredScriptNumbersMustBeContiguous_Invalid(params string[] discovered)
    {
        var sqlScripts = discovered.Select(CreateSqlScript);
        Assert.ThrowsAny<DatabaseValidationException>(() => _target.EnsureScriptNamingConventionsFollowed(sqlScripts));
    }
        
    private SqlScript CreateSqlScript(string scriptName)
    {
        return new(scriptName, "");
    }
}