using System.Text.RegularExpressions;
using DbUp.Engine;

namespace Application.Database
{
    public class DatabaseScriptsValidator
    {
        private static readonly Regex ScriptNamePattern = new(@"\.(?<scriptNumber>\d{4})_");
        
        public void EnsureScriptNamingConventionsFollowed(IEnumerable<SqlScript> discoveredScripts)
        {
            var scripts = discoveredScripts
                .Select(s => new { Script = s, Match = ScriptNamePattern.Match(s.Name) })
                .ToArray();
                
            if (scripts.Any(x => !x.Match.Success))
                throw new DatabaseValidationException($"Script name(s) do not match expected pattern: {string.Join(", ", scripts.Select(x => x.Script.Name))}");

            var scriptNumbers = scripts.Select(x => int.Parse(x.Match.Groups["scriptNumber"].Value)).ToArray();

            if (scriptNumbers.Length != scriptNumbers.Distinct().Count())
                throw new DatabaseValidationException("Duplicate script numbers");

            if (scriptNumbers.LastOrDefault() != scriptNumbers.Length)
                throw new DatabaseValidationException("Script numbers must be contiguous");
        }
    }
}