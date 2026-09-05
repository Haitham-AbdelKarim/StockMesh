using Application.Common.Models;
using Application.DTOs.Auth;
using Domain.Enums;
using MediatR;

namespace Application.Features.Auth.Commands.RegisterStore;

public sealed record RegisterStoreCommand(
    string StoreName,
    VerticalCategory VerticalCategory,
    double Latitude,
    double Longitude,
    double MaxSearchRadiusKm,
    string Email,
    string Password) : IRequest<Result<RegisterStoreResponse>>;