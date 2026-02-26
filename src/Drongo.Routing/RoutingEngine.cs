using System.Text.RegularExpressions;
using Drongo.Routing.Routes;
using Drongo.Routing.Telco;

namespace Drongo.Routing;

public class RoutingEngine : IRoutingEngine
{
    private readonly TelcoExpressionParser _parser = new();

    public IRoutePlan? Route(IInboundRoute route, string destinationAddress, string? senderAddress = null)
    {
        var normalizedDestination = NormalizeAddress(destinationAddress);
        var digitsOnly = normalizedDestination.Replace("+", "");
        
        if (!MatchesPattern(route.ReceiverAddress, digitsOnly))
        {
            return null;
        }

        var classification = Classify(normalizedDestination, route.ReceiverAddress);
        
        var isAuthorized = CheckAuthorization(route, senderAddress, normalizedDestination);
        
        return new RoutePlan
        {
            Classification = classification,
            IsAuthorized = isAuthorized,
            DestinationEndpoint = route.ReceiverAddress,
            TransformedAddress = normalizedDestination,
            DestinationHost = route.Location
        };
    }

    private bool MatchesPattern(string pattern, string digitsOnly)
    {
        if (pattern == "*")
        {
            return true;
        }
        
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern[..^1];
            return digitsOnly.StartsWith(prefix);
        }
        
        return _parser.Match(pattern, digitsOnly);
    }

    private RouteClassification Classify(string address, string pattern)
    {
        var digits = address.Replace("+", "");
        
        if (digits.Length == 7)
        {
            return new RouteClassification("Local", "LOCAL");
        }
        
        if (digits.Length == 10)
        {
            return new RouteClassification("Local", "LOCAL");
        }
        
        if (digits.StartsWith("1") && digits.Length == 11)
        {
            var areaCode = digits.Substring(1, 3);
            
            if (areaCode == "800" || areaCode == "888" || areaCode == "877" || 
                areaCode == "866" || areaCode == "855" || areaCode == "844" || areaCode == "833")
            {
                return new RouteClassification("Toll Free", "TOLLFREE");
            }
        }
        
        var leadingDigits = CountLeadingDigits(pattern);
        if (leadingDigits >= 4 && digits.Length == 11 && digits.StartsWith("1"))
        {
            return new RouteClassification("Local", "LOCAL");
        }
        
        if (digits.StartsWith("1") && digits.Length == 11)
        {
            return new RouteClassification("Long Distance", "LONGDISTANCE");
        }
        
        if (!digits.StartsWith("1") && digits.Length >= 10)
        {
            var countryCode = "+" + digits.Substring(0, Math.Min(2, digits.Length));
            if (countryCode != "+1")
            {
                return new RouteClassification("International", "INTERNATIONAL");
            }
        }
        
        return new RouteClassification("Unknown", "UNKNOWN");
    }

    private int CountLeadingDigits(string pattern)
    {
        var count = 0;
        foreach (var c in pattern)
        {
            if (char.IsDigit(c))
            {
                count++;
            }
            else if (c != 'X' && c != 'x' && c != 'N' && c != 'n' && c != 'Z' && c != 'z')
            {
                break;
            }
        }
        return count;
    }

    private bool CheckAuthorization(IInboundRoute route, string? senderAddress, string normalizedDestination)
    {
        if (string.IsNullOrEmpty(route.SenderAddress) || route.SenderAddress == "*")
        {
            return true;
        }

        if (string.IsNullOrEmpty(senderAddress))
        {
            return false;
        }

        if (route.SenderAddress == "*")
        {
            return true;
        }

        var normalizedSender = NormalizeAddress(senderAddress);
        var senderDigitsOnly = normalizedSender.Replace("+", "");
        
        if (!MatchesPattern(route.SenderAddress, senderDigitsOnly))
        {
            return false;
        }
        
        return true;
    }

    private string NormalizeAddress(string address)
    {
        var normalized = address.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");
        
        if (!normalized.StartsWith("+"))
        {
            if (normalized.Length == 10)
            {
                normalized = "+1" + normalized;
            }
            else if (normalized.Length == 11 && normalized.StartsWith("1"))
            {
                normalized = "+" + normalized;
            }
        }
        
        return normalized;
    }

    private sealed class RoutePlan : RoutePlanBase, IRoutePlan
    {
    }
}
