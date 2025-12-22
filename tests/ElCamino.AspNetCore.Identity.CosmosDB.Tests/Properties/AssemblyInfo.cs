using Microsoft.VisualStudio.TestTools.UnitTesting;

#if DEBUG
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
#else
// Parallel execution disabled for Release builds
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel, Workers = 1)]
#endif
