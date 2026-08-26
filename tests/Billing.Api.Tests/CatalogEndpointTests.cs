using System.Net;
using System.Net.Http.Json;
using Billing.Application.Catalog;
using Billing.Api.Contracts;
using Billing.Domain;

namespace Billing.Api.Tests;

public sealed class CatalogEndpointTests : IClassFixture<BillingApiFactory>
{
    private readonly BillingApiFactory _factory;

    public CatalogEndpointTests(BillingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProduct_CreatesProduct()
    {
        var client = _factory.CreateClient();
        var request = CreateProductRequest("create-product", active: true);

        var httpResponse = await client.PostAsJsonAsync("/catalog/products", request);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(response);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Description, response.Description);
        Assert.True(response.Active);
        Assert.Equal($"/catalog/products/{response.Id}", httpResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreateProduct_CreatesInactiveProduct()
    {
        var client = _factory.CreateClient();
        var request = CreateProductRequest("create-inactive-product", active: false);

        var httpResponse = await client.PostAsJsonAsync("/catalog/products", request);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(response);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Description, response.Description);
        Assert.False(response.Active);
    }

    [Fact]
    public async Task DeactivateProduct_MarksProductInactive()
    {
        var client = _factory.CreateClient();
        var product = await CreateProductAsync(client, "deactivate-product");

        var httpResponse = await client.PostAsync($"/catalog/products/{product.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(response);
        Assert.False(response.Active);
    }

    [Fact]
    public async Task CreatePricePlan_CreatesActiveRecurringPlan()
    {
        var client = _factory.CreateClient();
        var product = await CreateProductAsync(client, "recurring-plan-product");
        var request = new CreatePricePlanRequest(
            product.Id,
            BillingTypes.Recurring,
            29m,
            "usd",
            BillingIntervals.Month,
            null);

        var httpResponse = await client.PostAsJsonAsync("/catalog/price-plans", request);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<PricePlanResponse>();
        Assert.NotNull(response);
        Assert.Equal(product.Id, response.ProductId);
        Assert.Equal(BillingTypes.Recurring, response.BillingType);
        Assert.Equal(29m, response.Amount);
        Assert.Equal("USD", response.Currency);
        Assert.Equal(BillingIntervals.Month, response.BillingInterval);
        Assert.Null(response.UsageUnit);
        Assert.True(response.Active);
        Assert.Equal($"/catalog/price-plans/{response.Id}", httpResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreatePricePlan_CreatesActiveUsageBasedPlan()
    {
        var client = _factory.CreateClient();
        var product = await CreateProductAsync(client, "usage-plan-product");
        var request = new CreatePricePlanRequest(
            product.Id,
            BillingTypes.Usage,
            0.02m,
            "USD",
            BillingIntervals.Month,
            "API request");

        var httpResponse = await client.PostAsJsonAsync("/catalog/price-plans", request);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<PricePlanResponse>();
        Assert.NotNull(response);
        Assert.Equal(product.Id, response.ProductId);
        Assert.Equal(BillingTypes.Usage, response.BillingType);
        Assert.Equal(0.02m, response.Amount);
        Assert.Equal("USD", response.Currency);
        Assert.Equal(BillingIntervals.Month, response.BillingInterval);
        Assert.Equal("API request", response.UsageUnit);
        Assert.True(response.Active);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("US1")]
    [InlineData("USDE")]
    public async Task CreatePricePlan_ReturnsBadRequest_WhenCurrencyIsNotThreeLetters(string currency)
    {
        var client = _factory.CreateClient();
        var product = await CreateProductAsync(client, "invalid-currency-product");
        var request = new CreatePricePlanRequest(
            product.Id,
            BillingTypes.Recurring,
            10m,
            currency,
            BillingIntervals.Month,
            null);

        var httpResponse = await client.PostAsJsonAsync("/catalog/price-plans", request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("invalid_price_plan", error.Code);
    }

    [Fact]
    public async Task CreatePricePlan_ReturnsBadRequest_WhenAmountIsNegative()
    {
        var client = _factory.CreateClient();
        var product = await CreateProductAsync(client, "negative-amount-product");
        var request = new CreatePricePlanRequest(
            product.Id,
            BillingTypes.Recurring,
            -0.01m,
            "USD",
            BillingIntervals.Month,
            null);

        var httpResponse = await client.PostAsJsonAsync("/catalog/price-plans", request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("invalid_price_plan", error.Code);
    }

    [Fact]
    public async Task CreatePricePlan_ReturnsBadRequest_WhenProductIsInactive()
    {
        var client = _factory.CreateClient();
        var product = await CreateProductAsync(client, "inactive-product", active: false);
        var request = new CreatePricePlanRequest(
            product.Id,
            BillingTypes.Recurring,
            10m,
            "USD",
            BillingIntervals.Month,
            null);

        var httpResponse = await client.PostAsJsonAsync("/catalog/price-plans", request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("product_not_found", error.Code);
    }

    private static async Task<ProductResponse> CreateProductAsync(
        HttpClient client,
        string name,
        bool active = true)
    {
        var httpResponse = await client.PostAsJsonAsync(
            "/catalog/products",
            CreateProductRequest(name, active));

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(response);
        return response;
    }

    private static CreateProductRequest CreateProductRequest(string name, bool active)
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        return new CreateProductRequest(
            $"{name}-{uniqueSuffix}",
            $"{name} Description",
            active);
    }
}
