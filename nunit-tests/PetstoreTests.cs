using Petstore;

namespace PetstoreNunit;

public class PetstoreTests
{
    public static IEnumerable<string> Cases => PetstoreCases.Ids;

    [TestCaseSource(nameof(Cases))]
    public async Task CheckOperation(string caseId)
    {
        await PetstoreCases.Run(caseId);
    }
}
