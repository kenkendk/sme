using SME;
using System;
using System.Threading.Tasks;

namespace StateMachineTester
{

    public class EmptyStates : StateMachineTest
    {
        public EmptyStates()
        {
            go1s = new bool[] { };
            go2s = new bool[] { };
            values = new int[] { };
            states = new int[] { 1, 1, 1, 2 };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 1;
            await ClockAsync();
            await ClockAsync();
            await ClockAsync();
            result.State = 2;
        }
    }

    public class NestedForLoop : StateMachineTest
    {
        public NestedForLoop()
        {
            go1s = new bool[] { };
            go2s = new bool[] { };
            values = new int[] { };
            states = new int[] {
                1, 1, 1, 1, 1, 2, 3, 3, 3, 3, 3,
                1, 1, 1, 1, 1, 2, 3, 3, 3, 3, 3,
                1, 1, 1, 1, 1, 2, 3, 3, 3, 3, 3,
                1, 1, 1, 1, 1, 2, 3, 3, 3, 3, 3,
                1, 1, 1, 1, 1, 2, 3, 3, 3, 3, 3,
                4,
            };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            for (var i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    result.State = 1;
                    await ClockAsync();
                    result.State = 2;
                }
                await ClockAsync();
                for (int j = 0; j < 5; j++)
                {
                    result.State = 3;
                    await ClockAsync();
                    result.State = 4;
                }
            }
        }
    }

    public class NestedForWithinWhile : StateMachineTest
    {
        public NestedForWithinWhile()
        {
            go1s = new bool[] {
                false,
                true, true, true, true, true, true, true, true, true, true, true,
                true, false
            };
            go2s = new bool[] { };
            values = new int[] { };
            states = new int[] {
                7,
                2, 2, 2, 2, 2, 3, 4, 4, 4, 4, 4,
                2, 2, 2, 2, 2, 3, 4, 4, 4, 4, 4,
                7, 7
            };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            while (control.Go1)
            {
                result.State = 1;
                for (int i = 0; i < 5; i++)
                {
                    result.State = 2;
                    await ClockAsync();
                    result.State = 3;
                }
                await ClockAsync();
                for (int i = 0; i < 5; i++)
                {
                    result.State = 4;
                    await ClockAsync();
                    result.State = 5;
                }
                result.State = 6;
            }
            result.State = 7;
        }
    }

    public class NestedIfElseStatement : StateMachineTest
    {
        public NestedIfElseStatement()
        {
            go1s = new bool[] { true, true, true,  true, false, false, false, false };
            go2s = new bool[] { true, true, false, false, true, true,  false, false };
            values = new int[] { };
            states = new int[] {   2,    3,     4,     5,    7,    8,      9,    10 };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            if (control.Go1)
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
                    await ClockAsync();
                    result.State = 5;
                }
            }
            else
            {
                result.State = 6;
                if (control.Go2)
                {
                    result.State = 7;
                    await ClockAsync();
                    result.State = 8;
                }
                else
                {
                    result.State = 9;
                    await ClockAsync();
                    result.State = 10;
                }
            }
        }
    }

    public class NestedIfStatement : StateMachineTest
    {
        public NestedIfStatement()
        {
            go1s = new bool[] { false, true,  true,  true, true, true };
            go2s = new bool[] { false, false, false, true, true, true };
            values = new int[] { };
            states = new int[] {    6,     1,     6,    1,    3,    6 };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            if (control.Go1)
            {
                result.State = 1;
                await ClockAsync();
                result.State = 2;
                if (control.Go2)
                {
                    result.State = 3;
                    await ClockAsync();
                    result.State = 4;
                }
                result.State = 5;
            }
            result.State = 6;
        }
    }

    public class NestedIfWithinWhile : StateMachineTest
    {
        public NestedIfWithinWhile()
        {
            go1s = new bool[] { false, true,  true,  true, true, false };
            go2s = new bool[] { false, false, false, true, true, true };
            values = new int[] { };
            states = new int[] {    7,     4,     4,    2,    2,     7 };
        }

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
                    await ClockAsync();
                    result.State = 5;
                }
                result.State = 6;
            }
            result.State = 7;
        }
    }

    public class NestedSwitchStatement : StateMachineTest
    {
        public NestedSwitchStatement()
        {
            go1s = new bool[] { };
            go2s = new bool[] { };
            values = new int[] { 0, 0,  0, 0, 1,  1, 0, 2,  2,  1,  1,  2,  2 };
            states = new int[] { 1, 3, 14, 1, 5, 14, 1, 7, 14, 10, 14, 12, 14 };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            switch (control.Value)
            {
                case 0:
                    result.State = 1;
                    await ClockAsync();
                    result.State = 2;
                    switch (control.Value)
                    {
                        case 0:
                            result.State = 3;
                            await ClockAsync();
                            result.State = 4;
                            break;
                        case 1:
                            result.State = 5;
                            await ClockAsync();
                            result.State = 6;
                            break;
                        default:
                            result.State = 7;
                            await ClockAsync();
                            result.State = 8;
                            break;

                    }
                    result.State = 9;
                    break;
                case 1:
                    result.State = 10;
                    await ClockAsync();
                    result.State = 11;
                    break;
                default:
                    result.State = 12;
                    await ClockAsync();
                    result.State = 13;
                    break;
            }
            result.State = 14;
        }
    }

    public class NestedSwitchWithinWhile : StateMachineTest
    {
        public NestedSwitchWithinWhile()
        {
            go1s = new bool[] { false,  true,  true,  true,  true, false };
            go2s = new bool[] { };
            values = new int[] {    0,     0,     1,     2,     5,     5 };
            states = new int[] {    9,     2,     4,     6,     6,     9 };
        }

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
                    default:
                        result.State = 6;
                        await ClockAsync();
                        result.State = 7;
                        break;
                }
                result.State = 8;
            }
            result.State = 9;
        }
    }

    public class NestedWhileLoop : StateMachineTest
    {
        public NestedWhileLoop()
        {
            go1s = new bool[] { false, true,  true,  true, true, false, false, false, false, false };
            go2s = new bool[] { false, false, false, true, true, true,  false, false, true,  false };
            values = new int[] { };
            states = new int[] {    7,     1,     4,    2,    2,     2,     3,     4,     7,      7 };
        }

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
                    result.State = 3;
                }
                await ClockAsync();
                while (!control.Go2)
                {
                    result.State = 4;
                    await ClockAsync();
                    result.State = 5;
                }
                result.State = 6;
            }
            result.State = 7;
        }
    }

    public class SingleForLoop : StateMachineTest
    {
        public SingleForLoop()
        {
            go1s = new bool[] { };
            go2s = new bool[] { };
            values = new int[] { };
            states = new int[] { 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 3 };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            for (var i = 0; i < 5; i++)
            {
                result.State = 1;
                await ClockAsync();
                result.State = 2;
            }
            result.State = 3;
        }
    }

    public class SingleIfElseStatement : StateMachineTest
    {
        public SingleIfElseStatement()
        {
            go1s = new bool[] { false, false, true, true, false, false };
            go2s = new bool[] { };
            values = new int[] { };
            states = new int[] { 3, 5, 1, 5, 3, 5 };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            if (control.Go1)
            {
                result.State = 1;
                await ClockAsync();
                result.State = 2;
            }
            else
            {
                result.State = 3;
                await ClockAsync();
                result.State = 4;
            }
            result.State = 5;
        }
    }

    public class SingleIfStatement : StateMachineTest
    {
        public SingleIfStatement()
        {
            go1s = new bool[] { false, true, false };
            go2s = new bool[] { };
            values = new int[] { };
            states = new int[] {    3,    1,     3 };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            if (control.Go1)
            {
                result.State = 1;
                await ClockAsync();
                result.State = 2;
            }
            result.State = 3;
        }
    }

    public class SingleSwitchStatement : StateMachineTest
    {
        public SingleSwitchStatement()
        {
            go1s = new bool[] { };
            go2s = new bool[] { };
            values = new int[] { 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 3, 3, 4, 5 };
            states = new int[] { 0, 1, 9, 0, 3, 4, 9, 0, 6, 9, 0, 9, 0, 9 };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            await ClockAsync();
            switch (control.Value)
            {
                case 0:
                    result.State = 1;
                    await ClockAsync();
                    result.State = 2;
                    break;
                case 1:
                    result.State = 3;
                    await ClockAsync();
                    result.State = 4;
                    await ClockAsync();
                    result.State = 5;
                    break;
                case 2:
                    result.State = 6;
                    await ClockAsync();
                    break;
                case 3:
                    result.State = 7;
                    break;
                default:
                    result.State = 8;
                    break;
            }
            result.State = 9;
        }
    }

    public class SingleWhileLoop : StateMachineTest
    {
        public SingleWhileLoop()
        {
            go1s = new bool[] { false, true, true, true, false };
            go2s = new bool[] { };
            values = new int[] { };
            states = new int[] {    3,    1,    1,    1,     3 };
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            while (control.Go1)
            {
                result.State = 1;
                await ClockAsync();
                result.State = 2;
            }
            result.State = 3;
        }
    }
}