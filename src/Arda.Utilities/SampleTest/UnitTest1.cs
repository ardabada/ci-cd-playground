using Xunit;

namespace SampleTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Assert.True(true);
        }

        [Fact]
        public void Test2()
        {
            bool x = false;
            Assert.True(x);
        }
    }
}