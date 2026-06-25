using Billing.Application.Common;
using Billing.Application.Subscriptions;

namespace Billing.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/subscriptions");

        group.MapPost("", CreateSubscriptionAsync)
            .WithName("CreateSubscription");

        group.MapGet("/{subscriptionId:guid}", GetSubscriptionAsync)
            .WithName("GetSubscription");

        group.MapPost("/{subscriptionId:guid}/cancel", CancelSubscriptionAsync)
            .WithName("CancelSubscription");

        group.MapPost("/{subscriptionId:guid}/change-plan", ChangeSubscriptionPlanAsync)
            .WithName("ChangeSubscriptionPlan");

        app.MapGet("/customers/{customerId:guid}/subscriptions", ListCustomerSubscriptionsAsync)
            .WithName("ListCustomerSubscriptions");

        return app;
    }

    private static async Task<IResult> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        ISubscriptionService subscriptionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await subscriptionService.CreateSubscriptionAsync(request, cancellationToken);
            return Results.Created($"/subscriptions/{subscription.Id}", subscription);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new ApiError("customer_not_found", exception.Message));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ApiError("price_plan_not_found", exception.Message));
        }
        catch (NotSupportedException exception)
        {
            return Results.BadRequest(new ApiError("billing_interval_not_supported", exception.Message));
        }
    }

    private static async Task<IResult> GetSubscriptionAsync(
        Guid subscriptionId,
        ISubscriptionService subscriptionService,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionService.GetSubscriptionAsync(subscriptionId, cancellationToken);

        return subscription is null
            ? Results.NotFound(new ApiError("subscription_not_found", "Subscription does not exist."))
            : Results.Ok(subscription);
    }

    private static async Task<IResult> ListCustomerSubscriptionsAsync(
        Guid customerId,
        ISubscriptionService subscriptionService,
        CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionService.ListCustomerSubscriptionsAsync(customerId, cancellationToken);
        return Results.Ok(subscriptions);
    }

    private static async Task<IResult> CancelSubscriptionAsync(
        Guid subscriptionId,
        CancelSubscriptionRequest request,
        ISubscriptionService subscriptionService,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionService.CancelSubscriptionAsync(subscriptionId, request, cancellationToken);

        return subscription is null
            ? Results.NotFound(new ApiError("subscription_not_found", "Subscription does not exist."))
            : Results.Ok(subscription);
    }

    private static async Task<IResult> ChangeSubscriptionPlanAsync(
        Guid subscriptionId,
        ChangeSubscriptionPlanRequest request,
        ISubscriptionService subscriptionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await subscriptionService.ChangeSubscriptionPlanAsync(
                subscriptionId,
                request,
                cancellationToken);

            return subscription is null
                ? Results.NotFound(new ApiError("subscription_not_found", "Subscription does not exist."))
                : Results.Ok(subscription);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ApiError("price_plan_not_found", exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new ApiError("subscription_not_active", exception.Message));
        }
        catch (NotSupportedException exception)
        {
            return Results.BadRequest(new ApiError("billing_interval_not_supported", exception.Message));
        }
    }
}
