// Integration tests for restaurant browse and suggestion endpoints.
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TasteBudz.Backend.Controllers;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.IntegrationTests.Api;


internal sealed class StubRestaurantGeocodingService(Queue<RestaurantGeocodeResult?> results) : IRestaurantGeocodingService
{
    public Task<RestaurantGeocodeResult?> GeocodeAsync(
        string restaurantName,
        string? streetAddress,
        string city,
        string state,
        string zipCode,
        CancellationToken cancellationToken = default)
    {
        if (results.Count == 0)
        {
            return Task.FromResult<RestaurantGeocodeResult?>(null);
        }

        return Task.FromResult(results.Dequeue());
    }
}
