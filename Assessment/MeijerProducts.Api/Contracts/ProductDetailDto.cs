namespace MeijerProducts.Api.Contracts;

public record ProductDetailDto(int Id, string ImageUrl, string Summary, string Title, string Description, string Price);
