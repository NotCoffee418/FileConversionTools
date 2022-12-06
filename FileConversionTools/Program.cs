OperationManager.RegisterOperationsBulk(
    new List<Type>() {
        typeof(FindMislabeledMp3Files),
    }
);
await OperationManager.StartListeningAsync();
