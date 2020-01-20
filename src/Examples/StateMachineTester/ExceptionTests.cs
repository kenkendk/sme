using SME;
using System.Threading.Tasks;

namespace StateMachineTester
{

    public class NestedForWithinWhileWithoutAwait : ExceptionTest
    {
        protected async override Task OnTickAsync()
        {
            result.State = 0;
            while (control.Go1)
            {
                result.State = 1;
                for (int i = 0; i < 10; i++)
                {
                    result.State = 0;
                    await ClockAsync();
                    result.State = 1;
                }
                for (int i = 0; i < 10; i++)
                {
                    result.State = 2;
                    await ClockAsync();
                    result.State = 3;
                }
            }
        }
    }

    // TODO public class NestedIfWithinWhileWithoutAwait : ExceptionTest

    public class NestedWhileWithoutAwait : ExceptionTest
    {
        protected async override Task OnTickAsync()
        {
            result.State = 0;
            while (control.Go1)
            {
                result.State = 1;
                while (control.Go2)
                {
                    result.State = 2;
                    await ClockAsync();
                }
                while (!control.Go2)
                {
                    result.State = 3;
                    await ClockAsync();
                }
            }
        }
    }

    public class WhileWithoutAwait : ExceptionTest
    {
        protected async override Task OnTickAsync()
        {
            result.State = 0;
            await ClockAsync();
            while (control.Go1) 
            {
                result.State = 1;
            }
        }
    }

}