using Xunit;

namespace SampleTest
{
    public class Test
    {
        [Fact]
        public void Test1()
        {
            Assert.True(true);
        }

        //dummy edits to trigger pipeline
        [Fact]
        public void Test2()
        {
            bool value = true;
            Assert.True(value);
        }
    }
}
