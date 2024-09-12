namespace AggregateConfigBuildTask.Tests.Unit
{
    [TestClass]
    public class TaskWindowsTests : TaskTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            base.TestInitialize(isWindowsMode: true, testPath: "C:\\MockDirectory");
        }
    }
}
