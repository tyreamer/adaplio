using Adaplio.Api.Services;
using FluentAssertions;
using Xunit;

namespace Adaplio.Api.Tests.Services;

public class AliasServiceTests
{
    private readonly AliasService _aliasService;

    public AliasServiceTests()
    {
        _aliasService = new AliasService();
    }

    [Fact]
    public void GenerateClientAlias_ShouldReturnValidFormat()
    {
        // Act
        var alias = _aliasService.GenerateClientAlias(1, 1);

        // Assert
        alias.Should().StartWith("C-");
        alias.Should().HaveLength(6); // C- + 4 characters
        alias.Should().MatchRegex(@"^C-[0-9A-Z]{4}$");
    }

    [Fact]
    public void GenerateClientAlias_ShouldBeDeterministic()
    {
        // Act
        var alias1 = _aliasService.GenerateClientAlias(123, 456);
        var alias2 = _aliasService.GenerateClientAlias(123, 456);

        // Assert
        alias1.Should().Be(alias2);
    }

    [Fact]
    public void GenerateClientAlias_ShouldBeDifferent_ForDifferentInputs()
    {
        // Act
        var alias1 = _aliasService.GenerateClientAlias(1, 1);
        var alias2 = _aliasService.GenerateClientAlias(1, 2);
        var alias3 = _aliasService.GenerateClientAlias(2, 1);

        // Assert
        alias1.Should().NotBe(alias2);
        alias1.Should().NotBe(alias3);
        alias2.Should().NotBe(alias3);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(999, 999)]
    [InlineData(12345, 67890)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void GenerateClientAlias_ShouldHandleVariousInputs(int clientId, int trainerId)
    {
        // Act
        var alias = _aliasService.GenerateClientAlias(clientId, trainerId);

        // Assert
        alias.Should().StartWith("C-");
        alias.Should().MatchRegex(@"^C-[0-9A-Z]{4}$");
    }

    [Fact]
    public void GenerateUniqueCode_ShouldReturn8CharacterCode()
    {
        // Act
        var code = _aliasService.GenerateUniqueCode();

        // Assert
        code.Should().HaveLength(8);
        code.Should().MatchRegex(@"^[A-Z0-9]{8}$");
    }

    [Fact]
    public void GenerateUniqueCode_ShouldBeUnique()
    {
        // Act
        var code1 = _aliasService.GenerateUniqueCode();
        var code2 = _aliasService.GenerateUniqueCode();
        var code3 = _aliasService.GenerateUniqueCode();

        // Assert
        code1.Should().NotBe(code2);
        code1.Should().NotBe(code3);
        code2.Should().NotBe(code3);
    }

    [Fact]
    public void GenerateUniqueCode_ShouldGenerateMultipleDifferentCodes()
    {
        // Act - Generate 100 codes
        var codes = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            codes.Add(_aliasService.GenerateUniqueCode());
        }

        // Assert - All should be unique
        codes.Should().HaveCount(100);
    }

    [Fact]
    public void GenerateUniqueCode_ShouldOnlyContainAlphanumeric()
    {
        // Act
        for (int i = 0; i < 50; i++)
        {
            var code = _aliasService.GenerateUniqueCode();

            // Assert
            code.Should().MatchRegex(@"^[A-Z0-9]+$");
            code.Should().NotContain("-");
            code.Should().NotContain("_");
            code.Should().NotContain(" ");
        }
    }
}
