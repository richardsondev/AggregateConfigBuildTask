namespace AggregateConfig.FileHandlers
{
    public interface IOutputWriter
    {
        void WriteOutput(object mergedData, string outputPath);
    }
}
