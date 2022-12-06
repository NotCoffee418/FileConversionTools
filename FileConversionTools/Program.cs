using CSharpScriptOperations;

OperationManager.RegisterOperationsBulk(
    new List<Type>() {
        //typeof(TwoPlusTwo),
    }
);
await OperationManager.StartListeningAsync();
