\# Assumptions and Design Decisions



This document describes assumptions made where the assignment did not specify implementation details and explains the decisions taken.



\## 1. Student Identification



\- A student is identified by a 17-digit birth registration number.

\- The identifier is sent through the `X-Student-Id` request header.

\- No login system is implemented because student authentication was not required.

\- This header-based identification is an assignment-level mechanism, not production-grade authentication.



\## 2. Admission Classes



\- Each admission class represents a specific grade and shift combination.

\- For example, Class 6 Morning Shift and Class 6 Day Shift are separate records.

\- Each record has its own seat capacity.



\## 3. Temporary Seat Reservation



\- An application initially creates a `HELD` reservation.

\- The default hold duration is 600 seconds (10 minutes), configurable through `HOLD\_DURATION\_SECONDS`.

\- Successful simulated payment changes the status to `CONFIRMED`.

\- Expired or cancelled reservations release their seats.

\- A student can have only one active or confirmed application per admission class.



\## 4. Concurrent Backend Processes and Data Consistency



The assignment requires two backend processes to run simultaneously on different ports while sharing the same database and maintaining consistent admission rules.



To achieve this:



\- Both backend instances connect to the same PostgreSQL database.

\- Seat allocation is performed inside a database transaction.

\- Before allocating a seat, the application locks the relevant admission class row using PostgreSQL `SELECT ... FOR UPDATE`.

\- If two requests attempt to reserve the last available seat simultaneously, the database makes them wait for the same row lock. The second request checks the updated seat count after the first transaction completes.

\- Seat availability is calculated using confirmed reservations and unexpired held reservations.

\- A database partial unique index additionally prevents duplicate active or confirmed applications for the same student and admission class.

\- Hold confirmation and cancellation also use database transactions and row-level locking.



These mechanisms are enforced by PostgreSQL, rather than in-memory application locks, so they work across both backend processes.



The two instances are independently accessible on ports 8081 and 8082. No load balancer is configured.



\## 5. Payment and Administration



\- Payment is simulated; no real payment gateway is integrated.

\- Admin class creation does not require authentication because admin authentication was not specified in the assignment.



\## 6. Database Persistence



\- PostgreSQL uses a named Docker volume to preserve data across normal container restarts.

\- Running `docker compose down -v` removes the volume and its stored data.



\## 7. Identifiers



\- GUIDs are used for admission class and hold identifiers.



