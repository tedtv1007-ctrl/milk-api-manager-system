# Architecture Flow (System Overview)

This document describes the interaction flow between the various components of the Milk API Manager System.

```mermaid
flowchart TD
    %% User Interacting with Admin UI
    User([Admin / Developer]) --> UI["Admin UI (Blazor)"]
    
    %% Management Plane Logic
    UI --> API["MilkApiManager API"]
    
    subgraph "Control Plane (.NET 8)"
        API --> Controllers
        API --> Workers["Background Workers (Auto-Defense, Route Sync)"]
        Controllers --> Services
        Services --> Db[(Postgres 17)]
        Services --> Vault["Vault Service (Key Rotation)"]
    end

    %% Data Plane Sync
    Services --> ApisixClient["ApisixClient"]
    ApisixClient --> AdminAPI["APISIX Admin API (9180)"]
    
    subgraph "Data Plane (APISIX)"
        AdminAPI --> Gateway["APISIX Gateway (9080)"]
        Gateway --> Plugins["Plugins (PII Masking, Traffic Blocking, Auth)"]
    end

    %% Consumer Access
    App([Third-party App]) --> Gateway
    Plugins --> Upstream["Internal Services / Upstream APIs"]

    %% Observability Stack
    Gateway -->|Metrics| PROM["Prometheus"]
    Gateway -->|Logs| ELK["ELK Stack 9.2.3 (Logstash)"]
    API -->|Logs| ELK
    PROM --> Grafana["Grafana"]
    PROM --> API
```

### Key Interaction Flows:
1.  **PII Protection**: Defined in UI -> Persisted in Postgres -> Synced via ApisixClient -> Executed in Lua Plugin.
2.  **Auto-Defense**: Prometheus metrics queried by Worker -> Malicious IP detected -> Pushed to Blacklist via Admin API.
3.  **Self-Service**: Request submitted in Dev Portal -> Approved by Admin -> Consumer & API Key auto-provisioned in APISIX.
