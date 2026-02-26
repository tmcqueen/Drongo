# Eventing Subsystem

Almost every action taken by the system will raise an event.  The event system will accept registrations from other subsystems and will forward event notifications to every registraton, until either the event is cancelled or the last registrant is notified.  

When a subsystem registers with the event system, they will pass their own identifier in the format "subsystem-name.instance-id".  The subsystem will then ask for the available events, then send back a list of events to subscribe to with their desired priority.  All communication with the EventManager subsystem is through JSON-RPC 2.0.  The flow would look like:

Subsystem Sends:
```JSON-RPC
{ "jsonrpc": "2.0", "method": "register", "params": { "subsystem": "flow-processer", "instance": "019c9a2f-5bba-72e9-99e0-532601745f1f"} }
```

EventManager Responds:
```JSON-RPC
{ "jsonrpc": "2.0", "result": "OK", "params": { "id": "flow-processer:019c9a2f-5bba-72e9-99e0-532601745f1f"} }
```

## Message Types
There are several message types that will be defined:

- One-Way Single Recipient Notification: receives an acknowledgement
- One-Way Single Recipient Notification(Unacknowledged): expects no response
- Two-Way Single Recipient Request: sender waits for request
- Two-Way Single Recipient Async Request: sender expects to be notified with the response
- One-Way Broadcast Notification: goes to every topic subscriber
- Two-Way Broadcast Notification: sender expects to receive notifications of responses

Messages have Priorities:
- Routine: all message traffic - defined as int 0
- Priority: handled before Routine traffic - defined as int 1
- Immediate: handled before Priority Traffic - defined as int 2
- Flash: handled before Immediate traffic - defined as int 4,
- FlashOverride: handled before ALL traffic - defined as int 255

Messages have Types
- Request
- Response
- Notification

Messages have Classes
- Event
- Alert
- Log
- System

## Communications
Drongo Subsystems do not communicate directly with the backend of EventManager.  A Standalone EventManager will implement JSON-RPC over TCP, WebSocket, and Unix File Socket.

As part of the development for the EventManager, the following SIP Message handlers need to be implemented:

- PUBLISH
- SUBSCRIBE
- MESSAGE

## In-Memory EventManager
The prototype implementation of IEventManager will be InMemoryEventManager.  It will run in-process. It will be implemented as a FIFO Queue with a single worker that will take the next message out of the queue and process it.

### Assemblies
The following Assemblies will be create for EventManager:

- Drongo.Events
- Drongo.Events.Abstractions
- Drongo.Events.Extensions
- Drongo.Events.EventManager.InMemory