namespace AggregateConfigBuildTask.Tests.Unit
{
    [TestClass]
    public class TaskWindowsTests : TaskTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            if (TestContext is not null)
            {
                TestContext.CooperativeCancellation = true;
            }
            base.TestInitialize(isWindowsMode: true, testPath: "C:\\MockDirectory");
        }
    }
}
