using com.jobsite.chat.Domain.Exceptions;
using com.jobsite.chat.Domain.Validation;

namespace com.jobsite.chat.Tests.Domain.Validation;

// Behaviors 1–4: Ensure guard helper.
public class EnsureTests
{
    // Behavior 1
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_NullOrBlank_ThrowsDomainException(string? value)
    {
        DomainException ex = Assert.Throws<DomainException>(() => Ensure.NotNullOrWhiteSpace(value, "field"));
        Assert.Contains("field", ex.Message);
    }

    // Behavior 1
    [Fact]
    public void NotNullOrWhiteSpace_MessageContainsSuppliedName()
    {
        DomainException ex = Assert.Throws<DomainException>(() => Ensure.NotNullOrWhiteSpace(null, "userId"));
        Assert.Contains("userId", ex.Message);
    }

    // Behavior 1
    [Fact]
    public void NotNullOrWhiteSpace_ValidPaddedValue_ReturnsTrimmed()
    {
        string result = Ensure.NotNullOrWhiteSpace("  ab  ", "field");
        Assert.Equal("ab", result);
    }

    // Behavior 2
    [Fact]
    public void MaxLength_LongerThanMax_ThrowsDomainException()
    {
        DomainException ex = Assert.Throws<DomainException>(() => Ensure.MaxLength("abcd", 3, "field"));
        Assert.Contains("field", ex.Message);
    }

    // Behavior 2
    [Fact]
    public void MaxLength_ExactlyMax_ReturnsValue()
    {
        string result = Ensure.MaxLength("abc", 3, "field");
        Assert.Equal("abc", result);
    }

    // Behavior 2
    [Fact]
    public void MaxLength_ShorterThanMax_ReturnsValue()
    {
        string result = Ensure.MaxLength("ab", 3, "field");
        Assert.Equal("ab", result);
    }

    // Behavior 3
    [Fact]
    public void NotEmpty_GuidEmpty_ThrowsDomainException()
    {
        DomainException ex = Assert.Throws<DomainException>(() => Ensure.NotEmpty(Guid.Empty, "roomId"));
        Assert.Contains("roomId", ex.Message);
    }

    // Behavior 3
    [Fact]
    public void NotEmpty_NonEmptyGuid_ReturnsValue()
    {
        Guid id = Guid.NewGuid();
        Guid result = Ensure.NotEmpty(id, "roomId");
        Assert.Equal(id, result);
    }

    // Behavior 4
    [Fact]
    public void NotNull_Null_ThrowsDomainException()
    {
        DomainException ex = Assert.Throws<DomainException>(() => Ensure.NotNull<object>(null, "author"));
        Assert.Contains("author", ex.Message);
    }

    // Behavior 4
    [Fact]
    public void NotNull_Instance_ReturnsSameInstance()
    {
        object instance = new object();
        object result = Ensure.NotNull(instance, "author");
        Assert.Same(instance, result);
    }
}
