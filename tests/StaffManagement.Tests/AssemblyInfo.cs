using Xunit;

// The API reads configuration from environment variables, which are process
// wide, so test classes must not run concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
