# Student Management System

Full-stack student management dashboard built with Vue 3 + Vite frontend and ASP.NET Core Web API backend.

## Key Features

- **Student Records:** Manage data with type-safe validation using **TanStack Form** and **Zod**.
- **Filtering:** Client-side pagination with filters for search queries, faculty, education level, and academic year.
- **Analytics Dashboard:** A visual panel summarizing student demographics, including age segments, program distribution, and enrollment totals.
- **Modern UI/UX:** A fully responsive interface with **shadcn-vue** components.
- **Data Portability:** Integrated CSV import/export utilities with built-in validation and duplicate detection.

### Student Identifier (NIM) System

The system implements an automated, server-side generator for the *Nomor Induk Mahasiswa* (NIM) to ensure data consistency.

#### Generation Logic
The NIM is constructed using a composite prefix followed by an auto-incrementing sequence:

**Formula:** `[Faculty Code][Level][Program][Year] + [Sequence]`

| Component | Description | Example |
| :--- | :--- | :--- |
| **Faculty** | Single-letter code | `C` (Teknologi Industri) |
| **Level** | One-digit Jenjang code | `1` (S1) |
| **Program** | One-digit Program code | `4` (Informatika) |
| **Year** | Two-digit Angkatan | `23` (2023) |
| **Sequence** | 4-digit zero-padded index | `0001` |

**Example Result:**
Combining the components above produces the prefix `C1423`. The system appends the next available sequence to generate the final ID: **`C14230001`**.

#### Collision Handling
* **Sequential Integrity:** The repository locks the prefix to find the last created index and increments it safely.
* **Import Safety:** During CSV uploads, rows containing a `NomorIndukMahasiswa` that already exists in the database are flagged as duplicates and automatically skipped.

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

	Default launch settings, `VITE_API_URL=http://localhost:5091`.
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

	Vite hosts at `http://localhost:5173` and proxies API calls to `VITE_API_URL`.
