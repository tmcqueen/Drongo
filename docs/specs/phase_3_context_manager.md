# Context Management Subsystem

The ContextManager is a key-value store that holds information about an interaction.  When an interaction arrives on the system, ContextManager assigns an ID to the interaction, and gives it an initial context.

```JSON
{
    "contextId": "019c9a2f-5bba-72e9-99e0-532601745f1f", // a version 7 UUID
    "startDateTimeUTC": "2026-02-26T13:43:17+00:00", // UTC timestamp in RFC8601
    "startTimeStamp": "1772113397", // Unix 64-bit timestamp 
}
```
All properties are stored as string.

The InteractionContextObject is stored in the KV store with the key 'context-id:paramater'.  For Example, the above InteractionContextObject would be stored as:

```
019c9a2f-5bba-72e9-99e0-532601745f1f:contextId => "019c9a2f-5bba-72e9-99e0-532601745f1f"
019c9a2f-5bba-72e9-99e0-532601745f1f:startDateTimeUTC => "2026-02-26T13:43:17+00:00"
019c9a2f-5bba-72e9-99e0-532601745f1f:startTimeStamp => "1772113397"
```

All values are serialized to string. If a complex object is stored, then it is serialized to JSON first.  No attempt is made by the ContextManager to de-serialize objects to their original form.

## Implementation
Ultimately there should be many options for backend implementatons of the ContextManager.  As long as the connector adheres to the Interface it shouldn't matter.  The connector will implement IContextManagerConnector and will be registered to the host as IContextManager.  

For the inital build, an in-memory key-value store will be implemented and will run in-process. The KV store will be a ConcurrentBag<string,string>.

### Assemblies
The following Assemblies will be generatef for the ContextManager Subsystem:

- Drongo.Context
- Drongo.Context.Abstractions
- Drongo.Context.Extensions
- Drongo.Context.ContextManager.InMemory