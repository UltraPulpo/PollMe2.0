@echo off
start "Backend" cmd /c "cd backend\PollApp.Api && dotnet run"
start "Frontend" cmd /c "cd frontend && npm run dev"
