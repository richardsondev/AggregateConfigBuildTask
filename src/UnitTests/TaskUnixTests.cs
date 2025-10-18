namespace AggregateConfigBuildTask.Tests.Unit
{
    [TestClass]
    public class TaskUnixTests : TaskTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            if (TestContext is not null)
            {
                TestContext.CooperativeCancellation = true;
            }
            base.TestInitialize(isWindowsMode: false, testPath: "//mnt/drive/MockDirectory");
        }
    }
}
