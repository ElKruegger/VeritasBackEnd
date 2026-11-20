using LuxuryStore.Infrastructure.Persistence;

namespace LuxuryStore.UnitTests.Infrastructure;

public class LikePatternTests
{
    [Fact]
    public void Plain_text_becomes_a_contains_pattern()
    {
        LikePattern.Contains("oxford").ShouldBe("%oxford%");
    }

    [Fact]
    public void A_percent_sign_is_searched_literally_instead_of_matching_everything()
    {
        LikePattern.Contains("%").ShouldBe(@"%\%%");
    }

    [Fact]
    public void An_underscore_is_searched_literally_instead_of_matching_any_character()
    {
        LikePattern.Contains("a_b").ShouldBe(@"%a\_b%");
    }

    [Fact]
    public void A_backslash_is_escaped_first_so_it_cannot_cancel_another_escape()
    {
        LikePattern.Contains(@"\%").ShouldBe(@"%\\\%%");
    }

    [Fact]
    public void Surrounding_whitespace_is_ignored()
    {
        LikePattern.Contains("  zegna ").ShouldBe("%zegna%");
    }
}
