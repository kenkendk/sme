using System;
using System.Linq;

namespace SME.AST
{
    /// <summary>
    /// Helper class for cloning AST items.
    /// </summary>
    public static class CloneHelper
    {
        /// <summary>
        /// Clones the given AST item.
        /// </summary>
        /// <typeparam name="T">The type of the AST item.</typeparam>
        /// <param name="self">The AST item to clone.</param>
        public static T Clone<T>(this T self)
            where T : ASTItem
        {
            var target = (ASTItem)Activator.CreateInstance(self.GetType());
            foreach (var f in self.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                if (f.Name == "Parent")
                    continue;

                if (typeof(ASTItem).IsAssignableFrom(f.FieldType) && !typeof(DataElement).IsAssignableFrom(f.FieldType))
                {
                    var v = (ASTItem)f.GetValue(self);

                    if (v != null)
                    {
                        v = Clone(v);
                        v.Parent = target;
                    }
                    f.SetValue(target, v);
                }
                else if (f.FieldType.IsArray)
                {
                    var fe = f.FieldType.GetElementType();
                    if (typeof(ASTItem).IsAssignableFrom(fe))
                    {
                        var cur = (Array)f.GetValue(self);
                        if (cur != null)
                        {
                            var ncur = Array.CreateInstance(fe, cur.Length);
                            for (var i = 0; i < cur.Length; i++)
                            {
                                var tmp = Clone((ASTItem)cur.GetValue(i));
                                tmp.Parent = target;
                                ncur.SetValue(tmp, i);
                            }
                            cur = ncur;
                        }
                        f.SetValue(target, cur);
                    }
                    else if (fe.GetGenericTypeDefinition() == typeof(Tuple<,>)) // Switch statements! :)
                    {
                        var cur = (Array)f.GetValue(self);
                        if (cur != null)
                        {
                            var ncur = Array.CreateInstance(fe, cur.Length);
                            for (var i = 0; i < cur.Length; i++)
                            {
                                var tmp = (Tuple<Expression[],Statement[]>)cur.GetValue(i);
                                var tmp1 = tmp.Item1.Select(x => Clone(x) as Expression).Select(x => { x.Parent = target; return x; }).ToArray();
                                var tmp2 = tmp.Item2.Select(x => Clone(x) as Statement).Select(x => { x.Parent = target; return x; }).ToArray();
                                ncur.SetValue(new Tuple<Expression[],Statement[]>(tmp1, tmp2), i);
                            }
                            cur = ncur;
                        }
                        f.SetValue(target, cur);
                    }
                }
                else
                {
                    f.SetValue(target, f.GetValue(self));
                }
            }

            return (T)target;
        }
    }
}
