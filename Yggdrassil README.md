# Yggdrassil — Source Engine Model Porting Tool

Yggdrassil is a project-driven tool for importing, configuring, and exporting 3D models for **Source Engine games**, with **Garry’s Mod (GMod)** as the primary target.

The tool is intentionally architected to be:
- Deterministic
- Modular
- Pipeline-driven
- UI-agnostic at its core

This README describes the **architecture, responsibilities, and development rules** for the solution.

---

## High-Level Goals

- Import rigged 3D models from common formats (FBX, DAE, etc.)
- Configure Source Engine–specific data (QC, materials, bones, animations)
- Bake textures and generate SMD/DMX, QC, and related files
- Output a folder that can be compiled immediately with `studiomdl`
- Support additional targets later (SFM, L4D2) without rewrites

---

## Architectural Overview

The solution is split into **strict layers**, each enforced via separate projects.

```
Yggdrassil.Domain         → Pure data + validation
Yggdrassil.Application    → Pipelines + use-cases
Yggdrassil.Infrastructure → Concrete implementations (Assimp, QC, SMD, IO)
Yggdrassil.Cli            → Headless runner (Phase 1)
Yggdrassil.Presentation   → WinUI editor (Phase 2+)
Yggdrassil.Rendering      → Vortice viewport (Phase 3+)
```

### Core Rule
**Dependencies only point inward.**
UI and rendering must never own or mutate core logic.

---

## Project Responsibilities

### Yggdrassil.Domain
- Serializable project models
- Import settings
- Skeletons and bone definitions
- Material intent (not VMT/VTF)
- QC configuration and animation profiles
- Validation rules

**Forbidden:**
- File IO
- Assimp
- DirectX / Vortice
- WinUI

---

### Yggdrassil.Application
- Orchestrates workflows via use-cases
- Owns the build pipeline
- Handles progress, cancellation, and warnings
- Defines interfaces for infrastructure

**Allowed:**
- Async logic
- Pipeline steps
- Domain mutation (explicit only)

**Forbidden:**
- WinUI
- DirectX
- Format-specific implementations

---

### Yggdrassil.Infrastructure
- Assimp importers
- Texture baking and VTF/VMT generation
- SMD/DMX exporters
- QC assembly and writing
- Filesystem access

This is the *only* place where “messy” code is allowed.

---

### Yggdrassil.Cli
- Headless entry point
- Used to validate the pipeline end-to-end
- Must remain functional even if the UI is removed

This project is the safety net for refactors.

---

### Yggdrassil.Presentation (future)
- WinUI MVVM editor
- Edits project data only
- Triggers use-cases and pipeline runs
- No format or engine logic

---

### Yggdrassil.Rendering (future)
- Vortice-based viewport
- Mesh and bone visualization
- Picking and camera traversal
- Emits interaction events only

Rendering must never mutate project state directly.

---

## Pipeline Philosophy

All exports are produced by a **deterministic build pipeline**.

Typical steps:
1. Import model
2. Normalize scene (scale, axes, bones)
3. Bake materials
4. Export meshes (SMD/DMX)
5. Export animations (proportion trick, ragdoll, etc.)
6. Generate QC / QCIs
7. Pack output folder

Each step:
- Has a single responsibility
- Produces artifacts + warnings
- Can be reasoned about independently

---

## QC Generation Strategy

QC files are assembled from **modular template blocks**:
- Core QC
- Material lines (generated)
- Optional includes (hitboxes, IK)
- Animation profile blocks

Templates are treated as resources and combined deterministically.
No full QC parser is required.

---

## Targeting Strategy

The tool targets **GMod (Source 1)** first.

Other games (SFM, L4D2) are handled via:
- Target profiles
- Different QC defaults
- Optional format switches (SMD vs DMX)

This avoids pipeline duplication.

---

## Development Rules (Non-Negotiable)

- Domain must stay pure and serializable
- Rendering must not own logic
- No cross-layer shortcuts
- No hidden global state
- Prefer boring, explicit code
- Rebuild rather than untangle legacy systems

If a file violates its stated role, it should be split or moved.

---

## Status

- Phase 1: Headless pipeline + CLI (current)
- Phase 2: WinUI editor
- Phase 3: Vortice viewport and rigging UI

---

## Guiding Principle

This tool is not a “quick exporter”.
It is a **reliable, inspectable, repeatable build system** for Source Engine assets.
