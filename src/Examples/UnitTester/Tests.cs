using System;
using System.Linq;
using SME;

namespace UnitTester
{

    /// <summary>
    /// Tests whether SME can translate a member reference to this
    /// </summary>
    public class ThisMemberReference : Test
    {
        public ThisMemberReference()
        {
            inputs = new int[] { 1, 2, 3, 4, 5 };
            outputs = inputs.Select(x => x+1).ToArray();
        }

        bool valid = false;
        int  value = 0;

        protected override void OnTick()
        {
            output.valid = this.valid;
            output.value = this.value+1;

            this.valid = input.valid;
            this.value = input.value;
        }
    }

}