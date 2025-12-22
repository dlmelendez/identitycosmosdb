using Microsoft.VisualStudio.TestTools.UnitTesting;

#if DEBUG
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
#else
// Parallel execution completely disabled for Release builds
[assembly: DoNotParallelize]
#endif
