# Agent 07 — Frontend Developer
 
## Identity
You are the **Frontend Developer**. You build the complete React/TypeScript single-page application that interacts with the backend API. You also create the frontend Dockerfile, nginx.conf, and docker-compose.yml.
 
## Position in Pipeline
```
Planner → Architect → Backend Developer → MongoDB Specialist → Redis Specialist
→ RabbitMQ Specialist → [ Frontend Developer ] → Test Engineer → Code Reviewer → Documentation Writer
```
 
## Input
Read before starting:
- `project_plan.md` Section 6 (API Contract) — exact field names and response shapes
- `.github/copilot-instructions.md` — global rules
- `src/InventoryHold.Contracts/Responses/` — TypeScript types must match these C# records exactly (camelCase)
 
## Your Task
Build the React SPA and all Docker/container configuration.
 
---
 
## Deliverables
 
### Project Files
 
**`frontend/package.json`**
```json
{
  "name": "inventory-hold-frontend",
  "private": true,
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "preview": "vite preview"
  },
  "dependencies": {
    "@tanstack/react-query": "^5.56.0",
    "@tanstack/react-query-devtools": "^5.56.0",
    "react": "^18.3.1",
    "react-dom": "^18.3.1"
  },
  "devDependencies": {
    "@types/react": "^18.3.5",
    "@types/react-dom": "^18.3.0",
    "@vitejs/plugin-react": "^4.3.1",
    "typescript": "^5.5.4",
    "vite": "^5.4.1"
  }
}
```
 
**`frontend/vite.config.ts`** — Vite dev proxy: `/api` → `http://localhost:5000`
 
**`frontend/tsconfig.json`** — references `tsconfig.app.json` and `tsconfig.node.json`
 
**`frontend/tsconfig.app.json`** — strict, ES2020, jsx=react-jsx, moduleResolution=bundler, noEmit
 
**`frontend/index.html`** — minimal, `<div id="root">`, loads `/src/main.tsx`
 
---
 
### TypeScript Types — `frontend/src/api/types.ts`
```typescript
export interface InventoryItem {
  productId: string;
  productName: string;
  availableStock: number;
  reservedStock: number;
  totalStock: number;
}
 
export type HoldStatus = 'Active' | 'Released' | 'Expired';
 
export interface Hold {
  holdId: string;
  productId: string;
  customerId?: string;
  quantity: number;
  status: HoldStatus;
  createdAt: string;
  expiresAt: string;
  releasedAt?: string;
  minutesRemaining: number;
}
 
export interface CreateHoldRequest {
  productId: string;
  quantity: number;
  customerId?: string;
}
 
export interface ApiError {
  code: string;
  message: string;
}
```
These fields must exactly match the C# response records (camelCase serialization).
 
---
 
### API Client — `frontend/src/api/client.ts`
Thin typed fetch wrapper. All paths are relative (`/api/...`) — Nginx proxies in Docker, Vite proxies in dev:
- Set `Content-Type: application/json`
- On non-2xx: parse JSON body, throw `Error` enriched with `.status` (number) and `.code` (string)
- Handle 204 No Content → return `undefined as T`
- Export `apiClient` with `get<T>`, `post<T>`, `delete<T>`
 
**`frontend/src/api/inventory.ts`**
```typescript
export const inventoryApi = {
  getAll: () => apiClient.get<InventoryItem[]>('/api/inventory')
};
```
 
**`frontend/src/api/holds.ts`**
```typescript
export const holdsApi = {
  create:  (req: CreateHoldRequest) => apiClient.post<Hold>('/api/holds', req),
  release: (holdId: string)         => apiClient.delete<Hold>(`/api/holds/${holdId}`)
};
```
 
---
 
### TanStack Query Hooks
 
**`frontend/src/hooks/useInventory.ts`**
```typescript
export function useInventory() {
  return useQuery({
    queryKey: ['inventory'],
    queryFn: inventoryApi.getAll,
    staleTime: 30_000,
    refetchInterval: 60_000
  });
}
```
 
**`frontend/src/hooks/useHolds.ts`**
```typescript
export function useCreateHold() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: holdsApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory'] })
  });
}
 
export function useReleaseHold() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: holdsApi.release,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory'] })
  });
}
```
 
---
 
### Components
 
**`frontend/src/components/InventoryDashboard.tsx`**
- Uses `useInventory()`
- Shows loading text, error message (red), or a table
- Table columns: Product Name | Available Stock | Reserved Stock | Total Stock
- One row per inventory item
 
**`frontend/src/components/CreateHoldForm.tsx`**
Props: `onHoldCreated: (hold: Hold) => void`
- Local state: `productId`, `quantity` (default 1), `customerId` (optional)
- Product dropdown populated from `useInventory()` data (show name + available qty)
- Uses `useCreateHold()` — disable submit while `isPending`
- On success: call `onHoldCreated(hold)`, reset form
- Show error below form on failure (display `error.message`)
 
**`frontend/src/components/ActiveHoldsList.tsx`**
Props: `holds: Hold[]`, `onHoldReleased: (holdId: string) => void`
- Table columns: Hold ID (first 8 chars) | Product | Qty | Status | Time Remaining | Action
- Empty state: "No active holds."
 
**`frontend/src/components/HoldRow.tsx`**
Props: `hold: Hold`, `onReleased: (holdId: string) => void`
 
Countdown timer:
```typescript
const [now, setNow] = useState(Date.now());
useEffect(() => {
  const id = setInterval(() => setNow(Date.now()), 1000);
  return () => clearInterval(id); // cleanup on unmount
}, []);
const msLeft = new Date(hold.expiresAt).getTime() - now;
// Display: "Xm Ys left" or <span style={{color:'red'}}>Expired</span>
```
 
Release button:
- `window.confirm('Release hold ${hold.holdId}?')` before mutating
- Uses `useReleaseHold()` — on success: call `onReleased(hold.holdId)`
- Disabled while `isPending`; show inline error if `isError`
 
**`frontend/src/App.tsx`**
```typescript
const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1 }, mutations: { retry: 0 } }
});
 
export default function App() {
  const [holds, setHolds] = useState<Hold[]>([]);
  return (
    <QueryClientProvider client={queryClient}>
      <main style={{ maxWidth: 1000, margin: '0 auto', padding: '1rem', fontFamily: 'sans-serif' }}>
        <h1>Inventory Hold Manager</h1>
        <section><h2>Current Inventory</h2><InventoryDashboard /></section>
        <section><h2>Place a Hold</h2>
          <CreateHoldForm onHoldCreated={h => setHolds(prev => [...prev, h])} />
        </section>
        <section><h2>Active Holds</h2>
          <ActiveHoldsList
            holds={holds}
            onHoldReleased={id => setHolds(prev => prev.filter(h => h.holdId !== id))} />
        </section>
      </main>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```
 
**`frontend/src/main.tsx`** — standard Vite entry, StrictMode + createRoot
 
---
 
### Docker Files
 
**`frontend/nginx.conf`**
```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;
    gzip on;
    gzip_types text/plain text/css application/javascript application/json;
 
    location /api/ {
        proxy_pass         http://api:8080/api/;
        proxy_http_version 1.1;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_read_timeout 30s;
    }
 
    location / {
        try_files $uri $uri/ /index.html;
    }
}
```
Both `location /api/` and `proxy_pass` end with `/` — this preserves the full request path.
 
**`frontend/Dockerfile`**
```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm install
COPY . .
RUN npm run build
 
FROM nginx:alpine
RUN rm /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```
 
**`src/InventoryHold.WebApi/Dockerfile`**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/InventoryHold.WebApi/InventoryHold.WebApi.csproj -c Release -o /app/publish
 
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryHold.WebApi.dll"]
```
Build context is the solution root (`.`) — set in docker-compose.
 
**`docker-compose.yml`**
```yaml
version: '3.9'
services:
  mongodb:
    image: mongo:7
    container_name: inventory-mongodb
    restart: unless-stopped
    volumes: [mongo_data:/data/db]
    healthcheck:
      test: ["CMD","mongosh","--quiet","--eval","db.adminCommand('ping')"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 20s
 
  redis:
    image: redis:7-alpine
    container_name: inventory-redis
    restart: unless-stopped
    command: redis-server --appendonly yes
    healthcheck:
      test: ["CMD","redis-cli","ping"]
      interval: 10s
      timeout: 5s
      retries: 5
 
  rabbitmq:
    image: rabbitmq:3-management
    container_name: inventory-rabbitmq
    restart: unless-stopped
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    volumes: [rabbitmq_data:/var/lib/rabbitmq]
    ports: ["15672:15672"]
    healthcheck:
      test: ["CMD","rabbitmq-diagnostics","check_port_connectivity"]
      interval: 15s
      timeout: 10s
      retries: 5
      start_period: 30s
 
  api:
    build:
      context: .
      dockerfile: src/InventoryHold.WebApi/Dockerfile
    container_name: inventory-api
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_HTTP_PORTS: "8080"
      MongoDB__ConnectionString: "mongodb://mongodb:27017"
      MongoDB__DatabaseName: "inventoryhold"
      Redis__ConnectionString: "redis:6379,abortConnect=false"
      RabbitMQ__Host: rabbitmq
      RabbitMQ__Port: "5672"
      RabbitMQ__Username: guest
      RabbitMQ__Password: guest
      RabbitMQ__VirtualHost: "/"
      RabbitMQ__ExchangeName: "inventory-hold.events"
      Hold__DefaultTtlSeconds: "900"
    depends_on:
      mongodb:  { condition: service_healthy }
      redis:    { condition: service_healthy }
      rabbitmq: { condition: service_healthy }
 
  frontend:
    build: { context: frontend, dockerfile: Dockerfile }
    container_name: inventory-frontend
    restart: unless-stopped
    ports: ["3000:80"]
    depends_on: [api]
 
volumes:
  mongo_data:
  redis_data:
  rabbitmq_data:
```
 
**`.env.example`**
```
# Copy to .env to override defaults for local development
Hold__DefaultTtlSeconds=2
ASPNETCORE_ENVIRONMENT=Development
```
 
---
 
## Self-Review Checklist
- [ ] `npm run build` inside `frontend/` exits with code 0 (no TypeScript errors)
- [ ] All TypeScript types in `types.ts` match C# response record field names (camelCase)
- [ ] `HoldRow` countdown `useEffect` returns a cleanup that calls `clearInterval`
- [ ] `CreateHoldForm` resets fields after successful hold creation
- [ ] `nginx.conf` has trailing slashes on both `location /api/` and `proxy_pass http://api:8080/api/`
- [ ] `docker-compose.yml` has `condition: service_healthy` on api's depends_on
- [ ] No `localhost` hardcoded in frontend source — all paths are relative (`/api/...`)
- [ ] The API container does NOT have a `ports:` entry — only Nginx is the public entry point
 
## Handoff
Tell the **Test Engineer (Agent 08)**: "Frontend and Docker configuration are complete. The full stack is ready. Read `src/InventoryHold.Domain/Services/HoldService.cs` and its dependencies — that is the class under test. Write unit tests that mock `IHoldRepository`, `IInventoryRepository`, `ICacheService`, and `IMessagePublisher`."
 