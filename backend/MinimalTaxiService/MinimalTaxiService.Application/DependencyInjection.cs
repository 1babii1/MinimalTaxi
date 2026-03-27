using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MinimalTaxiService.Application.Profiles.Commands;
using MinimalTaxiService.Application.Profiles.Queries;
using MinimalTaxiService.Application.Trips.Commands;
using MinimalTaxiService.Application.Trips.Invitations;
using MinimalTaxiService.Application.Trips.Queries;

namespace MinimalTaxiService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddSingleton<IIntercityInvitationStore, InMemoryIntercityInvitationStore>();

        services.AddScoped<CreateLocalTripCommandHandler>();
        services.AddScoped<CreateIntercityTripCommandHandler>();
        services.AddScoped<AcceptTripCommandHandler>();
        services.AddScoped<CancelTripCommandHandler>();
        services.AddScoped<CompleteTripCommandHandler>();
        services.AddScoped<JoinIntercityTripCommandHandler>();
        services.AddScoped<RemoveIntercityParticipantCommandHandler>();
        services.AddScoped<CreateIntercityInvitationCommandHandler>();
        services.AddScoped<AcceptIntercityInvitationCommandHandler>();
        services.AddScoped<DeclineIntercityInvitationCommandHandler>();
        services.AddScoped<UpdateProfileCommandHandler>();
        services.AddScoped<CreateSavedLocationCommandHandler>();
        services.AddScoped<DeleteSavedLocationCommandHandler>();

        services.AddScoped<GetNearbyTripsQueryHandler>();
        services.AddScoped<GetUserTripsQueryHandler>();
        services.AddScoped<GetPassengerIntercityInvitationsQueryHandler>();
        services.AddScoped<GetProfileQueryHandler>();
        services.AddScoped<GetSavedLocationsQueryHandler>();

        return services;
    }
}
