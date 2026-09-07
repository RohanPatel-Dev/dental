# Deploying

Two supported shapes. Neither is the way to run the system while you are working on it — for that,
`dotnet run --project src/Host/Dental.AppHost` gives you the same topology with dashboards and log
streaming and no image builds.

## One machine: `deploy/docker`

```bash
cd deploy/docker
cp .env.example .env        # then fill in every value; the compose file refuses to start without them
docker compose up -d
```

* API on 8080, operator console on 5173, practice app on 5174.
* The migrator runs to completion first; the API waits for it.
* Each SPA gets its `config.json` mounted over the one baked into the image, so the same image runs
  in every environment.

## Azure Container Apps: `deploy/terraform`

```bash
cd deploy/terraform
terraform init -backend-config=environments/prod.backend.hcl
terraform apply -var environment=prod -var api_image=... -var migrator_image=... -var notifications_image=...
```

The topology mirrors `AppHost.cs`: one API with ingress, one extracted notifications host with
**none**, and the migrator as a job rather than an app because it runs to completion.

### The release order that matters

1. Build and push the images (CI).
2. Start the migrator job and **wait for it to succeed**.
3. Roll the API and the notifications host onto the new images.

Rolling the API first means requests hit a schema that has not been migrated yet. The advisory lock
protects the database from two concurrent migrators; nothing protects it from that ordering mistake.

### What this Terraform does not do

* Build or push images — CI does.
* Manage DNS or certificates — they outlive the stack.
* Run migrations on a schedule — a release triggers the job.
* Configure sticky sessions, deliberately: SignalR runs over the Redis backplane so any replica can
  serve any connection.
