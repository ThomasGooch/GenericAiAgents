# Tool Discovery & Registration System

This diagram illustrates the sophisticated automatic tool discovery mechanism using attributes and reflection, showing how tools are found, validated, and registered.

```mermaid
sequenceDiagram
    participant Startup as 🚀 Application Startup
    participant HostedSvc as 🔄 ToolDiscoveryHostedService
    participant Scanner as 🔍 Assembly Scanner
    participant Validator as ✅ Tool Validator
    participant Registry as 📋 Tool Registry
    participant DI as 💉 DI Container
    participant Cache as 🗄️ Tool Cache
    
    %% Application Startup
    Startup->>DI: Register ToolDiscoveryHostedService
    Startup->>HostedSvc: StartAsync()
    
    %% Discovery Phase
    rect rgb(240, 248, 255)
        Note over HostedSvc,Registry: Discovery Phase
        HostedSvc->>Scanner: Scan Assemblies
        
        loop For Each Assembly
            Scanner->>Scanner: Load Assembly
            Scanner->>Scanner: Find Classes with [Tool] Attribute
            
            loop For Each Tool Class
                Scanner->>Scanner: Extract Metadata
                Note right of Scanner: Name, Description<br/>Category, Version
                Scanner->>Validator: Validate Tool
                
                alt Valid Tool
                    Validator->>Registry: Register Tool
                    Registry->>Cache: Cache Tool Metadata
                else Invalid Tool
                    Validator->>HostedSvc: Log Warning
                end
            end
        end
    end
    
    %% Registration Details
    rect rgb(248, 255, 248)
        Note over Registry,DI: Registration Phase
        Registry->>Registry: Create Tool Descriptor
        Registry->>DI: Register Tool in Container
        Registry->>Registry: Build Tool Index
        Registry->>Cache: Update Tool Cache
    end
    
    %% Runtime Tool Access
    rect rgb(255, 248, 248)
        Note over DI,Cache: Runtime Access
        participant Agent as 🤖 Agent
        Agent->>Registry: GetTool(name)
        Registry->>Cache: Check Cache
        
        alt Cache Hit
            Cache-->>Registry: Tool Instance
        else Cache Miss
            Registry->>DI: Resolve Tool
            DI-->>Registry: Tool Instance
            Registry->>Cache: Cache Instance
        end
        
        Registry-->>Agent: Tool Instance
    end
```

## Tool Discovery Architecture

### 🏗️ **Tool Definition & Attributes**

```mermaid
classDiagram
    class ToolAttribute {
        +string Name
        +string Description
        +string Category
        +string Version
        +bool IsEnabled
        +Priority Priority
    }
    
    class ITool {
        <<interface>>
        +string Name
        +string Description
        +ExecuteAsync(params) ToolResult
        +ValidateParameters(params) bool
        +GetParameterSchema() Dictionary
    }
    
    class BaseTool {
        <<abstract>>
        +string Name
        +string Description
        +ExecuteAsync(params) ToolResult*
        +ValidateParameters(params) bool
        +GetParameterSchema() Dictionary*
        #ValidateParametersInternal(params) bool
        #LogExecution(context) void
    }
    
    class SampleTool {
        +ExecuteAsync(params) ToolResult
        +GetParameterSchema() Dictionary
    }
    
    ToolAttribute --> ITool : decorates
    ITool <|-- BaseTool
    BaseTool <|-- SampleTool
    SampleTool : @Tool("sample", "Sample tool")
```

### 🔍 **Discovery Process Flow**

```mermaid
flowchart TD
    Start([Application Startup]) --> LoadAssemblies[📁 Load All Assemblies]
    LoadAssemblies --> ScanTypes[🔍 Scan for [Tool] Attribute]
    ScanTypes --> ValidateInterface{🧪 Implements ITool?}
    
    ValidateInterface -->|Yes| ValidateConstructor{🏗️ Has Public Constructor?}
    ValidateInterface -->|No| LogError[❌ Log Error]
    
    ValidateConstructor -->|Yes| ExtractMetadata[📋 Extract Attribute Metadata]
    ValidateConstructor -->|No| LogError
    
    ExtractMetadata --> ValidateMetadata{✅ Valid Metadata?}
    ValidateMetadata -->|Yes| RegisterTool[📝 Register in Registry]
    ValidateMetadata -->|No| LogWarning[⚠️ Log Warning]
    
    RegisterTool --> CacheMetadata[🗄️ Cache Tool Info]
    CacheMetadata --> RegisterDI[💉 Register in DI Container]
    RegisterDI --> BuildIndex[📊 Build Search Index]
    
    LogError --> NextType{More Types?}
    LogWarning --> NextType
    BuildIndex --> NextType
    NextType -->|Yes| ScanTypes
    NextType -->|No| Complete([Discovery Complete])
    
    classDef success fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    classDef error fill:#ffebee,stroke:#c62828,stroke-width:2px
    classDef process fill:#e3f2fd,stroke:#1565c0,stroke-width:2px
    
    class RegisterTool,CacheMetadata,RegisterDI,BuildIndex,Complete success
    class LogError,LogWarning error
    class LoadAssemblies,ScanTypes,ExtractMetadata,ValidateInterface,ValidateConstructor,ValidateMetadata process
```

### 📋 **Tool Registry Structure**

```mermaid
graph TB
    subgraph "🏪 Tool Registry"
        Registry[IToolRegistry<br/>• Tool Discovery<br/>• Metadata Storage<br/>• Instance Management]
        
        subgraph "📊 Storage Layers"
            MetadataStore[Tool Metadata<br/>• Name & Description<br/>• Parameter Schema<br/>• Category & Tags]
            
            InstanceCache[Instance Cache<br/>• Active Instances<br/>• Lifecycle Management<br/>• Performance Optimization]
            
            SearchIndex[Search Index<br/>• Name-based Lookup<br/>• Category Filtering<br/>• Full-text Search]
        end
        
        subgraph "🔍 Query Interface"
            GetTool[GetTool(name)]
            GetTools[GetToolsByCategory(category)]
            SearchTools[SearchTools(query)]
            GetSchema[GetParameterSchema(name)]
        end
    end
    
    Registry --> MetadataStore
    Registry --> InstanceCache
    Registry --> SearchIndex
    Registry --> GetTool
    Registry --> GetTools
    Registry --> SearchTools
    Registry --> GetSchema
    
    subgraph "🤖 Tool Implementations"
        FileSystem[📁 FileSystemTool<br/>@Tool("filesystem")]
        HttpClient[🌐 HttpClientTool<br/>@Tool("http")]
        TextManip[📝 TextManipulationTool<br/>@Tool("text")]
    end
    
    MetadataStore -.->|contains| FileSystem
    MetadataStore -.->|contains| HttpClient
    MetadataStore -.->|contains| TextManip
    
    classDef registry fill:#e3f2fd,stroke:#1565c0,stroke-width:3px
    classDef storage fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef query fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    classDef tools fill:#fff3e0,stroke:#ef6c00,stroke-width:2px
    
    class Registry registry
    class MetadataStore,InstanceCache,SearchIndex storage
    class GetTool,GetTools,SearchTools,GetSchema query
    class FileSystem,HttpClient,TextManip tools
```

## Discovery Patterns & Benefits

### 🎯 **Convention over Configuration**
```csharp
[Tool("file-reader", "Reads content from files", Category = "IO")]
public class FileReaderTool : BaseTool
{
    public override async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken = default)
    {
        // Implementation automatically discovered and registered
    }
}
```

### 🚀 **Performance Optimizations**

#### **Assembly Scanning Caching**
- One-time assembly scan at startup
- Cached metadata prevents re-scanning
- Hot-path tool resolution bypasses reflection

#### **Lazy Loading**
- Tool instances created on-demand
- Singleton pattern for stateless tools
- Scoped instances for stateful tools

#### **Search Indexing**
- Pre-built search indices for fast lookups
- Category-based filtering
- Full-text search on descriptions

### 🔄 **Lifecycle Management**

```mermaid
stateDiagram-v2
    [*] --> Discovered
    Discovered --> Validated : Validation Pass
    Discovered --> Invalid : Validation Fail
    Validated --> Registered : Register in DI
    Registered --> Cached : Add to Cache
    Cached --> Available : Ready for Use
    Available --> InUse : Agent Request
    InUse --> Available : Execution Complete
    Available --> Disposed : Application Shutdown
    Invalid --> [*]
    Disposed --> [*]
```

### ✅ **Validation Rules**
- **Interface Compliance**: Must implement `ITool`
- **Constructor Access**: Public parameterless constructor required
- **Attribute Validity**: Required metadata fields present
- **Name Uniqueness**: No duplicate tool names
- **Parameter Schema**: Valid parameter definitions
- **Category Validation**: Recognized category values

### 📈 **Monitoring & Diagnostics**
- **Discovery Metrics**: Number of tools found, validation failures
- **Performance Tracking**: Resolution times, cache hit rates
- **Health Monitoring**: Tool availability, execution success rates
- **Usage Analytics**: Most/least used tools, performance patterns