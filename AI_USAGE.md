\# AI Usage Disclosure



\## 1. AI Tools Used



\- ChatGPT (OpenAI)



\## 2. Areas Where AI Assistance Was Used



ChatGPT was used as a development assistant for:



\- ASP.NET Core backend architecture, API development, and Entity Framework Core.

\- PostgreSQL database design, transactions, row-level locking, and concurrency control.

\- React frontend development and debugging.

\- Dockerfile and Docker Compose configuration.

\- Backend and frontend automated testing.

\- Troubleshooting, code review, and technical documentation.



\## 3. AI Limitations and How I Addressed Them



AI-generated suggestions were reviewed and tested before being accepted. Some situations required additional investigation and corrections:



\- \*\*React rendering and state management:\*\* An initial implementation used `Date.now()` during rendering and updated state directly inside an effect. ESLint identified these patterns as problematic. I reviewed the lint errors, moved time handling into state, adjusted the effect logic, and used appropriate callbacks and cleanup. I reran ESLint and verified that the frontend tests and production build passed.



\- \*\*Concurrent seat allocation:\*\* A simple application-level availability check would not be sufficient when two backend processes attempt to reserve the final seat simultaneously. I used PostgreSQL transactions and `SELECT ... FOR UPDATE` to lock the relevant admission class row during allocation. I then tested concurrent requests against both backend instances to verify that only one request could reserve the final available seat.



\- \*\*Docker networking and project restructuring:\*\* Some initial Docker and project configurations required corrections after moving the backend into the `backend/` directory. I inspected the actual folder structure, updated the Docker build contexts and project references, validated the Compose configuration, and rebuilt the containers successfully.



\- \*\*Database persistence and integration:\*\* I did not assume that a successful container startup guaranteed correct database behavior. I checked API responses from both backend instances and tested that admission data remained available after normal container shutdown and restart.



\## 4. Verification and Developer Responsibility



I used AI as a supporting development tool rather than accepting its output without verification. I investigated errors, checked implementation behavior, modified suggested solutions, and validated the final application through automated tests, manual API testing, frontend linting, production builds, and Docker deployment.



The final implementation and technical decisions were reviewed and verified by me.



