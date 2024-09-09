namespace AggregateConfig.FileHandlers
{
    public interface IInputReader
    {
        object ReadInput(string inputPath);
    }
}
