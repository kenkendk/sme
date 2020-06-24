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

    public class NestedIfWithinWhileWithoutAwait : ExceptionTest
    {
        protected async override Task OnTickAsync()
        {
            result.State = 0;
            while (control.Go1)
            {
                result.State = 1;
                if (control.Go2)
                {
                    result.State = 2;
                    await ClockAsync();
                    result.State = 3;
                }
                else
                {
                    result.State = 4;
                }
                result.State = 5;
            }
            result.State = 6;
        }
    }

    public class NestedSwitchWithinWhileWithoutAwait : ExceptionTest
    {
        protected async override Task OnTickAsync()
        {
            result.State = 0;
            while (control.Go1)
            {
                result.State = 1;
                switch (control.Value)
                {
                    case 0:
                        result.State = 2;
                        await ClockAsync();
                        result.State = 3;
                        break;
                    default:
                        result.State = 4;
                        break;
                }
                result.State = 5;
            }
            result.State = 6;
        }
    }

    public class NestedSwitchWithinWhileWithoutDefault : ExceptionTest
    {
        protected async override Task OnTickAsync()
        {
            result.State = 0;
            while (control.Go1)
            {
                result.State = 1;
                switch (control.Value)
                {
                    case 0:
                        result.State = 2;
                        await ClockAsync();
                        result.State = 3;
                        break;
                    case 1:
                        result.State = 4;
                        await ClockAsync();
                        result.State = 5;
                        break;
                }
                result.State = 6;
            }
            result.State = 7;
        }
    }

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