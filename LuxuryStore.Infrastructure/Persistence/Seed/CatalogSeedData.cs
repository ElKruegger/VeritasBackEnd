namespace LuxuryStore.Infrastructure.Persistence.Seed;

/// <summary>
/// Demo catalog for development and the portfolio deployment: every
/// navigation category from brief section 6 (Home is a page, not a
/// category) and the brand strip from 7.1, so the storefront has real
/// content to render on every page.
///
/// PRICES ARE ILLUSTRATIVE (USD, rounded) — close to real-world retail to
/// look credible, but not quotes. Images are bucket object paths
/// ("products/&lt;slug&gt;/01.jpg"); until real photos are uploaded and
/// Storage:PublicBaseUrl is set, ImageUrlResolver renders placeholders.
///
/// Plain records, no EF types: this file is data only. CatalogSeeder turns
/// it into entities.
/// </summary>
internal static class CatalogSeedData
{
    internal sealed record CategorySeed(string Slug, string Name, string Description);

    internal sealed record BrandSeed(string Slug, string Name);

    internal sealed record VariantSeed(string Sku, string Label, int Stock, decimal? PriceOverride = null);

    internal sealed record ProductSeed(
        string Slug,
        string Name,
        string BrandSlug,
        string CategorySlug,
        decimal BasePrice,
        string Description,
        VariantSeed[] Variants,
        string[] ImageViews);

    // Default gallery: front, detail, styled. Alt text is generated as
    // "<brand> <product name> — <view>" (see CatalogSeeder).
    private static readonly string[] Gallery = ["front view", "detail", "styled"];

    public static readonly CategorySeed[] Categories =
    [
        new("clothing", "Clothing", "Tailoring, shirting and knitwear cut from the world's finest cloths."),
        new("footwear", "Footwear", "Goodyear-welted Oxfords and loafers, made to be resoled for decades."),
        new("watches", "Watches", "Mechanical timepieces from the great Swiss and Japanese maisons."),
        new("accessories", "Accessories", "Leather goods, eyewear and the finishing details of a wardrobe."),
        new("perfumes", "Perfumes", "Signature fragrances, from fresh citrus to deep oud."),
        new("outlet", "Outlet", "Previous-season pieces at reduced prices, while sizes last.")
    ];

    public static readonly BrandSeed[] Brands =
    [
        new("rolex", "Rolex"),
        new("omega", "Omega"),
        new("grand-seiko", "Grand Seiko"),
        new("seiko", "Seiko"),
        new("hugo-boss", "Hugo Boss"),
        new("zegna", "Zegna"),
        new("loro-piana", "Loro Piana"),
        new("brunello-cucinelli", "Brunello Cucinelli"),
        new("crockett-jones", "Crockett & Jones"),
        new("john-lobb", "John Lobb"),
        new("montblanc", "Montblanc"),
        new("persol", "Persol"),
        new("tom-ford", "Tom Ford"),
        new("creed", "Creed")
    ];

    public static readonly ProductSeed[] Products =
    [
        // --- Clothing ---------------------------------------------------
        new("zegna-trofeo-wool-suit", "Trofeo Wool Two-Piece Suit", "zegna", "clothing", 3995m,
            "A single-breasted two-piece in Zegna's own Trofeo wool, woven in Trivero. Half-canvassed construction, natural shoulder and a softly suppressed waist.",
            [new("ZEG-TRO-48R", "EU 48 Regular", 3), new("ZEG-TRO-50R", "EU 50 Regular", 4), new("ZEG-TRO-52R", "EU 52 Regular", 4), new("ZEG-TRO-54R", "EU 54 Regular", 2)],
            Gallery),
        new("hugo-boss-slim-virgin-wool-suit", "Slim-Fit Virgin Wool Suit", "hugo-boss", "clothing", 895m,
            "A sharp, modern slim-fit suit in stretch virgin wool with a notch lapel and flat-front trousers. Equally at home in the boardroom and at dinner.",
            [new("HB-SUIT-38R", "US 38 Regular", 5), new("HB-SUIT-40R", "US 40 Regular", 6), new("HB-SUIT-42R", "US 42 Regular", 6), new("HB-SUIT-44R", "US 44 Regular", 3)],
            Gallery),
        new("loro-piana-linen-trousers", "Pleated Linen Trousers", "loro-piana", "clothing", 1150m,
            "Relaxed single-pleat trousers in breathable Italian linen, finished with a side-adjuster waistband for a clean, beltless line.",
            [new("LP-LIN-30", "Waist 30", 3), new("LP-LIN-32", "Waist 32", 5), new("LP-LIN-34", "Waist 34", 5), new("LP-LIN-36", "Waist 36", 2)],
            Gallery),
        new("brunello-cucinelli-poplin-dress-shirt", "Cotton Poplin Dress Shirt", "brunello-cucinelli", "clothing", 795m,
            "A crisp poplin shirt with a spread collar and mother-of-pearl buttons, made in Solomeo from long-staple cotton.",
            [new("BC-POP-15", "Collar 15", 4), new("BC-POP-155", "Collar 15.5", 5), new("BC-POP-16", "Collar 16", 5), new("BC-POP-165", "Collar 16.5", 3)],
            Gallery),
        new("hugo-boss-poplin-dress-shirt", "Slim-Fit Easy-Iron Dress Shirt", "hugo-boss", "clothing", 165m,
            "An everyday essential: easy-iron cotton poplin, a slim cut through the body and a kent collar that sits neatly under a tie.",
            [new("HB-SHIRT-15", "Collar 15", 10), new("HB-SHIRT-155", "Collar 15.5", 12), new("HB-SHIRT-16", "Collar 16", 12), new("HB-SHIRT-165", "Collar 16.5", 8)],
            Gallery),
        new("loro-piana-cashmere-crewneck", "Baby Cashmere Crewneck Sweater", "loro-piana", "clothing", 1695m,
            "Knitted from baby cashmere — the undercoat of Hircus kids under twelve months old — for a sweater of exceptional softness and lightness.",
            [new("LP-CASH-S", "S", 3), new("LP-CASH-M", "M", 4), new("LP-CASH-L", "L", 4), new("LP-CASH-XL", "XL", 2)],
            Gallery),
        new("zegna-oasi-cashmere-overcoat", "Oasi Cashmere Overcoat", "zegna", "clothing", 4800m,
            "A double-faced overcoat in Oasi cashmere, unlined for lightness, with a notch lapel and a length that clears a suit jacket.",
            [new("ZEG-COAT-48", "EU 48", 2), new("ZEG-COAT-50", "EU 50", 3), new("ZEG-COAT-52", "EU 52", 3), new("ZEG-COAT-54", "EU 54", 1)],
            Gallery),

        // --- Footwear ---------------------------------------------------
        new("crockett-jones-connaught-oxford", "Connaught Cap-Toe Oxford", "crockett-jones", "footwear", 715m,
            "The definitive black cap-toe Oxford, Goodyear-welted in Northampton on the 337 last with a single leather sole.",
            [new("CJ-CON-7", "UK 7", 3), new("CJ-CON-8", "UK 8", 5), new("CJ-CON-9", "UK 9", 5), new("CJ-CON-10", "UK 10", 4), new("CJ-CON-11", "UK 11", 2)],
            Gallery),
        new("john-lobb-city-ii-oxford", "City II Oxford", "john-lobb", "footwear", 1890m,
            "A refined two-eyelet-row Oxford in burnished calf, handcrafted in Northampton with a closed channel sole.",
            [new("JL-CITY-7", "UK 7", 2), new("JL-CITY-8", "UK 8", 3), new("JL-CITY-9", "UK 9", 3), new("JL-CITY-10", "UK 10", 2)],
            Gallery),
        new("crockett-jones-boston-loafer", "Boston Penny Loafer", "crockett-jones", "footwear", 650m,
            "A classic penny loafer in dark brown calf with a hand-sewn apron and a Goodyear-welted leather sole.",
            [new("CJ-BOS-7", "UK 7", 3), new("CJ-BOS-8", "UK 8", 4), new("CJ-BOS-9", "UK 9", 4), new("CJ-BOS-10", "UK 10", 3), new("CJ-BOS-11", "UK 11", 1)],
            Gallery),
        new("loro-piana-summer-walk-loafer", "Summer Walk Suede Loafer", "loro-piana", "footwear", 975m,
            "Unstructured suede loafers on a flexible rubber sole — the signature warm-weather shoe, light enough to pack anywhere.",
            [new("LP-SW-40", "EU 40", 3), new("LP-SW-41", "EU 41", 4), new("LP-SW-42", "EU 42", 4), new("LP-SW-43", "EU 43", 3), new("LP-SW-44", "EU 44", 2)],
            Gallery),

        // --- Watches ----------------------------------------------------
        new("rolex-submariner-date", "Submariner Date 41 mm", "rolex", "watches", 10950m,
            "Reference 126610LN. Oystersteel case, black Cerachrom bezel insert, calibre 3235 with a 70-hour power reserve, water-resistant to 300 metres.",
            [new("ROL-126610LN", "41 mm Oystersteel, Black", 1)],
            Gallery),
        new("rolex-datejust-41", "Datejust 41", "rolex", "watches", 9150m,
            "Reference 126300. The archetype of the classic watch: a 41 mm Oystersteel case, smooth bezel and Oyster bracelet, with the date at 3 o'clock.",
            [new("ROL-126300-BLU", "41 mm, Blue Dial", 1), new("ROL-126300-SLV", "41 mm, Silver Dial", 1)],
            Gallery),
        new("omega-speedmaster-moonwatch", "Speedmaster Moonwatch Professional", "omega", "watches", 7600m,
            "The watch that went to the Moon. 42 mm steel case, Master Chronometer calibre 3861 with manual winding.",
            [new("OMG-SPD-HES", "42 mm, Hesalite Crystal", 2), new("OMG-SPD-SAP", "42 mm, Sapphire Sandwich", 1, 8700m)],
            Gallery),
        new("omega-seamaster-diver-300m", "Seamaster Diver 300M", "omega", "watches", 6200m,
            "A 42 mm diver with a laser-engraved wave dial, ceramic bezel and Master Chronometer calibre 8800.",
            [new("OMG-SMD-BLU", "42 mm, Blue Dial", 2), new("OMG-SMD-BLK", "42 mm, Black Dial", 2)],
            Gallery),
        new("grand-seiko-snowflake", "Heritage Collection \"Snowflake\"", "grand-seiko", "watches", 6200m,
            "Reference SBGA211. A high-intensity titanium case around the famous snowflake dial, powered by the Spring Drive 9R65 with a gliding seconds hand.",
            [new("GS-SBGA211", "41 mm Titanium", 2)],
            Gallery),
        new("seiko-presage-cocktail-time", "Presage Cocktail Time", "seiko", "watches", 495m,
            "A 40.5 mm automatic dress watch whose textured dial evokes the shimmer of a classic cocktail.",
            [new("SKO-SRPB43", "40.5 mm, Blue Dial", 6), new("SKO-SRPB41", "40.5 mm, Silver Dial", 5)],
            Gallery),

        // --- Accessories ------------------------------------------------
        new("montblanc-meisterstuck-belt", "Meisterstück Reversible Leather Belt", "montblanc", "accessories", 425m,
            "Reversible black/brown calfskin with a palladium-finish pin buckle — two belts in one, cut to size.",
            [new("MB-BELT-32", "Waist 32", 4), new("MB-BELT-34", "Waist 34", 5), new("MB-BELT-36", "Waist 36", 5), new("MB-BELT-38", "Waist 38", 3)],
            Gallery),
        new("montblanc-meisterstuck-wallet", "Meisterstück Wallet 6cc", "montblanc", "accessories", 395m,
            "A slim bifold in full-grain black cowhide with six card slots and the Montblanc emblem in palladium.",
            [new("MB-WAL-6CC", "One Size, Black", 8)],
            Gallery),
        new("persol-714-folding-sunglasses", "714 Folding Sunglasses", "persol", "accessories", 480m,
            "The folding icon made famous on screen: hand-finished acetate, the Meflecto flexible temple system and crystal lenses.",
            [new("PER-714-HAV", "52 mm, Havana / Green", 4), new("PER-714-BLK", "54 mm, Black / Polarized Grey", 3, 540m)],
            Gallery),
        new("tom-ford-snowdon-sunglasses", "Snowdon Sunglasses", "tom-ford", "accessories", 495m,
            "Bold square acetate frames with the signature T hinge detail and gradient lenses.",
            [new("TF-SNOW-BLK", "Black / Grey Gradient", 4), new("TF-SNOW-HAV", "Dark Havana / Brown Gradient", 3)],
            Gallery),
        new("zegna-silk-tie", "Hand-Rolled Silk Tie", "zegna", "accessories", 250m,
            "Woven silk in a discreet micro-pattern, hand-rolled edges and a full lining for a clean, structured knot.",
            [new("ZEG-TIE-NVY", "Navy", 10), new("ZEG-TIE-GRY", "Charcoal", 8)],
            Gallery),

        // --- Perfumes ---------------------------------------------------
        new("creed-aventus", "Aventus Eau de Parfum", "creed", "perfumes", 395m,
            "Pineapple, bergamot and blackcurrant over a heart of birch and a base of oakmoss and ambergris.",
            [new("CRD-AVE-50", "50 ml", 6), new("CRD-AVE-100", "100 ml", 6, 535m)],
            Gallery),
        new("tom-ford-oud-wood", "Oud Wood Eau de Parfum", "tom-ford", "perfumes", 305m,
            "Rare oud wood, sandalwood and rosewood, warmed with cardamom, vetiver and amber.",
            [new("TF-OUD-50", "50 ml", 6), new("TF-OUD-100", "100 ml", 5, 435m)],
            Gallery),
        new("creed-green-irish-tweed", "Green Irish Tweed Eau de Parfum", "creed", "perfumes", 495m,
            "A crisp green fragrance of lemon verbena, Florentine iris and violet leaves over sandalwood and ambergris.",
            [new("CRD-GIT-100", "100 ml", 5)],
            Gallery),

        // --- Outlet -----------------------------------------------------
        new("hugo-boss-wool-blazer-outlet", "Regular-Fit Wool Blazer", "hugo-boss", "outlet", 395m,
            "Previous season. A versatile navy blazer in Italian wool with patch pockets and a half lining.",
            [new("HB-BLZ-40R", "US 40 Regular", 2), new("HB-BLZ-42R", "US 42 Regular", 2)],
            Gallery),
        new("zegna-linen-shirt-outlet", "Linen Casual Shirt", "zegna", "outlet", 295m,
            "Previous season. A garment-washed linen shirt with a soft cutaway collar, ideal for summer.",
            [new("ZEG-LSH-M", "M", 3), new("ZEG-LSH-L", "L", 2)],
            Gallery)
    ];
}
