using SME;
using System;
using System.Linq;
using System.Reflection;

namespace StateMachineTester
{
    public abstract class ExceptionTest : StateProcess
    {
        [InputBus] public IControlBus control = Scope.CreateBus<IControlBus>();
        [OutputBus] public IResultBus result = Scope.CreateBus<IResultBus>();
    }

    public abstract class StateMachineTest : StateProcess
    {
        [InputBus] public IControlBus control = Scope.CreateBus<IControlBus>();
        [OutputBus] public IResultBus result = Scope.CreateBus<IResultBus>();

        [Ignore] public bool[] go1s;
        [Ignore] public bool[] go2s;
        [Ignore] public int[] values;
        [Ignore] public int[] states;
    }

    public class MainClass
    {
        public static void Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var sim = new Simulation())
            {
                assembly.GetTypes()
                    .Where(x =>
                        x.IsSubclassOf(typeof(StateMachineTest)))
                    .Select(x =>
                        new Tester(
                            (StateMachineTest)Activator.CreateInstance(x)))
                    .ToArray();

                sim
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }

            var ex_tests = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(ExceptionTest)));
            foreach (var ex_test in ex_tests)
            {
                try
                {
                    using (var sim = new Simulation())
                    {
                        var tester = new ExceptionTester((ExceptionTest)Activator.CreateInstance(ex_test));

                        sim
                            .BuildVHDL()
                            .Run();
                    }
                }
                catch (SME.AST.Transform.WhileWithoutAwaitException)
                {
                    continue;
                }
                throw new Exception($"Test {ex_test.Name} did not throw exception!");
            }
        }
    }
}
