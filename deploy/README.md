# Deployment: Oracle Cloud + Caddy

The app runs as a systemd service on an Oracle Cloud Always Free ARM server (Ubuntu, Frankfurt), with Caddy
(HTTPS) in front. The database is Neon (Frankfurt), payments are Stripe (test mode), e-mail is Resend.

## First set-up (once, on the server)

```sh
sudo mkdir -p /opt/sahaf /etc/sahaf
sudo install -m 600 /dev/null /etc/sahaf/sahaf.env   # then fill it with sudoedit
sudo cp sahaf.service /etc/systemd/system/
sudo systemctl daemon-reload && sudo systemctl enable sahaf
# Add the block in Caddyfile to /etc/caddy/Caddyfile, then: sudo systemctl reload caddy
```

`/etc/sahaf/sahaf.env` uses the names in [.env.example](../.env.example):

```
DATABASE_URL=postgresql://...      # Neon
Admin__Email=...
Admin__Password=...
Stripe__SecretKey=sk_test_...
Stripe__WebhookSecret=whsec_...    # the webhook URL stays https://sahaf.miracdeprem.com/...
Email__ResendApiKey=re_...
Email__From=Sahaf <no-reply@miracdeprem.com>
AllowedHosts=sahaf.miracdeprem.com
```

## New version

**Update the SDK first.** The package carries the .NET runtime inside it, and the SDK on the computer decides its
version.

```powershell
winget install Microsoft.DotNet.SDK.10
dotnet --list-runtimes
```

On the computer (repo root), a single-folder Linux ARM build that needs no .NET installed. ReadyToRun compiles
the code ahead of time, so the first requests are fast on a small CPU.

```sh
dotnet publish src/Sahaf.Web -c Release -r linux-arm64 --self-contained -p:PublishReadyToRun=true -o dist/linux-arm64
tar -czf dist/sahaf.tar.gz -C dist/linux-arm64 .
scp dist/sahaf.tar.gz ubuntu@SERVER:/tmp/
ssh ubuntu@SERVER 'sudo rm -rf /opt/sahaf/* && sudo tar --no-same-owner -xzf /tmp/sahaf.tar.gz -C /opt/sahaf && sudo chown -R root:root /opt/sahaf && sudo chmod 755 /opt/sahaf/Sahaf.Web && sudo systemctl restart sahaf'
```

An archive made on Windows does not carry the "executable" flag or the owner: without `chmod` the service fails
with `status=203/EXEC`, and without `--no-same-owner` and `chown` the files belong to a Windows user number.

Migrations run when the app starts. Check: `curl -s https://sahaf.miracdeprem.com/healthz` → `Healthy`.
