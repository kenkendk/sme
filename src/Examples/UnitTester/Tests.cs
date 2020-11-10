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
            outputs = inputs.Select(x => x + const_val).ToArray();
        }

        protected bool valid = false;
        protected int  value = 0;
        protected readonly int const_val = 1;

        protected override void OnTick()
        {
            output.valid = this.valid;
            output.value = this.value + const_val;

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
            output.value = base.value + base.const_val;

            base.valid = input.valid;
            base.value = input.value;
        }
    }

    /// <summary>
    /// Tests whether SME can translate a member reference with process type
    /// prefix
    /// </summary>
    public class SelfTypeMemberReference : Test
    {
        public SelfTypeMemberReference()
        {
            inputs = new int[] { 1, 2, 3, 4, 5 };
            outputs = inputs.Select(x => x + const_val).ToArray();
        }

        private static readonly int const_val = 1;

        protected override void OnTick()
        {
            output.valid = input.valid;
            output.value = input.value + SelfTypeMemberReference.const_val;
        }
    }

    // TODO exceptiontest: Non-constant static variable, which is not allowed in SME, as it implies multiple instances share a variable.

    // TODO test: constant array
}