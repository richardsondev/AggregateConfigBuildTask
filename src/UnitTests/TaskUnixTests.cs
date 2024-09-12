namespace AggregateConfigBuildTask.Tests.Unit
{
    [TestClass]
    public class TaskUnixTests : TaskTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            base.TestInitialize(isWindowsMode: false, testPath: "//mnt/drive/MockDirectory");
        }
    }
}
