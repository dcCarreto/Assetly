// Vários testes limpam o pool global de conexões do SQLite (SqliteConnection.ClearAllPools) ao apagar seus bancos
// temporários; em paralelo, isso fecha a conexão que outro teste está usando no meio (falha intermitente). Em série.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
