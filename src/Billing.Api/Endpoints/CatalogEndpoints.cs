using Billing.Application.Catalog;
using Billing.Api.Contracts;

namespace Billing.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/catalog");

        group.MapPost("/products", CreateProductAsync)
            .WithName("CreateProduct");

        group.MapGet("/products", ListProductsAsync)
            .WithName("ListProducts");

        group.MapGet("/products/{productId:guid}", GetProductAsync)
            .WithName("GetProduct");

        group.MapPut("/products/{productId:guid}", UpdateProductAsync)
            .WithName("UpdateProduct");

        group.MapPost("/products/{productId:guid}/deactivate", DeactivateProductAsync)
            .WithName("DeactivateProduct");

        group.MapPost("/price-plans", CreatePricePlanAsync)
            .WithName("CreatePricePlan");

        group.MapGet("/price-plans", ListPricePlansAsync)
            .WithName("ListPricePlans");

        group.MapGet("/price-plans/{pricePlanId:guid}", GetPricePlanAsync)
            .WithName("GetPricePlan");

        group.MapPut("/price-plans/{pricePlanId:guid}", UpdatePricePlanAsync)
            .WithName("UpdatePricePlan");

        group.MapPost("/price-plans/{pricePlanId:guid}/deactivate", DeactivatePricePlanAsync)
            .WithName("DeactivatePricePlan");

        return app;
    }

    private static async Task<IResult> CreateProductAsync(
        CreateProductRequest request,
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await catalogService.CreateProductAsync(request, cancellationToken);
            return Results.Created($"/catalog/products/{product.Id}", product);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ApiError("invalid_product", exception.Message));
        }
    }

    private static async Task<IResult> GetProductAsync(
        Guid productId,
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.GetProductAsync(productId, cancellationToken);
        return product is null
            ? Results.NotFound(new ApiError("product_not_found", "Product does not exist."))
            : Results.Ok(product);
    }

    private static async Task<IResult> ListProductsAsync(
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        var products = await catalogService.ListProductsAsync(cancellationToken);
        return Results.Ok(products);
    }

    private static async Task<IResult> UpdateProductAsync(
        Guid productId,
        CreateProductRequest request,
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await catalogService.UpdateProductAsync(productId, request, cancellationToken);
            return product is null
                ? Results.NotFound(new ApiError("product_not_found", "Product does not exist."))
                : Results.Ok(product);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ApiError("invalid_product", exception.Message));
        }
    }

    private static async Task<IResult> DeactivateProductAsync(
        Guid productId,
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.DeactivateProductAsync(productId, cancellationToken);
        return product is null
            ? Results.NotFound(new ApiError("product_not_found", "Product does not exist."))
            : Results.Ok(product);
    }

    private static async Task<IResult> CreatePricePlanAsync(
        CreatePricePlanRequest request,
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        try
        {
            var pricePlan = await catalogService.CreatePricePlanAsync(request, cancellationToken);
            return Results.Created($"/catalog/price-plans/{pricePlan.Id}", pricePlan);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ApiError("invalid_price_plan", exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new ApiError("product_not_found", exception.Message));
        }
    }

    private static async Task<IResult> GetPricePlanAsync(
        Guid pricePlanId,
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        var pricePlan = await catalogService.GetPricePlanAsync(pricePlanId, cancellationToken);
        return pricePlan is null
            ? Results.NotFound(new ApiError("price_plan_not_found", "Price plan does not exist."))
            : Results.Ok(pricePlan);
    }

    private static async Task<IResult> ListPricePlansAsync(
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        var pricePlans = await catalogService.ListPricePlansAsync(cancellationToken);
        return Results.Ok(pricePlans);
    }

    private static async Task<IResult> UpdatePricePlanAsync(
        Guid pricePlanId,
        CreatePricePlanRequest request,
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        try
        {
            var pricePlan = await catalogService.UpdatePricePlanAsync(pricePlanId, request, cancellationToken);
            return pricePlan is null
                ? Results.NotFound(new ApiError("price_plan_not_found", "Price plan does not exist."))
                : Results.Ok(pricePlan);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ApiError("invalid_price_plan", exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new ApiError("product_not_found", exception.Message));
        }
    }

    private static async Task<IResult> DeactivatePricePlanAsync(
        Guid pricePlanId,
        ICatalogService catalogService,
        CancellationToken cancellationToken)
    {
        var pricePlan = await catalogService.DeactivatePricePlanAsync(pricePlanId, cancellationToken);
        return pricePlan is null
            ? Results.NotFound(new ApiError("price_plan_not_found", "Price plan does not exist."))
            : Results.Ok(pricePlan);
    }
}
