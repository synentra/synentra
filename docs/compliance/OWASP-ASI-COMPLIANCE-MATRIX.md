# Synentra OWASP ASI 2026 Compliance Matrix

## Document Purpose

This document provides a practical, implementation-focused mapping of Synentra capabilities against OWASP Agentic Security Initiative (ASI) 2026 risk themes. It is intended for engineering, architecture, risk, and compliance stakeholders who need an auditable view of current control coverage.

## Scope and Assessment Method

### Status Legend

- **Supported**: control objective is materially implemented and operationally usable.
- **Partial**: meaningful control elements exist, but important depth or guardrails are missing.
- **Gap**: no explicit native control identified for the mapped risk objective.
- **Gap/Partial**: early signals exist, but not enough to claim consistent control coverage.

> Important: this is **not** an official OWASP certification artifact. It is an evidence-based internal mapping aligned to publicly available OWASP ASI materials.

## OWASP ASI 2026 Compliance Matrix

| ASI ID | ASI risk name | Status | Synentra implementation coverage |
| --- | --- | --- | --- |
| ASI-01 | Agent Goal Hijack | ⚠️ Partial | Intent classification, policy/risk decisioning, and HITL gating provide defenses against malicious goal redirection before action execution |
| ASI-02 | Tool Misuse | ⚠️ Partial | Proxy governance, allow/deny/HITL outcomes, header filtering, rate limiting, and circuit breaker controls reduce unsafe tool invocation impact |
| ASI-03 | Identity & Privilege Abuse | ✅ Supported | JWT-based agent authentication, custom auth header controls, optional external identity integration, and trust/quarantine controls |
| ASI-04 | Agentic Supply Chain Vulnerabilities | ⚠️ Gap/Partial | Some deployment hardening exists, but no dedicated provenance/attestation controls for agentic components, models, tools, or MCP dependencies |
| ASI-05 | Unexpected Code Execution | ⚠️ Partial | Policy/risk/HITL controls can block dangerous requests, but there is no dedicated runtime code execution sandboxing or execution-policy engine |
| ASI-06 | Memory & Context Poisoning | ⚠️ Gap/Partial | Historical trust signals exist, but no dedicated memory poisoning detection, provenance validation, or memory integrity controls |
| ASI-07 | Insecure Inter-Agent Communication | ⚠️ Gap | Synentra secures agent-to-API gateway traffic; no explicit inter-agent channel trust/authenticity framework is currently defined |
| ASI-08 | Cascading Failures | ✅ Supported | Rate limits, circuit breaker protections, request validation, and pending workflow limits reduce blast radius and systemic propagation |
| ASI-09 | Human-Agent Trust Exploitation | ⚠️ Partial | Human approval workflow exists, but reviewer anti-manipulation safeguards and operator deception controls are not yet explicit |
| ASI-10 | Rogue Agents | ✅ Supported | HITL suspension workflow, deny controls, quarantine support, and trust-score thresholds provide containment for unsafe autonomous behavior |
| Extension | Agent Traceability | ✅ Supported | Tamper-evident audit log |

## Executive Snapshot

- **Supported:** 3
- **Partial:** 4
- **Gap:** 1
- **Gap/Partial:** 2

## Versioning

- Version: `v1.0`
- Status: `Publishable draft`
- Last updated: `2026-09-27`
