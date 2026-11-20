using LuxuryStore.Application.Common;

namespace LuxuryStore.UnitTests.Application.Common;

public class NaturalStringComparerTests
{
    private static List<string> Sorted(params string[] values) =>
        values.Order(NaturalStringComparer.Instance).ToList();

    [Fact]
    public void Numbers_inside_labels_are_compared_as_numbers()
    {
        Sorted("UK 10", "UK 7", "UK 11", "UK 9").ShouldBe(["UK 7", "UK 9", "UK 10", "UK 11"]);
    }

    [Fact]
    public void Decimal_sizes_sit_between_their_neighbours()
    {
        Sorted("Collar 16", "Collar 15.5", "Collar 15", "Collar 16.5")
            .ShouldBe(["Collar 15", "Collar 15.5", "Collar 16", "Collar 16.5"]);
    }

    [Fact]
    public void Labels_with_the_same_number_are_ordered_by_their_text()
    {
        Sorted("EU 48 Regular", "EU 48 Long", "EU 46 Regular")
            .ShouldBe(["EU 46 Regular", "EU 48 Long", "EU 48 Regular"]);
    }

    [Fact]
    public void Plain_text_labels_still_sort_alphabetically()
    {
        Sorted("M", "L", "S").ShouldBe(["L", "M", "S"]);
    }

    [Fact]
    public void A_label_that_is_a_prefix_of_another_comes_first()
    {
        Sorted("M Tall", "M").ShouldBe(["M", "M Tall"]);
    }
}
