# Unity 6 Engineering Standards

- **Modern APIs**: Utilize Unity 6 features where applicable (e.g., `Awaitable` instead of custom Coroutine wrappers for async operations).
- **GC Allocation Control**: Avoid per-frame allocations (`LINQ` in hot paths, string concatenations in update loops, unnecessary boxing).
- **Serialization Safety**: Never modify `.unity` or `.prefab` YAML text directly unless explicitly instructed. Prefer updating serialized data via scriptable objects or code-driven factories.
