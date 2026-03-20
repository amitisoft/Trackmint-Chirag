# Deployment Notes

This repository is intentionally prepared for container-based Azure deployment, but the exact Azure service is not finalized yet because the company deployment document has not been provided.

## What Is Ready

- backend Dockerfile
- frontend Dockerfile
- `compose.yaml` for local multi-service runs
- environment separation points
- PostgreSQL-ready backend configuration

## What Is Pending

- Azure target service decision
- registry workflow
- production env variables
- managed PostgreSQL choice
- public frontend/backend hostname wiring

When you share the company Azure deployment doc, this file will be updated to the exact deployment flow.
