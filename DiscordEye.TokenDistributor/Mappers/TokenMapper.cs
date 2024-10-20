using System.Diagnostics.CodeAnalysis;
using DiscordEye.TokenDistributor.Dto;

namespace DiscordEye.TokenDistributor.Mappers;

public static class TokenMapper
{
    public static bool TryToTokenVault(
        this IDictionary<string, object> data,
        [NotNullWhen(true)] out TokenVault? tokenVault)
}