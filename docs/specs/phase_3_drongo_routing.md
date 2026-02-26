# Routing Subsystem

## Routes
Interactions (Calls, Chats, Emails, etc.) will move into and out of the system through Routes.  A Route is matched on the following criteria:

- Organization (in multi-tenent situations)
- Protocol
- Location
- Sender Address
- Receiver Address

The sender and receiver address will be in RFC822 format, i.e. <protocol>:<address>@<host>:<port?>.  For example, a SIP message will have the recipient address sip:5551212@example.com.  

## Route Plans
Once we have a route, we can decide a Route Plan.  The Route plan does the following things:

- Matching: The destination address is matched according to criteria
- Classification: What class of interaction? in the case of a call, it could be Local, Long Distance, Toll Free, Emergency, Intercom (internal communications), International, Premium.  These classifications are created by the Organizational admin, not hard-coded at design time.
- Security: is the Sender allowed to send a message to that Classification?
- Destination Endpoint
- Transformation: The destination address might not be in a format that the destination endpoint can handle.  In that case, the address needs to be transformed according to 

The process is:

- The destination address is matched
- The destination address is classified
- Rules are checked to see if the sender is allowed to send to the classification
- The destination address is Normalized (transformed into a common format) 
- The destination host is assigned
- The final routing information is passed back to the subsystem handling the interaction

## Matching Outbound
Matching is handled differently depending on the interaction type.  With SIP interactions, we rely on two parts of the destination address: user and host.  The user address is usually a phone number, but it can be anything.  Examples:

destination 5551212@example.com => is broken into destination.user 5551212 and destination.host example.com.  The destination.user field is matched against a ruleset using a custom template language we'll call 'Telco Expressions':

- XXXX => no match (an 'X' or 'x' represents any numeric character, 0-9), too short
- XXXXX => no match, still too short
- NxxXXXX => matches (and 'N' or 'n' represents any numeric character, 1-9)
- Nxx-XXXX => matches ('-' character is discarded)
- Nxx XXXX => matches (' ' character is discarded)
- N...... => matches ('.' matches any character) because it's the same length
- N55Z => matches ('Z' character is a wildcard an will match any numeric pattern)
- N55x+ => matches ('+' character means 'one or more of')
- N55x* => matches ('*' character means 'zero or more of')
- N55....? => matches ('?' character means 'zero or one of')
- (555)xxxx => matches (parentheses match a group)
- [(554)|(555)]xxxx => matches (square brackets define a condition) would also match 5541212 

The transformer is based on the sequence the number is in.  For example:

Input => Matcher => Transformer => Output
'5551212' => 'NxxXXXX' => '1 (212) $$$-$$$$' => '1 (212) 555-1212'
'5551212345' => 'NxxXXXXZ' => '1 (212) $$$-$$$$ Z' => '1 (212) 555-1212 345'

The Organizational admin could alternatively use C#-flavored Regular Expressions like '^555.*$'

## Matching Inbound
Inbound interactions are matched in the same manner as Outbound, except they're done in reverse order.

- The Destination host is read and matched to an organization
- The Destination address is normalized
- The Destination address is classified
- Organizational rules are checked to see if the interaction is allowed
- The Destination address is matched to an endpoint

## Structure
Probably split into three assemblies:
- Drongo.Routing: Contains sealed implementation classes
- Drongo.Routing.Abstractions: All of the Interfaces and Abstract classes
- Drongo.Routing.Extensions: All of the Extension methods for use in Host

## Interfaces
- IInteraction: the base of all interactions in the system
- IInboundInteraction: derives from IInteraction
- IOutboundInteraction: derives from IInteraction
- IInboundCall: Derives from IIncomingInteraction
- IOutboundCall: Derives from IOutgoingInteraction
- IRoute
- IRoutePlan
- IOutboundRoute
- IOutboundRoutePlan
- IOutboundCallRoute
- IOutboundCallRoutePlan
- IInboundRoute
- IInboundRoutePlan
- IInboundCallRoute
- IInboundCallRoutePlan

## Implementations
- RouteBase
- OutboundRouteBase
- OutboundRoutePlanBase
- InboundRouteBase
- InboundRoutePlanBase
- OutboundCallRoute
- OutboundCallRoutePlan
- InboundCallRoute
- InboundCallRoutePlan

