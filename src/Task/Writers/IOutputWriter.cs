namespace AggregateConfig.Writers
{
    public interface IOutputWriter
    {
        void WriteOutput(object mergedData, string outputPath);
    }
}
