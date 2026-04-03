## Integration Test Matrix

Comparative results between .NET 10 (unfixed) and .NET 11 (PR #125520).

### Legend
- ✅ Works as expected
- ❌ Bug — does not work as expected
- ⚠️ Pre-existing issue, unrelated to this PR
- `journal` — systemd journal log format (`<6>`/`<7>` prefixes visible in journalctl)
- `standard` — standard .NET console log format (`info:`/`dbug:` prefixes)
- `(unset)` — environment variable cleared after host initialization
- `present` — environment variable still set after host initialization

| Scenario | IHostLifetime .NET 10 | IHostLifetime .NET 11 | Log format .NET 10 | Log format .NET 11 | NOTIFY_SOCKET after .NET 10 | NOTIFY_SOCKET after .NET 11 | Result .NET 10 | Result .NET 11 |
|---|---|---|---|---|---|---|---|---|
| Direct execution | ConsoleLifetime | ConsoleLifetime | standard | standard | (unset) | (unset) | ✅ | ✅ |
| system-basic (Type=simple) | SystemdLifetime | SystemdLifetime | journal | journal | (unset) | (unset) | ✅ | ✅ |
| system-notify (Type=notify) | SystemdLifetime | SystemdLifetime | journal | journal | present | (unset) ✅ | ✅ | ✅ |
| system-notify + ProtectProc=invisible | ConsoleLifetime ❌ | SystemdLifetime ✅ | standard ❌ | journal ✅ | present | (unset) ✅ | ❌ timeout | ✅ |
| user-basic (Type=simple) | SystemdLifetime | SystemdLifetime | journal | journal | (unset) | (unset) | ✅ | ✅ |
| user-notify (Type=notify) | SystemdLifetime | SystemdLifetime | journal | journal | present | (unset) ✅ | ✅ | ✅ |
| user-notify + ProtectProc=invisible | SystemdLifetime | SystemdLifetime | journal | journal | present | (unset) ✅ | ✅ | ✅ |
| docker-basic | ConsoleLifetime | ConsoleLifetime | standard | standard | (unset) | (unset) | ✅ | ✅ |
| docker-notify | SystemdLifetime | SystemdLifetime | standard† | standard† | present | (unset) ✅ | ⚠️ timeout‡ | ⚠️ timeout‡ |
| docker-notify + ProtectProc=invisible | SystemdLifetime | SystemdLifetime | standard† | standard† | present | (unset) ✅ | ⚠️ timeout‡ | ⚠️ timeout‡ |
| podman-basic | ConsoleLifetime | ConsoleLifetime | standard | standard | (unset) | (unset) | ✅ | ✅ |
| podman-notify (--sdnotify=container) | SystemdLifetime | SystemdLifetime | journal | journal | present | (unset) ✅ | ✅ | ✅ |
| podman-notify + ProtectProc=invisible | SystemdLifetime | SystemdLifetime | journal | journal | present | (unset) ✅ | ✅ | ✅ |
| podman-notify + SYSTEMD_EXEC_PID mismatch | SystemdLifetime | SystemdLifetime | journal | journal | present | (unset) ✅ | ✅ | ✅ |

† Docker relays logs without preserving journal prefixes — journal format is active inside
the container but lost in transit.  
‡ Pre-existing issue — Docker does not proxy the notify socket into the container.

### Key fixes introduced by PR #125520

1. **`ProtectProc=invisible` (system service, Type=notify)**: timeout → Started,
   `ConsoleLifetime` → `SystemdLifetime`, standard log format → journal format.
2. **`NOTIFY_SOCKET` unset after host initialization** in all notify scenarios —
   prevents child processes from inheriting the socket and interfering with
   service manager notifications.