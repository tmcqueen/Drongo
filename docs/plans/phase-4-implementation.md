# Phase 4 Implementation Plan: Distributed Architecture

## Phase Goal
Scale from single-server B2BUA to distributed architecture capable of handling enterprise-level load with high availability.

---

## 1. Distributed Registrar

### 1.1 Current State
Phase 2 adds Redis-backed registrar, but dialogs remain node-local.

### 1.2 Enhanced Distributed Registrar
- Full registration replication across nodes
- Atomic compare-and-swap operations
- Watchdog for stale registrations
- Multi-region support

---

## 2. Dialog Distribution

### 2.1 Dialog State Externalization
- Dialog state stored in distributed store (Redis)
- Any node can handle any in-dialog request
- Event-sourced or snapshot-based dialog persistence

### 2.2 Dialog Replication Strategies
```csharp
public interface IDialogReplicationStrategy
{
    Task<DialogSnapshot?> LoadAsync(string dialogId);
    Task SaveAsync(DialogSnapshot snapshot);
    Task<bool> TryAcquireLockAsync(string dialogId, string nodeId);
}
```

Strategies:
- None (sticky routing only)
- Lazy load (load on demand)
- Hot standby (replicate to secondary)

---

## 3. Transaction Coordination

### 3.1 Hybrid Model (Recommended)
- Keep transactions node-local
- Require sticky routing for in-dialog requests
- Dialog state replicated, not transaction

### 3.2 Fully Distributed (Future)
- Transaction state in distributed store
- Deterministic timer reconstruction
- Event ordering guarantees

---

## 4. Media Orchestration

### 4.1 Abstract Media Coordination
```csharp
public interface IMediaOrchestrator
{
    Task<MediaAllocation> AllocateAsync(MediaRequest request);
    Task ReleaseAsync(string allocationId);
    Task<bool> MigrateAsync(string allocationId, string targetNode);
}
```

### 4.2 Kubernetes Integration
- Media pods discovered via K8s service
- gRPC control API
- Health checking
- Metrics export

### 4.3 Media Session HA
- Graceful migration between media nodes
- Session checkpointing
- Failover handling

---

## 5. Cluster Coordination

### 5.1 Drongo.Cluster Component
- Node identity and heartbeating
- Leader election (for coordination tasks)
- Distributed locking (optional)
- Member discovery

### 5.2 Event Bus Integration
- NATS or Kafka for event streaming
- Dialog events across cluster
- Metrics aggregation

---

## 6. Load Balancing

### 6.1 SIP-Aware Load Balancer
- Call-ID hash based routing
- Source IP affinity
- Cookie-based (WebRTC)

### 6.2 Health Monitoring
- Node health checks
- Automatic removal from pool
- Gradual recovery

---

## 7. Task Breakdown

1. **Dialog State Externalization** - Store dialog state in Redis
2. **Dialog Replication Strategy** - Implement IDialogReplicationStrategy
3. **Media Orchestrator** - Abstract media allocation across nodes
4. **Kubernetes Media Pods** - K8s integration for media
5. **Cluster Coordination** - Drongo.Cluster with node management
6. **Event Bus Integration** - NATS/Kafka for cluster events
7. **SIP-Aware Load Balancing** - Intelligent routing
8. **Health Monitoring** - Node health and failover

---

## Open Questions

1. **State Store**: Redis or distributed cache (e.g., Memcached)?
2. **Event Bus**: NATS, Kafka, or RabbitMQ?
3. **Leader Election**: Built-in or external (Consul, etcd)?
