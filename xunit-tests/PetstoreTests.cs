using Petstore;

namespace PetstoreXunit;

public class PetstoreTests
{
    public static IEnumerable<object[]> Cases => PetstoreCases.Ids.Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task CheckOperation(string caseId)
    {
        await PetstoreCases.Run(caseId);
    }
}
