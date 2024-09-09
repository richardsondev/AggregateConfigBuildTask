namespace AggregateConfig.Writers
{
    public interface IInputReader
    {
        object ReadInput(string inputPath);
    }
}
