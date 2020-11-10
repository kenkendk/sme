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

        protected bool valid = false;
        protected int  value = 0;

        protected override void OnTick()
        {
            output.valid = this.valid;
            output.value = this.value+1;

            this.valid = input.valid;
            this.value = input.value;
        }
    }

    /// <summary>
    /// Tests whether SME can translate a member reference to base
    /// </summary>
    public class BaseMemberReference : ThisMemberReference
    {
        public BaseMemberReference() : base() { }

        protected override void OnTick()
        {
            output.valid = base.valid;
            output.value = base.value+1;

            base.valid = input.valid;
            base.value = input.value;
        }
    }

}