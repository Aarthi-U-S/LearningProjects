using Auth.Enums;

namespace Auth.RateLimiting;

public class RateLimitOptions
{
    public bool Enabled { get; set; } = true;
    public int DefaultRequestLimit { get; set; } = 100;
    public TimeSpan DefaultTimeWindow { get; set; } = TimeSpan.FromMinutes(1);
    public RateLimitAlgorithm DefaultAlgorithm { get; set; } = RateLimitAlgorithm.FixedWindow;
    public RateLimitStrategy DefaultStrategy { get; set; } = RateLimitStrategy.PerIp;
    public int TokenBucketDefaultCapacity { get; set; } = 100;
    public int TokenBucketDefaultRefillRate { get; set; } = 10;
    public Dictionary<string, EndpointRateLimitRule> EndpointRules { get; set; } = new();
}



//┌────────────────────────────────────────────────────────────┐
//│              RateLimitOptions(Singleton)                  │
//│                                                            │
//│  Global Defaults:                                          │
//│  ├── DefaultRequestLimit: 100                             │
//│  ├── DefaultTimeWindow: 1 minute                          │
//│  ├── DefaultAlgorithm: FixedWindow                        │
//│  └── DefaultStrategy: PerIp                               │
//│                                                            │
//│  Dictionary<string, EndpointRateLimitRule>:               │
//│  ├── ["/api/auth/login"] ──────────┐                     │
//│  ├── ["/api/data/export"] ─────────┼──┐                  │
//│  └── ["/api/admin/users"] ─────────┼──┼──┐               │
//└────────────────────────────────────┼──┼──┼───────────────┘
//                                     │  │  │
//                ┌────────────────────┘  │  │
//                │  ┌────────────────────┘  │
//                │  │  ┌─────────────────────┘
//                ▼  ▼  ▼
//    ┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
//    │EndpointRateLimitRule│  │EndpointRateLimitRule│  │EndpointRateLimitRule│
//    ├─────────────────────┤  ├─────────────────────┤  ├─────────────────────┤
//    │ RequestLimit: 5     │  │ RequestLimit: 10    │  │ RequestLimit: 50    │
//    │ TimeWindow: 1min    │  │ TimeWindow: 60min   │  │ TimeWindow: 1min    │
//    │ Algorithm: Sliding  │  │ Algorithm: Token    │  │ Algorithm: Fixed    │
//    │ Strategy: PerUser   │  │ Strategy: PerIp     │  │ Strategy: PerUser   │
//    └─────────────────────┘  └─────────────────────┘  └─────────────────────┘
//         Login endpoint           Export endpoint          Admin endpoint