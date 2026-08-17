using BeautyCommerce.Infrastructure.Configurations;
using FluentAssertions;
using Xunit;

namespace BeautyCommerce.Tests.Configurations;

public class ConnectionStringHelperTests
{
    [Fact]
    public void Should_Return_Unchanged_When_Already_In_Host_Format()
    {
        const string input = "Host=localhost;Port=5432;Database=Db;Username=user;Password=pass";

        var result = ConnectionStringHelper.Normalize(input);

        result.Should().Be(input);
    }

    [Fact]
    public void Should_Convert_Postgres_Uri_To_Host_Format()
    {
        const string input = "postgres://myuser:mypass@dpg-example.render.com:5432/beautydb";

        var result = ConnectionStringHelper.Normalize(input);

        result.Should().Be(
            "Host=dpg-example.render.com;Port=5432;Database=beautydb;Username=myuser;Password=mypass;Ssl Mode=Require;Trust Server Certificate=true");
    }

    [Fact]
    public void Should_Url_Decode_Special_Characters_In_Credentials()
    {
        const string input = "postgresql://my%40user:p%23ss%2Fw0rd@host:5432/db";

        var result = ConnectionStringHelper.Normalize(input);

        result.Should().Contain("Username=my@user");
        result.Should().Contain("Password=p#ss/w0rd");
    }

    [Fact]
    public void Should_Default_Port_When_Missing_From_Uri()
    {
        const string input = "postgres://user:pass@host/db";

        var result = ConnectionStringHelper.Normalize(input);

        result.Should().Contain("Port=5432");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Return_Input_Unchanged_When_Null_Or_Whitespace(string? input)
    {
        var result = ConnectionStringHelper.Normalize(input);

        result.Should().Be(input);
    }

    [Fact]
    public void Should_Return_Original_String_When_Not_A_Recognized_Format()
    {
        const string input = "not-a-valid-connection-string";

        var result = ConnectionStringHelper.Normalize(input);

        result.Should().Be(input);
    }
}
