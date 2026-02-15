# Immutable Result – Performance Options

With immutable `Result`, every `WithError`, `WithValue`, and `MergeResults` returns a **new** instance. That implies more allocations than the current mutable design (where merge/add mutate in place). Below are options to reduce allocation and CPU cost, similar in spirit to `ValueTask` and pooled collections.

---

## 1. **Singleton / cached “empty” results** (low effort, high impact)

**Idea:** The very common case `return Result.Ok()` (or `Result.Ok(null)` for `Result<T>`) always returns the same instance.

- **Implementation:**  
  - Cache one static instance for `Result` with no message (e.g. `Result.Ok()` → return `s_successResult`).  
  - Optionally cache `Result.Ok(null)` for `Result<T>` (or one per `T` if you want to avoid boxing; usually one shared “success, no value” is enough).
- **Effect:** “Return success” hot path goes from **1 allocation** (Result + 2 Lists today; or Result + 2 read-only collections in the new design) to **0** when using the cached instance.
- **Caveat:** Only safe if that instance is never mutated. The immutable design guarantees that, so this is a natural fit.

**Recommendation:** Do this as part of the immutable refactor. Use `Array.Empty<IError>()` and `Array.Empty<Exception>()` (or equivalent) inside the singleton so you don’t allocate lists there either.

---

## 2. **Merge / With* short-circuits** (low effort, medium impact)

**Idea:** When the operation is a no-op, return the existing instance instead of allocating.

- **MergeResults:**  
  - If `other` has **no errors and no exceptions**, return `this` (or a new result only if you need to adopt `other.Message` and it differs).  
  - So `result.MergeResults(Result.Ok())` → **no allocation** (especially when combined with singleton `Result.Ok()`).
- **WithError / WithValue:**  
  - “Success + add error” must allocate (you’re creating a result that has errors).  
  - “Success + WithValue(x)” could return a new instance that shares the same empty errors/exceptions (via `Array.Empty<>()` or shared readonly list), so you only allocate the new `Result<T>` wrapper, not new collections.

**Effect:** Merging with a “success” result (e.g. after a chain of `result = result.Invoke(...)`) doesn’t multiply allocations when the inner call succeeded.

**Recommendation:** Implement MergeResults so that “merge with empty result” returns `this` (or a clone only when success message differs). Document that merging with success is allocation-friendly.

---

## 3. **Lazy / structural sharing for error lists** (medium effort, medium impact)

**Idea:** Don’t concatenate error lists on merge; represent “combined” as a lazy sequence (e.g. `this.Errors.Concat(other.Errors)`). Allocate a single array only when the full list is materialized (e.g. when someone iterates or calls `.ToList()`).

- **Implementation:**  
  - `Errors` and `Exceptions` become something that can be either “a single array” or “a concatenation of two such things” (e.g. a small wrapper or `IEnumerable` built from `Concat`).  
  - `MergeResults` and `WithError` then only create a new Result object and a thin wrapper over existing collections; no new array for the combined list until needed.
- **Effect:**  
  - Fewer allocations on merge/WithError in code that only checks `Success` or reads the first error.  
  - If callers often do `result.Errors.ToList()` or iterate multiple times, you might cache the materialized list (lazy + cache).
- **Caveats:**  
  - `Count` / `HasErrors` become O(n) unless you store/cache counts.  
  - Slightly more complex type (e.g. internal “ErrorList” that can be either array or concat).

**Recommendation:** Consider for a v2 if profiling shows merge/WithError on hot paths; start with simple “new array on merge” and add this if needed.

---

## 4. **ValueResult / struct wrapper** (high effort, high impact for hot paths)

**Idea:** Like `ValueTask`, provide a **struct** `ValueResult` and `ValueResult<T>` that can hold either:

- an inline “success” or “single error” representation (no heap), or  
- a reference to a heap-allocated `Result` / `Result<T>` when there are many errors or merged state.

- **Implementation:**  
  - Struct contains something like `Result _resultOrNull` and a few fields (e.g. one error code/message, or success flag).  
  - When you have only success or one error, store it inline; when you need multiple errors or merge, allocate a `Result` and store the reference.  
  - API can mirror `Result` (with a conversion to `Result` when you need to pass to APIs that expect a class).
- **Effect:** Hot path “return success” or “return one error” can be **zero heap allocations**. Heavier paths (merge, many errors) still allocate.
- **Caveats:**  
  - Two types to maintain and document (Result vs ValueResult).  
  - Boxing if someone uses ValueResult where object/Result is expected.  
  - More complex implementation and testing.

**Recommendation:** Optional follow-up or separate package for allocation-sensitive scenarios. Not required for the first immutable release.

---

## 5. **Object pooling** (medium effort, situational impact)

**Idea:** Pool `Result` (and maybe internal collections) and rent/return instead of always `new`.

- **Challenges with immutability:**  
  - Once you return a result to a caller, you don’t know when they’re done with it, so “return to pool” is unclear.  
  - Pooling works best for short-lived, clearly scoped objects (e.g. created and discarded inside a single request).
- **Possible approach:**  
  - Pool only the “empty” or “single-error” instances used as **starting points** (e.g. `Result.Ok()` or `Result.Error("x")`). When the caller does `result = result.WithError(...)`, the new instance is not pooled; the original could be returned to a pool if it was rented and no longer referenced (hard to enforce without more context).  
  - Or: pool internal buffers (e.g. list used to build the error list in a constructor), not the Result itself.
- **Recommendation:** Lower priority; singleton (1) and short-circuits (2) are simpler and cover the most common hot path. Consider pooling only if profiling shows heavy allocation in code that creates many short-lived results in a tight loop.

---

## 6. **Cheap construction for the common case** (already in plan, reinforce)

- Use **`Array.Empty<IError>()`** and **`Array.Empty<Exception>()()`** for the “no errors” case so you don’t allocate two lists per result.  
- Keep **one constructor path** that takes pre-built `IReadOnlyList<IError>` / `IReadOnlyList<Exception>` (or `ImmutableArray`) so that `Result.Error(...)` and `WithError` build one list and pass it in; no extra copy.  
- Where possible, **avoid allocating a new list on merge** by building a single new list from `this.Errors.Concat(other.Errors)` and passing it to the new Result’s constructor (one allocation for the new Result, one for the combined list), rather than multiple intermediate collections.

---

## Summary

| Option                         | Effort | Impact on “return Result.Ok()” / merge-with-success | When to do it           |
|--------------------------------|--------|------------------------------------------------------|--------------------------|
| 1. Singleton empty/success     | Low    | Removes allocation for common success path          | As part of immutable refactor |
| 2. Merge/With short-circuits   | Low    | No allocation when merging with empty result        | As part of immutable refactor |
| 3. Lazy error list             | Medium | Fewer allocations when only Success/first error used| If profiling shows need  |
| 4. ValueResult struct          | High   | Zero heap for success/one-error in hot path          | Optional / follow-up     |
| 5. Pooling                     | Medium | Situational                                          | Only if profiling shows  |
| 6. Cheap construction          | Low    | Fewer/smaller allocations per new result            | As part of immutable refactor |

**Practical order:** Implement **1**, **2**, and **6** with the immutable design so that the default “success” path and “merge with success” path are allocation-friendly. Add **3**, **4**, or **5** only if you have measured allocation or CPU pressure in real workloads.
