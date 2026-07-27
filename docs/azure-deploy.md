# Deploying MeijerProducts.Api to Azure Container Instances

Covers issue #34: run the containerized API (`Assessment/MeijerProducts.Api/Dockerfile`, from #12) on
Azure Container Instances (ACI), pulling from Azure Container Registry (ACR). See
`docs/DECISIONS.md` ("Deploy the API to Azure Container Instances with ephemeral SQLite storage") for
the reasoning behind the choices below.

## Prerequisites

- An Azure subscription, with a resource group and ACR instance already created:
  - Resource group: `rg-meijer-assessment` (South Central US)
  - ACR: `acrmeijer.azurecr.io`, admin user enabled (`az acr update --name acrmeijer --admin-enabled true`)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed and logged in
  (`az login`) to the subscription that owns the resource group above.
- Docker, for building the image locally.

## Deploy

From `Assessment/`:

```bash
# 1. Build the image
docker build -t acrmeijer.azurecr.io/meijerproducts-api:latest MeijerProducts.Api

# 2. Log in to ACR (uses the az CLI session, no static credential needed for the push)
az acr login --name acrmeijer

# 3. Push
docker push acrmeijer.azurecr.io/meijerproducts-api:latest

# 4. Fetch ACR admin credentials (ACI needs a username/password to pull, unlike the docker push above)
ACR_USER=$(az acr credential show --name acrmeijer --query username -o tsv)
ACR_PWD=$(az acr credential show --name acrmeijer --query 'passwords[0].value' -o tsv)

# 5. Create (or recreate) the container group
az container create --resource-group rg-meijer-assessment --name meijerproducts-api --location southcentralus \
  --image acrmeijer.azurecr.io/meijerproducts-api:latest \
  --registry-login-server acrmeijer.azurecr.io --registry-username "$ACR_USER" --registry-password "$ACR_PWD" \
  --dns-name-label meijerproducts-api --ports 8080 --os-type Linux --cpu 1 --memory 1 \
  --restart-policy OnFailure \
  --environment-variables ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__Default="Data Source=/app/products.db"
```

Note `ConnectionStrings__Default` points at `/app/products.db` (the image's own `WORKDIR`), **not**
`/app/data/products.db` like `docker-compose.yml` uses — `/app/data` only exists locally because
Compose's named volume creates it on mount. Without an equivalent volume in ACI, that path doesn't
exist and the container crash-loops with `SQLite Error 14: unable to open database file`.

To redeploy after a code change, rerun steps 1, 3, then delete and recreate the container group
(`az container delete --resource-group rg-meijer-assessment --name meijerproducts-api --yes`, then
step 5) — `az container create` won't pull a new image over an existing group by itself.

## Verify

```bash
az container show --resource-group rg-meijer-assessment --name meijerproducts-api --query ipAddress.fqdn -o tsv
```

Then hit `http://<fqdn>:8080/products` and `http://<fqdn>:8080/swagger/index.html` — both should
respond the same as local `docker compose up`.

## Data persistence

No Azure Files share is mounted. The SQLite file lives in the container's writable layer and is lost
on restart/redeploy — the app's existing idempotent migrate-and-seed-on-startup logic simply
re-populates the same 30-product dataset each time, so this is a non-issue for this deployment.
