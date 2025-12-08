# Student Management System

Full-stack student management dashboard built with Vue 3 + Vite frontend and ASP.NET Core Web API backend.

## Tech Stack

- **Frontend:** Vue 3, TypeScript, Vite, Tailwind with shadcn-vue components
- **Backend:** ASP.NET Core Web API, C#
- **Tooling:** TanStack Form, Zod validation, Reka UI, Sonner


## Quick Start

1. **Install prerequisites**
	- Node.js 18+
	- .NET 10 SDK
    - npm

2. **Configure the frontend**

	```powershell
	cd client
	copy .env.example .env
	```

	Default launch settings, `VITE_API_BASE_URL=http://localhost:5091`.
3. **Prepare the backend database**

	```powershell
	cd api
	dotnet tool update --global dotnet-ef
	dotnet ef database update --project StudentApp.Infrastructure --startup-project StudentApp.Api
	```

	This command creates `student.db` under `api/StudentApp.Api/` so the API has a SQLite database.

4. **Run the backend API**

	```powershell
	cd api/StudentApp.Api
	dotnet restore
	dotnet run
	```

5. **Run the frontend**

	```powershell
	cd client
	npm install
	npm run dev
	```

	Vite hosts at `http://localhost:5173` and proxies API calls to `VITE_API_BASE_URL`.
