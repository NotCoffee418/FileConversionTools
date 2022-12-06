OperationManager.RegisterOperationsBulk(
    new List<Type>() {
        typeof(FindMislabeledMp3Files),
        typeof(ConvertMislabeledMp3),
    }
);
await OperationManager.StartListeningAsync();
