# Inventory Hold Microservice — GitHub Copilot Root Instructions
 
## What This Project Is

 A production-quality Inventory Hold Microservice for an e-commerce checkout system. When a customer begins checkout, their items are temporarily held so they cannot be sold to another customer. Holds expire after a configurable duration (default 15 minutes).
 
## Technology Stack

 | Layer | Technology |

 |-------|-----------|

 | Runtime | .NET 10 (net10.0), C# 14 |

 | API | ASP.NET Core Web API with controllers |

 | Database | MongoDB 7 — atomic stock operations |

 | Cache | Redis via StackExchange.Redis 3.x |

 | Messaging | RabbitMQ via RabbitMQ.Client 7.x (async API) |

 | Testing | xUnit + Moq + FluentAssertions 8.x |

 | Frontend | React 18 + TypeScript + Vite + TanStack Query v5 |

 | Container | Docker + docker-compose |
 
## Agent Pipeline

 This project is built by a sequential pipeline of 10 specialist agents governed by a single Orchestrator. The Orchestrator dispatches each agent, runs a quality gate after each one, and retries or halts on failure.
 
```

 00-orchestrator.md      → governs the entire pipeline, runs all quality gates

 01-planner.md           → produces: project_plan.md

 02-architect.md         → produces: solution scaffold + architecture decisions

 03-backend-developer.md → produces: Domain + Contracts + WebApi layers

 04-mongodb-specialist.md→ produces: MongoDB repository implementations

 05-redis-specialist.md  → produces: Redis cache implementation

 06-rabbitmq-specialist.md→produces: RabbitMQ publisher + topology

 07-frontend-developer.md→ produces: React/TypeScript SPA

 08-test-engineer.md     → produces: xUnit unit test suite

 09-code-reviewer.md     → produces: review report + fixes

 10-documentation-writer.md→produces: README.md + AI-USAGE.md

 ```
 
**To start the pipeline:** Invoke `00-orchestrator.md`. It dispatches all other agents in order.
 
## Global Rules (apply to ALL agents)

 - Target `net10.0` — .NET 10 SDK is installed

 - NuGet source: use only `nuget.org` — a local `NuGet.Config` clears private feeds

 - No hardcoded connection strings — always from `appsettings.json` / environment variables

 - `CancellationToken ct = default` on all async methods

 - Nullable reference types enabled (`<Nullable>enable</Nullable>`)

 - RabbitMQ.Client v7 uses async channel API: `IChannel`, `CreateChannelAsync()`, `BasicPublishAsync()`

 - Records for DTOs/events (immutable), classes for domain entities (mutable state)

 - No comments explaining WHAT code does — only WHY when non-obvious
 
## Solution Structure

 ```

 C:\pocs\testing-service\

 ├── src\

 │   ├── InventoryHold.Contracts\       # DTOs, enums, request/response, events

 │   ├── InventoryHold.Domain\          # Entities, interfaces, HoldService

 │   ├── InventoryHold.Infrastructure\  # MongoDB, Redis, RabbitMQ implementations

 │   ├── InventoryHold.WebApi\          # Controllers, Program.cs

 │   └── InventoryHold.UnitTests\       # xUnit tests

 ├── frontend\                          # React SPA

 ├── .github\

 │   ├── copilot-instructions.md

 │   └── agents\                        # The 10 agent templates

 ├── docker-compose.yml

 ├── InventoryHold.sln

 └── NuGet.Config

 ```
 
## API Endpoints

 | Method | Path | Returns |

 |--------|------|---------|

 | POST | /api/holds | 201 + HoldResponse |

 | GET | /api/holds/{holdId} | 200 + HoldResponse |

 | DELETE | /api/holds/{holdId} | 200 + HoldResponse (released) |

 | GET | /api/inventory | 200 + InventoryItemResponse[] |