using System;
using System.Collections.Generic;

namespace SME.AST.Transform
{
    /// <summary>
    /// Renames duplicate variables names in a method.
    /// </summary>
    public class RenameDuplicateVariables : IASTTransform
    {
        /// <summary>
        /// Transform the specified item.
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="item">The item to transform.</param>
        public ASTItem Transform(ASTItem item)
        {
            var mt = item as Method;
            if (mt == null)
                return item;

            var usednames = new Dictionary<string, string>();
            foreach (var v in mt.AllVariables)
            {
                var basename = v.Name;
                var i = 2;
                while (usednames.ContainsKey(v.Name))
                {
                    v.Name = basename + i.ToString();
                    i++;
                    if (i > 200)
                        throw new Exception("More than 200 identical variables? Something is wrong ...");
                }

                usednames[v.Name] = string.Empty;
            }

            return mt;
        }
    }
}
