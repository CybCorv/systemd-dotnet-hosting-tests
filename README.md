# kestrelTests

Validation harness for `Microsoft.Extensions.Hosting.Systemd`, focused on dotnet/runtime issue [#88660](https://github.com/dotnet/runtime/issues/88660).

The repository now exposes a full 4x3 matrix:
- 4 contexts: direct system, direct userland, Docker, Podman
- 3 test levels per context: basic, notify (no ProtectProc), notify + ProtectProc

## Prerequisites

- Linux machine with `systemd`
- .NET SDK (project defaults to `net10.0` for public repro mode)
- Docker CLI/runtime for Docker scenarios
- Podman CLI/runtime for Podman scenarios
- Repository available in VM at `/mnt/hgfs/kestrel`

## Test Matrix

### 1) Direct system (`sudo systemctl`)

- `services/WebApp-system-basic.service`
- `services/WebApp-system-notify.service`
- `services/WebApp-system-notify-protectproc.service`

Important: these units intentionally use `User=user`.
Without `User=...`, the process runs as root and root bypasses `ProtectProc=invisible` by design.

### 2) Direct userland (`systemctl --user`)

- `services/WebApp-user-basic.service`
- `services/WebApp-user-notify.service`
- `services/WebApp-user-notify-protectproc.service`

⚠️ ProtectProc limitation: "This option is only available for system services and is not supported for services running in per-user instances of the service manager" (systemd docs). The `*-protectproc` variant exists for completeness but has no effect in userland context.

### 3) Docker (system units)

- `services/WebApp-docker-basic.service`
- `services/WebApp-docker-notify.service`
- `services/WebApp-docker-notify-protectproc.service`

### 4) Podman (user units)

- `services/WebApp-podman-basic.service`
- `services/WebApp-podman-notify.service`
- `services/WebApp-podman-notify-protectproc.service`

⚠️ ProtectProc limitation: same as Direct userland—ProtectProc is not supported in per-user service manager (systemd docs). The `*-protectproc` variant exists for matrix completeness but has no effect.

## ProtectProc and Repro Conditions

The bug in issue #88660 requires:
1. `ProtectProc=invisible` enabled (restricts `/proc` visibility)
2. Process running as non-root, via `User=user` directive
3. `Type=notify` for systemd readiness protocol test

From systemd documentation:
> "If the kernel does not support per-mount point hidepid= mount options this setting remains without effect, and the unit's processes will be able to access and see other process as if the option was not used."

**Effective repro cases:**
- system context: `WebApp-system-notify-protectproc.service` ✓ (system service with User=user)
- docker context: `WebApp-docker-notify-protectproc.service` ✓ (system service managing container)
- user context: no effect (ProtectProc unsupported in --user scoped services)
- podman user context: no effect (ProtectProc unsupported in --user scoped services)

## Quick Start

Assumptions:
- Repository is mounted in VM at `/mnt/hgfs/kestrel`
- Commands are run from repository root (`/mnt/hgfs/kestrel`)

0. Move to repository root in VM:

```bash
cd /mnt/hgfs/kestrel
```

1. Publish app:

```bash
dotnet publish kestrelTests.csproj -c Release -r linux-x64 --self-contained -o ./publish
```

2. Build container images from repository root (`.` points to the root `Dockerfile`):

```bash
docker build -t test-kestrel .
podman build -t test-kestrel .
```

3. Register all services:

```bash
SHARE_PATH=/mnt/hgfs/kestrel

SUDO_UNITS=(
	WebApp-system-basic.service
	WebApp-system-notify.service
	WebApp-system-notify-protectproc.service
	WebApp-docker-basic.service
	WebApp-docker-notify.service
	WebApp-docker-notify-protectproc.service
)

USER_UNITS=(
	WebApp-user-basic.service
	WebApp-user-notify.service
	WebApp-user-notify-protectproc.service
	WebApp-podman-basic.service
	WebApp-podman-notify.service
	WebApp-podman-notify-protectproc.service
	WebApp-podman-notify-execpid-mismatch.service
)

sudo systemctl daemon-reload
for UNIT in "${SUDO_UNITS[@]}"; do
	sudo systemctl enable "$SHARE_PATH/services/$UNIT"
done

systemctl --user daemon-reload
for UNIT in "${USER_UNITS[@]}"; do
	systemctl --user enable "$SHARE_PATH/services/$UNIT"
done
```

4. Run the first repro case:

```bash
UNIT=WebApp-system-notify-protectproc.service
sudo systemctl restart "$UNIT"
sudo systemctl status "$UNIT"
sudo journalctl -u "$UNIT" -f

# Or for the Podman repro case (no sudo required):
UNIT=WebApp-podman-notify-protectproc.service
systemctl --user restart "$UNIT"
systemctl --user status "$UNIT"
journalctl --user -u "$UNIT" -f
```

## Additional Regression Test

This repository also includes a targeted regression case for `SYSTEMD_EXEC_PID` mismatch fallback behavior:
- `services/WebApp-podman-notify-execpid-mismatch.service`

Purpose:
- Force `SYSTEMD_EXEC_PID` to a non-matching value inside the container.
- Validate that detection still succeeds via fallback container checks (`PID 1 + NOTIFY_SOCKET`) when runtime logic is defensive.

Run:

```bash
UNIT=WebApp-podman-notify-execpid-mismatch.service
systemctl --user restart "$UNIT"
systemctl --user status "$UNIT"
journalctl --user -u "$UNIT" -f
```

## Local Runtime Development Mode

Default mode is public/repro (`net10.0`).

To test with the fix applied (net11-dev runtime bits), set `RuntimeRepoRoot` in `kestrelTests.local.props` and build with `../runtime/dotnet.sh build`.

For runtime work, create local override file:
- `kestrelTests.local.props` (based on `kestrelTests.local.props.example`)
- Set `RuntimeRepoRoot` to your runtime checkout

Then build with runtime tooling:

```bash
../runtime/dotnet.sh build
```

This keeps machine-specific settings out of the public branch.
