# The containerised mock

The WitCloud *shape* with none of the WitCloud *substance*: no database, no
identity, no WitRPC. It exists to answer the questions the Avalonia bench cannot
ask — and those are the ones whose failure mode is expensive, because it is
silent, delayed, and first noticed by a customer.

It can be destroyed and recreated in a loop. That is the point.

## What it answers

| Question | Answer |
|---|---|
| Does `installId` survive `docker compose up --force-recreate`? | **Yes.** New container, same identity, same licence |
| Does the store land somewhere a non-root container can write? | **Yes.** Runs as `uid=1654(app)` and persists to `/app/license` |
| Does a licence apply **without a restart**? | **Yes**, both by `POST /license` and by dropping a `.lic` into the volume — the latter needs `WithPeriodicReload` |
| Does `Licensing__License` work beside a dropped file, and which wins? | Both are read. **Neither door wins** — the better licence does, whichever way it arrived |
| Do two deployments get two identities? | **Yes.** A fresh volume is a fresh installation, and it refuses the other's licence |
| Is a copied volume at a different address refused? | **Yes** — `installId` matches, `publicBaseUrl` does not. This is the whole argument for the third factor |
| Is a copied volume at the *licensed* address accepted? | **Yes**, correctly: that is a replica of one deployment. It is also the honest limit of an offline binding |
| The volume is lost — generated id vs configured id | Generated: fresh identity, needs a Transfer. **Configured survives**, which is why `.env` is the primary form and the file only the fallback |

## Running it

```bash
# a key ring and a private key to sign with, standing in for WitLicense
Licensing__KeyRing=/path/to/keyring.json \
Licensing__PublicBaseUrl=https://first.example \
Licensing__Issuer=https://auth.example \
dotnet run
```

```bash
curl localhost:8080/identity          # installId, address, issuer, store
curl localhost:8080/health            # the licence field a real service exposes
curl localhost:8080/license           # the full snapshot
curl localhost:8080/license/request   # the .owlreq blob to send for a licence
curl localhost:8080/license -X POST --data-binary @licence.lic
curl localhost:8080/jobs -X POST      # the gate: 402 when Restricted
```

Two deployments, the arrangement the interesting questions need:

```bash
docker compose -f Licensing/Samples/OutWit.Common.Licensing.Samples.Server/docker-compose.yml up -d --build
```

They differ in exactly what a real pair of deployments differ in — the address
served and the identity owned — and in nothing else, so any difference in
outcome is attributable.

## Configuration

| Key | Meaning |
|---|---|
| `Licensing__InstallId` | The installation id. Blank falls back to the generated file, which is what a hand-rolled `docker compose up` gets |
| `Licensing__PublicBaseUrl` | The address served. A binding factor, and the one that reaches a clone |
| `Licensing__Issuer` | The identity authority. A binding factor |
| `Licensing__KeyRing` | The exported ring, inline or as a path |
| `Licensing__License` | A licence delivered as an environment variable, the compose way |
| `Licensing__Directory` | Where licences and the installation id live. A volume |
| `Licensing__ReloadSeconds` | How often to re-evaluate. Short here so a file drop is observable |
| `Licensing__GraceDays` | Renewal grace. 14 days, the server value |

## What it is not

It is not a product, and it is not a template to copy. The gate is two lines
because that is the claim being tested; a real service refuses its own specific
verbs for its own specific reasons, and abstracting that away is the one thing
the design forbids.

It also cannot prove the Blazor half of the service integration — it has no
Blazor host. That is a known limit, recorded in `ENFORCEMENT.md` §12.1.
