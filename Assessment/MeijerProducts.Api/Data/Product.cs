namespace MeijerProducts.Api.Data;

public class Product
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Summary { get; set; }
    public required string Description { get; set; }

    // Stored as a display-ready string (e.g. "$0.59/lb") rather than decimal — the sample
    // data mixes unit suffixes (/lb, /each, /bunch, /head, /bag) that aren't a single price
    // + unit-of-measure schema, and neither endpoint needs to do arithmetic on it.
    public required string Price { get; set; }
    public required string ImageUrl { get; set; }
}
