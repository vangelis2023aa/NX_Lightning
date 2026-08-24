# MeloNX RAM Optimization Project

## Mission

Reduce MeloNX host RAM usage—especially peak and sustained resident memory—**without reducing emulated functionality, compatibility, FPS, frame pacing, or guest-visible Switch memory**.

# MeloNX RAM Optimization Project

## Repository

This CLAUDE.md is located at the root of the local MeloNX source repository.

The repository itself is the source of truth.

Do NOT depend on accessing the remote repository or browsing the remote website.

Inspect the local source tree, project files, solution files, git history, submodules, and locally available dependencies directly.

If the local repository differs from information found online, prefer the local repository.

MeloNX is a Nintendo Switch emulator derived from the Ryujinx ecosystem and primarily uses C#, ARMeilleure/JIT CPU emulation, and Metal/MoltenVK-based GPU functionality.

## Primary Goal

Target a substantial reduction in host memory consumption while preserving performance.

Do NOT optimize toward "lowest possible RAM usage" at the expense of:

- FPS
- frame pacing
- CPU performance
- GPU performance
- shader compilation behavior
- game compatibility
- save functionality
- guest-visible memory
- emulator stability

A good target is approximately:

- 20–30% reduction in peak resident RAM if realistically achievable
- measurable reduction in sustained RAM
- no meaningful performance regression
- no new compatibility regressions

Do not assume these percentages are achievable before profiling.

---

# Core Engineering Principle

The desired architecture is:

> Reserve address space cheaply, commit physical memory lazily, retain hot resources, and make cold/reconstructible resources reclaimable.

Do NOT simply reduce allocation sizes arbitrarily.

The major categories to investigate are:

1. JIT/code memory
2. Guest DRAM mapping and physical page commitment
3. GPU textures
4. GPU buffers
5. pipeline/shader caches
6. staging buffers
7. duplicate CPU/GPU copies
8. large temporary allocations
9. managed .NET heap
10. native allocations
11. long-lived metadata
12. memory-pressure behavior

---

# IMPORTANT: Profile Before Modifying

Do not begin by making speculative optimizations.

First determine where the memory actually goes.

Separate:

- virtual address space
- resident physical memory
- managed .NET memory
- native allocations
- JIT executable memory
- JIT metadata
- guest memory
- Metal/GPU resources
- caches
- temporary allocations

A large virtual allocation is NOT automatically a large physical RAM problem.

---

# Required Baseline

Before significant modifications, establish a baseline for several representative games.

Measure:

```text
Launch
Firmware boot
Game launch
Title screen
First gameplay
10 minutes
30 minutes
Scene transition
Shader-heavy area
Save/load
Suspend/resume
Game exit
```

Record:

```text
Peak resident memory
Steady-state resident memory
Virtual memory
Managed heap
Native memory where available
JIT memory
GPU memory/resources
FPS
1% lows
Frame-time variance
Crash/stability behavior
```

Create a comparison table for every experiment.

---

# Performance Guardrails

A memory optimization should generally satisfy:

```text
Average FPS:
    >= 99% of baseline

1% lows:
    >= 97% of baseline

Frame-time variance:
    no significant regression

Compatibility:
    no new crashes
    no graphical corruption
    no save/load regressions
    no shader failures
```

These are guidelines, not absolute laws. Explain and justify deviations.

Never trade significant performance for a cosmetic RAM reduction.

---

# Priority 1 — JIT Memory

Investigate ARMeilleure and all JIT allocation paths.

Determine:

- how much JIT memory is reserved
- how much is actually committed
- how JIT regions grow
- how translated blocks are stored
- how much metadata exists per translation
- whether temporary compiler data survives unnecessarily
- whether executable memory can be allocated incrementally

## Desired Direction

Prefer elastic JIT allocation:

```text
Initial region
    ↓
Allocate additional region only when necessary
    ↓
Continue growing on demand
```

Do NOT impose an arbitrary small maximum.

The emulator must still support games that require substantial JIT memory.

## Investigate JIT eviction

Determine whether cold translated blocks can safely be reclaimed.

Potential classification:

```text
HOT
WARM
COLD
```

Potential metadata:

```text
guest address
host address
size
last execution
execution count
dependencies
invalidation state
```

Only implement eviction after understanding:

- block chaining
- indirect branches
- function pointers
- invalidation
- self-modifying code
- exception paths
- signal/trap mechanisms

Correctness takes priority.

---

# Priority 2 — Guest Memory

Investigate the memory manager.

Determine whether MeloNX:

```text
reserves large virtual ranges
```

and whether it:

```text
commits physical pages eagerly
```

or lazily.

Keep the same guest-visible Switch memory semantics.

Do NOT solve host RAM usage by simply reducing guest RAM.

Desired direction:

```text
Large guest address space
        ↓
lazy physical commitment
        ↓
physical pages only where needed
```

Carefully distinguish:

- virtual reservation reduction
- actual physical RAM reduction

They are separate optimizations.

---

# Priority 3 — GPU Memory

Investigate all Metal/MoltenVK-related resource caches.

Especially:

```text
TextureCache
BufferCache
PipelineCache
ShaderCache
Staging buffers
Command buffers
GPU resource metadata
IOSurfaces
Metal buffers
Metal textures
```

Measure:

```text
current bytes
peak bytes
object count
last-used time
creation cost
recreation cost
```

Avoid unlimited caches.

---

# GPU Cache Strategy

Prefer budgeted caches.

Example architecture:

```text
GPU memory budget
        │
        ├── textures
        ├── buffers
        ├── pipelines
        └── staging
```

When memory pressure increases, evict resources intelligently.

Do not use a simplistic "delete oldest object" algorithm without considering:

- resource size
- access frequency
- recreation cost
- upload cost
- shader compilation cost

Prefer something resembling:

```text
large + old + cheap to recreate
```

as a strong eviction candidate.

Avoid evicting:

```text
small + frequently used + expensive to recreate
```

---

# Priority 4 — Duplicate Memory

Search for cases where the same resource exists simultaneously in multiple representations.

Examples:

```text
Guest memory
    ↓
CPU buffer
    ↓
staging buffer
    ↓
GPU buffer
```

or:

```text
guest texture
decoded texture
converted texture
staging texture
Metal texture
```

Every large duplicate allocation should have a documented reason.

Classify allocations:

```text
Required for correctness
Required by API
Required for synchronization
Temporary
Cache
Probably redundant
```

The last two categories are priority targets.

---

# Priority 5 — Large Temporary Allocations

Look for memory spikes during:

- game loading
- scene transitions
- shader compilation
- texture decompression
- texture conversion
- filesystem operations
- GPU uploads
- firmware processing

Avoid patterns such as:

```text
500 MB input
+
500 MB decompressed output
+
500 MB upload buffer
```

where possible.

Prefer streaming/chunked processing:

```text
chunk
 ↓
process
 ↓
upload
 ↓
release
 ↓
next chunk
```

Peak memory is especially important on iOS.

---

# Priority 6 — Managed .NET Memory

Do not make GC tuning the primary strategy.

First investigate why large managed objects are retained.

Look for:

```text
byte[]
List<T>
Dictionary<TKey,TValue>
ConcurrentDictionary
LINQ allocations
boxing
closures
per-frame allocations
temporary arrays
duplicate metadata
```

Hot paths should ideally have near-zero unnecessary allocations.

Use pooling only when justified.

Do not create enormous pools that retain memory indefinitely.

Potential tools:

```text
ArrayPool<T>
object pools
custom slab allocators
```

Benchmark pooling carefully.

---

# Priority 7 — Memory Pressure Manager

Consider creating a central memory-pressure system.

Conceptually:

```text
MemoryManager
├── RegisterCache()
├── RegisterAllocation()
├── GetPressure()
├── RequestTrim(bytes)
└── Trim()
```

Potential clients:

```text
JIT
TextureCache
BufferCache
PipelineCache
ShaderCache
Staging buffers
Filesystem cache
Other reclaimable resources
```

When a subsystem needs memory:

```text
RequestTrim(requiredBytes)
```

The memory manager should ask reclaimable subsystems to release cold resources.

---

# Device-Adaptive Budgets

Do not assume every iPhone/iPad should have the same cache sizes.

Prefer memory budgets that adapt to available device memory and current pressure.

Conceptually:

```text
NORMAL
    ↓
PRESSURE
    ↓
AGGRESSIVE TRIM
    ↓
CRITICAL
    ↓
MAXIMUM SAFE RECLAMATION
```

High-memory devices can retain larger caches.

Low-memory devices should become more aggressive.

---

# Asynchronous Reclamation

Do not perform expensive cache eviction on the frame-critical path if avoidable.

Prefer:

```text
Frame thread
    ↓
request trim
    ↓
background worker
    ↓
release cold resources
```

Avoid turning RAM optimization into frame-time spikes.

---

# Two/Tier Cache Design

For expensive resources, consider:

```text
L1:
small, very hot, highly retained

L2:
larger, reclaimable runtime cache

L3:
persistent disk cache
```

A disk cache does not require every resource to remain resident in RAM.

---

# What NOT To Do

Do NOT:

- reduce emulated Switch RAM just to lower host RAM
- disable JIT caching entirely
- arbitrarily cap JIT memory
- continuously force .NET GC
- clear all caches periodically
- replace fast memory paths with slower ones without evidence
- introduce aggressive eviction without measuring recreation cost
- sacrifice compatibility for benchmark numbers
- optimize virtual memory while ignoring resident memory
- assume every large allocation is a leak
- make speculative changes before profiling

---

# Experimental Branches

Prefer isolated experiments:

```text
baseline
jit-elastic
jit-eviction
guest-memory-lazy
gpu-budget
texture-eviction
pipeline-eviction
staging-reduction
managed-allocation
pressure-manager
```

Benchmark each independently.

Then combine successful changes incrementally.

Example:

```text
baseline
    ↓
JIT optimization
    ↓
JIT + GPU optimization
    ↓
JIT + GPU + guest memory
    ↓
full memory-pressure system
```

This makes regressions attributable.

---

# Required Documentation for Every Optimization

For every memory optimization, record:

```text
Problem
Root cause
Files changed
Before memory usage
After memory usage
Peak reduction
Steady-state reduction
CPU impact
GPU impact
FPS impact
Frame pacing impact
Compatibility results
Known risks
Rollback strategy
```

Do not claim an optimization works without measurements.

---

# Success Criteria

The project is successful when MeloNX demonstrates:

```text
Lower peak resident RAM
Lower sustained resident RAM
No meaningful FPS regression
No meaningful frame-pacing regression
No new compatibility regressions
No new crashes
No guest-memory reduction
```

An ideal result would resemble:

```text
                   Baseline       Optimized

Peak RAM            5.5 GB        3.8–4.3 GB
Steady RAM          4.8 GB        3.5–4.0 GB
FPS                 60            60
1% low              52            51–52
Frame pacing        Good          Good
Compatibility       100%          100%
Long-session growth +800 MB       +100 MB
```

These numbers are targets, NOT assumptions.

---

# Recommended Investigation Order

1. Build memory instrumentation.
2. Establish baseline.
3. Map JIT memory.
4. Investigate elastic JIT regions.
5. Investigate JIT metadata and cold-block eviction.
6. Map GPU resource memory.
7. Implement budgeted GPU caches.
8. Investigate duplicate CPU/GPU resources.
9. Investigate large temporary allocations.
10. Investigate guest physical-memory commitment.
11. Analyze retained .NET objects.
12. Build central memory-pressure management.
13. Combine proven optimizations.
14. Stress test extensively.
15. Optimize thresholds for different devices.

---

# Final Principle

The objective is NOT:

> "Make MeloNX use less memory by deleting things."

The objective is:

> "Make MeloNX retain only the memory that provides measurable performance or correctness value, while allowing everything else to be recreated or reclaimed safely."

Optimize for:

```text
minimum resident memory
+
maximum retained useful state
+
unchanged emulator behavior
```