using System;
using System.Linq;
using System.Reflection;
using SME;

// TODO find a better name?
namespace UnitTester
{
    public class Helper
    {
        public static readonly Random generator = new Random();
        public static readonly int[] random_values = Enumerable.Range(0, 100).Select(x => generator.Next()).ToArray();
    }

    public abstract class Test : SimpleProcess
    {
        [InputBus]  public ValueBus input  = Scope.CreateBus<ValueBus>();
        [OutputBus] public ValueBus output = Scope.CreateBus<ValueBus>();

        [Ignore] public int[] inputs  = Helper.random_values;
        [Ignore] public int[] outputs = Helper.random_values;

        protected override void OnTick()
        {
            output.valid = input.valid;
            output.value = input.value;
        }
    }

    public abstract class ExceptionTest : Test
    {
        public Exception exception_to_catch;
    }

    public class Program
    {
        static void Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var sim = new Simulation())
            {
                assembly
                    .GetTypes()
                    .Where(x => x.IsSubclassOf(typeof(Test)))
                    .Where(x => !x.IsAbstract)
                    .Select(x => new Tester((Test)Activator.CreateInstance(x)))
                    .ToArray();

                sim
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }

            var ex_test_types = assembly
                .GetTypes()
                .Where(x => x.IsSubclassOf(typeof(ExceptionTest)));
            foreach (var ex_test_type in ex_test_types)
            {
                Exception expected = null;
                try
                {
                    using (var sim = new Simulation())
                    {
                        var ex_test = (ExceptionTest)Activator.CreateInstance(ex_test_type);
                        var tester = new Tester(ex_test);

                        expected = ex_test.exception_to_catch;

                        sim
                            .BuildVHDL()
                            .Run();
                    }
                    throw new Exception($"Test {ex_test_type.Name} did not throw exception");
                }
                catch (Exception e)
                {
                    if (e.GetType() != expected.GetType())
                        throw new Exception($"Test {ex_test_type.Name} threw an incorrect exception. Expected {expected.GetType().Name}, got {e.GetType().Name}");
                }
            }
        }
    }
}
